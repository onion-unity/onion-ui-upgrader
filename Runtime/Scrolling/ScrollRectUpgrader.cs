using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Scrolling {
    /// <summary>
    /// Where <see cref="ScrollRectUpgrader"/> brings the selected item in the viewport.
    /// </summary>
    public enum ScrollAlignment {
        /// <summary>Only scrolls when the item is outside the viewport, and only as far as needed.</summary>
        Nearest,
        /// <summary>Scrolls the item to the center of the viewport on every selection change.</summary>
        Center,
    }

    /// <summary>
    /// Scrolls the ScrollRect so the selection is visible when it changes to an item under the content, e.g. by
    /// keyboard or gamepad navigation. Selections made by a pointer press don't scroll, and neither does anything
    /// while the ScrollRect is being dragged. Works with or without the Navigation upgrade.
    /// </summary>
    [AddComponentMenu("Onion/UI/ScrollRect Upgrader")]
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    public sealed class ScrollRectUpgrader : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler {
        // Closer than this (in the content's parent space) counts as reached.
        private const float ReachedDistance = 0.5f;

        private static readonly Vector3[] _corners = new Vector3[4];

        [Tooltip("Nearest = only scrolls when the selection is outside the viewport, as far as needed.\nCenter = scrolls the selection to the center of the viewport on every change.")]
        public ScrollAlignment alignment = ScrollAlignment.Nearest;

        [Tooltip("Space kept between the selection and the viewport's edge (Nearest only).")]
        public float padding;

        [Tooltip("Approximate time to reach the selection, in unscaled seconds. 0 = instant.")]
        public float smoothTime = 0.1f;

        private ScrollRect _scrollRect;
        private GameObject _lastSelected;
        // The selection being scrolled to; null when not scrolling.
        private RectTransform _target;
        private Vector2 _velocity;
        private bool _dragging;

        private void Awake() {
            _scrollRect = GetComponent<ScrollRect>();
        }

        // Clamped here instead of [Min], whose IMGUI drawer misaligns with the UI Toolkit default inspector.
        private void OnValidate() {
            padding = Mathf.Max(0f, padding);
            smoothTime = Mathf.Max(0f, smoothTime);
        }

        private void OnEnable() {
            // A selection made before enabling doesn't scroll.
            var eventSystem = EventSystem.current;
            _lastSelected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        }

        private void OnDisable() {
            Stop();
            _dragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left) {
                _dragging = true;
                Stop();
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left) {
                _dragging = false;
            }
        }

        public void OnScroll(PointerEventData eventData) {
            Stop();
        }

        // After EventSystem.Update, so this frame's move or click has already changed the selection.
        private void LateUpdate() {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != _lastSelected) {
                _lastSelected = selected;

                // A pointer press takes over: the clicked item is already on screen. A selection leaving the content
                // lets a scroll in progress finish, instead of stopping halfway.
                var content = _scrollRect.content;
                if (UIInput.WasPointerPressed(eventSystem)) {
                    Stop();
                } else if (selected != null && content != null && !_dragging && selected.transform != content
                    && selected.transform.IsChildOf(content)) {
                    _target = selected.transform as RectTransform;
                    _scrollRect.StopMovement();
                }
            }

            if (_target != null) {
                Follow();
            }
        }

        private void Stop() {
            _target = null;
            _velocity = Vector2.zero;
        }

        // Moves the content toward the target's position, recomputed every frame since the layout may only be
        // built after the selection changed (e.g. a popup opened this frame). Stops once the target is reached.
        private void Follow() {
            var content = _scrollRect.content;
            if (content == null || !_target.gameObject.activeInHierarchy || !_target.IsChildOf(content)) {
                Stop();
                return;
            }

            var offset = GetOffset(content);
            var parent = content.parent;
            if (parent != null) {
                offset = parent.InverseTransformVector(Viewport.TransformVector(offset));
            }

            var current = content.anchoredPosition;
            var goal = current + offset;
            if (offset.sqrMagnitude < ReachedDistance * ReachedDistance) {
                content.anchoredPosition = goal;
                Stop();
                return;
            }

            content.anchoredPosition = smoothTime > 0f
                ? Vector2.SmoothDamp(current, goal, ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
                : goal;
        }

        private RectTransform Viewport => _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)_scrollRect.transform;

        // How far the content has to move, in the viewport's space, to bring the target where it belongs.
        private Vector2 GetOffset(RectTransform content) {
            var viewport = Viewport;
            var view = viewport.rect;
            var item = GetBounds(viewport, _target);
            var bounds = GetBounds(viewport, content);

            var offset = Vector2.zero;
            if (_scrollRect.horizontal) {
                offset.x = GetAxisOffset(view.xMin, view.xMax, item.xMin, item.xMax, bounds.xMin, bounds.xMax, false);
            }

            if (_scrollRect.vertical) {
                offset.y = GetAxisOffset(view.yMin, view.yMax, item.yMin, item.yMax, bounds.yMin, bounds.yMax, true);
            }

            return offset;
        }

        // fromMax: an item larger than the space shows its max side (the top on the vertical axis).
        private float GetAxisOffset(float viewMin, float viewMax, float itemMin, float itemMax, float contentMin, float contentMax, bool fromMax) {
            float offset;
            if (alignment == ScrollAlignment.Center) {
                offset = (viewMin + viewMax - itemMin - itemMax) * 0.5f;
            } else {
                float min = viewMin + padding;
                float max = viewMax - padding;
                if (itemMax - itemMin > max - min) {
                    offset = fromMax ? max - itemMax : min - itemMin;
                } else if (itemMin < min) {
                    offset = min - itemMin;
                } else if (itemMax > max) {
                    offset = max - itemMax;
                } else {
                    offset = 0f;
                }
            }

            if (_scrollRect.movementType == ScrollRect.MovementType.Unrestricted) {
                return offset;
            }

            // Keep the content covering the viewport; content smaller than the viewport doesn't scroll.
            if (contentMax - contentMin <= viewMax - viewMin) {
                return 0f;
            }

            return Mathf.Clamp(offset, viewMax - contentMax, viewMin - contentMin);
        }

        // The rect's bounds in the viewport's local space.
        private static Rect GetBounds(RectTransform viewport, RectTransform rect) {
            rect.GetWorldCorners(_corners);
            var toViewport = viewport.worldToLocalMatrix;
            Vector2 min = toViewport.MultiplyPoint3x4(_corners[0]);
            var max = min;
            for (int i = 1; i < 4; i++) {
                Vector2 corner = toViewport.MultiplyPoint3x4(_corners[i]);
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
