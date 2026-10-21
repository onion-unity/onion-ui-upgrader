using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// The hold shared by <see cref="HoldButton"/> and <see cref="HoldToggle"/>: started by a left pointer press or a
    /// Submit, it completes once held for the duration. A pointer holds while it is down over the Selectable; a Submit
    /// holds while the module's submit input is held and the Selectable stays selected, or while a
    /// <see cref="HotkeyUpgrader"/> on it is held.
    /// </summary>
    internal sealed class HoldPress {
        private bool _pointer;
        private bool _submit;
        private int _submitFrame = -1;
        private float _startTime;
        private bool _completed;

        /// <summary>
        /// How far the hold is, from 0 to 1. Stays 1 after completing until released.
        /// </summary>
        internal float progress { get; private set; }

        private bool isHolding => _pointer || _submit;

        internal void BeginPointer(Selectable selectable, PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left && CanBegin(selectable)) {
                _pointer = true;
                _startTime = Time.unscaledTime;
            }
        }

        // On pointer up, and on exit, since dragging out cancels like Unity's click.
        internal void EndPointer(UnityEvent<float> onProgress) {
            if (_pointer) {
                End(onProgress);
            }
        }

        internal void BeginSubmit(Selectable selectable) {
            if (CanBegin(selectable)) {
                _submit = true;
                _submitFrame = Time.frameCount;
                _startTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Updates the hold, after EventSystem.Update. True on the frame it completes.
        /// </summary>
        internal bool Update(Selectable selectable, float duration, UnityEvent<float> onProgress) {
            if (!isHolding) {
                return false;
            }

            if (!selectable.IsActive() || !selectable.IsInteractable() || (_submit && !IsSubmitHeld(selectable))) {
                End(onProgress);
                return false;
            }

            if (_completed) {
                return false;
            }

            float elapsed = Time.unscaledTime - _startTime;
            SetProgress(duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f, onProgress);
            _completed = progress >= 1f;
            return _completed;
        }

        internal void End(UnityEvent<float> onProgress) {
            _pointer = false;
            _submit = false;
            _completed = false;
            SetProgress(0f, onProgress);
        }

        private bool CanBegin(Selectable selectable) {
            return !isHolding && selectable.IsActive() && selectable.IsInteractable();
        }

        // Custom modules can't be read, so there a Submit only holds on its own frame and never completes.
        private bool IsSubmitHeld(Selectable selectable) {
            if (Time.frameCount == _submitFrame || HotkeyUpgrader.IsHeld(selectable)) {
                return true;
            }

            var eventSystem = EventSystem.current;
            return eventSystem != null
                && eventSystem.currentSelectedGameObject == selectable.gameObject
                && UIInput.IsSubmitHeld(eventSystem);
        }

        private void SetProgress(float value, UnityEvent<float> onProgress) {
            if (value != progress) {
                progress = value;
                onProgress.Invoke(value);
            }
        }
    }
}
