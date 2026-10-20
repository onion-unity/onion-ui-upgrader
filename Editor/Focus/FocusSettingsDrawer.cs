using Onion.UI.Focus;
using Onion.UI.Selectables;
using UnityEditor;

namespace Onion.UI.Editor {
    /// <summary>
    /// The focus section of the UIUpgraderSettings inspector.
    /// </summary>
    internal static class FocusSettingsDrawer {
        internal static void Draw(SerializedProperty focus) {
            EditorGUILayout.PropertyField(focus.FindPropertyRelative(nameof(FocusSettings.continueFromHover)));
            EditorGUILayout.PropertyField(focus.FindPropertyRelative(nameof(FocusSettings.revealOnFirstInput)));
            EditorGUILayout.PropertyField(focus.FindPropertyRelative(nameof(FocusSettings.cancelSwitchesMode)));
            NavigationSettingsDrawer.AssetField<SelectableStyle>(focus.FindPropertyRelative(nameof(FocusSettings.projectWideStyle)), "Selectable Style");
        }
    }
}
