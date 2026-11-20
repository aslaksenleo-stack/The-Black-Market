using System;
using BlackMarket.Player;
using UnityEngine;

namespace BlackMarket.Interaction
{
    /// <summary>
    /// Finds what the player can interact with and uses it.
    /// Two ways to interact, both mobile-friendly:
    /// 1. Look at something (screen centre). The interact button appears; press it.
    /// 2. Tap the object directly on screen.
    /// Put this on the Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class InteractionController : MonoBehaviour
    {
        private static readonly Vector3 ScreenCentre = new Vector3(0.5f, 0.5f, 0f);

        [Tooltip("How far away (metres) objects can be used from.")]
        [SerializeField] private float maxDistance = 3f;

        [SerializeField] private LayerMask interactionMask = ~0;

        private Camera viewCamera;
        private IPlayerInput input;

        /// <summary>The object at the centre of the screen, or null.</summary>
        public IInteractable Focused { get; private set; }

        /// <summary>Raised when the object at the centre of the screen changes (null = nothing).</summary>
        public event Action<IInteractable> FocusChanged;

        /// <summary>Raised after an object has been used.</summary>
        public event Action<IInteractable> Interacted;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            input = GetComponentInParent<IPlayerInput>();
            if (input == null)
            {
                Debug.LogError("[InteractionController] No IPlayerInput found on this object or its parents.");
            }
        }

        private void OnDisable()
        {
            // Clear focus so the interact button hides while interaction is paused (e.g. a panel is open).
            SetFocused(null);
        }

        private void Update()
        {
            SetFocused(FindTarget(viewCamera.ViewportPointToRay(ScreenCentre)));

            if (input == null)
            {
                return;
            }

            if (input.InteractPressed && Focused != null && Focused.CanInteract)
            {
                Use(Focused);
            }
            else if (input.Tapped)
            {
                IInteractable tapped = FindTarget(viewCamera.ScreenPointToRay(input.TapPosition));
                if (tapped != null && tapped.CanInteract)
                {
                    Use(tapped);
                }
            }
        }

        private IInteractable FindTarget(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return hit.collider.GetComponentInParent<IInteractable>();
            }

            return null;
        }

        private void SetFocused(IInteractable target)
        {
            if (ReferenceEquals(target, Focused))
            {
                return;
            }

            Focused = target;
            FocusChanged?.Invoke(target);
        }

        private void Use(IInteractable target)
        {
            target.Interact();
            Interacted?.Invoke(target);
        }
    }
}
