using UnityEngine;
#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlackMarket.Player
{
    /// <summary>
    /// Turns touchscreen (phone) and mouse/keyboard (editor) input into IPlayerInput values.
    ///
    /// Touch: a finger that starts to the right of LookZoneStartX drags the camera.
    ///        In the shop this is the whole screen (0). In marketplaces it becomes 0.4 so the
    ///        left side is free for the virtual joystick.
    /// Editor: hold the right mouse button and drag to look, WASD to move, E to interact.
    ///
    /// On-screen buttons (joystick, interact button) are added later and call
    /// SetVirtualMove / RequestInteract.
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

        private Vector2 virtualMove;
        private bool interactRequested;
        private int lookTouchId = -1;

        public Vector2 LookDelta { get; private set; }
        public Vector2 Move { get; private set; }
        public bool InteractPressed { get; private set; }

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

        private void OnDisable()
        {
            LookDelta = Vector2.zero;
            Move = Vector2.zero;
            InteractPressed = false;
            lookTouchId = -1;
        }

        private void Update()
        {
            Vector2 look = Vector2.zero;
            Vector2 move = virtualMove;
            bool interact = interactRequested;
            interactRequested = false;

#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
            ReadTouchLook(ref look);

            Mouse mouse = Mouse.current;
            if (mouse != null && lookTouchId < 0 && (!requireMouseButtonForLook || mouse.rightButton.isPressed))
            {
                look += mouse.delta.ReadValue();
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
            ReadLegacyTouchLook(ref look);

            if (lookTouchId < 0 && (!requireMouseButtonForLook || Input.GetMouseButton(1)))
            {
                // Legacy mouse axes are not in pixels; this scale roughly matches pixel deltas.
                const float legacyMouseToPixels = 10f;
                look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * legacyMouseToPixels;
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

#if ENABLE_INPUT_SYSTEM && BLACKMARKET_HAS_INPUT_SYSTEM
        private void ReadTouchLook(ref Vector2 look)
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
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int id = touch.touchId.ReadValue();

                // Claim a new finger for looking only when it first touches inside the look zone.
                if (lookTouchId < 0 && touch.press.wasPressedThisFrame &&
                    touch.position.ReadValue().x >= lookZoneStartPixels)
                {
                    lookTouchId = id;
                }

                if (id == lookTouchId)
                {
                    trackedTouchStillDown = true;
                    if (!touch.press.wasPressedThisFrame)
                    {
                        look += touch.delta.ReadValue();
                    }
                }
            }

            if (!trackedTouchStillDown)
            {
                lookTouchId = -1;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private void ReadLegacyTouchLook(ref Vector2 look)
        {
            bool trackedTouchStillDown = false;
            float lookZoneStartPixels = Screen.width * lookZoneStartX;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (lookTouchId < 0 && touch.phase == TouchPhase.Began && touch.position.x >= lookZoneStartPixels)
                {
                    lookTouchId = touch.fingerId;
                }

                if (touch.fingerId != lookTouchId ||
                    touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    continue;
                }

                trackedTouchStillDown = true;
                if (touch.phase == TouchPhase.Moved)
                {
                    look += touch.deltaPosition;
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
