using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace InputFramework
{
    /// <summary>
    /// 记录一次 action ID 冲突的详情
    /// </summary>
    public class ActionIdConflictRecord
    {
        /// <summary>受影响的 Mod Action 名称</summary>
        public string ModActionName;
        /// <summary>Mod Action 类型</summary>
        public InputActionType ModActionType;
        /// <summary>Mod Action 所属分类</summary>
        public string ModActionCategory;
        /// <summary>期望使用的（已保存的）ID</summary>
        public int ExpectedId;
        /// <summary>被迫分配的新 ID</summary>
        public int NewId;
        /// <summary>冲突来源的描述（游戏原生Action 或 其他Mod Action）</summary>
        public string ConflictingSourceName;
        /// <summary>冲突来源占用的 ID</summary>
        public int ConflictingSourceId;
    }

    /// <summary>
    /// 添加了一坨新的石山来提示用户他们的键位绑定爆炸了。
    /// 当检测到任意 action ID 被迫变更时，全屏显示冲突详情，
    /// 数秒后缩小为底部横幅，点击可关闭。
    /// </summary>
    public static class ConflictPromptManager
    {
        private static readonly List<ActionIdConflictRecord> conflicts = new();
        private static GameObject promptOverlay;
        private static bool promptShown;

        /// <summary>所有已记录的冲突</summary>
        public static IReadOnlyList<ActionIdConflictRecord> Conflicts => conflicts;

        /// <summary>是否存在冲突</summary>
        public static bool HasConflicts => conflicts.Count > 0;

        /// <summary>
        /// 记录一次 ID 冲突。
        /// 由 InjectActions 在检测到冲突时调用。
        /// </summary>
        public static void RecordConflict(ActionIdConflictRecord record)
        {
            if (record == null) return;
            conflicts.Add(record);
        }

        /// <summary>
        /// 异步显示冲突提示。
        /// 仅在存在冲突且尚未显示过时生效。
        /// 先全屏展示冲突详情，数秒后自动缩小为底部可关闭横幅。
        /// </summary>
        public static async void ShowConflictPromptAsync()
        {
            if (promptShown || conflicts.Count == 0) return;
            promptShown = true;

            try
            {
                // 等待 MainCanvas 出现（场景可能尚未加载）
                Canvas mainCanvas = null;
                for (int i = 0; i < 100; i++)
                {
                    GameObject go = GameObject.Find("MainCanvas");
                    if (go != null)
                    {
                        mainCanvas = go.GetComponent<Canvas>();
                        if (mainCanvas != null) break;
                    }
                    await Task.Delay(100);
                }

                if (mainCanvas == null)
                {
                    Debug.LogWarning("[ExtraInputManager] MainCanvas not found, cannot show ID conflict prompt.");
                    return;
                }

                // 构建冲突详情的文本
                string message = BuildConflictMessage();

                // ---- 全屏遮罩 ----
                promptOverlay = new GameObject("InputFrameworkIdConflictPrompt");
                promptOverlay.transform.SetParent(mainCanvas.transform, false);

                RectTransform rt = promptOverlay.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image bg = promptOverlay.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.85f);
                bg.raycastTarget = true;

                // 文字区域
                GameObject textGo = new GameObject("ConflictText");
                textGo.transform.SetParent(promptOverlay.transform, false);
                RectTransform textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.08f, 0.05f);
                textRt.anchorMax = new Vector2(0.92f, 0.95f);
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                Text promptText = textGo.AddComponent<Text>();
                promptText.text = message;
                promptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                promptText.fontSize = 20;
                promptText.color = Color.white;
                promptText.alignment = TextAnchor.MiddleCenter;
                promptText.raycastTarget = false;

                promptOverlay.transform.SetAsLastSibling();

                Debug.Log($"[ExtraInputManager] ID conflict prompt displayed with {conflicts.Count} conflict(s).");

                // ---- 等待 8 秒后缩小为底部横幅 ----
                await Task.Delay(8000);

                if (promptOverlay != null)
                {
                    // 缩小到屏幕底部
                    rt.anchorMin = new Vector2(0.02f, 0f);
                    rt.anchorMax = new Vector2(0.98f, 0.06f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    bg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

                    promptText.text = $"⚠ {conflicts.Count} 个Mod输入绑定ID已变更 - 点击此处关闭";
                    promptText.fontSize = 15;
                    promptText.color = new Color(1f, 0.65f, 0.2f);
                    promptText.alignment = TextAnchor.MiddleCenter;
                    promptText.raycastTarget = true;

                    // 点击关闭
                    Button closeBtn = promptOverlay.AddComponent<Button>();
                    closeBtn.targetGraphic = bg;
                    closeBtn.onClick.AddListener(() =>
                    {
                        GameObject.Destroy(promptOverlay);
                        promptOverlay = null;
                        Debug.Log("[ExtraInputManager] ID conflict prompt dismissed by user.");
                    });

                    Debug.Log($"[ExtraInputManager] ID conflict prompt shrunk to banner.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExtraInputManager] Conflict prompt error: {ex}");
            }
        }

        /// <summary>
        /// 构建冲突详情的多行文本，用于全屏显示。
        /// 列出所有被迫变更 ID 的 Mod Action 及其冲突来源。
        /// </summary>
        private static string BuildConflictMessage()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("<b>⚠ 输入绑定 ID 冲突警告</b>");
            sb.AppendLine();
            sb.AppendLine("检测到以下 Mod Action 的键位绑定 ID 已被迫变更：");
            sb.AppendLine("（之前保存的键位设置可能已丢失，请重新绑定）");
            sb.AppendLine();
            sb.AppendLine("────────────────────────────────");

            foreach (var c in conflicts)
            {
                sb.AppendLine();
                sb.AppendLine($"▸ {c.ModActionName}");
                sb.AppendLine($"  类型: {c.ModActionType}  分类: {c.ModActionCategory ?? "默认"}");
                sb.AppendLine($"  原 ID: {c.ExpectedId}  →  新 ID: {c.NewId}");
                if (!string.IsNullOrEmpty(c.ConflictingSourceName))
                {
                    sb.AppendLine($"  冲突来源: {c.ConflictingSourceName} (占用 ID {c.ConflictingSourceId})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("────────────────────────────────");
            sb.AppendLine("此提示将在数秒后自动缩小，之后可点击关闭");

            return sb.ToString();
        }
    }
}
