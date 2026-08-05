using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;

namespace InputFramework
{
    [HarmonyPatch(typeof(InputManager_Base), "Awake")]
    public static class RewiredActionInjector
    {
        /// <summary>
        /// 采用一个类似堆栈内存管理的方案解决哈基米更新可能导致的连环爆炸ID冲突问题。
        /// 这个值作为栈底基准值，mod引入的Action ID从此值向下递减分配，游戏原生Action ID从0向上递增分配，避免冲突。
        /// 1000000应该足够大，能够完全避免冲突......的吧？
        /// </summary>
        private const int MOD_ACTION_ID_CEILING = 1000000;

        static void Prefix(InputManager_Base __instance)
        {
            InjectActions(__instance);
        }

        private static void InjectActions(InputManager_Base manager)
        {
            var userData = manager._userData;
            if (userData == null) return;

            var actions = userData.actions;
            if (actions == null) return;

            var categories = userData.actionCategories;
            if (categories == null) return;

            var debugCategory = categories.FirstOrDefault(c => c.name == "Debug");

            // Build a set of already-used action IDs to detect conflicts
            var usedIds = new HashSet<int>(actions.Select(a => a.id));

            // Mod IDs are allocated from the ceiling downward (stack direction).
            // This is the opposite of game IDs which grow upward from 0 (heap direction).
            int nextModId = MOD_ACTION_ID_CEILING;

            foreach (var modAction in ExtraInputManager.PendingActions)
            {
                if (actions.Any(a => a.name == modAction.Name))
                {
                    Debug.Log($"[ExtraInputManager] Action \"{modAction.Name}\" already exists in Rewired, skipping injection.");
                    continue;
                }

                var targetCategory = debugCategory;
                if (modAction.Category != null)
                {
                    targetCategory = categories.FirstOrDefault(c => c.name == modAction.Category);
                }

                if (targetCategory == null)
                {
                    Debug.LogWarning($"[ExtraInputManager] Category \"{modAction.Category ?? "Debug"}\" not found, skipping action \"{modAction.Name}\".");
                    continue;
                }

                int actionId;

                // Try to reuse the previously assigned ID for consistency
                if (modAction.AssignedId >= 0)
                {
                    if (!usedIds.Contains(modAction.AssignedId))
                    {
                        // Saved ID is available — reuse it to keep key bindings stable
                        actionId = modAction.AssignedId;
                    }
                    else
                    {
                        // --- ID 冲突：已保存的 ID 被占用，无法维持一致性 ---

                        // 查找冲突来源：先检查是否为其他 Mod Action（含级联冲突），
                        // 若不是则必为游戏原生 Action。
                        string conflictSourceName = null;
                        var otherMod = ExtraInputManager.PendingActions
                            .FirstOrDefault(a => a.AssignedId == modAction.AssignedId && a != modAction);
                        if (otherMod != null)
                        {
                            conflictSourceName = $"[Mod Action] {otherMod.Name}";
                        }
                        else
                        {
                            // 不在 Mod 列表中，一定是游戏原生 Action
                            var gameAction = actions.FirstOrDefault(a => a.id == modAction.AssignedId);
                            if (gameAction != null)
                            {
                                conflictSourceName = $"[游戏Action] {gameAction.name}";
                            }
                        }

                        // 从天花板向下寻找第一个空闲 ID（栈方向递减）
                        while (usedIds.Contains(nextModId))
                            nextModId--;
                        actionId = nextModId--;

                        // 记录冲突详情，供全屏提示使用
                        ConflictPromptManager.RecordConflict(new ActionIdConflictRecord
                        {
                            ModActionName = modAction.Name,
                            ModActionType = modAction.Type,
                            ModActionCategory = modAction.Category,
                            ExpectedId = modAction.AssignedId,
                            NewId = actionId,
                            ConflictingSourceName = conflictSourceName ?? "未知来源",
                            ConflictingSourceId = modAction.AssignedId,
                        });

                        Debug.LogWarning(
                            $"[ExtraInputManager] Saved ID {modAction.AssignedId} for action \"{modAction.Name}\" " +
                            $"(Type={modAction.Type}, Category={modAction.Category ?? "null"}) " +
                            $"conflicts with {(conflictSourceName ?? "an unknown source")}. " +
                            $"Key bindings saved under this ID will be lost. " +
                            $"New ID {actionId} assigned from mod ceiling (stack direction).");
                    }
                }
                else
                {
                    // No previously saved ID — allocate from ceiling downward (stack direction)
                    while (usedIds.Contains(nextModId))
                        nextModId--;
                    actionId = nextModId--;
                }

                usedIds.Add(actionId);

                var action = new InputAction
                {
                    id = actionId,
                    name = modAction.Name,
                    type = modAction.Type,
                    descriptiveName = modAction.Name,
                    categoryId = targetCategory.id,
                };
                action._userAssignable = true;

                actions.Add(action);
                userData.actionCategoryMap.AddAction(targetCategory.id, action.id);

                modAction.AssignedId = actionId;
            }

            // Persist assigned IDs so they can be reused next launch
            ExtraInputManager.SavePendingActions();

            // If any ID conflicts were detected, show the fullscreen warning prompt
            if (ConflictPromptManager.HasConflicts)
            {
                ConflictPromptManager.ShowConflictPromptAsync();
            }

            ExtraInputManager.RewiredInitialized = true;
        }
    }
}
