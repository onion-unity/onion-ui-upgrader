using System;
using Onion.UI.Focus;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// Applies the Selectable's own Transition (Color Tint, Sprite Swap or Animation, as set in its inspector) in
    /// the upgraded states. While applied, the Selectable's transition is None so Unity doesn't apply it as well;
    /// it is restored on release.
    /// </summary>
    [Serializable]
    public sealed class SelectableTransitionEffect : SelectableEffect {
        private sealed class Data {
            internal Selectable.Transition transition;
        }

        public override object CreateData(Selectable target) {
            var data = new Data { transition = target.transition };
            target.transition = Selectable.Transition.None;
            return data;
        }

        public override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            var slot = Transitions.SlotOf(state, mode);
            switch (((Data)data).transition) {
                case Selectable.Transition.ColorTint:
                    Transitions.Tint(target.targetGraphic, target.colors, slot, instant);
                    break;
                case Selectable.Transition.SpriteSwap:
                    Transitions.Swap(target.image, target.spriteState, slot);
                    break;
                case Selectable.Transition.Animation:
                    Transitions.Trigger(target.animator, target.animationTriggers, slot);
                    break;
            }
        }

        public override void Release(Selectable target, object data) {
            // The setter applies Unity's current state.
            target.transition = ((Data)data).transition;
        }
    }

    /// <summary>
    /// Tints the Selectable's Target Graphic with the colors set here.
    /// </summary>
    [Serializable]
    public sealed class ColorTintEffect : SelectableEffect {
        public ColorBlock colors = ColorBlock.defaultColorBlock;

        public override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            Transitions.Tint(target.targetGraphic, colors, Transitions.SlotOf(state, mode), instant);
        }

        public override void Release(Selectable target, object data) {
            if (target.targetGraphic != null) {
                target.targetGraphic.CrossFadeColor(Color.white, 0f, true, true);
            }
        }
    }

    /// <summary>
    /// Swaps the sprite of the Selectable's Target Graphic (an Image) with the sprites set here.
    /// </summary>
    [Serializable]
    public sealed class SpriteSwapEffect : SelectableEffect {
        public SpriteState spriteState;

        public override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            Transitions.Swap(target.image, spriteState, Transitions.SlotOf(state, mode));
        }

        public override void Release(Selectable target, object data) {
            if (target.image != null) {
                target.image.overrideSprite = null;
            }
        }
    }

    /// <summary>
    /// Sets the triggers set here on the Selectable's Animator.
    /// </summary>
    [Serializable]
    public sealed class AnimationEffect : SelectableEffect {
        public AnimationTriggers animationTriggers = new AnimationTriggers();

        public override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            Transitions.Trigger(target.animator, animationTriggers, Transitions.SlotOf(state, mode));
        }
    }

    /// <summary>
    /// Unity's transition slots, and applying them the way Selectable.DoStateTransition does.
    /// </summary>
    internal static class Transitions {
        internal enum Slot {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled,
        }

        // Focused looks hovered in Pointer mode and selected in Navigation mode.
        internal static Slot SlotOf(SelectableState state, InputMode mode) {
            return state switch {
                SelectableState.Focused => mode == InputMode.Pointer ? Slot.Highlighted : Slot.Selected,
                SelectableState.Pressed => Slot.Pressed,
                SelectableState.Disabled => Slot.Disabled,
                _ => Slot.Normal,
            };
        }

        internal static void Tint(Graphic graphic, ColorBlock colors, Slot slot, bool instant) {
            if (graphic == null) {
                return;
            }

            var color = slot switch {
                Slot.Highlighted => colors.highlightedColor,
                Slot.Pressed => colors.pressedColor,
                Slot.Selected => colors.selectedColor,
                Slot.Disabled => colors.disabledColor,
                _ => colors.normalColor,
            };
            graphic.CrossFadeColor(color * colors.colorMultiplier, instant ? 0f : colors.fadeDuration, true, true);
        }

        internal static void Swap(Image image, SpriteState sprites, Slot slot) {
            if (image == null) {
                return;
            }

            image.overrideSprite = slot switch {
                Slot.Highlighted => sprites.highlightedSprite,
                Slot.Pressed => sprites.pressedSprite,
                Slot.Selected => sprites.selectedSprite,
                Slot.Disabled => sprites.disabledSprite,
                _ => null,
            };
        }

        internal static void Trigger(Animator animator, AnimationTriggers triggers, Slot slot) {
            string trigger = slot switch {
                Slot.Highlighted => triggers.highlightedTrigger,
                Slot.Pressed => triggers.pressedTrigger,
                Slot.Selected => triggers.selectedTrigger,
                Slot.Disabled => triggers.disabledTrigger,
                _ => triggers.normalTrigger,
            };

            if (animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables || string.IsNullOrEmpty(trigger)) {
                return;
            }

            animator.ResetTrigger(triggers.normalTrigger);
            animator.ResetTrigger(triggers.highlightedTrigger);
            animator.ResetTrigger(triggers.pressedTrigger);
            animator.ResetTrigger(triggers.selectedTrigger);
            animator.ResetTrigger(triggers.disabledTrigger);
            animator.SetTrigger(trigger);
        }
    }
}
