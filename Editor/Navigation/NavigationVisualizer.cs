using System;
using System.Collections.Generic;
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

        // Group boxes: margin and dash length are × the handle size, so they stay constant on screen.
        private const float GroupThickness = 2f;
        private const float GroupMargin = 0.1f;
        private const float GroupDash = 0.06f;
        private static readonly Color GroupColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        private static readonly NavigationGroup[] _entered = new NavigationGroup[4];
        private static readonly Dictionary<NavigationGroup, Rect?> _boxes = new();
        private static readonly HashSet<NavigationGroup> _shownGroups = new();
        private static readonly Vector3[] _corners = new Vector3[4];
        private static Selectable[] _selectables;

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
            ObjectChangeEvents.changesPublished += OnObjectChanged;
        }

        // The Scene view only repaints on its own events, so inspector edits (a group's boundary, a
        // Selectable's mode, ...) would otherwise show only after the mouse moves over it.
        private static void OnObjectChanged(ref ObjectChangeEventStream stream) {
            if (_enabled) {
                SceneView.RepaintAll();
            }
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

            _selectables = Selectable.allSelectablesArray;
            _boxes.Clear();
            _shownGroups.Clear();

            // A selected group shows its direct members at full strength. A selected Selectable in a group
            // shows only that group's direct members. Otherwise every Selectable is shown, like Unity's.
            // Any other object (e.g. a container of groups) shows every arrow, and the boxes of every group under it.
            NavigationGroup focus = null;
            bool groupSelected = false;
            Transform overview = null;
            var activeTransform = Selection.activeTransform;
            if (!editingProfile && activeTransform != null) {
                if (activeTransform.TryGetComponent(out NavigationGroup group) && group.isActiveAndEnabled) {
                    focus = group;
                    groupSelected = true;
                }
                else if (activeTransform.TryGetComponent(out Selectable _)) {
                    focus = NavigationGroup.ScopeOf(activeTransform);
                }
                else {
                    overview = activeTransform;
                }
            }

            foreach (var selectable in _selectables) {
                if (!StageUtility.IsGameObjectRenderedByCamera(selectable.gameObject, Camera.current)) {
                    continue;
                }

                if (focus != null && NavigationGroup.ScopeOf(selectable.transform) != focus) {
                    continue;
                }

                bool active = editingProfile || groupSelected || Array.IndexOf(selected, selectable.transform) >= 0;
                Draw(selectable, active, collectGroups: active && overview == null);
            }

            // Boxes: every group while a profile is being edited; the groups under the selected object in the
            // overview; otherwise the focused group plus the groups the full-strength arrows enter or land in.
            if (editingProfile) {
                _shownGroups.UnionWith(NavigationGroup.activeGroups);
            }
            else if (overview != null) {
                foreach (var group in NavigationGroup.activeGroups) {
                    if (group.transform.IsChildOf(overview)) {
                        _shownGroups.Add(group);
                    }
                }
            }
            else if (focus != null) {
                _shownGroups.Add(focus);
            }

            foreach (var group in _shownGroups) {
                if (StageUtility.IsGameObjectRenderedByCamera(group.gameObject, Camera.current)) {
                    DrawGroup(group);
                }
            }
        }

        private static void Draw(Selectable selectable, bool active, bool collectGroups) {
            if (selectable.navigation.mode == UINavigation.Mode.None) {
                return;
            }

            // Upgraded modes show what the upgrader computes; the rest keep Unity's own result.
            bool upgraded = NavigationUpgrader.TryResolve(selectable, out var navigation, _entered);
            var left = upgraded ? navigation.selectOnLeft : selectable.FindSelectableOnLeft();
            var right = upgraded ? navigation.selectOnRight : selectable.FindSelectableOnRight();
            var up = upgraded ? navigation.selectOnUp : selectable.FindSelectableOnUp();
            var down = upgraded ? navigation.selectOnDown : selectable.FindSelectableOnDown();

            // Same colors as Unity's visualizer; Explicit uses purple / blue
            // so hand-set links stand out.
            bool isExplicit = selectable.navigation.mode == UINavigation.Mode.Explicit;
            float alpha = active ? 1f : 0.4f;

            Handles.color = isExplicit ? new Color(0.65f, 0.45f, 0.9f, alpha) : new Color(1f, 0.6f, 0.2f, alpha);
            DrawArrow(Vector2.left, selectable, left, _entered[0]);
            DrawArrow(Vector2.up, selectable, up, _entered[2]);

            Handles.color = isExplicit ? new Color(0.3f, 0.6f, 1f, alpha) : new Color(1f, 0.9f, 0.1f, alpha);
            DrawArrow(Vector2.right, selectable, right, _entered[1]);
            DrawArrow(Vector2.down, selectable, down, _entered[3]);

            if (collectGroups) {
                CollectGroup(left, _entered[0]);
                CollectGroup(right, _entered[1]);
                CollectGroup(up, _entered[2]);
                CollectGroup(down, _entered[3]);
            }
        }

        // The group an arrow enters, or else the group its target already belongs to (e.g. a Pass Through
        // move back out into the parent group).
        private static void CollectGroup(Selectable target, NavigationGroup entered) {
            var group = entered != null ? entered : target != null ? NavigationGroup.ScopeOf(target.transform) : null;
            if (group != null) {
                _shownGroups.Add(group);
            }
        }

        // Mirrors Unity's SelectableEditor.DrawNavigationArrow. A move that enters a group points at the group's box.
        private static void DrawArrow(Vector2 direction, Selectable from, Selectable to, NavigationGroup group) {
            if (from == null || to == null) {
                return;
            }

            var fromTransform = from.transform;
            Transform toTransform;
            Rect toRect;
            if (group != null && TryGetBox(group, out toRect)) {
                toTransform = group.transform;
            }
            else {
                toTransform = to.transform;
                toRect = RectOf(toTransform);
            }

            var sideDirection = new Vector2(direction.y, -direction.x);
            var fromPoint = fromTransform.TransformPoint(GetPointOnRectEdge(RectOf(fromTransform), direction));
            var toPoint = toTransform.TransformPoint(GetPointOnRectEdge(toRect, -direction));
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

        private static Rect RectOf(Transform transform) {
            return transform is RectTransform rectTransform ? rectTransform.rect : default;
        }

        private static Vector3 GetPointOnRectEdge(Rect rect, Vector2 direction) {
            if (direction != Vector2.zero) {
                direction /= Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            }

            return rect.center + Vector2.Scale(rect.size, direction * 0.5f);
        }

        // Contain = solid outline, Pass Through = dashed.
        private static void DrawGroup(NavigationGroup group) {
            if (!TryGetBox(group, out var box)) {
                return;
            }

            var transform = group.transform;
            var a = transform.TransformPoint(new Vector3(box.xMin, box.yMin));
            var b = transform.TransformPoint(new Vector3(box.xMax, box.yMin));
            var c = transform.TransformPoint(new Vector3(box.xMax, box.yMax));
            var d = transform.TransformPoint(new Vector3(box.xMin, box.yMax));

            Handles.color = GroupColor;
            if (group.boundary == NavigationBoundary.Contain) {
                Handles.DrawAAPolyLine(GroupThickness, a, b, c, d, a);
            }
            else {
                DrawDashedLine(a, b);
                DrawDashedLine(b, c);
                DrawDashedLine(c, d);
                DrawDashedLine(d, a);
            }
        }

        private static void DrawDashedLine(Vector3 from, Vector3 to) {
            float length = Vector3.Distance(from, to);
            float dash = HandleUtility.GetHandleSize(from) * GroupDash;
            if (length <= 0f || dash <= 0f) {
                return;
            }

            var step = (to - from) / length;
            for (float start = 0f; start < length; start += dash * 2f) {
                Handles.DrawAAPolyLine(GroupThickness, from + step * start, from + step * Mathf.Min(start + dash, length));
            }
        }

        /// <summary>
        /// The group's visible area in its local space: the bounds of its direct member Selectables and of its
        /// child groups' boxes, plus a margin. False when it has no active member. Cached per repaint.
        /// </summary>
        private static bool TryGetBox(NavigationGroup group, out Rect box) {
            if (_boxes.TryGetValue(group, out var cached)) {
                box = cached ?? default;
                return cached.HasValue;
            }

            var space = group.transform;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var selectable in _selectables) {
                if (selectable.transform is not RectTransform rectTransform || NavigationGroup.ScopeOf(rectTransform) != group) {
                    continue;
                }

                rectTransform.GetWorldCorners(_corners);
                Encapsulate(space, ref min, ref max);
            }

            foreach (var child in NavigationGroup.activeGroups) {
                if (child.parent != group || !TryGetBox(child, out var childBox)) {
                    continue;
                }

                var childSpace = child.transform;
                _corners[0] = childSpace.TransformPoint(new Vector3(childBox.xMin, childBox.yMin));
                _corners[1] = childSpace.TransformPoint(new Vector3(childBox.xMax, childBox.yMin));
                _corners[2] = childSpace.TransformPoint(new Vector3(childBox.xMax, childBox.yMax));
                _corners[3] = childSpace.TransformPoint(new Vector3(childBox.xMin, childBox.yMax));
                Encapsulate(space, ref min, ref max);
            }

            if (min.x > max.x) {
                _boxes[group] = null;
                box = default;
                return false;
            }

            // A constant on-screen margin, converted to the group's local units.
            float margin = HandleUtility.GetHandleSize(space.TransformPoint((min + max) * 0.5f)) * GroupMargin;
            var scale = space.lossyScale;
            var padding = new Vector2(
                margin / Mathf.Max(Mathf.Abs(scale.x), 1e-5f),
                margin / Mathf.Max(Mathf.Abs(scale.y), 1e-5f));

            box = Rect.MinMaxRect(min.x - padding.x, min.y - padding.y, max.x + padding.x, max.y + padding.y);
            _boxes[group] = box;
            return true;
        }

        private static void Encapsulate(Transform space, ref Vector2 min, ref Vector2 max) {
            foreach (var corner in _corners) {
                Vector2 point = space.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }
    }
}
