using System;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// Swaps the sprite of the Selectable's Target Graphic (an Image) with the sprites set here.
    /// An empty slot shows the Image's own sprite.
    /// </summary>
    [Serializable]
    public sealed class SpriteSwapEffect : StateValueEffect<Sprite> {
        protected override void Apply(Selectable target, object data, Sprite value, bool instant) {
            Swap(target.image, value);
        }

        public override void Release(Selectable target, object data) {
            Swap(target.image, null);
        }

        /// <summary>
        /// Swaps like Selectable's Sprite Swap transition.
        /// </summary>
        public static void Swap(Image image, Sprite sprite) {
            if (image != null) {
                image.overrideSprite = sprite;
            }
        }
    }
}
