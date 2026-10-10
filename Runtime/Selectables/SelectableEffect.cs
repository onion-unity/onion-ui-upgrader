using System;
using Onion.UI.Focus;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// One effect of a <see cref="SelectableStyle"/>, applied to each Selectable using the style when its state
    /// changes. Derive from it to add your own; it then shows up in the style's + menu.
    /// A style is shared by many Selectables, so keep per-Selectable runtime state in the object returned by
    /// <see cref="CreateData"/>, not in fields. Fields are the effect's settings.
    /// </summary>
    [Serializable]
    public abstract class SelectableEffect {
        /// <summary>
        /// Called when a Selectable starts using this effect. The result is passed back to
        /// <see cref="Apply"/> and <see cref="Release"/> for that Selectable.
        /// </summary>
        public virtual object CreateData(Selectable target) {
            return null;
        }

        /// <summary>
        /// Shows <paramref name="state"/> on the target. Called when the state changes, when the mode changes while
        /// Focused or Pressed, and once right after <see cref="CreateData"/> with <paramref name="instant"/> set.
        /// </summary>
        public abstract void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant);

        /// <summary>
        /// Called when the Selectable stops using this effect (the upgrader is disabled, the style is changed, or
        /// the Focus upgrade is turned off), so Unity's default behavior can take over again.
        /// </summary>
        public virtual void Release(Selectable target, object data) {
        }
    }
}
