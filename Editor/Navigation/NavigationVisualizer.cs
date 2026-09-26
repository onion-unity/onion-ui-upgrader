using System;
using System.Reflection;
using Onion.UI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using UINavigation = UnityEngine.UI.Navigation;

namespace Onion.UI.Editor {
    /// <summary>
    /// Scene view navigation arrows that reflect the upgrader, drawn like Unity's own "Visualize".
    /// Unity's version calls Selectable.FindSelectableOnX (its own algorithm), which can't be changed
    /// without subclassing, so this draws the upgraded result separately.
    /// </summary>
    [InitializeOnLoad]
    internal static class NavigationVisualizer {
        private const string EnabledKey = "Onion.UI.Navigation.Visualize";
        private const string RestoreUnityKey = "Onion.UI.Navigation.RestoreUnityVisualize";
        private const float ArrowThickness = 2.5f;
        private const float ArrowHeadSize = 1.2f;

        // Unity's Visualize toggle: a private static in SelectableEditor, mirrored in EditorPrefs.
        // Only the EditorPrefs value is used if the field can't be found (e.g. a future uGUI version).
        private const string UnityEnabledKey = "SelectableEditor.ShowNavigation";
        private static readonly FieldInfo UnityEnabledField = Type.GetType("UnityEditor.UI.SelectableEditor, UnityEditor.UI")
            ?.GetField("s_ShowNavigation", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool _enabled;

        /// <summary>
        /// Turning this on hides Unity's own Visualize (the arrows would overlap);
        /// turning it off brings Unity's back if it was on before.
        /// </summary>
        internal static bool enabled {
            get => _enabled;
            set {
                if (_enabled == value) {
                    return;
                }

                if (value) {
                    EditorPrefs.SetBool(RestoreUnityKey, unityEnabled);
                    unityEnabled = false;
                }
                else {
                    if (EditorPrefs.GetBool(RestoreUnityKey, false)) {
                        unityEnabled = true;
                    }

                    EditorPrefs.DeleteKey(RestoreUnityKey);
                }

                _enabled = value;
                EditorPrefs.SetBool(EnabledKey, value);
                SceneView.RepaintAll();
            }
        }

        private static bool unityEnabled {
            get => UnityEnabledField != null ? (bool)UnityEnabledField.GetValue(null) : EditorPrefs.GetBool(UnityEnabledKey, false);
            set {
                UnityEnabledField?.SetValue(null, value);
                EditorPrefs.SetBool(UnityEnabledKey, value);
            }
        }

        static NavigationVisualizer() {
            _enabled = EditorPrefs.GetBool(EnabledKey, false);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView view) {
            if (!_enabled || Event.current.type != EventType.Repaint) {
                return;
            }

            // Unity's Visualize was switched on while ours is on: step aside instead of overlapping.
            if (unityEnabled) {
                _enabled = false;
                EditorPrefs.SetBool(EnabledKey, false);
                EditorPrefs.DeleteKey(RestoreUnityKey);
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            var selected = Selection.transforms;
            // While a profile is being edited, nothing in the scene is selected, so show every arrow at full strength.
            bool editingProfile = Selection.activeObject is NavigationProfile;
            // Nothing selected in the scene: draw nothing rather than a faint web of every arrow.
            if (!editingProfile && selected.Length == 0) {
                return;
            }

            foreach (var selectable in Selectable.allSelectablesArray) {
                if (!StageUtility.IsGameObjectRenderedByCamera(selectable.gameObject, Camera.current)) {
                    continue;
                }

                Draw(selectable, editingProfile || Array.IndexOf(selected, selectable.transform) >= 0);
            }
        }

        private static void Draw(Selectable selectable, bool active) {
            if (selectable.navigation.mode == UINavigation.Mode.None) {
                return;
            }

            // Upgraded modes show what the upgrader computes; the rest keep Unity's own result.
            bool upgraded = NavigationUpgrader.TryResolve(selectable, out var navigation);
            var left = upgraded ? navigation.selectOnLeft : selectable.FindSelectableOnLeft();
            var right = upgraded ? navigation.selectOnRight : selectable.FindSelectableOnRight();
            var up = upgraded ? navigation.selectOnUp : selectable.FindSelectableOnUp();
            var down = upgraded ? navigation.selectOnDown : selectable.FindSelectableOnDown();

            // Same colors as Unity's visualizer; Explicit uses purple / blue
            // so hand-set links stand out.
            bool isExplicit = selectable.navigation.mode == UINavigation.Mode.Explicit;
            float alpha = active ? 1f : 0.4f;

            Handles.color = isExplicit ? new Color(0.65f, 0.45f, 0.9f, alpha) : new Color(1f, 0.6f, 0.2f, alpha);
            DrawArrow(Vector2.left, selectable, left);
            DrawArrow(Vector2.up, selectable, up);

            Handles.color = isExplicit ? new Color(0.3f, 0.6f, 1f, alpha) : new Color(1f, 0.9f, 0.1f, alpha);
            DrawArrow(Vector2.right, selectable, right);
            DrawArrow(Vector2.down, selectable, down);
        }

        // Mirrors Unity's SelectableEditor.DrawNavigationArrow.
        private static void DrawArrow(Vector2 direction, Selectable from, Selectable to) {
            if (from == null || to == null) {
                return;
            }

            var fromTransform = from.transform;
            var toTransform = to.transform;

            var sideDirection = new Vector2(direction.y, -direction.x);
            var fromPoint = fromTransform.TransformPoint(GetPointOnRectEdge(fromTransform as RectTransform, direction));
            var toPoint = toTransform.TransformPoint(GetPointOnRectEdge(toTransform as RectTransform, -direction));
            float fromSize = HandleUtility.GetHandleSize(fromPoint) * 0.05f;
            float toSize = HandleUtility.GetHandleSize(toPoint) * 0.05f;
            fromPoint += fromTransform.TransformDirection(sideDirection) * fromSize;
            toPoint += toTransform.TransformDirection(sideDirection) * toSize;

            float length = Vector3.Distance(fromPoint, toPoint);
            var fromTangent = fromTransform.rotation * direction * length * 0.3f;
            var toTangent = toTransform.rotation * -direction * length * 0.3f;

            Handles.DrawBezier(fromPoint, toPoint, fromPoint + fromTangent, toPoint + toTangent, Handles.color, null, ArrowThickness);
            Handles.DrawAAPolyLine(ArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction - sideDirection) * toSize * ArrowHeadSize);
            Handles.DrawAAPolyLine(ArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction + sideDirection) * toSize * ArrowHeadSize);
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 direction) {
            if (rect == null) {
                return Vector3.zero;
            }

            if (direction != Vector2.zero) {
                direction /= Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            }

            return rect.rect.center + Vector2.Scale(rect.rect.size, direction * 0.5f);
        }
    }
}
