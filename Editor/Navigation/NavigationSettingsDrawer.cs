using Onion.UI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// The navigation section of the UIUpgraderSettings inspector.
    /// </summary>
    internal static class NavigationSettingsDrawer {
        private const float NewButtonWidth = 22f;

        internal static void Draw(SerializedProperty navigation) {
            var enabled = navigation.FindPropertyRelative(nameof(NavigationSettings.enabled));
            var profile = navigation.FindPropertyRelative(nameof(NavigationSettings.projectWideProfile));

            if (enabled.boolValue && profile.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("No profile is assigned, so Unity's default navigation is still used.\nAssign a profile or click + to create one.", MessageType.Warning);
            }

            AssetField<NavigationProfile>(profile, "Navigation Profile");

            EditorGUILayout.PropertyField(navigation.FindPropertyRelative(nameof(NavigationSettings.recoverSelection)));
            EditorGUILayout.PropertyField(navigation.FindPropertyRelative(nameof(NavigationSettings.skipClippedSelectables)));
        }

        /// <summary>
        /// An asset field with a + button to its right, which creates a new <typeparamref name="T"/> and assigns it.
        /// </summary>
        internal static void AssetField<T>(SerializedProperty property, string title) where T : ScriptableObject {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.PropertyField(property);
                var newContent = EditorGUIUtility.TrIconContent("Toolbar Plus", $"Create a new {title}");
                if (GUILayout.Button(newContent, EditorStyles.miniButton, GUILayout.Width(NewButtonWidth))) {
                    CreateAsset<T>(property, title);
                }
            }
        }

        private static void CreateAsset<T>(SerializedProperty property, string title) where T : ScriptableObject {
            string directory = AssetDatabase.IsValidFolder(UIUpgraderSettingsProvider.SettingsDirectory)
                ? UIUpgraderSettingsProvider.SettingsDirectory
                : "Assets";
            string path = EditorUtility.SaveFilePanelInProject($"New {title}", typeof(T).Name, "asset", $"Choose where to save the new {title}.", directory);
            if (!string.IsNullOrEmpty(path)) {
                var asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                property.objectReferenceValue = asset;
                property.serializedObject.ApplyModifiedProperties();
            }

            // The modal dialog breaks the current IMGUI layout pass.
            GUIUtility.ExitGUI();
        }
    }
}
