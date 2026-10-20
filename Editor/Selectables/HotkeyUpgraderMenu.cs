using Onion.UI.Selectables;
using UnityEditor;
using UnityEngine.UI;

namespace Onion.UI.Editor {
    /// <summary>
    /// "Upgrade/Add Hotkey Upgrader" in the context menu of Button and Toggle.
    /// </summary>
    internal static class HotkeyUpgraderMenu {
        private const string UpgradeMenu = "Upgrade/Add Hotkey Upgrader";

        // Runs once per selected object.
        [MenuItem("CONTEXT/Button/" + UpgradeMenu)]
        [MenuItem("CONTEXT/Toggle/" + UpgradeMenu)]
        private static void Add(MenuCommand command) {
            var selectable = (Selectable)command.context;
            if (!selectable.TryGetComponent(out HotkeyUpgrader _)) {
                Undo.AddComponent<HotkeyUpgrader>(selectable.gameObject);
            }
        }

        [MenuItem("CONTEXT/Button/" + UpgradeMenu, true)]
        [MenuItem("CONTEXT/Toggle/" + UpgradeMenu, true)]
        private static bool CanAdd(MenuCommand command) {
            return command.context is Selectable selectable && !selectable.TryGetComponent(out HotkeyUpgrader _);
        }
    }
}
