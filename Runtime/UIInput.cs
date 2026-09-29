using UnityEngine;
using UnityEngine.EventSystems;
#if ONION_INPUTSYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Onion.UI {
    /// <summary>
    /// Reads UI input from the current input module, the way the module itself does.
    /// Only StandaloneInputModule and InputSystemUIInputModule are supported; custom modules read as no input.
    /// </summary>
    internal static class UIInput {
        // The dead zone StandaloneInputModule uses to pick a move direction (BaseInputModule.DetermineMoveDirection).
        private const float MoveDeadZone = 0.6f;

        /// <summary>
        /// Whether the move input is past the module's threshold, i.e. the module sends Move while it is held.
        /// </summary>
        internal static bool IsMoving(EventSystem eventSystem) {
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

        /// <summary>
        /// Whether <see cref="IsMoving"/> reflects the moves the current input module sends. False for custom modules.
        /// </summary>
        internal static bool CanReadMove(EventSystem eventSystem) {
            return eventSystem.currentInputModule switch {
                // No module sends no moves, which IsMoving reports correctly.
                null => true,
                StandaloneInputModule => true,
#if ONION_INPUTSYSTEM
                InputSystemUIInputModule => true,
#endif
                _ => false,
            };
        }

        internal static bool WasSubmitPressed(EventSystem eventSystem) {
            if (!eventSystem.sendNavigationEvents) {
                return false;
            }

            switch (eventSystem.currentInputModule) {
                case StandaloneInputModule standalone:
                    return standalone.input.GetButtonDown(standalone.submitButton);
#if ONION_INPUTSYSTEM
                case InputSystemUIInputModule inputSystem:
                    return WasPerformed(inputSystem.submit);
#endif
                default:
                    return false;
            }
        }

        internal static bool WasCancelPressed(EventSystem eventSystem) {
            if (!eventSystem.sendNavigationEvents) {
                return false;
            }

            switch (eventSystem.currentInputModule) {
                case StandaloneInputModule standalone:
                    return standalone.input.GetButtonDown(standalone.cancelButton);
#if ONION_INPUTSYSTEM
                case InputSystemUIInputModule inputSystem:
                    return WasPerformed(inputSystem.cancel);
#endif
                default:
                    return false;
            }
        }

        /// <summary>
        /// The pointer's screen position: a touch while touching, else the mouse. False when there is no pointer
        /// or the cursor is locked (the modules then don't point at anything).
        /// </summary>
        internal static bool TryGetPointerPosition(EventSystem eventSystem, out Vector2 position) {
            position = default;
            if (Cursor.lockState == CursorLockMode.Locked) {
                return false;
            }

            switch (eventSystem.currentInputModule) {
                case StandaloneInputModule standalone:
                    var input = standalone.input;
                    for (int i = 0; i < input.touchCount; i++) {
                        var touch = input.GetTouch(i);
                        if (touch.type != TouchType.Indirect) {
                            position = touch.position;
                            return true;
                        }
                    }

                    if (!input.mousePresent) {
                        return false;
                    }

                    position = input.mousePosition;
                    return true;
#if ONION_INPUTSYSTEM
                case InputSystemUIInputModule inputSystem:
                    var reference = inputSystem.point;
                    if (reference == null || reference.action == null) {
                        return false;
                    }

                    position = reference.action.ReadValue<Vector2>();
                    return true;
#endif
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether a pointer button was pressed or a touch began this frame.
        /// </summary>
        internal static bool WasPointerPressed(EventSystem eventSystem) {
            switch (eventSystem.currentInputModule) {
                case StandaloneInputModule standalone:
                    var input = standalone.input;
                    for (int i = 0; i < input.touchCount; i++) {
                        var touch = input.GetTouch(i);
                        if (touch.type != TouchType.Indirect && touch.phase == TouchPhase.Began) {
                            return true;
                        }
                    }

                    return input.mousePresent && (input.GetMouseButtonDown(0) || input.GetMouseButtonDown(1) || input.GetMouseButtonDown(2));
#if ONION_INPUTSYSTEM
                case InputSystemUIInputModule inputSystem:
                    return WasPressed(inputSystem.leftClick) || WasPressed(inputSystem.rightClick) || WasPressed(inputSystem.middleClick);
#endif
                default:
                    return false;
            }
        }

#if ONION_INPUTSYSTEM
        private static bool WasPerformed(UnityEngine.InputSystem.InputActionReference reference) {
            return reference != null && reference.action != null && reference.action.WasPerformedThisFrame();
        }

        private static bool WasPressed(UnityEngine.InputSystem.InputActionReference reference) {
            return reference != null && reference.action != null && reference.action.WasPressedThisFrame();
        }
#endif
    }
}
