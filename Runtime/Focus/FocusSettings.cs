using System;
using UnityEngine;

namespace Onion.UI.Focus {
    /// <summary>
    /// The focus section of <see cref="UIUpgraderSettings"/>.
    /// </summary>
    [Serializable]
    internal sealed class FocusSettings : UpgradeSettings {
        [Tooltip("When switching to keyboard/gamepad, the hovered Selectable becomes the selection before the input is handled, so input continues from what was highlighted.")]
        [SerializeField]
        internal bool continueFromHover = true;

        [Tooltip("When switching to keyboard/gamepad and the selection wasn't highlighted, the first move or submit only shows the selection.")]
        [SerializeField]
        internal bool revealOnFirstInput = true;

        [Tooltip("Whether Cancel also switches to keyboard/gamepad. Off: only Move and Submit do.")]
        [SerializeField]
        internal bool cancelSwitchesMode;

        /// <summary>
        /// The settings while the upgrade is on, else null.
        /// </summary>
        internal static FocusSettings current {
            get {
                var settings = UIUpgraderSettings.instance;
                return settings != null && settings.focus.enabled ? settings.focus : null;
            }
        }
    }
}
