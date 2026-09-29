namespace Onion.UI.Focus {
    /// <summary>
    /// Which input the UI is being used with, which decides what is focused.
    /// </summary>
    public enum InputMode {
        /// <summary>
        /// Keyboard or gamepad: the selection is focused.
        /// </summary>
        Navigation,

        /// <summary>
        /// Mouse, pen or touch: the hovered Selectable is focused.
        /// </summary>
        Pointer,
    }
}
