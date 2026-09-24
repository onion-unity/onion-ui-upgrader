using System.Collections.Generic;
using Onion.UI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Onion.UI.Editor {
    [CustomEditor(typeof(NavigationProfile))]
    internal sealed class NavigationProfileEditor : UnityEditor.Editor {
        private const float PreviewHeight = 180f;

        // Example layout in the origin's local space (y up), all buttons 40x20.
        private static readonly Vector2 ButtonSize = new(40f, 20f);
        private static readonly Vector2 PreviewExtent = new(320f, 180f);
        private static readonly Rect Origin = Around(Vector2.zero);
        private static readonly Rect[] Candidates = {
            Around(new Vector2(130f, 0f)),
            Around(new Vector2(55f, 35f)),
            Around(new Vector2(60f, -50f)),
            Around(new Vector2(0f, 65f)),
            Around(new Vector2(-50f, 30f)),
            Around(new Vector2(-110f, 0f)),
            Around(new Vector2(-35f, -42f)),
            Around(new Vector2(0f, -72f)),
        };
        private static readonly Vector2[] Directions = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        // Axis colors are for detection areas and arrows only; boxes use their own state colors
        // so the center/picked/other buttons stay distinguishable regardless of direction.
        private static readonly Color HorizontalColor = new(0.25f, 0.6f, 1f);
        private static readonly Color VerticalColor = new(1f, 0.6f, 0.2f);
        private static readonly Color PickedFill = new(1f, 1f, 1f, 0.6f);

        // Mouse position in the IMGUI container's space, tracked through UI Toolkit pointer events
        // because the Unity 6 inspector doesn't forward MouseMove to IMGUI.
        private Vector2? _mouse;
        private Rect _previewRect;

        public override VisualElement CreateInspectorGUI() {
            var container = new IMGUIContainer(OnInspectorGUI);

            container.RegisterCallback<PointerMoveEvent>(evt => {
                Vector2 position = evt.localPosition;
                bool wasHovering = _mouse.HasValue;
                _mouse = _previewRect.Contains(position) ? position : null;

                if (_mouse.HasValue || wasHovering) {
                    container.MarkDirtyRepaint();
                }
            });

            container.RegisterCallback<PointerLeaveEvent>(_ => {
                if (_mouse.HasValue) {
                    _mouse = null;
                    container.MarkDirtyRepaint();
                }
            });

            return container;
        }

        public override void OnInspectorGUI() {
            var settings = UIUpgraderSettings.instance;
            
            if (settings != null && settings.navigation.projectWideProfile == target) {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(settings.navigation.enabled
                    ? "This is the project-wide navigation profile."
                    : "This is the project-wide navigation profile, but the navigation upgrade is off.", MessageType.Info);
                EditorGUILayout.Space();
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");

            // 90° accepts everything ahead and bias 0.25 is Unity's own scoring, so this reproduces
            // Unity's built-in automatic navigation.
            var reset = new GUIContent("Reset", "Same as Unity's built-in automatic navigation.");
            var visualize = new GUIContent("Visualize", "Show the upgraded navigation in the Scene view. Uses the default profile, like the runtime does.");
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                NavigationVisualizer.enabled = GUILayout.Toggle(NavigationVisualizer.enabled, visualize, GUI.skin.button, GUILayout.Width(70f));

                if (GUILayout.Button(reset, GUILayout.Width(60f))) {
                    serializedObject.FindProperty(nameof(NavigationProfile.directionTolerance)).floatValue = 90f;
                    serializedObject.FindProperty(nameof(NavigationProfile.alignmentBias)).floatValue = 0.25f;
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() && NavigationVisualizer.enabled) {
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            var previewRect = GUILayoutUtility.GetRect(0f, PreviewHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) {
                _previewRect = previewRect;
            }

            DrawPreview(previewRect, (NavigationProfile)target, _mouse);
            DrawLegend(GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true)));
        }

        // Origin is a bit darker than the background; other buttons are lighter than the origin.
        private static bool isDark => EditorGUIUtility.isProSkin;
        private static Color BackgroundColor => isDark ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.82f, 0.82f, 0.82f);
        private static Color OriginFill => isDark ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.68f, 0.68f, 0.68f);
        private static Color CandidateFill => isDark ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f);
        private static Color OutlineColor => isDark ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.45f, 0.45f, 0.45f);
        private static Color HatchColor => isDark ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.2f);

        private static void DrawLegend(Rect row) {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            const float swatchSize = 10f;
            var style = EditorStyles.miniLabel;
            float x = row.x;

            void Item(Color fill, string label) {
                var swatch = new Rect(x, row.center.y - swatchSize * 0.5f, swatchSize, swatchSize);
                DrawBox(swatch, fill, OutlineColor);

                var content = new GUIContent(label);
                float width = style.CalcSize(content).x;
                style.Draw(new Rect(swatch.xMax + 4f, row.y, width, row.height), content, false, false, false, false);
                x = swatch.xMax + 4f + width + 12f;
            }

            Item(OriginFill, "Current");
            Item(PickedFill, "Selected");
            Item(CandidateFill, "Not selected");
        }

        /// <param name="mouse">
        /// GUI-space mouse position. When it is over the preview, only the direction of the
        /// diagonal-split quarter under it is highlighted; otherwise every direction is shown.
        /// </param>
        private static void DrawPreview(Rect area, NavigationProfile profile, Vector2? mouse = null) {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            var originFill = OriginFill;
            var candidateFill = CandidateFill;
            var outline = OutlineColor;

            const float cornerRadius = 4f;
            GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, BackgroundColor, 0f, cornerRadius);

            // Content is clipped 1px inside so the border drawn last stays on top.
            var clip = new Rect(area.x + 1f, area.y + 1f, area.width - 2f, area.height - 2f);
            GUI.BeginClip(clip);
            var local = new Rect(0f, 0f, clip.width, clip.height);
            float scale = Mathf.Min(local.width / PreviewExtent.x, local.height / PreviewExtent.y);

            Vector3 ToGui(Vector2 point) => new(local.center.x + point.x * scale, local.center.y - point.y * scale);
            Rect ToGuiRect(Rect rect) {
                var min = ToGui(new Vector2(rect.xMin, rect.yMax));
                var max = ToGui(new Vector2(rect.xMax, rect.yMin));
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            Vector3[] ToGuiPolygon(IReadOnlyList<Vector2> polygon) {
                var points = new Vector3[polygon.Count];
                for (int i = 0; i < points.Length; i++) {
                    points[i] = ToGui(polygon[i]);
                }

                return points;
            }

            float radians = profile.directionTolerance * Mathf.Deg2Rad;
            var areas = new Vector2[Directions.Length][];
            for (int i = 0; i < Directions.Length; i++) {
                areas[i] = DetectionArea(Directions[i], radians);
            }

            // The preview is split into four triangles by its diagonals; hovering one focuses that
            // direction. Otherwise every direction is shown.
            var active = new bool[Directions.Length];
            Vector2? hovered = null;
            if (mouse.HasValue && clip.Contains(mouse.Value)) {
                var offset = mouse.Value - clip.center;
                float x = offset.x / (clip.width * 0.5f);
                float y = -offset.y / (clip.height * 0.5f);
                hovered = Mathf.Abs(x) >= Mathf.Abs(y)
                    ? (x >= 0f ? Vector2.right : Vector2.left)
                    : (y >= 0f ? Vector2.up : Vector2.down);
            }

            for (int i = 0; i < active.Length; i++) {
                active[i] = !hovered.HasValue || Directions[i] == hovered.Value;
            }

            for (int i = 0; i < Directions.Length; i++) {
                var color = AxisColor(Directions[i]);
                Handles.color = new Color(color.r, color.g, color.b, active[i] ? 0.12f : 0.04f);
                Handles.DrawAAConvexPolygon(ToGuiPolygon(areas[i]));
            }

            // Past 45° the horizontal and vertical areas overlap; mark that part so each boundary line
            // visibly belongs to its own area instead of looking like it borders the other one.
            for (int i = 0; i < Directions.Length; i++) {
                for (int j = i + 1; j < Directions.Length; j++) {
                    if ((Directions[i].x != 0f) == (Directions[j].x != 0f) || !active[i] || !active[j]) {
                        continue;
                    }

                    var overlap = Intersect(areas[i], areas[j]);
                    if (overlap.Count >= 3) {
                        DrawHatch(ToGuiPolygon(overlap), local, HatchColor);
                    }
                }
            }

            // Boundary lines: area points 0→5 and 1→2 are the two edges fanning out from the origin's corners.
            for (int i = 0; i < Directions.Length; i++) {
                var color = AxisColor(Directions[i]);
                var detection = areas[i];

                Handles.color = new Color(color.r, color.g, color.b, active[i] ? 0.6f : 0.15f);
                Handles.DrawAAPolyLine(1.5f, ToGui(detection[0]), ToGui(detection[5]));
                Handles.DrawAAPolyLine(1.5f, ToGui(detection[1]), ToGui(detection[2]));
            }

            foreach (var candidate in Candidates) {
                DrawBox(ToGuiRect(candidate), candidateFill, outline);
            }

            for (int d = 0; d < Directions.Length; d++) {
                if (!active[d]) {
                    continue;
                }

                var direction = Directions[d];
                var search = new NeighborSearch(Origin, direction, false, profile);
                for (int i = 0; i < Candidates.Length; i++) {
                    search.Consider(i, Candidates[i]);
                }

                if (search.result < 0) {
                    continue;
                }

                var pick = Candidates[search.result];
                DrawBox(ToGuiRect(pick), PickedFill, outline);
                // Like Unity's Selectable navigation gizmo: leave the origin's edge in the move direction
                // and enter the picked button's facing edge, curving in between.
                var from = Origin.center + direction * Mathf.Abs(Vector2.Dot(Origin.size * 0.5f, direction));
                var to = pick.center - direction * Mathf.Abs(Vector2.Dot(pick.size * 0.5f, direction));
                DrawArrow(ToGui(from), ToGui(to), new Vector3(direction.x, -direction.y), AxisColor(direction));
            }

            DrawBox(ToGuiRect(Origin), originFill, outline);
            GUI.EndClip();

            GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, outline, 1f, cornerRadius);
        }

        private static Color AxisColor(Vector2 direction) {
            return direction.x != 0f ? HorizontalColor : VerticalColor;
        }

        /// <summary>
        /// Area (in example space) where a candidate's center gets detected: beyond the origin's leading
        /// edge, fanning out from its two corners by the tolerance. Points 0/1 are the corners, 5/2 the
        /// far ends of their boundary lines; 3/4 push the far side forward so the polygon keeps its area at 90°.
        /// </summary>
        private static Vector2[] DetectionArea(Vector2 direction, float radians) {
            float length = PreviewExtent.x;
            var perpendicular = new Vector2(-direction.y, direction.x);
            var halfExtent = Origin.size * 0.5f;
            float along = Mathf.Abs(Vector2.Dot(halfExtent, direction));
            float across = Mathf.Abs(Vector2.Dot(halfExtent, perpendicular));

            var cornerA = Origin.center + direction * along - perpendicular * across;
            var cornerB = Origin.center + direction * along + perpendicular * across;
            var boundaryA = direction * Mathf.Cos(radians) - perpendicular * Mathf.Sin(radians);
            var boundaryB = direction * Mathf.Cos(radians) + perpendicular * Mathf.Sin(radians);

            return new[] {
                cornerA,
                cornerB,
                cornerB + boundaryB * length,
                cornerB + (boundaryB + direction) * length,
                cornerA + (boundaryA + direction) * length,
                cornerA + boundaryA * length,
            };
        }

        /// <summary>
        /// Fills a convex polygon (in GUI space) with diagonal lines spaced a few pixels apart.
        /// </summary>
        private static void DrawHatch(Vector3[] polygon, Rect bounds, Color color) {
            const float spacing = 6f;

            var points = new Vector2[polygon.Length];
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < points.Length; i++) {
                points[i] = polygon[i];
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            // Nothing outside the visible area needs lines.
            min = Vector2.Max(min, bounds.min);
            max = Vector2.Min(max, bounds.max);

            float orientation = Mathf.Sign(SignedArea(points));
            Handles.color = color;

            // Lines x + y = c sweep the bounding box; each is clipped to the polygon. Snapping c to the
            // spacing keeps the pattern continuous across separate overlap areas.
            for (float c = Mathf.Ceil((min.x + min.y) / spacing) * spacing; c <= max.x + max.y; c += spacing) {
                var from = new Vector2(min.x, c - min.x);
                var to = new Vector2(max.x, c - max.x);

                if (ClipSegment(from, to, points, orientation, out var start, out var end)) {
                    Handles.DrawAAPolyLine(1f, start, end);
                }
            }
        }

        // Cyrus–Beck: clips a segment to a convex polygon.
        private static bool ClipSegment(Vector2 from, Vector2 to, Vector2[] polygon, float orientation, out Vector2 start, out Vector2 end) {
            var delta = to - from;
            float enter = 0f;
            float exit = 1f;
            start = end = default;

            for (int i = 0; i < polygon.Length; i++) {
                var a = polygon[i];
                var edge = polygon[(i + 1) % polygon.Length] - a;

                // Inside when Cross(edge, point - a) * orientation >= 0.
                float distance = Cross(edge, from - a) * orientation;
                float rate = Cross(edge, delta) * orientation;

                if (Mathf.Approximately(rate, 0f)) {
                    if (distance < 0f) {
                        return false;
                    }

                    continue;
                }

                float t = -distance / rate;
                if (rate > 0f) {
                    enter = Mathf.Max(enter, t);
                }
                else {
                    exit = Mathf.Min(exit, t);
                }

                if (enter > exit) {
                    return false;
                }
            }

            start = from + delta * enter;
            end = from + delta * exit;
            return true;
        }

        // Sutherland–Hodgman: clips the convex subject polygon by the convex clip polygon.
        private static List<Vector2> Intersect(Vector2[] subject, Vector2[] clip) {
            var output = new List<Vector2>(subject);
            float orientation = Mathf.Sign(SignedArea(clip));

            for (int i = 0; i < clip.Length && output.Count > 0; i++) {
                var a = clip[i];
                var b = clip[(i + 1) % clip.Length];
                var input = output;
                output = new List<Vector2>(input.Count + 1);

                for (int j = 0; j < input.Count; j++) {
                    var p = input[j];
                    var q = input[(j + 1) % input.Count];
                    bool pInside = Cross(b - a, p - a) * orientation >= 0f;
                    bool qInside = Cross(b - a, q - a) * orientation >= 0f;

                    if (pInside) {
                        output.Add(p);
                    }

                    if (pInside != qInside) {
                        float t = Cross(a - p, b - a) / Cross(q - p, b - a);
                        output.Add(p + (q - p) * t);
                    }
                }
            }

            return output;
        }

        private static float SignedArea(Vector2[] polygon) {
            float area = 0f;
            for (int i = 0; i < polygon.Length; i++) {
                area += Cross(polygon[i], polygon[(i + 1) % polygon.Length]);
            }

            return area * 0.5f;
        }

        private static float Cross(Vector2 a, Vector2 b) {
            return a.x * b.y - a.y * b.x;
        }

        // DrawSolidRectangleWithOutline multiplies its colors by Handles.color, so reset it first.
        private static void DrawBox(Rect rect, Color fill, Color outline) {
            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(rect, fill, outline);
        }

        /// <param name="direction">GUI-space move direction; the curve starts and ends along it.</param>
        private static void DrawArrow(Vector3 from, Vector3 to, Vector3 direction, Color color) {
            const float headLength = 6f;
            const float headWidth = 4f;

            var normal = new Vector3(-direction.y, direction.x);
            var head = to - direction * headLength;
            float tangent = Vector3.Distance(from, head) * 0.5f;

            Handles.color = color;
            Handles.DrawBezier(from, head, from + direction * tangent, head - direction * tangent, color, null, 3f);
            Handles.DrawAAConvexPolygon(to, head + normal * headWidth, head - normal * headWidth);
        }

        private static Rect Around(Vector2 center) {
            return new Rect(center - ButtonSize * 0.5f, ButtonSize);
        }
    }
}
