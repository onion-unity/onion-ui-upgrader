using System;
using Onion.UI.Focus;
using UnityEngine.UI;

namespace Onion.UI.Selectables {
    /// <summary>
    /// An effect that shows each state as one value, such as a color or a scale. Derive from it and implement
    /// the value overload of Apply; picking the value for the state and input mode is done here.
    /// </summary>
    [Serializable]
    public abstract class StateValueEffect<T> : SelectableEffect {
        public StateValues<T> values;

        public sealed override void Apply(Selectable target, object data, SelectableState state, InputMode mode, bool instant) {
            Apply(target, data, values.Get(state, mode), instant);
        }

        /// <summary>
        /// Shows <paramref name="value"/>, the one set for the current state, on the target.
        /// </summary>
        protected abstract void Apply(Selectable target, object data, T value, bool instant);
    }
}
