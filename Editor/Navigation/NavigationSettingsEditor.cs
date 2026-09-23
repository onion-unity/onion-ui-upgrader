using Onion.UI.Navigation;
using UnityEditor;

namespace Onion.UI.Editor {
    [CustomEditor(typeof(NavigationSettings))]
    internal sealed class NavigationSettingsEditor : UnityEditor.Editor {
        private SerializedProperty _defaultProfile;

        private void OnEnable() {
            _defaultProfile = serializedObject.FindProperty(nameof(NavigationSettings.defaultProfile));
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            // Always say how to switch, so clearing the field is discoverable while it's on.
            if (_defaultProfile.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("The navigation upgrade is off. Selectables use Unity's default navigation.\nAssign a Navigation Profile to turn it on.", MessageType.Info);
            }
            else {
                EditorGUILayout.HelpBox("The navigation upgrade is on.\nClear the profile to turn it off and use Unity's default navigation.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(_defaultProfile);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
