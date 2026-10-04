using System.Collections.Generic;
using Onion.UI.Focus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UINavigation = UnityEngine.UI.Navigation;

namespace Onion.UI.Navigation {
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    // Runs before EventSystem.Update (default order 0), which performs the move, so the move uses this frame's result.
    [DefaultExecutionOrder(-10000)]
    internal sealed class NavigationUpgrader : MonoBehaviour {
        private const string _instanceName = "[Navigation Upgrader]";

        private static readonly Vector3[] _corners = new Vector3[4];
        private static Selectable[] _candidates = new Selectable[64];
        // Each candidate's rect in the origin's local space, computed on first use and shared by every direction.
        private static Rect[] _rects = new Rect[64];
        private static bool[] _hasRect = new bool[64];
        // Each candidate's index in _scopes (the innermost scope it is under), or -1 when it can't be picked.
        private static int[] _levels = new int[64];
        // Whether each candidate can be picked, and its world corners (computed on first use), per collection.
        private static bool[] _isCandidate = new bool[64];
        private static Vector3[] _worldCorners = new Vector3[64 * 4];
        private static bool[] _hasCorners = new bool[64];
        private static int _count;
        // While batching, the candidates are collected once and shared by every Resolve until EndBatch.
        private static bool _batching;
        private static bool _collected;
        // The origin's scope, then each parent a Pass Through search can continue into (null = root).
        private static readonly List<NavigationGroup> _scopes = new();
        private static Matrix4x4 _toOrigin;
        private static readonly List<NavigationGroup> _chain = new();

        private Selectable _target;
        private UINavigation _originalNavigation;
        private UINavigation _appliedNavigation;
        // Whether _appliedNavigation is a Hold, which must be replaced on the next frame even without a move.
        private bool _held;
        private Selectable _selected;
        private readonly SelectionRecovery _recovery = new();
        private readonly InputModeTracker _focus = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            var go = new GameObject(_instanceName);
            DontDestroyOnLoad(go);
            
            go.AddComponent<NavigationUpgrader>();
        }

        private void Update() {
            // Taken even while off, so a group enabled then doesn't select later when the upgrade is turned on.
            var entry = TakePendingEntry();
            var restore = TakePendingRestore();

            // Null while the upgrade is off (or has no profile): Selectables are then left to Unity,
            // except for a move the focus tracker holds back.
            var profile = NavigationSettings.profile;
            var eventSystem = EventSystem.current;
            if (profile != null && eventSystem != null) {
                if (entry != null) {
                    eventSystem.SetSelectedGameObject(entry.gameObject);
                }

                // After Select On Enable, so a group opened while another closed keeps the selection it took.
                if (restore != null && restore.HoldsSelection()) {
                    eventSystem.SetSelectedGameObject(restore.restoreTarget.gameObject);
                }
            }

            // May move the selection to the hovered Selectable, so before the selection is read.
            bool holdMove = _focus.Update(eventSystem);

            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var selectable = selected != null ? selected.GetComponent<Selectable>() : null;

            if (profile != null && selectable != _selected) {
                _selected = selectable;
                RememberSelection(selectable);
            }

            // Whether what was applied no longer fits the Selectable, so it is resolved even without a move.
            bool stale = false;
            if (selectable != _target) {
                Release();

                if (selectable != null && (holdMove || (profile != null && IsUpgradable(selectable, selectable.navigation.mode)))) {
                    _target = selectable;
                    _originalNavigation = selectable.navigation;
                    stale = true;
                }
            } else if (_target != null && IsChangedExternally()) {
                // A script set the navigation while selected: that becomes the original to restore.
                _originalNavigation = _target.navigation;
                stale = true;
            }

            if (_target == null) {
                return;
            }

            if (holdMove) {
                _appliedNavigation = Hold(_target, _originalNavigation);
                _held = true;
            } else if (profile != null && IsUpgradable(_target, _originalNavigation.mode)) {
                // Neighbors are only used by a move, so without one the applied navigation is kept as is. A hold is
                // replaced anyway, or a script's FindSelectableOnX() would keep returning the Selectable itself.
                // Moves can't be read from custom input modules, so there it is resolved every frame.
                if (!stale && !_held && !UIInput.IsMoving(eventSystem) && UIInput.CanReadMove(eventSystem)) {
                    return;
                }

                _appliedNavigation = Resolve(_target, _originalNavigation, profile);
                _held = false;
            } else {
                Release();
                return;
            }

            _target.navigation = _appliedNavigation;
        }

