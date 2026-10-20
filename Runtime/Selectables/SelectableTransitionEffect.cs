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

        // The Selectable's settings are read on every apply, so script changes to them still show.
        public override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            switch (((Data)data).transition) {
                case Selectable.Transition.ColorTint:
                    var colors = target.colors;
                    var color = new StateValues<Color>(colors.normalColor, colors.highlightedColor, colors.pressedColor, colors.selectedColor, colors.disabledColor).Get(state, mode);
                    ColorTintEffect.Tint(target.targetGraphic, color * colors.colorMultiplier, colors.fadeDuration, instant);
                    break;
                case Selectable.Transition.SpriteSwap:
                    var sprites = target.spriteState;
                    // Normal shows the Image's own sprite, like Unity.
                    SpriteSwapEffect.Swap(target.image, new StateValues<Sprite>(null, sprites.highlightedSprite, sprites.pressedSprite, sprites.selectedSprite, sprites.disabledSprite).Get(state, mode));
                    break;
                case Selectable.Transition.Animation:
                    var t = target.animationTriggers;
                    var triggers = new StateValues<string>(t.normalTrigger, t.highlightedTrigger, t.pressedTrigger, t.selectedTrigger, t.disabledTrigger);
                    AnimationEffect.Trigger(target.animator, triggers, triggers.Get(state, mode));
                    break;
            }
        }

        public override void Release(Selectable target, object data) {
            // The setter applies Unity's current state.
            target.transition = ((Data)data).transition;
        }
    }
}
