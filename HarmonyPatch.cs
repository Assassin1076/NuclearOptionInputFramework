using HarmonyLib;
using System.Linq;
using Rewired;
namespace InputFramework
{
    [HarmonyPatch(typeof(InputManager_Base), "Awake")]
    public static class RewiredActionInjector
    {
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

            int nextId = GetNextActionId(actions);

            foreach (var modAction in ExtraInputManager.PendingActions)
            {
                if (actions.Any(a => a.name == modAction.Name))
                    continue;

                var action = new InputAction
                {
                    id = nextId++,
                    name = modAction.Name,
                    type = modAction.Type,
                    descriptiveName = modAction.Name,
                    categoryId = debugCategory.id,
                };
                action._userAssignable = true;

                actions.Add(action);

                userData.actionCategoryMap.AddAction(debugCategory.id, action.id);

                modAction.AssignedId = action.id;
            }
            ExtraInputManager.RewiredInitialized = true;
        }

        private static int GetNextActionId(System.Collections.Generic.List<InputAction> actions)
        {
            if (actions.Count == 0)
                return 1000;

            return actions.Max(a => a.id) + 1;
        }
    }
}
