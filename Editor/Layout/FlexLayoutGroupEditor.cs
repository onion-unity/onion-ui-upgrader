using Onion.UI.Layout;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Editor {
    /// <summary>
    /// Laid out like Unity's Horizontal / Vertical Layout Group inspector, plus Direction and Justify.
    /// Also adds "Upgrade/Convert to Flex Layout Group" to the context menu of Unity's layout groups.
    /// </summary>
    [CustomEditor(typeof(FlexLayoutGroup), true)]
    [CanEditMultipleObjects]
    internal class FlexLayoutGroupEditor : UnityEditor.Editor {
        private const string UpgradeMenu = "Upgrade/" + UndoName;
        private const string UndoName = "Convert to Flex Layout Group";

        private SerializedProperty _direction;
        private SerializedProperty _wrap;
        private SerializedProperty _lineSpacing;
        private SerializedProperty _padding;
        private SerializedProperty _spacing;
        private SerializedProperty _childAlignment;
        private SerializedProperty _justify;
        private SerializedProperty _reverseArrangement;
        private SerializedProperty _childControlWidth;
        private SerializedProperty _childControlHeight;
        private SerializedProperty _childScaleWidth;
        private SerializedProperty _childScaleHeight;
        private SerializedProperty _childForceExpandWidth;
        private SerializedProperty _childForceExpandHeight;

        private void OnEnable() {
            _direction = serializedObject.FindProperty("_direction");
            _wrap = serializedObject.FindProperty("_wrap");
            _lineSpacing = serializedObject.FindProperty("_lineSpacing");
            _padding = serializedObject.FindProperty("m_Padding");
            _spacing = serializedObject.FindProperty("_spacing");
            _childAlignment = serializedObject.FindProperty("m_ChildAlignment");
            _justify = serializedObject.FindProperty("_justify");
            _reverseArrangement = serializedObject.FindProperty("_reverseArrangement");
            _childControlWidth = serializedObject.FindProperty("_childControlWidth");
            _childControlHeight = serializedObject.FindProperty("_childControlHeight");
            _childScaleWidth = serializedObject.FindProperty("_childScaleWidth");
            _childScaleHeight = serializedObject.FindProperty("_childScaleHeight");
            _childForceExpandWidth = serializedObject.FindProperty("_childForceExpandWidth");
            _childForceExpandHeight = serializedObject.FindProperty("_childForceExpandHeight");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_wrap);
            EditorGUILayout.PropertyField(_padding, true);
            EditorGUILayout.PropertyField(_spacing);
            if (_wrap.boolValue || _wrap.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(_lineSpacing);
            EditorGUILayout.PropertyField(_childAlignment);
            EditorGUILayout.PropertyField(_justify);
            EditorGUILayout.PropertyField(_reverseArrangement);
            TogglePair("Control Child Size", _childControlWidth, _childControlHeight);
            TogglePair("Use Child Scale", _childScaleWidth, _childScaleHeight);
            // Justify ignores Child Force Expand along Direction, so that toggle is shown off and locked.
            // The stored value is kept and applies again once Justify is back to Child Alignment.
            bool justifies = !_justify.hasMultipleDifferentValues && _justify.enumValueIndex != (int)Justify.ChildAlignment
                && !_direction.hasMultipleDifferentValues;
            bool vertical = _direction.enumValueIndex == (int)FlexDirection.Vertical;
            TogglePair("Child Force Expand", _childForceExpandWidth, _childForceExpandHeight, justifies && !vertical, justifies && vertical);
            serializedObject.ApplyModifiedProperties();
        }

        // Same layout as HorizontalOrVerticalLayoutGroupEditor: a label, then Width and Height toggles.
        private static void TogglePair(string label, SerializedProperty width, SerializedProperty height, bool lockWidth = false, bool lockHeight = false) {
            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent(label));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, width, EditorGUIUtility.TrTextContent("Width"), lockWidth);
            rect.x += rect.width + 2;
            ToggleLeft(rect, height, EditorGUIUtility.TrTextContent("Height"), lockHeight);
            EditorGUIUtility.labelWidth = 0;
        }

        private static void ToggleLeft(Rect position, SerializedProperty property, GUIContent label, bool locked) {
            if (locked) {
                using (new EditorGUI.DisabledScope(true)) {
                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUI.ToggleLeft(position, label, false);
                    EditorGUI.indentLevel = indent;
                }
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            bool toggle = EditorGUI.ToggleLeft(position, label, property.boolValue);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
                property.boolValue = property.hasMultipleDifferentValues || toggle;
            EditorGUI.EndProperty();
        }

        // Runs once per selected object.
        [MenuItem("CONTEXT/HorizontalLayoutGroup/" + UpgradeMenu)]
        [MenuItem("CONTEXT/VerticalLayoutGroup/" + UpgradeMenu)]
        private static void Upgrade(MenuCommand command) {
            var source = (HorizontalOrVerticalLayoutGroup)command.context;
            GameObject gameObject = source.gameObject;
            int index = System.Array.IndexOf(gameObject.GetComponents<Component>(), source);

            var direction = source is VerticalLayoutGroup ? FlexDirection.Vertical : FlexDirection.Horizontal;
            var padding = new RectOffset(source.padding.left, source.padding.right, source.padding.top, source.padding.bottom);
            TextAnchor childAlignment = source.childAlignment;
            float spacing = source.spacing;
            bool reverseArrangement = source.reverseArrangement;
            bool childControlWidth = source.childControlWidth, childControlHeight = source.childControlHeight;
            bool childScaleWidth = source.childScaleWidth, childScaleHeight = source.childScaleHeight;
            bool childForceExpandWidth = source.childForceExpandWidth, childForceExpandHeight = source.childForceExpandHeight;
            bool enabled = source.enabled;

            Undo.SetCurrentGroupName(UndoName);
            int group = Undo.GetCurrentGroup();

            // LayoutGroup disallows multiple components, so the old one goes first.
            Undo.DestroyObjectImmediate(source);
            if (source != null) {
                // Another component requires it; Unity has already logged why.
                Undo.CollapseUndoOperations(group);
                return;
            }

            var flex = Undo.AddComponent<FlexLayoutGroup>(gameObject);
            // So Redo brings the copied values back, not the defaults.
            Undo.RecordObject(flex, UndoName);
            flex.direction = direction;
            flex.padding = padding;
            flex.childAlignment = childAlignment;
            flex.spacing = spacing;
            flex.reverseArrangement = reverseArrangement;
            flex.childControlWidth = childControlWidth;
            flex.childControlHeight = childControlHeight;
            flex.childScaleWidth = childScaleWidth;
            flex.childScaleHeight = childScaleHeight;
            flex.childForceExpandWidth = childForceExpandWidth;
            flex.childForceExpandHeight = childForceExpandHeight;
            flex.enabled = enabled;

            // Keep the original component order: layout controllers on one GameObject run in that order.
            for (int i = gameObject.GetComponents<Component>().Length - 1; i > index; i--)
                ComponentUtility.MoveComponentUp(flex);

            Undo.CollapseUndoOperations(group);
        }
    }
}
