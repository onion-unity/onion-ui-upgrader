using System.Collections.Generic;
using Onion.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Focus {
    /// <summary>
    /// Tracks the input mode and publishes the focus through <see cref="UIFocus"/>.
    /// Owned and driven by <see cref="NavigationUpgrader"/>: Update before EventSystem.Update handles this frame's
    /// input, LateUpdate after it.
    /// </summary>
    internal sealed class InputModeTracker {
        private readonly List<RaycastResult> _raycasts = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        private InputMode _mode;
        private bool _wasMoving;
        private bool _hasPointer;
        private Vector2 _pointerPosition;
        // The EventSystem whose navigation events are off for this frame to hold back a submit.
        private EventSystem _suppressed;

        /// <summary>
        /// Switches the mode on this frame's input. Returns whether this frame's move on the selection must be
        /// held back (Reveal On First Input); the caller does that through the selection's navigation.
        /// </summary>
        internal bool Update(EventSystem eventSystem) {
            if (eventSystem == null) {
                return false;
            }

            bool moving = UIInput.IsMoving(eventSystem);
            bool moveStarted = moving && !_wasMoving;
            _wasMoving = moving;

            bool hasPointer = UIInput.TryGetPointerPosition(eventSystem, out var position);
            // A layout moving under a still cursor isn't pointer input.
            bool pointerMoved = hasPointer && _hasPointer && position != _pointerPosition;
            _hasPointer = hasPointer;
            _pointerPosition = position;

            var settings = FocusSettings.current;
            if (settings == null) {
                _mode = InputMode.Navigation;
                return false;
            }

            bool submitted = UIInput.WasSubmitPressed(eventSystem);
            if (moveStarted || submitted || (settings.cancelSwitchesMode && UIInput.WasCancelPressed(eventSystem))) {
                if (_mode == InputMode.Pointer) {
                    _mode = InputMode.Navigation;
                    return ContinueFromFocus(eventSystem, settings, submitted);
                }
            } else if (pointerMoved || UIInput.WasPointerPressed(eventSystem)) {
                _mode = InputMode.Pointer;
            }

            return false;
        }

        // Input continues from what was on screen: the hovered Selectable becomes the selection, else the unseen
        // selection is only revealed. A lost selection is left to Selection Recovery.
        private bool ContinueFromFocus(EventSystem eventSystem, FocusSettings settings, bool submitted) {
            var selected = eventSystem.currentSelectedGameObject;
            // Still the hover published in the last LateUpdate.
            var shown = UIFocus.focused;
            if (shown != null && shown.gameObject == selected) {
                return false;
            }

            if (settings.continueFromHover && NavigationUpgrader.IsUsable(shown)) {
                eventSystem.SetSelectedGameObject(shown.gameObject);
                return false;
            }

            if (!settings.revealOnFirstInput || SelectionRecovery.IsLost(selected)) {
                return false;
            }

            // Turning navigation events off would also skip the module's move repeat timing, so a held move
            // would move on the next frame. Only a submit is held back this way; moves go through navigation.
            if (submitted) {
                eventSystem.sendNavigationEvents = false;
                _suppressed = eventSystem;
            }

            return true;
        }

        internal void LateUpdate(EventSystem eventSystem) {
            if (_suppressed != null) {
                _suppressed.sendNavigationEvents = true;
                _suppressed = null;
            }

            Selectable focused = null;
            if (eventSystem != null) {
                focused = _mode == InputMode.Pointer ? FindHovered(eventSystem) : SelectionOf(eventSystem);
            }

            UIFocus.Set(_mode, focused);
        }

        private static Selectable SelectionOf(EventSystem eventSystem) {
            var selected = eventSystem.currentSelectedGameObject;
            return !SelectionRecovery.IsLost(selected) && selected.TryGetComponent(out Selectable selectable) ? selectable : null;
        }

        // The Selectable under the pointer's topmost hit, when a click would select it
        // (interactable and not None, like Selectable.OnPointerDown).
        private Selectable FindHovered(EventSystem eventSystem) {
            if (!UIInput.TryGetPointerPosition(eventSystem, out var position)) {
                return null;
            }

            if (_pointerEventSystem != eventSystem) {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = position;
            eventSystem.RaycastAll(_pointerData, _raycasts);

            Selectable hovered = null;
            foreach (var result in _raycasts) {
                if (result.gameObject != null) {
                    hovered = result.gameObject.GetComponentInParent<Selectable>();
                    break;
                }
            }

            return NavigationUpgrader.IsUsable(hovered) ? hovered : null;
        }
    }
}
