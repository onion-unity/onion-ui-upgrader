using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UINavigation = UnityEngine.UI.Navigation;

namespace Onion.UI.Navigation {
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class NavigationUpgrader : MonoBehaviour {
        private const string _instanceName = "[Navigation Upgrader]";

        private static readonly Vector3[] _corners = new Vector3[4];
        private static Selectable[] _candidates = new Selectable[64];

        private Selectable _target;
        private UINavigation _originalNavigation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            var go = new GameObject(_instanceName);
            DontDestroyOnLoad(go);
            
            go.AddComponent<NavigationUpgrader>();
        }

        private void LateUpdate() {
            // Upgrade turned off (or no profile): leave every Selectable to Unity.
            var profile = NavigationSettings.profile;
            if (profile == null) {
                Release();
                return;
            }

            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var selectable = selected != null ? selected.GetComponent<Selectable>() : null;

            if (selectable != _target) {
                Release();

                if (selectable != null && IsUpgradable(selectable)) {
                    _target = selectable;
                    _originalNavigation = selectable.navigation;
                }
            }

            if (_target != null) {
                _target.navigation = Resolve(_target, _originalNavigation, profile);
            }
        }

        private void OnDisable() {
            Release();
        }

        private void Release() {
            if (_target != null) {
                _target.navigation = _originalNavigation;
            }

            _target = null;
        }

        /// <summary>
        /// The navigation the upgrader would apply to this Selectable, or false when it isn't upgraded
        /// (upgrade off, Explicit/None mode, or not a RectTransform). Also used by the Scene view visualizer.
        /// </summary>
        internal static bool TryResolve(Selectable selectable, out UINavigation navigation) {
            navigation = selectable.navigation;
            var profile = NavigationSettings.profile;
            if (profile == null || !IsUpgradable(selectable)) {
                return false;
            }

            navigation = Resolve(selectable, navigation, profile);
            return true;
        }

        private static bool IsUpgradable(Selectable selectable) {
            var mode = selectable.navigation.mode;
            bool isAutomatic = mode == UINavigation.Mode.Automatic
                || mode == UINavigation.Mode.Horizontal
                || mode == UINavigation.Mode.Vertical;

            return isAutomatic && selectable.transform is RectTransform;
        }

        private static UINavigation Resolve(Selectable origin, UINavigation original, NavigationProfile profile) {
            var mode = original.mode;
            // Unity only wraps around in Horizontal/Vertical mode.
            bool wrap = original.wrapAround && mode != UINavigation.Mode.Automatic;
            var from = ((RectTransform)origin.transform).rect;
            int count = CollectCandidates();

            var navigation = original;
            navigation.mode = UINavigation.Mode.Explicit;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;

            if ((mode & UINavigation.Mode.Horizontal) != 0 && !ControlsValue(origin, mode, horizontal: true)) {
                navigation.selectOnLeft = FindNeighbor(origin, from, Vector2.left, wrap, profile, count);
                navigation.selectOnRight = FindNeighbor(origin, from, Vector2.right, wrap, profile, count);
            }

            if ((mode & UINavigation.Mode.Vertical) != 0 && !ControlsValue(origin, mode, horizontal: false)) {
                navigation.selectOnUp = FindNeighbor(origin, from, Vector2.up, wrap, profile, count);
                navigation.selectOnDown = FindNeighbor(origin, from, Vector2.down, wrap, profile, count);
            }

            return navigation;
        }

        // In Automatic mode, Slider/Scrollbar use moves along their own axis to change the value
        // (they return null from FindSelectableOnX). Leaving that axis empty keeps this behavior.
        private static bool ControlsValue(Selectable selectable, UINavigation.Mode mode, bool horizontal) {
            if (mode != UINavigation.Mode.Automatic) {
                return false;
            }

            return selectable switch {
                Slider slider => IsHorizontal(slider.direction) == horizontal,
                Scrollbar scrollbar => IsHorizontal(scrollbar.direction) == horizontal,
                _ => false,
            };
        }

