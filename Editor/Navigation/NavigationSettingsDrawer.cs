using Onion.UI.Navigation;
using UnityEditor;

namespace Onion.UI.Editor {
    /// <summary>
    /// The navigation section of the UIUpgraderSettings inspector.
    /// </summary>
    internal static class NavigationSettingsDrawer {
        internal static void Draw(SerializedProperty navigation) {
            var enabled = navigation.FindPropertyRelative(nameof(NavigationSettings.enabled));
            var profile = navigation.FindPropertyRelative(nameof(NavigationSettings.projectWideProfile));

            if (enabled.boolValue && profile.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("No profile is assigned, so Unity's default navigation is still used.\nAssign a profile or click + to create one.", MessageType.Warning);
            }

            UIUpgraderEditorGUI.AssetField<NavigationProfile>(profile, "Navigation Profile");

            EditorGUILayout.PropertyField(navigation.FindPropertyRelative(nameof(NavigationSettings.recoverSelection)));
        }
    }
}
