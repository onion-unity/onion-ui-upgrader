using Onion.UI.Selectables;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// Draws the five slots of a StateValues inline, without a foldout, like Unity's ColorBlock.
    /// </summary>
    [CustomPropertyDrawer(typeof(StateValues<>))]
    internal sealed class StateValuesDrawer : PropertyDrawer {
        private const float BottomSpace = 4f;

        private static readonly string[] _slots = {
            nameof(StateValues<int>.normal),
            nameof(StateValues<int>.highlighted),
            nameof(StateValues<int>.pressed),
            nameof(StateValues<int>.selected),
            nameof(StateValues<int>.disabled),
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var rect = position;
            foreach (string slot in _slots) {
                var child = property.FindPropertyRelative(slot);
                rect.height = EditorGUI.GetPropertyHeight(child);
                EditorGUI.PropertyField(rect, child);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0f;
            foreach (string slot in _slots) {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(slot)) + EditorGUIUtility.standardVerticalSpacing;
            }

            // The last slot's spacing plus a little more, so the fields below don't read as a sixth slot.
            return height + BottomSpace;
        }
    }
}
