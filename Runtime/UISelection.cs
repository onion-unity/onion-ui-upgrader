using UnityEngine;
using UnityEngine.UI;
using UINavigation = UnityEngine.UI.Navigation;

namespace Onion.UI {
    /// <summary>
    /// Checks on the EventSystem selection and the Selectables that can take it, shared by every upgrade.
    /// </summary>
    internal static class UISelection {
        /// <summary>
        /// Whether nothing usable is selected: null, inactive, or a disabled Selectable. Input modules don't send
        /// events to inactive objects or disabled components, so these can't move either.
        /// </summary>
        internal static bool IsLost(GameObject selected) {
            if (selected == null || !selected.activeInHierarchy) {
                return true;
            }

            return selected.TryGetComponent(out Selectable selectable) && !selectable.isActiveAndEnabled;
        }

        /// <summary>
        /// Whether the Selectable can be selected now: active, enabled, and a <see cref="IsCandidate"/>.
        /// </summary>
        internal static bool IsUsable(Selectable selectable) {
            return selectable != null && selectable.isActiveAndEnabled && IsCandidate(selectable);
        }

        /// <summary>
        /// Whether navigation can move to the Selectable, apart from being active: interactable, navigation not
        /// None, and a RectTransform.
        /// </summary>
        internal static bool IsCandidate(Selectable selectable) {
            return selectable.IsInteractable()
                && selectable.navigation.mode != UINavigation.Mode.None
                && selectable.transform is RectTransform;
        }
    }
}
