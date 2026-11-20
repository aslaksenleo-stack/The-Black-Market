using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlackMarket.Player
{
    /// <summary>
    /// Turns touchscreen (phone) and mouse/keyboard (editor) input into IPlayerInput values.
    ///
    /// Touch: a finger that starts on the 3D view (not on a button, and right of LookZoneStartX)
    ///        drags the camera. A quick, still touch counts as a tap on whatever is under it.
    /// Editor: hold the right mouse button and drag to look, left-click to tap, WASD to move, E to interact.
    ///
    /// On-screen buttons call SetVirtualMove / RequestInteract.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Read input before anything uses it this frame.
    [DisallowMultipleComponent]
    public sealed class MobileInputController : MonoBehaviour, IPlayerInput
    {
        [Tooltip("Fraction of the screen width (from the left) where touches do NOT control the camera. 0 = whole screen.")]
        [Range(0f, 1f)]
        [SerializeField] private float lookZoneStartX = 0f;

        [Tooltip("Editor only: when ticked, the right mouse button must be held to look.")]
        [SerializeField] private bool requireMouseButtonForLook = true;

        [Tooltip("Longest touch that still counts as a tap.")]
        [SerializeField] private float tapMaxSeconds = 0.3f;

        [Tooltip("How far (in inches) a finger may move and still count as a tap.")]
        [SerializeField] private float tapMaxMoveInches = 0.12f;

        private readonly TapDetector tapDetector = new TapDetector();
        private Vector2 virtualMove;
        private bool interactRequested;
        private int lookTouchId = -1;

        public Vector2 LookDelta { get; private set; }
        public Vector2 Move { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool Tapped { get; private set; }
        public Vector2 TapPosition { get; private set; }

        public float LookZoneStartX
        {
            get => lookZoneStartX;
            set => lookZoneStartX = Mathf.Clamp01(value);
        }

        /// <summary>Called by the on-screen joystick.</summary>
        public void SetVirtualMove(Vector2 direction)
        {
            virtualMove = Vector2.ClampMagnitude(direction, 1f);
        }

        /// <summary>Called by the on-screen interact button.</summary>
        public void RequestInteract()
        {
            interactRequested = true;
        }

        private void Awake()
        {
#if !(ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM) && ENABLE_LEGACY_INPUT_MANAGER
            // Otherwise every touch is ALSO reported as a mouse click, causing double taps.
            Input.simulateMouseWithTouches = false;
#endif
        }

        private void OnDisable()
        {
            LookDelta = Vector2.zero;
            Move = Vector2.zero;
            InteractPressed = false;
            Tapped = false;
            lookTouchId = -1;
            tapDetector.Cancel();
        }

        private void Update()
        {
            Vector2 look = Vector2.zero;
            Vector2 move = virtualMove;
            bool interact = interactRequested;
            interactRequested = false;
            Tapped = false;

            float now = Time.unscaledTime;
            tapDetector.MaxDurationSeconds = tapMaxSeconds;
            tapDetector.MaxMovePixels = tapMaxMoveInches * LookMath.EffectiveDpi(Screen.dpi);

#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
            ReadTouches(ref look, now);

            Mouse mouse = Mouse.current;
            if (mouse != null && lookTouchId < 0)
            {
                if (!requireMouseButtonForLook || mouse.rightButton.isPressed)
                {
                    look += mouse.delta.ReadValue();
                }

                Vector2 mousePosition = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI(-1))
                {
                    tapDetector.Begin(mousePosition, now);
                }

                if (mouse.leftButton.wasReleasedThisFrame && tapDetector.End(mousePosition, now))
                {
                    RegisterTap(mousePosition);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 keys = new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
                move = Vector2.ClampMagnitude(move + keys, 1f);

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    interact = true;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            ReadLegacyTouches(ref look, now);

            if (lookTouchId < 0 && Input.touchCount == 0)
            {
                if (!requireMouseButtonForLook || Input.GetMouseButton(1))
                {
                    // Legacy mouse axes are not in pixels; this scale roughly matches pixel deltas.
                    const float legacyMouseToPixels = 10f;
                    look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * legacyMouseToPixels;
                }

                Vector2 mousePosition = Input.mousePosition;
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI(-1))
                {
                    tapDetector.Begin(mousePosition, now);
                }

                if (Input.GetMouseButtonUp(0) && tapDetector.End(mousePosition, now))
                {
                    RegisterTap(mousePosition);
                }
            }

            Vector2 keys = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
            move = Vector2.ClampMagnitude(move + keys, 1f);

            if (Input.GetKeyDown(KeyCode.E))
            {
                interact = true;
            }
#endif

            LookDelta = look;
            Move = move;
            InteractPressed = interact;
        }

        private void RegisterTap(Vector2 screenPosition)
        {
            Tapped = true;
            TapPosition = screenPosition;
        }

        private static bool IsPointerOverUI(int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
        }

#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
        private void ReadTouches(ref Vector2 look, float now)
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
            {
                lookTouchId = -1;
                return;
            }

            bool trackedTouchStillDown = false;
            float lookZoneStartPixels = Screen.width * lookZoneStartX;

            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                // Claim a new finger only when it first touches the 3D view inside the look zone.
                if (lookTouchId < 0 && touch.press.wasPressedThisFrame &&
                    position.x >= lookZoneStartPixels && !IsPointerOverUI(id))
                {
                    lookTouchId = id;
                    tapDetector.Begin(position, now);
                }

                if (id != lookTouchId)
                {
                    continue;
                }

                if (touch.press.wasReleasedThisFrame)
                {
                    if (tapDetector.End(position, now))
                    {
                        RegisterTap(position);
                    }

                    continue;
                }

                if (!touch.press.isPressed)
                {
                    continue;
                }

                trackedTouchStillDown = true;
                if (!touch.press.wasPressedThisFrame)
                {
                    look += touch.delta.ReadValue();
                    tapDetector.Move(position);
                }
            }

            if (!trackedTouchStillDown)
            {
                lookTouchId = -1;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private void ReadLegacyTouches(ref Vector2 look, float now)
        {
            bool trackedTouchStillDown = false;
            float lookZoneStartPixels = Screen.width * lookZoneStartX;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (lookTouchId < 0 && touch.phase == TouchPhase.Began &&
                    touch.position.x >= lookZoneStartPixels && !IsPointerOverUI(touch.fingerId))
                {
                    lookTouchId = touch.fingerId;
                    tapDetector.Begin(touch.position, now);
                }

                if (touch.fingerId != lookTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Ended)
                {
                    if (tapDetector.End(touch.position, now))
                    {
                        RegisterTap(touch.position);
                    }

                    continue;
                }

                if (touch.phase == TouchPhase.Canceled)
                {
                    tapDetector.Cancel();
                    continue;
                }

                trackedTouchStillDown = true;
                if (touch.phase == TouchPhase.Moved)
                {
                    look += touch.deltaPosition;
                    tapDetector.Move(touch.position);
                }
            }

            if (!trackedTouchStillDown)
            {
                lookTouchId = -1;
            }
        }
#endif
    }
}
