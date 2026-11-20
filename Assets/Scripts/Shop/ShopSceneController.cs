using BlackMarket.Interaction;
using BlackMarket.Player;
using BlackMarket.UI;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Connects the shop scene's pieces: the counter, the player's camera and interaction, and the HUD.
    /// Holds no game data itself. Wallet, inventory and customers will be connected here
    /// as their milestones arrive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopSceneController : MonoBehaviour
    {
        [SerializeField] private MobileInputController input;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private InteractionController interaction;
        [SerializeField] private CounterInteractable counter;
        [SerializeField] private ShopHUD hud;

        private bool wired;

        private void Start()
        {
            // References can be assigned in the Inspector; otherwise find the scene's only instance.
            if (input == null) input = FindFirstObjectByType<MobileInputController>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (interaction == null) interaction = FindFirstObjectByType<InteractionController>();
            if (counter == null) counter = FindFirstObjectByType<CounterInteractable>();
            if (hud == null) hud = FindFirstObjectByType<ShopHUD>();

            if (input == null || cameraController == null || interaction == null || counter == null || hud == null)
            {
                Debug.LogError("[ShopSceneController] The shop scene is missing a required object " +
                               "(MobileInputController, CameraController, InteractionController, CounterInteractable or ShopHUD).");
                enabled = false;
                return;
            }

            // Placeholder values until the wallet and progression systems exist (Milestones 4 and 6).
            hud.SetMoney(0);
            hud.SetRank("Unknown Shop", 1);
            hud.SetCustomerStatus("No customers waiting");

            hud.InteractRequested += input.RequestInteract;
            hud.InventoryRequested += OnInventoryRequested;
            hud.UpgradesRequested += OnUpgradesRequested;
            hud.TravelRequested += OnTravelRequested;
            hud.PanelVisibilityChanged += OnPanelVisibilityChanged;
            interaction.FocusChanged += hud.SetFocus;
            counter.Used += hud.ShowCounterPanel;
            wired = true;

            hud.SetFocus(interaction.Focused);
        }

        private void OnDestroy()
        {
            if (!wired)
            {
                return;
            }

            if (hud != null)
            {
                hud.InteractRequested -= input.RequestInteract;
                hud.InventoryRequested -= OnInventoryRequested;
                hud.UpgradesRequested -= OnUpgradesRequested;
                hud.TravelRequested -= OnTravelRequested;
                hud.PanelVisibilityChanged -= OnPanelVisibilityChanged;
            }

            if (interaction != null) interaction.FocusChanged -= hud.SetFocus;
            if (counter != null) counter.Used -= hud.ShowCounterPanel;
        }

        // While a panel is open the world is paused: no looking around, no interacting.
        private void OnPanelVisibilityChanged(bool panelOpen)
        {
            cameraController.enabled = !panelOpen;
            interaction.enabled = !panelOpen;
        }

        private void OnInventoryRequested()
        {
            hud.ShowToast("The inventory opens once you have items to store.");
        }

        private void OnUpgradesRequested()
        {
            hud.ShowToast("Upgrades unlock once your shop starts earning coins.");
        }

        private void OnTravelRequested()
        {
            hud.ShowToast("The portal device isn't powered yet.");
        }
    }
}
