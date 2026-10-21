using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// A Toggle that switches only after being held for <see cref="holdDuration"/>, by a pointer, Submit, or a
    /// <see cref="HotkeyUpgrader"/>. A shorter press does nothing.
    /// </summary>
    [AddComponentMenu("Onion/UI/Hold Toggle")]
    public class HoldToggle : Toggle {
        [Tooltip("How long it has to be held to switch, in seconds (unscaled time).")]
        [Min(0f)]
        public float holdDuration = 1f;

        [Tooltip("Called with the hold's progress from 0 to 1 while held, 1 when it switches, and 0 when released.")]
        public UnityEvent<float> onHoldProgress = new();

        private readonly HoldPress _hold = new();

        /// <summary>
        /// How far the hold is, from 0 to 1. Stays 1 after switching until released.
        /// </summary>
        public float holdProgress => _hold.progress;

        protected override void OnDisable() {
            _hold.End(onHoldProgress);
            base.OnDisable();
        }

        // After EventSystem.Update, which sends this frame's pointer and Submit events.
        private void LateUpdate() {
            if (_hold.Update(this, holdDuration, onHoldProgress)) {
                isOn = !isOn;
            }
        }

        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            _hold.BeginPointer(this, eventData);
        }

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) {
                _hold.EndPointer(onHoldProgress);
            }
        }

        public override void OnPointerExit(PointerEventData eventData) {
            base.OnPointerExit(eventData);
            _hold.EndPointer(onHoldProgress);
        }

        public override void OnPointerClick(PointerEventData eventData) {
        }

        public override void OnSubmit(BaseEventData eventData) {
            _hold.BeginSubmit(this);
        }
    }
}
