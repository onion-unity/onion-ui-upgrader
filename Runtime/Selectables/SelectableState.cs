namespace Onion.UI.Selectables {
    /// <summary>
    /// The visible state of a Selectable with a <see cref="SelectableUpgrader"/>.
    /// </summary>
    public enum SelectableState {
        Normal,

        /// <summary>
        /// The one focused Selectable (<see cref="Focus.UIFocus.focused"/>): hovered in Pointer mode, selected in
        /// Navigation mode.
        /// </summary>
        Focused,

        /// <summary>
        /// Held down by the pointer while focused, or by Submit while selected.
        /// </summary>
        Pressed,

        /// <summary>
        /// Not interactable, including through a CanvasGroup.
        /// </summary>
        Disabled,
    }
}
