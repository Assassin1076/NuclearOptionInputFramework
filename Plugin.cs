using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Rewired;

namespace InputFramework
{
    [BepInPlugin("experimental.assassin1076.extrainputframework", "Extra Input Framework", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            
            ExtraInputManager.LoadPendingActions();
            ExtraInputManager.RegisterAction(
                "TestAction",
                InputActionType.Button
            );

            var harmony = new Harmony("experimental.assassin1076.extrainputframework");
            harmony.PatchAll();

            Logger.LogInfo($"Plugin experimental.assassin1076.extrainputframework is loaded!");
        }
    }
}
