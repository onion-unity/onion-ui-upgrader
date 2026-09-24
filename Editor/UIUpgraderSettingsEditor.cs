using System;
using UnityEditor;
using UnityEngine;

namespace Onion.UI.Editor {
    [CustomEditor(typeof(UIUpgraderSettings))]
    internal sealed class UIUpgraderSettingsEditor : UnityEditor.Editor {
        private const float HeaderHeight = 20f;
        private const float IconSize = 13f;
        private const float IconSpacing = 3f;

        private static readonly Color _upgradedColor = new Color(0.4f, 0.8f, 0.45f);
        private static readonly Color _defaultColor = new Color(0.55f, 0.55f, 0.55f);

        private static GUIStyle _badgeStyle;

        private SerializedProperty _navigation;

        private void OnEnable() {
            _navigation = serializedObject.FindProperty(nameof(UIUpgraderSettings.navigation));
        }

        public override void OnInspectorGUI() {
            EditorGUILayout.Space();
            if (GUILayout.Button("Edit in Project Settings Window", GUILayout.Height(32f))) {
                UIUpgraderProjectSettings.Open();
            }

            EditorGUILayout.Space();
            DrawSettings();
        }

        /// <summary>
        /// The settings without the "Edit in Project Settings Window" button, for the Project Settings page itself.
        /// </summary>
        internal void DrawSettings() {
            serializedObject.Update();

            DrawSection(_navigation, "Navigation", NavigationSettingsDrawer.Draw);
            DrawSplitter();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// One upgrade: a URP-style header band (foldout, enable checkbox, title, state badge) and its settings,
        /// which are locked while the upgrade is off.
        /// </summary>
        private static void DrawSection(SerializedProperty section, string title, Action<SerializedProperty> drawBody) {
            _badgeStyle ??= new GUIStyle(EditorStyles.label);

            var enabled = section.FindPropertyRelative(nameof(UpgradeSettings.enabled));

            DrawSplitter();
            var header = GUILayoutUtility.GetRect(1f, HeaderHeight);

            // The band spans the whole view, like URP's headers; anything outside is clipped.
            var band = header;
            band.xMin = 0f;
            band.xMax = EditorGUIUtility.currentViewWidth;
            float tint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(band, new Color(tint, tint, tint, 0.2f));

            float iconY = header.y + (header.height - IconSize) * 0.5f;
            var foldout = new Rect(header.x, iconY, IconSize, IconSize);
            var toggle = new Rect(foldout.xMax + IconSpacing, iconY, IconSize, IconSize);
            // Badge starts at the value column, so it lines up with the fields below.
            var badge = new Rect(header.x + EditorGUIUtility.labelWidth, header.y, header.xMax - header.x - EditorGUIUtility.labelWidth, header.height);
            var label = new Rect(toggle.xMax + IconSpacing, header.y, badge.x - toggle.xMax - IconSpacing, header.height);

            section.isExpanded = GUI.Toggle(foldout, section.isExpanded, GUIContent.none, EditorStyles.foldout);

            bool wasEnabled = enabled.boolValue;
            enabled.boolValue = GUI.Toggle(toggle, wasEnabled, GUIContent.none, EditorStyles.toggle);
            if (enabled.boolValue && !wasEnabled) {
                section.isExpanded = true;
            }

            EditorGUI.LabelField(label, title, EditorStyles.boldLabel);

            _badgeStyle.normal.textColor = enabled.boolValue ? _upgradedColor : _defaultColor;
            GUI.Label(badge, enabled.boolValue ? "● Upgraded" : "○ Unity Default", _badgeStyle);

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && label.Contains(e.mousePosition)) {
                section.isExpanded = !section.isExpanded;
                e.Use();
            }

            if (section.isExpanded) {
                EditorGUILayout.Space(2f);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!enabled.boolValue)) {
                    drawBody(section);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4f);
            }
        }

        private static void DrawSplitter() {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            rect.xMin = 0f;
            rect.xMax = EditorGUIUtility.currentViewWidth;
            float tint = EditorGUIUtility.isProSkin ? 0.12f : 0.6f;
            EditorGUI.DrawRect(rect, new Color(tint, tint, tint, 1f));
        }
    }
}
