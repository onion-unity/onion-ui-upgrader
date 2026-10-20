using System;
using Onion.UI.Focus;

namespace Onion.UI.Selectables {
    /// <summary>
    /// One value per Unity transition slot. Focused uses <see cref="highlighted"/> in Pointer mode and
    /// <see cref="selected"/> in Navigation mode.
    /// </summary>
    [Serializable]
    public struct StateValues<T> {
        public T normal;
        public T highlighted;
        public T pressed;
        public T selected;
        public T disabled;

        public StateValues(T normal, T highlighted, T pressed, T selected, T disabled) {
            this.normal = normal;
            this.highlighted = highlighted;
            this.pressed = pressed;
            this.selected = selected;
            this.disabled = disabled;
        }

        public T Get(SelectableState state, InputMode mode) {
            return state switch {
                SelectableState.Focused => mode == InputMode.Pointer ? highlighted : selected,
                SelectableState.Pressed => pressed,
                SelectableState.Disabled => disabled,
                _ => normal,
            };
        }
    }
}
