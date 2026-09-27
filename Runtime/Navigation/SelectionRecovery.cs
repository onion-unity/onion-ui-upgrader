using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ONION_INPUTSYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Onion.UI.Navigation {
    /// <summary>
    /// When the selection is lost (null, inactive, or a disabled Selectable), the next move selects a usable
    /// Selectable instead. Move input is read from the current input module; custom modules are not supported.
    /// Owned and driven by <see cref="NavigationUpgrader"/>.
    /// </summary>
    internal sealed class SelectionRecovery {
        // The dead zone StandaloneInputModule uses to pick a move direction (BaseInputModule.DetermineMoveDirection).
        private const float MoveDeadZone = 0.6f;

        private Selectable[] _candidates = new Selectable[64];

        private Selectable _last;
        private Vector3 _lastPosition;
        private bool _hasLast;
        private bool _wasMoving;
        // Groups the last selection was under, innermost first.
        private readonly List<NavigationGroup> _lastGroups = new();

        // Called in LateUpdate: after EventSystem.Update has sent this frame's move to the lost selection,
        // so the move that recovers doesn't also move away from the recovered Selectable.
        internal void LateUpdate() {
            var eventSystem = EventSystem.current;
            bool moving = eventSystem != null && IsMoving(eventSystem);
            bool moveStarted = moving && !_wasMoving;
            _wasMoving = moving;

            if (eventSystem == null || !NavigationSettings.recoversSelection) {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (!IsLost(selected)) {
                if (selected.TryGetComponent(out Selectable selectable)) {
                    Remember(selectable);
                }

                return;
            }

            // Nothing was ever selected: nothing to recover to.
            if (!moveStarted || !_hasLast) {
                return;
            }

            var target = FindTarget();
            if (target != null) {
                eventSystem.SetSelectedGameObject(target.gameObject);
            }
        }

        // Whether the current input module's move input is past the dead zone.
        private static bool IsMoving(EventSystem eventSystem) {
            if (!eventSystem.sendNavigationEvents) {
                return false;
            }

            switch (eventSystem.currentInputModule) {
                case StandaloneInputModule standalone:
                    var input = standalone.input;
                    var move = new Vector2(input.GetAxisRaw(standalone.horizontalAxis), input.GetAxisRaw(standalone.verticalAxis));
                    return move.sqrMagnitude >= MoveDeadZone * MoveDeadZone;
#if ONION_INPUTSYSTEM
                case InputSystemUIInputModule inputSystem:
                    var reference = inputSystem.move;
                    if (reference == null || reference.action == null) {
                        return false;
                    }

                    // No dead zone of its own: any nonzero move is sent (the action's processors apply one).
                    return reference.action.ReadValue<Vector2>().sqrMagnitude > 0f;
#endif
                default:
                    return false;
            }
        }

        // Input modules don't send events to inactive objects or disabled components, so these can't move either.
        private static bool IsLost(GameObject selected) {
            if (selected == null || !selected.activeInHierarchy) {
                return true;
            }

            return selected.TryGetComponent(out Selectable selectable) && !selectable.isActiveAndEnabled;
        }

        private void Remember(Selectable selectable) {
            _lastPosition = WorldCenter(selectable);
            _hasLast = true;
            if (selectable == _last) {
                return;
            }

            _last = selectable;
            _lastGroups.Clear();
            for (var group = NavigationGroup.ScopeOf(selectable.transform); group != null; group = group.parent) {
                _lastGroups.Add(group);
            }
        }

        // The last selection, else for each of its groups still enabled (innermost first) the group's entry
        // or its member nearest the last position, else the nearest Selectable anywhere.
        private Selectable FindTarget() {
            if (NavigationUpgrader.IsUsable(_last)) {
                return _last;
            }

            int count = CollectCandidates();
            foreach (var group in _lastGroups) {
                if (group == null || !group.isActiveAndEnabled) {
                    continue;
                }

                var entry = NavigationUpgrader.EntryOf(group);
                if (entry != null) {
                    return entry;
                }

                var nearest = FindNearest(group.transform, count);
                if (nearest != null) {
                    return nearest;
                }
            }

            return FindNearest(null, count);
        }

        private Selectable FindNearest(Transform scope, int count) {
            Selectable nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++) {
                var candidate = _candidates[i];
                if (!NavigationUpgrader.IsUsable(candidate)) {
                    continue;
                }

                if (scope != null && !candidate.transform.IsChildOf(scope)) {
                    continue;
                }

                float distance = (WorldCenter(candidate) - _lastPosition).sqrMagnitude;
                if (distance < nearestDistance) {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private int CollectCandidates() {
            int selectableCount = Selectable.allSelectableCount;
            if (_candidates.Length < selectableCount) {
                _candidates = new Selectable[Mathf.NextPowerOfTwo(selectableCount)];
            }

            return Selectable.AllSelectablesNoAlloc(_candidates);
        }

        private static Vector3 WorldCenter(Selectable selectable) {
            var transform = selectable.transform;
            return transform is RectTransform rectTransform
                ? rectTransform.TransformPoint(rectTransform.rect.center)
                : transform.position;
        }
    }
}
