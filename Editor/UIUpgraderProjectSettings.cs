using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// Project Settings > Onion > UI Upgrader. Shows the same inspector as the settings asset,
    /// so every upgrade can be switched without knowing where the asset lives.
    /// </summary>
    internal static class UIUpgraderProjectSettings {
        private const string Path = "Project/Onion/UI Upgrader";

        internal static void Open() {
            SettingsService.OpenProjectSettings(Path);
        }

        // Double-clicking the settings asset opens this page instead of just selecting it.
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line) {
            if (EditorUtility.InstanceIDToObject(instanceID) is not UIUpgraderSettings) {
                return false;
            }

            Open();
            return true;
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider() {
            UIUpgraderSettings settings = null;
            UnityEditor.Editor editor = null;
            var padding = new GUIStyle { margin = new RectOffset(10, 10, 10, 10) };

            return new SettingsProvider(Path, SettingsScope.Project) {
                label = "UI Upgrader",
                keywords = new HashSet<string> { "Navigation", "Selectable", "Profile", "Upgrade" },
                guiHandler = _ => {
                    if (settings == null) {
                        settings = UIUpgraderSettingsProvider.GetOrCreateSettings();
                    }

                    UnityEditor.Editor.CreateCachedEditor(settings, typeof(UIUpgraderSettingsEditor), ref editor);
                    using (new EditorGUILayout.VerticalScope(padding)) {
                        ((UIUpgraderSettingsEditor)editor).DrawSettings();
                    }
                },
            };
        }
    }
}
