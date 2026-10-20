using System;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// Sets the triggers named here on the Selectable's Animator.
    /// </summary>
    [Serializable]
    public sealed class AnimationEffect : StateValueEffect<string> {
        public AnimationEffect() {
            var triggers = new AnimationTriggers();
            values = new StateValues<string>(triggers.normalTrigger, triggers.highlightedTrigger, triggers.pressedTrigger, triggers.selectedTrigger, triggers.disabledTrigger);
        }

        protected override void Apply(Selectable target, object data, string value, bool instant) {
            Trigger(target.animator, values, value);
        }

        /// <summary>
        /// Resets every trigger in <paramref name="triggers"/>, then sets <paramref name="trigger"/>, like
        /// Selectable's Animation transition.
        /// </summary>
        public static void Trigger(Animator animator, StateValues<string> triggers, string trigger) {
            if (animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables || string.IsNullOrEmpty(trigger)) {
                return;
            }

            animator.ResetTrigger(triggers.normal);
            animator.ResetTrigger(triggers.highlighted);
            animator.ResetTrigger(triggers.pressed);
            animator.ResetTrigger(triggers.selected);
            animator.ResetTrigger(triggers.disabled);
            animator.SetTrigger(trigger);
        }
    }
}
