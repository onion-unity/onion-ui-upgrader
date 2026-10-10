using System;
using Onion.UI.Selectables;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Onion.UI.Editor {
    /// <summary>
    /// The effect list of a SelectableStyle, with a + menu listing every SelectableEffect type, including the project's own.
    /// </summary>
    [CustomEditor(typeof(SelectableStyle))]
    internal sealed class SelectableStyleEditor : UnityEditor.Editor {
        // Room for the foldout arrow inside the list.
        private const float FoldoutIndent = 12f;

        private SerializedProperty _effects;
        private ReorderableList _list;

        private void OnEnable() {
            _effects = serializedObject.FindProperty(SelectableStyle.EffectsField);
            _list = new ReorderableList(serializedObject, _effects, true, true, true, true) {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Effects"),
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(_effects.GetArrayElementAtIndex(index), true),
                drawElementCallback = DrawElement,
                onAddDropdownCallback = (_, _) => ShowAddMenu(),
            };
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            _list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused) {
            var element = _effects.GetArrayElementAtIndex(index);
            rect.xMin += FoldoutIndent;
            EditorGUI.PropertyField(rect, element, new GUIContent(DisplayName(element.managedReferenceValue?.GetType())), true);
        }

        private void ShowAddMenu() {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<SelectableEffect>()) {
                if (type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null) {
                    continue;
                }

                menu.AddItem(new GUIContent(DisplayName(type)), false, () => Add(type));
            }

            menu.ShowAsContext();
        }

        private void Add(Type type) {
            serializedObject.Update();
            int index = _effects.arraySize;
            _effects.arraySize++;
            var element = _effects.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }

        // "ColorTintEffect" → "Color Tint".
        private static string DisplayName(Type type) {
            if (type == null) {
                return "(Missing)";
            }

            string name = type.Name;
            if (name.EndsWith("Effect") && name.Length > "Effect".Length) {
                name = name.Substring(0, name.Length - "Effect".Length);
            }

            return ObjectNames.NicifyVariableName(name);
        }
    }
}