        private void LateUpdate() {
            _recovery.LateUpdate();
            // After recovery, so a recovered selection is focused this frame.
            _focus.LateUpdate(EventSystem.current);
        }

        private void OnDisable() {
            Release();
        }

        private void Release() {
            // Keep a value a script set since the last update instead of overwriting it.
            if (_target != null && !IsChangedExternally()) {
                _target.navigation = _originalNavigation;
            }

            _target = null;
            _held = false;
        }

        // Navigation.Equals ignores wrapAround, but so does the navigation setter, so it can't change on its own.
        private bool IsChangedExternally() {
            return !_target.navigation.Equals(_appliedNavigation);
        }

        // Every group the selection is under records it, so entering an outer group can restore a nested selection.
        private static void RememberSelection(Selectable selectable) {
            if (selectable == null) {
                return;
            }

            for (var group = NavigationGroup.ScopeOf(selectable.transform); group != null; group = group.parent) {
                group.lastSelected = selectable;
            }
        }

        // The entry of the most recently enabled group waiting for Select On Enable, or null.
        private static Selectable TakePendingEntry() {
            Selectable entry = null;
            foreach (var group in NavigationGroup.activeGroups) {
                if (!group.selectPending) {
                    continue;
                }

                group.selectPending = false;
                var candidate = EntryOf(group);
                if (candidate != null) {
                    entry = candidate;
                }
            }

            return entry;
        }

        // The most recently disabled group waiting for Restore On Disable whose target is still usable, or null.
        // Going back past unusable targets handles stacked popups closed together: when B (opened from a button
        // in A) closes with A, B's target is inactive and A's wins. A destroyed group (scene unloaded or the
        // object destroyed) compares as null by now and is skipped; so are unusable targets, which are left
        // to Selection Recovery.
        private static NavigationGroup TakePendingRestore() {
            var pending = NavigationGroup.pendingRestores;
            NavigationGroup restore = null;
            for (int i = pending.Count - 1; i >= 0; i--) {
                var group = pending[i];
                if (group != null && IsUsable(group.restoreTarget)) {
                    restore = group;
                    break;
                }
            }

            pending.Clear();
            return restore;
        }

        // The last selection when remembered and still usable, else the default when usable, else null.
        internal static Selectable EntryOf(NavigationGroup group) {
            var last = group.lastSelected;
            if (group.rememberSelection && IsUsable(last) && last.transform.IsChildOf(group.transform)) {
                return last;
            }

            return IsUsable(group.defaultSelectable) ? group.defaultSelectable : null;
        }

        internal static bool IsUsable(Selectable selectable) {
            return selectable != null && selectable.isActiveAndEnabled && IsCandidate(selectable);
        }

        /// <summary>
        /// The navigation the upgrader would apply to this Selectable, or false when it isn't upgraded
        /// (upgrade off, Explicit/None mode, or not a RectTransform). Also used by the Scene view visualizer.
        /// </summary>
        internal static bool TryResolve(Selectable selectable, out UINavigation navigation) {
            return TryResolve(selectable, out navigation, null);
        }

        /// <param name="entered">
        /// When not null (length 4: left, right, up, down), receives the outermost group each move
        /// enters, or null when it doesn't enter one.
        /// </param>
        internal static bool TryResolve(Selectable selectable, out UINavigation navigation, NavigationGroup[] entered) {
            if (entered != null) {
                System.Array.Clear(entered, 0, entered.Length);
            }

            navigation = selectable.navigation;
            var profile = NavigationSettings.profile;
            if (profile == null || !IsUpgradable(selectable, navigation.mode)) {
                return false;
            }

            navigation = Resolve(selectable, navigation, profile, entered);
            return true;
        }

