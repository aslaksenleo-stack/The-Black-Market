using UnityEngine;

namespace BlackMarket.Player
{
    /// <summary>
    /// Stationary first-person look for the shop. Turns the PlayerRig left/right (yaw) and tilts
    /// the camera up/down (pitch), within limits so the player always stays oriented toward the counter.
    /// Put this on PlayerRig, next to MobileInputController, with the Camera as a child.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraController : MonoBehaviour
    {
        [Tooltip("The transform that tilts up/down. Defaults to the child Camera.")]
        [SerializeField] private Transform pitchPivot;

        [Tooltip("How many degrees the view turns when the finger moves one inch.")]
        [SerializeField] private float degreesPerInch = 90f;

        [Tooltip("How far the player can turn left or right from the starting direction.")]
        [SerializeField] private float yawLimit = 100f;

        [Tooltip("Most upward look (negative = up).")]
        [SerializeField] private float minPitch = -35f;

        [Tooltip("Most downward look.")]
        [SerializeField] private float maxPitch = 60f;

        [Tooltip("Starting downward tilt, so the counter is in view.")]
        [SerializeField] private float startPitch = 12f;

        [SerializeField] private bool invertY;

        private IPlayerInput input;
        private float baseYaw;
        private Vector2 look; // x = yaw offset, y = pitch

        public Vector2 CurrentLook => look;

        private void Awake()
        {
            input = GetComponent<IPlayerInput>();
            if (input == null)
            {
                Debug.LogError("[CameraController] No IPlayerInput (e.g. MobileInputController) on this object.");
            }

            if (pitchPivot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null)
                {
                    pitchPivot = childCamera.transform;
                }
            }

            baseYaw = transform.eulerAngles.y;
            look = new Vector2(0f, Mathf.Clamp(startPitch, minPitch, maxPitch));
            ApplyRotation();
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            Vector2 delta = input.LookDelta;
            if (delta == Vector2.zero)
            {
                return;
            }

            look = LookMath.Apply(look, delta, Screen.dpi, degreesPerInch, yawLimit, minPitch, maxPitch, invertY);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0f, baseYaw + look.x, 0f);
            if (pitchPivot != null)
            {
                pitchPivot.localRotation = Quaternion.Euler(look.y, 0f, 0f);
            }
        }
    }
}
