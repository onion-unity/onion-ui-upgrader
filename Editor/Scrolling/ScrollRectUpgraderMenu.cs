using Onion.UI.Scrolling;
using UnityEditor;
using UnityEngine.UI;

namespace Onion.UI.Editor {
    /// <summary>
    /// "Upgrade/Add ScrollRect Upgrader" in the context menu of ScrollRect.
    /// </summary>
    internal static class ScrollRectUpgraderMenu {
        private const string UpgradeMenu = "CONTEXT/ScrollRect/Upgrade/Add ScrollRect Upgrader";

        // Runs once per selected object.
        [MenuItem(UpgradeMenu)]
        private static void Add(MenuCommand command) {
            var scrollRect = (ScrollRect)command.context;
            if (!scrollRect.TryGetComponent(out ScrollRectUpgrader _)) {
                Undo.AddComponent<ScrollRectUpgrader>(scrollRect.gameObject);
            }
        }

        [MenuItem(UpgradeMenu, true)]
        private static bool CanAdd(MenuCommand command) {
            return command.context is ScrollRect scrollRect && !scrollRect.TryGetComponent(out ScrollRectUpgrader _);
        }
    }
}
