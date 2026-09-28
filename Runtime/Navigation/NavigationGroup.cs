using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// The axes on which navigation inside a <see cref="NavigationGroup"/> wraps around.
    /// </summary>
    public enum NavigationWrap {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = Horizontal | Vertical,
    }

    /// <summary>
    /// Limits upgraded navigation to the Selectables under this object. From outside, its members are
    /// candidates as usual; picking one enters the group, which selects the last selection when
    /// <see cref="rememberSelection"/> is on, else <see cref="defaultSelectable"/> if set, otherwise the picked member.
    /// </summary>
    [AddComponentMenu("Onion/UI/Navigation Group")]
    // Registers in Edit Mode too, so the Scene view visualizer can find the groups.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class NavigationGroup : MonoBehaviour {
        private static readonly List<NavigationGroup> _activeGroups = new();
        private static readonly List<NavigationGroup> _pendingRestores = new();

        [Tooltip("Selected when navigation enters this group.\nWhen empty, the member picked by the move is selected.")]
        public Selectable defaultSelectable;

        [Tooltip("Entering this group selects the member that was selected last, before Default Selectable.")]
        public bool rememberSelection;

        [Tooltip("Selects the entry of this group (the last selection when remembered, otherwise Default Selectable) when the group is enabled in Play Mode.")]
        public bool selectOnEnable;

        [Tooltip("When this group is disabled in Play Mode while the selection is in it, selects what was selected just before the group was enabled.\nUseful for popups opened over a menu.")]
        public bool restoreOnDisable;

        [Tooltip("Contain = navigation stops at the group's edge.\nPass Through = when nothing is found inside, the search continues outside the group.")]
        public NavigationBoundary boundary = NavigationBoundary.Contain;

        [Tooltip("Axes on which navigation inside this group wraps around to the other side, in addition to each Selectable's own Wrap Around.")]
        public NavigationWrap wrapAround = NavigationWrap.None;

        [Tooltip("Profile used for navigation inside this group.\nWhen empty, the parent group's profile is used, and at the root the project-wide one.")]
        public NavigationProfile profile;

        internal static List<NavigationGroup> activeGroups => _activeGroups;

        /// <summary>
        /// Groups disabled with <see cref="restoreOnDisable"/>, in disable order; consumed by the upgrader on its
        /// next update.
        /// </summary>
        internal static List<NavigationGroup> pendingRestores => _pendingRestores;

        /// <summary>
        /// The Selectable under this group that was selected last (recorded by the upgrader).
        /// </summary>
        internal Selectable lastSelected { get; set; }

        /// <summary>
        /// Set on enable when <see cref="selectOnEnable"/> is on; consumed by the upgrader on its next update.
        /// </summary>
        internal bool selectPending { get; set; }

        /// <summary>
        /// The selection outside this group when it was last enabled in Play Mode, selected again by
        /// <see cref="restoreOnDisable"/>.
        /// </summary>
        internal Selectable restoreTarget { get; private set; }

        /// <summary>
        /// The group this one belongs to, or null when it is at the root.
        /// </summary>
        internal NavigationGroup parent => ScopeOf(transform.parent);

        // Groups disabled while exiting Play Mode would otherwise stay queued into the next Play session
        // when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPendingRestores() {
            _pendingRestores.Clear();
        }

        private void OnEnable() {
            _activeGroups.Add(this);
            // Reopened before the upgrader restored: nothing to restore.
            _pendingRestores.Remove(this);
            selectPending = selectOnEnable && Application.isPlaying;

            // Select On Enable acts on the next update, so this is still what was selected before opening.
            restoreTarget = null;
            if (Application.isPlaying) {
                var selected = CurrentSelection();
                if (selected != null && selected.TryGetComponent(out Selectable selectable) && !IsUnder(selected)) {
                    restoreTarget = selectable;
                }
            }
        }

        private void OnDisable() {
            _activeGroups.Remove(this);

            // Whether the selection is in this group is checked on restore, not now: a stacked popup disabled
            // later in the same frame may still hold it.
            if (restoreOnDisable && Application.isPlaying && restoreTarget != null) {
                _pendingRestores.Add(this);
            }
        }

        /// <summary>
        /// Whether the selection is under this group or lost, i.e. closing the group leaves nothing selected.
        /// </summary>
        internal bool HoldsSelection() {
            var selected = CurrentSelection();
            return SelectionRecovery.IsLost(selected) || IsUnder(selected);
        }

        private bool IsUnder(GameObject selected) {
            return selected.transform.IsChildOf(transform);
        }

        private static GameObject CurrentSelection() {
            var eventSystem = EventSystem.current;
            return eventSystem != null ? eventSystem.currentSelectedGameObject : null;
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
