using BepInEx;
using Newtonsoft.Json;
using Rewired;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InputFramework
{
    public class ModActionDefinition
    {
        public string Name;
        public InputActionType Type;
        public string Category;

        public int AssignedId = -1;

        public ModActionDefinition(string name, InputActionType type, string category = null)
        {
            Name = name;
            Type = type;
            Category = category;
        }
    }

    public static class ExtraInputManager
    {
        private static readonly List<ModActionDefinition> pendingActions = new();
        private static readonly string savePath = Path.Combine(Paths.PluginPath, "ExtraInput", "ExtraInputActions.json");

        internal static bool RewiredInitialized { get; set; } = false;

        public static IReadOnlyList<ModActionDefinition> PendingActions => pendingActions;

        public static void RegisterAction(string name, InputActionType type, string category = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Action name cannot be null or empty.");

            bool exists = pendingActions.Any(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase) &&
                a.Type == type &&
                string.Equals(a.Category ?? "", category ?? "", StringComparison.OrdinalIgnoreCase)
            );

            if (exists)
            {
                Debug.Log($"[ExtraInputManager] Action \"{name}\" (Type={type}, Category={category ?? "null"}) has been Registed, skipping.");
                return;
            }

            if (RewiredInitialized)
            {
                Debug.LogWarning($"[ExtraInputManager] Registering Action \"{name}\" after Rewired Awake. You need to restart the game to apply the changes");
            }

            var modAction = new ModActionDefinition(name, type, category);
            pendingActions.Add(modAction);

            SavePendingActions();
        }

        private static void SavePendingActions()
        {
            try
            {
                string json = JsonConvert.SerializeObject(pendingActions, Formatting.Indented);
                File.WriteAllText(savePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExtraInputManager] Savint Register of Action failed: {ex}");
            }
        }

        public static void LoadPendingActions()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    string json = File.ReadAllText(savePath);
                    var loaded = JsonConvert.DeserializeObject<List<ModActionDefinition>>(json);
                    if (loaded != null)
                    {
                        pendingActions.Clear();
                        pendingActions.AddRange(loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExtraInputManager] Reading registered Action list failed: {ex}");
            }
        }
    }
}
