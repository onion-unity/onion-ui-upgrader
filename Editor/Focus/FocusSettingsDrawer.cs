using Onion.UI.Focus;
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
        }
    }
}