        /// <summary>
        /// Until <see cref="EndBatch"/>, every <see cref="TryResolve(Selectable, out UINavigation)"/> shares one
        /// collection of the candidates and their world corners, for resolving many Selectables while nothing changes
        /// (the Scene view visualizer's repaint).
        /// </summary>
        internal static void BeginBatch() {
            _batching = true;
            _collected = false;
        }

        internal static void EndBatch() {
            _batching = false;
        }

        // Takes the mode separately, since the tracked Selectable's navigation holds what was applied.
        private static bool IsUpgradable(Selectable selectable, UINavigation.Mode mode) {
            bool isAutomatic = mode == UINavigation.Mode.Automatic
                || mode == UINavigation.Mode.Horizontal
                || mode == UINavigation.Mode.Vertical;

            return isAutomatic && selectable.transform is RectTransform;
        }

        private static UINavigation Resolve(Selectable origin, UINavigation original, NavigationProfile profile, NavigationGroup[] entered = null) {
            var mode = original.mode;
            // Unity only wraps around in Horizontal/Vertical mode.
            bool wrap = original.wrapAround && mode != UINavigation.Mode.Automatic;
            var from = ((RectTransform)origin.transform).rect;
            int count = CollectCandidates(origin);

            var navigation = original;
            navigation.mode = UINavigation.Mode.Explicit;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;

            if ((mode & UINavigation.Mode.Horizontal) != 0 && !ControlsValue(origin, mode, horizontal: true)) {
                navigation.selectOnLeft = FindNeighbor(origin, from, Vector2.left, wrap, profile, count, entered, 0);
                navigation.selectOnRight = FindNeighbor(origin, from, Vector2.right, wrap, profile, count, entered, 1);
            }

            if ((mode & UINavigation.Mode.Vertical) != 0 && !ControlsValue(origin, mode, horizontal: false)) {
                navigation.selectOnUp = FindNeighbor(origin, from, Vector2.up, wrap, profile, count, entered, 2);
                navigation.selectOnDown = FindNeighbor(origin, from, Vector2.down, wrap, profile, count, entered, 3);
            }

            return navigation;
        }

