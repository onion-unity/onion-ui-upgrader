using System.Collections.Generic;
using Onion.UI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// Project Settings > Onion > UI Upgrader. Shows the same inspector as the settings asset,
    /// so the on/off guidance is found without knowing where the asset lives.
    /// </summary>
    internal static class NavigationProjectSettings {
        private const string Path = "Project/Onion/UI Upgrader";

        [SettingsProvider]
        private static SettingsProvider CreateProvider() {
            NavigationSettings settings = null;
            UnityEditor.Editor editor = null;
            var padding = new GUIStyle { margin = new RectOffset(10, 10, 10, 10) };

            return new SettingsProvider(Path, SettingsScope.Project) {
                label = "UI Upgrader",
                keywords = new HashSet<string> { "Navigation", "Selectable", "Profile", "Upgrade" },
                guiHandler = _ => {
                    if (settings == null) {
                        settings = NavigationSettingsProvider.GetOrCreateSettings();
                    }

                    UnityEditor.Editor.CreateCachedEditor(settings, typeof(NavigationSettingsEditor), ref editor);
                    using (new EditorGUILayout.VerticalScope(padding)) {
                        editor.OnInspectorGUI();
                    }
                },
            };
        }
    }
}
