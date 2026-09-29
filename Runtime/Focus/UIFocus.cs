using System;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Focus {
    /// <summary>
    /// The current input mode and the one Selectable it focuses: the hovered one in Pointer mode, the selection
    /// in Navigation mode. Hovering never changes the EventSystem's selection.
    /// Updated in LateUpdate, after the EventSystem has handled this frame's input. While the Focus upgrade is off,
    /// the mode is always Navigation and the focus is the selection.
    /// </summary>
    public static class UIFocus {
        /// <summary>
        /// The input the UI is being used with.
        /// </summary>
        public static InputMode mode { get; private set; }

        /// <summary>
        /// The focused Selectable, or null when nothing is focused.
        /// </summary>
        public static Selectable focused { get; private set; }

        /// <summary>
        /// Raised when <see cref="mode"/> changes, with the new mode.
        /// </summary>
        public static event Action<InputMode> modeChanged;

        /// <summary>
        /// Raised when <see cref="focused"/> changes, with the previous and the new focus (either may be null).
        /// </summary>
        public static event Action<Selectable, Selectable> focusChanged;

        internal static void Set(InputMode newMode, Selectable newFocused) {
            var previous = focused;
            bool modeChanges = newMode != mode;
            // By reference, so a destroyed focus that became null is still reported.
            bool focusChanges = !ReferenceEquals(newFocused, previous);

            mode = newMode;
            focused = newFocused;

            if (modeChanges) {
                modeChanged?.Invoke(newMode);
            }

            if (focusChanges) {
                focusChanged?.Invoke(previous, newFocused);
            }
        }

        // For Enter Play Mode without domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            mode = InputMode.Navigation;
            focused = null;
            modeChanged = null;
            focusChanged = null;
        }
    }
}