        // Every direction points back at the Selectable itself: the move is consumed without moving or changing
        // a Slider/Scrollbar value (they only do that when FindSelectableOnX() returns null), and the input
        // module still starts its repeat delay, so a held move goes on after it.
        private static UINavigation Hold(Selectable selectable, UINavigation original) {
            var navigation = original;
            navigation.mode = UINavigation.Mode.Explicit;
            navigation.selectOnLeft = selectable;
            navigation.selectOnRight = selectable;
            navigation.selectOnUp = selectable;
            navigation.selectOnDown = selectable;
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

        // Also computes what every direction shares: the searchable scopes and each candidate's level in them.
        private static int CollectCandidates(Selectable origin) {
            if (!_batching || !_collected) {
                int selectableCount = Selectable.allSelectableCount;
                if (_candidates.Length < selectableCount) {
                    int capacity = Mathf.NextPowerOfTwo(selectableCount);
                    _candidates = new Selectable[capacity];
                    _rects = new Rect[capacity];
                    _hasRect = new bool[capacity];
                    _levels = new int[capacity];
                    _isCandidate = new bool[capacity];
                    _worldCorners = new Vector3[capacity * 4];
                    _hasCorners = new bool[capacity];
                }

                _count = Selectable.AllSelectablesNoAlloc(_candidates);
                System.Array.Clear(_hasCorners, 0, _count);
                for (int i = 0; i < _count; i++) {
                    _isCandidate[i] = IsCandidate(_candidates[i]);
                }

                _collected = true;
            }

            _scopes.Clear();
            var scope = NavigationGroup.ScopeOf(origin.transform);
            _scopes.Add(scope);
            while (scope != null && scope.boundary == NavigationBoundary.PassThrough) {
                scope = scope.parent;
                _scopes.Add(scope);
            }

            _toOrigin = origin.transform.worldToLocalMatrix;

            System.Array.Clear(_hasRect, 0, _count);
            for (int i = 0; i < _count; i++) {
                _levels[i] = LevelOf(i, origin);
            }

            return _count;
        }

        private static int LevelOf(int index, Selectable origin) {
            var candidate = _candidates[index];
            if (candidate == origin || !_isCandidate[index]) {
                return -1;
            }

            var transform = candidate.transform;
            for (int level = 0; level < _scopes.Count; level++) {
                var scope = _scopes[level];
                if (scope == null || transform.IsChildOf(scope.transform)) {
                    return level;
                }
            }

            return -1;
        }

        private static Selectable FindNeighbor(Selectable origin, Rect from, Vector2 direction, bool wrap, NavigationProfile profile, int count, NavigationGroup[] entered, int slot) {
            int level = 0;
            int index = Search(level, SearchIn(_scopes[level], from, direction, wrap, profile), count);

            // Nothing inside a Pass Through group: continue in its parent scope, without the group's own members.
            while (index < 0 && level + 1 < _scopes.Count) {
                level++;
                index = Search(level, SearchIn(_scopes[level], from, direction, wrap, profile), count);
            }

            if (index < 0) {
                return null;
            }

            var selectable = Enter(_candidates[index], _scopes[level], out var group);
            if (entered != null) {
                entered[slot] = group;
            }

            return selectable;
        }

        // The scope's own settings: its wrap axes on top of the Selectable's own wrap, and the nearest profile
        // set on it or its ancestors, falling back to the project-wide one.
        private static NeighborSearch SearchIn(NavigationGroup scope, Rect from, Vector2 direction, bool wrap, NavigationProfile profile) {
            if (scope != null) {
                var axis = direction.x != 0 ? NavigationWrap.Horizontal : NavigationWrap.Vertical;
                wrap |= (scope.wrapAround & axis) != 0;
            }

            for (var group = scope; group != null; group = group.parent) {
                if (group.profile != null) {
                    profile = group.profile;
                    break;
                }
            }

            return new NeighborSearch(from, direction, wrap, profile);
        }

        // Considers every Selectable under the level's scope, including those in its child groups, but not those
        // under the scope below it, which the previous level already searched.
        private static int Search(int level, NeighborSearch search, int count) {
            for (int i = 0; i < count; i++) {
                if (_levels[i] != level) {
                    continue;
                }

                if (!_hasRect[i]) {
                    _rects[i] = GetLocalRect(i);
                    _hasRect[i] = true;
                }

                search.Consider(i, _rects[i]);
            }

            return search.result;
        }

        // When the picked Selectable is in a group below the scope, the move enters that group: the first entry
        // (remembered, then default) from the outermost group inward wins, otherwise the picked Selectable itself.
        private static Selectable Enter(Selectable picked, NavigationGroup scope, out NavigationGroup entered) {
            _chain.Clear();
            for (var group = NavigationGroup.ScopeOf(picked.transform); group != scope; group = group.parent) {
                _chain.Add(group);
            }

            entered = _chain.Count > 0 ? _chain[_chain.Count - 1] : null;
            for (int i = _chain.Count - 1; i >= 0; i--) {
                var selectable = EntryOf(_chain[i]);
                if (selectable != null) {
                    return selectable;
                }
            }

            return picked;
        }

        private static bool IsCandidate(Selectable selectable) {
            return selectable.IsInteractable()
                && selectable.navigation.mode != UINavigation.Mode.None
                && selectable.transform is RectTransform;
        }

        // The candidate's rect in the origin's local space.
        private static Rect GetLocalRect(int index) {
            int offset = index * 4;
            if (!_hasCorners[index]) {
                ((RectTransform)_candidates[index].transform).GetWorldCorners(_corners);
                System.Array.Copy(_corners, 0, _worldCorners, offset, 4);
                _hasCorners[index] = true;
            }

            Vector2 min = _toOrigin.MultiplyPoint3x4(_worldCorners[offset]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++) {
                Vector2 point = _toOrigin.MultiplyPoint3x4(_worldCorners[offset + i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
