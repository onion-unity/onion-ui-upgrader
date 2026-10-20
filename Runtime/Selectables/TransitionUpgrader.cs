using System.Collections.Generic;
using Onion.UI.Focus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// Shows the Selectable in one of four states (Normal / Focused / Pressed / Disabled) through a
    /// <see cref="SelectableStyle"/>, so only the one focused Selectable (<see cref="UIFocus.focused"/>) is
    /// highlighted. Does nothing while the Focus upgrade is off, which leaves the Selectable to Unity.
    /// </summary>
    [AddComponentMenu("Onion/UI/Transition Upgrader")]
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
    public sealed class TransitionUpgrader : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISubmitHandler {
        [Tooltip("How this Selectable looks in each state.\nWhen empty, the project-wide style in the Focus settings is used.")]
        public SelectableStyle style;

        private Selectable _selectable;
        // The effects being applied and each one's data, taken from the style when applying started.
        private readonly List<SelectableEffect> _effects = new();
        private readonly List<object> _data = new();
        private SelectableStyle _appliedStyle;
        private bool _active;
        private bool _hasState;
        private InputMode _mode;

        private bool _pointerDown;
        private bool _submitDown;
        private int _submitFrame = -1;

        /// <summary>
        /// The current state. Normal while the Focus upgrade is off.
        /// </summary>
        public SelectableState state { get; private set; }

        private void Awake() {
            _selectable = GetComponent<Selectable>();
        }

        private void OnDisable() {
            Deactivate();
            _pointerDown = false;
            _submitDown = false;
        }

        // After NavigationUpgrader.LateUpdate (order -10000), which publishes this frame's focus.
        private void LateUpdate() {
            var settings = FocusSettings.current;
            if (settings == null || !_selectable.enabled) {
                Deactivate();
                return;
            }

            var currentStyle = style != null ? style : settings.projectWideStyle;
            if (!_active || currentStyle != _appliedStyle) {
                Deactivate();
                Activate(currentStyle);
            }

            var newState = FindState();
            var mode = UIFocus.mode;
            bool modeMatters = newState == SelectableState.Focused || newState == SelectableState.Pressed;
            if (_hasState && newState == state && (mode == _mode || !modeMatters)) {
                return;
            }

            bool instant = !_hasState;
            _hasState = true;
            state = newState;
            _mode = mode;

            for (int i = 0; i < _effects.Count; i++) {
                _effects[i].Apply(_selectable, _data[i], state, mode, instant);
            }
        }

        private SelectableState FindState() {
            if (!_selectable.IsInteractable()) {
                return SelectableState.Disabled;
            }

            var eventSystem = EventSystem.current;
            bool selected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
            // Pressed from the submit frame on, while the module's submit input is held and this stays selected.
            // Custom modules can't be read, so there it is only the submit frame.
            _submitDown = _submitDown && selected && (Time.frameCount == _submitFrame || UIInput.IsSubmitHeld(eventSystem));

            bool focused = UIFocus.focused == _selectable;
            // Like Unity, a pointer press shows only while the pointer is over this Selectable.
            if (_submitDown || (_pointerDown && focused)) {
                return SelectableState.Pressed;
            }

            return focused ? SelectableState.Focused : SelectableState.Normal;
        }

        private void Activate(SelectableStyle newStyle) {
            _active = true;
            _appliedStyle = newStyle;
            if (newStyle == null) {
                return;
            }

            // Copied, so editing the style's list while applied can't mismatch the data.
            foreach (var effect in newStyle.effects) {
                if (effect != null) {
                    _effects.Add(effect);
                    _data.Add(effect.CreateData(_selectable));
                }
            }
        }

        private void Deactivate() {
            if (!_active) {
                return;
            }

            // In reverse, so effects that change the same Selectable property restore it in order.
            for (int i = _effects.Count - 1; i >= 0; i--) {
                _effects[i].Release(_selectable, _data[i]);
            }

            _effects.Clear();
            _data.Clear();
            _appliedStyle = null;
            _active = false;
            _hasState = false;
            state = SelectableState.Normal;
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left) {
                _pointerDown = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left) {
                _pointerDown = false;
            }
        }

        public void OnSubmit(BaseEventData eventData) {
            _submitDown = true;
            _submitFrame = Time.frameCount;
        }
    }
}
