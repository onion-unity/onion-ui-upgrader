using Onion.UI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// The navigation section of the UIUpgraderSettings inspector.
    /// </summary>
    internal static class NavigationSettingsDrawer {
        private const float NewButtonWidth = 50f;

        internal static void Draw(SerializedProperty navigation) {
            var enabled = navigation.FindPropertyRelative(nameof(NavigationSettings.enabled));
            var profile = navigation.FindPropertyRelative(nameof(NavigationSettings.defaultProfile));

            if (enabled.boolValue && profile.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("No profile is assigned, so Unity's default navigation is still used.\nAssign a profile or click New.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.PropertyField(profile);
                if (GUILayout.Button("New", GUILayout.Width(NewButtonWidth))) {
                    CreateProfile(profile);
                }
            }
        }

        private static void CreateProfile(SerializedProperty profile) {
            string directory = AssetDatabase.IsValidFolder(UIUpgraderSettingsProvider.SettingsDirectory)
                ? UIUpgraderSettingsProvider.SettingsDirectory
                : "Assets";
            string path = EditorUtility.SaveFilePanelInProject("New Navigation Profile", "NavigationProfile", "asset", "Choose where to save the new profile.", directory);
            if (!string.IsNullOrEmpty(path)) {
                var asset = ScriptableObject.CreateInstance<NavigationProfile>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                profile.objectReferenceValue = asset;
                profile.serializedObject.ApplyModifiedProperties();
            }

            // The modal dialog breaks the current IMGUI layout pass.
            GUIUtility.ExitGUI();
        }
    }
}
