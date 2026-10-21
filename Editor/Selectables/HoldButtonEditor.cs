using Onion.UI.Selectables;
using UnityEditor;
using UnityEditor.UI;

namespace Onion.UI.Editor {
    // ButtonEditor draws only Button's own fields, so the hold fields are added below them.
    [CustomEditor(typeof(HoldButton), true)]
    [CanEditMultipleObjects]
    internal class HoldButtonEditor : ButtonEditor {
        private SerializedProperty _holdDuration;
        private SerializedProperty _onHoldProgress;

        protected override void OnEnable() {
            base.OnEnable();
            _holdDuration = serializedObject.FindProperty(nameof(HoldButton.holdDuration));
            _onHoldProgress = serializedObject.FindProperty(nameof(HoldButton.onHoldProgress));
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_holdDuration);
            EditorGUILayout.PropertyField(_onHoldProgress);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
