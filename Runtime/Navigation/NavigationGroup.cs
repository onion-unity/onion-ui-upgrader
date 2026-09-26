using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Navigation {
    /// <summary>
    /// What happens when navigation reaches the edge of a <see cref="NavigationGroup"/>.
    /// </summary>
    public enum NavigationBoundary {
        /// <summary>Navigation stops at the group's edge.</summary>
        Contain,
        /// <summary>When nothing is found inside, the search continues outside the group.</summary>
        PassThrough,
    }

    /// <summary>
    /// Limits upgraded navigation to the Selectables under this object. From outside, the group is
    /// considered as one rect; entering it selects <see cref="defaultSelectable"/> or the member
    /// closest in the move direction.
    /// </summary>
    [AddComponentMenu("Onion/UI/Navigation Group")]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class NavigationGroup : MonoBehaviour {
        private static readonly List<NavigationGroup> _activeGroups = new();

        [Tooltip("Selected when navigation enters this group.\nWhen empty, the member closest in the move direction is selected.")]
        public Selectable defaultSelectable;

        [Tooltip("Contain = navigation stops at the group's edge.\nPass Through = when nothing is found inside, the search continues outside the group.")]
        public NavigationBoundary boundary = NavigationBoundary.Contain;

        internal static List<NavigationGroup> activeGroups => _activeGroups;

        /// <summary>
        /// The group this one belongs to, or null when it is at the root.
        /// </summary>
        internal NavigationGroup parent => ScopeOf(transform.parent);

        private void OnEnable() {
            _activeGroups.Add(this);
        }

        private void OnDisable() {
            _activeGroups.Remove(this);
        }

        /// <summary>
        /// The nearest enabled group on this transform or its ancestors, or null (the root).
        /// </summary>
        internal static NavigationGroup ScopeOf(Transform transform) {
            for (; transform != null; transform = transform.parent) {
                if (transform.TryGetComponent(out NavigationGroup group) && group.isActiveAndEnabled) {
                    return group;
                }
            }

            return null;
        }
    }
}