        private static bool IsHorizontal(Slider.Direction direction) {
            return direction == Slider.Direction.LeftToRight || direction == Slider.Direction.RightToLeft;
        }

        private static bool IsHorizontal(Scrollbar.Direction direction) {
            return direction == Scrollbar.Direction.LeftToRight || direction == Scrollbar.Direction.RightToLeft;
        }

        private static int CollectCandidates() {
            int selectableCount = Selectable.allSelectableCount;
            if (_candidates.Length < selectableCount) {
                _candidates = new Selectable[Mathf.NextPowerOfTwo(selectableCount)];
            }

            return Selectable.AllSelectablesNoAlloc(_candidates);
        }

        private static Selectable FindNeighbor(Selectable origin, Rect from, Vector2 direction, bool wrap, NavigationProfile profile, int count) {
            var scope = NavigationGroup.ScopeOf(origin.transform);
            int index = Search(origin, scope, null, new NeighborSearch(from, direction, wrap, profile), count);

            // Nothing inside a Pass Through group: continue in its parent scope, where the group itself
            // is a candidate too and must be skipped.
            while (index < 0 && scope != null && scope.boundary == NavigationBoundary.PassThrough) {
                var leaving = scope;
                scope = leaving.parent;
                index = Search(origin, scope, leaving, new NeighborSearch(from, direction, wrap, profile), count);
            }

            return Pick(origin, from, direction, profile, index, count);
        }

        // Considers the Selectables directly in the scope and its direct child groups, each group as one rect.
        // Indices below count are Selectables, the rest are groups.
        private static int Search(Selectable origin, NavigationGroup scope, NavigationGroup excluded, NeighborSearch search, int count) {
            for (int i = 0; i < count; i++) {
                var candidate = _candidates[i];
                if (candidate == origin || !IsCandidate(candidate)) {
                    continue;
                }

                if (NavigationGroup.ScopeOf(candidate.transform) != scope) {
                    continue;
                }

                search.Consider(i, GetLocalRect(origin.transform, (RectTransform)candidate.transform));
            }

            var groups = NavigationGroup.activeGroups;
            for (int i = 0; i < groups.Count; i++) {
                var group = groups[i];
                if (group == excluded || group.parent != scope) {
                    continue;
                }

                search.Consider(count + i, GetLocalRect(origin.transform, (RectTransform)group.transform));
            }

            return search.result;
        }

        private static Selectable Pick(Selectable origin, Rect from, Vector2 direction, NavigationProfile profile, int index, int count) {
            if (index < 0) {
                return null;
            }

            if (index < count) {
                return _candidates[index];
            }

            return Enter(origin, from, direction, profile, NavigationGroup.activeGroups[index - count], count);
        }

        // The group's default if usable, otherwise its member closest in the move direction. Any angle is
        // accepted there, since only the group itself had to be within the tolerance.
        private static Selectable Enter(Selectable origin, Rect from, Vector2 direction, NavigationProfile profile, NavigationGroup group, int count) {
            var selectable = group.defaultSelectable;
            if (selectable != null && selectable.isActiveAndEnabled && IsCandidate(selectable)) {
                return selectable;
            }

            var search = new NeighborSearch(from, direction, false, 90f, profile.alignmentPower);
            int index = Search(origin, group, null, search, count);
            return Pick(origin, from, direction, profile, index, count);
        }

        private static bool IsCandidate(Selectable selectable) {
            return selectable.IsInteractable()
                && selectable.navigation.mode != UINavigation.Mode.None
                && selectable.transform is RectTransform;
        }

        private static Rect GetLocalRect(Transform space, RectTransform rectTransform) {
            rectTransform.GetWorldCorners(_corners);

            Vector2 min = space.InverseTransformPoint(_corners[0]);
            Vector2 max = min;
            for (int i = 1; i < _corners.Length; i++) {
                Vector2 point = space.InverseTransformPoint(_corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
