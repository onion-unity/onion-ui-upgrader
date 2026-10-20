using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ONION_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Onion.UI.Selectables {
    /// <summary>
    /// Presses the Button or Toggle (sends it Submit) when a key or gamepad button is pressed, without moving the
    /// selection. When several use the same key, only the usable one enabled most recently reacts, so a popup's
    /// button wins over the one behind it.
    /// </summary>
    [AddComponentMenu("Onion/UI/Hotkey Upgrader")]
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
    // After NavigationUpgrader.Update (order -10000), whose Continue From Hover may move the selection, and before
    // EventSystem.Update (order 0) handles this frame's UI input.
    [DefaultExecutionOrder(-1000)]
    public sealed class HotkeyUpgrader : MonoBehaviour {
        // Enabled upgraders in enable order; the last one wins a key.
        private static readonly List<HotkeyUpgrader> _enabled = new();
        private static readonly List<HotkeyUpgrader> _snapshot = new();
        // Pressed by their key, which is still held.
        private static readonly List<HotkeyUpgrader> _held = new();
        // The keys (controls) already handled this frame.
        private static readonly List<object> _handled = new();
        private static int _dispatchFrame = -1;
        private static bool _wasMoving;
        private static BaseEventData _eventData;
        private static EventSystem _eventDataSystem;

#if ONION_INPUTSYSTEM
        // Actions this component type enabled, with how many enabled upgraders use each.
        private static readonly Dictionary<InputAction, int> _enabledActions = new();

        [Tooltip("Pressing it presses this Selectable, like Submit.\nAn action from an asset is enabled while needed if it is off.")]
        public InputActionProperty action = new(new InputAction("Hotkey", InputActionType.Button));

        private InputAction _ownedAction;
#else
        [Tooltip("Pressing this key presses this Selectable, like Submit.")]
        public KeyCode key;

        [Tooltip("Pressing this Input Manager button presses this Selectable, like Submit.")]
        public string buttonName;
#endif

        private Selectable _selectable;
        // The key that pressed this Selectable, while it is held.
        private object _heldKey;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            _enabled.Clear();
            _held.Clear();
            _dispatchFrame = -1;
            _wasMoving = false;
            _eventData = null;
            _eventDataSystem = null;
#if ONION_INPUTSYSTEM
            _enabledActions.Clear();
#endif
        }

        /// <summary>
        /// Whether a hotkey pressed this Selectable and is still held, so it shows as Pressed.
        /// </summary>
        internal static bool IsHeld(Selectable selectable) {
            foreach (var upgrader in _held) {
                if (upgrader._selectable == selectable) {
                    return true;
                }
            }

            return false;
        }

        private void Awake() {
            _selectable = GetComponent<Selectable>();
        }

        private void OnEnable() {
            _enabled.Add(this);
            EnableAction();
        }

        private void OnDisable() {
            _enabled.Remove(this);
            _held.Remove(this);
            _heldKey = null;
            DisableAction();
        }

        // The first upgrader to update in a frame handles every upgrader.
        private void Update() {
            if (_dispatchFrame == Time.frameCount) {
                return;
            }

            _dispatchFrame = Time.frameCount;
            Dispatch();
        }

        private static void Dispatch() {
            for (int i = _held.Count - 1; i >= 0; i--) {
                var upgrader = _held[i];
                if (!upgrader.IsUsable() || !upgrader.IsKeyHeld(upgrader._heldKey)) {
                    upgrader._heldKey = null;
                    _held.RemoveAt(i);
                }
            }

            var eventSystem = EventSystem.current;
            bool moving = eventSystem != null && UIInput.IsMoving(eventSystem);
            bool moveStarted = moving && !_wasMoving;
            _wasMoving = moving;
            if (eventSystem == null) {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (IsEditingText(selected)) {
                return;
            }

            bool overlaps = OverlapsUIInput(eventSystem, selected, moveStarted);

            // A press can enable or disable upgraders (opening or closing a popup), so go through a copy.
            _snapshot.Clear();
            _snapshot.AddRange(_enabled);
            _handled.Clear();
            for (int i = _snapshot.Count - 1; i >= 0; i--) {
                var upgrader = _snapshot[i];
                if (upgrader == null || !upgrader.WasPressed(out object pressedKey) || _handled.Contains(pressedKey)) {
                    continue;
                }

                // Not handled, so an upgrader enabled earlier can take the key, e.g. one not behind a CanvasGroup.
                if (!upgrader.IsUsable()) {
                    continue;
                }

                _handled.Add(pressedKey);
                if (!overlaps) {
                    upgrader.Press(eventSystem, pressedKey);
                }
            }

            _snapshot.Clear();
        }

        private void Press(EventSystem eventSystem, object pressedKey) {
            if (_eventData == null || _eventDataSystem != eventSystem) {
                _eventData = new BaseEventData(eventSystem);
                _eventDataSystem = eventSystem;
            }

            _eventData.Reset();
            ExecuteEvents.Execute(gameObject, _eventData, ExecuteEvents.submitHandler);

            // The press may have disabled this (e.g. a Close button).
            if (IsUsable() && !_held.Contains(this)) {
                _heldKey = pressedKey;
                _held.Add(this);
            }
        }

        private bool IsUsable() {
            return this != null && isActiveAndEnabled && _selectable.IsActive() && _selectable.IsInteractable();
        }

        // Editing text: every key goes to the text, not to hotkeys.
        private static bool IsEditingText(GameObject selected) {
            if (selected == null) {
                return false;
            }

            return (selected.TryGetComponent(out InputField inputField) && inputField.isFocused)
                || (selected.TryGetComponent(out TMP_InputField tmpInputField) && tmpInputField.isFocused);
        }

        // The selection handles this frame's Submit, Cancel or move start, so a hotkey on the same input would act twice.
        private static bool OverlapsUIInput(EventSystem eventSystem, GameObject selected, bool moveStarted) {
            if (selected == null) {
                return false;
            }

            return (UIInput.WasSubmitPressed(eventSystem) && ExecuteEvents.CanHandleEvent<ISubmitHandler>(selected))
                || (UIInput.WasCancelPressed(eventSystem) && ExecuteEvents.CanHandleEvent<ICancelHandler>(selected))
                || (moveStarted && ExecuteEvents.CanHandleEvent<IMoveHandler>(selected));
        }

#if ONION_INPUTSYSTEM
        // The key is the control that pressed the action, so two upgraders on the same key share it even when their
        // actions differ.
        private bool WasPressed(out object pressedKey) {
            var inputAction = action.action;
            pressedKey = null;
            if (inputAction == null || !inputAction.WasPressedThisFrame()) {
                return false;
            }

            pressedKey = (object)inputAction.activeControl ?? inputAction;
            return true;
        }

        private bool IsKeyHeld(object pressedKey) {
            return pressedKey switch {
                InputControl control => control.IsPressed(),
                InputAction inputAction => inputAction.IsPressed(),
                _ => false,
            };
        }

        // Only an action that is off is enabled, and it is disabled again when no upgrader that enabled it needs it.
        private void EnableAction() {
            var inputAction = action.action;
            if (inputAction == null) {
                return;
            }

            if (_enabledActions.TryGetValue(inputAction, out int count)) {
                _enabledActions[inputAction] = count + 1;
            } else if (!inputAction.enabled) {
                inputAction.Enable();
                _enabledActions[inputAction] = 1;
            } else {
                return;
            }

            _ownedAction = inputAction;
        }

        private void DisableAction() {
            if (_ownedAction == null) {
                return;
            }

            if (_enabledActions.TryGetValue(_ownedAction, out int count)) {
                if (count > 1) {
                    _enabledActions[_ownedAction] = count - 1;
                } else {
                    _enabledActions.Remove(_ownedAction);
                    _ownedAction.Disable();
                }
            }

            _ownedAction = null;
        }
#else
        private bool WasPressed(out object pressedKey) {
            pressedKey = null;
            if (key != KeyCode.None && Input.GetKeyDown(key)) {
                pressedKey = key;
            } else if (!string.IsNullOrEmpty(buttonName) && Input.GetButtonDown(buttonName)) {
                pressedKey = buttonName;
            }

            return pressedKey != null;
        }

        private bool IsKeyHeld(object pressedKey) {
            return pressedKey switch {
                KeyCode keyCode => Input.GetKey(keyCode),
                string name => Input.GetButton(name),
                _ => false,
            };
        }

        private void EnableAction() {
        }

        private void DisableAction() {
        }
#endif
    }
}
