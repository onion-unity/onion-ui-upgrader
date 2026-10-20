using System;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// Tints the Selectable's Target Graphic with the colors set here.
    /// </summary>
    [Serializable]
    public sealed class ColorTintEffect : StateValueEffect<Color> {
        [Range(1f, 5f)]
        public float colorMultiplier = 1f;

        public float fadeDuration = 0.1f;

        public ColorTintEffect() {
            var colors = ColorBlock.defaultColorBlock;
            values = new StateValues<Color>(colors.normalColor, colors.highlightedColor, colors.pressedColor, colors.selectedColor, colors.disabledColor);
        }

        protected override void Apply(Selectable target, object data, Color value, bool instant) {
            Tint(target.targetGraphic, value * colorMultiplier, fadeDuration, instant);
        }

        public override void Release(Selectable target, object data) {
            Tint(target.targetGraphic, Color.white, 0f, true);
        }

        /// <summary>
        /// Tints like Selectable's Color Tint transition.
        /// </summary>
        public static void Tint(Graphic graphic, Color color, float fadeDuration, bool instant) {
            if (graphic != null) {
                graphic.CrossFadeColor(color, instant ? 0f : fadeDuration, true, true);
            }
        }
    }
}
