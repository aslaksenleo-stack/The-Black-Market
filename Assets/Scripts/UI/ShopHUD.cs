using System;
using BlackMarket.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    /// <summary>
    /// The main shop screen (GDD 8.1): money, shop rank, customer status, Inventory/Upgrades/Travel
    /// shortcuts, a contextual Interact button, a centre crosshair, short messages ("toasts"),
    /// and the counter panel.
    ///
    /// It only displays things and reports button presses through events. It never changes game data;
    /// ShopSceneController connects it to the gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopHUD : MonoBehaviour
    {
        private const float ToastFadeSeconds = 0.25f;
        private static readonly Vector2 CrosshairIdleSize = new Vector2(14f, 14f);
        private static readonly Vector2 CrosshairFocusSize = new Vector2(26f, 26f);

        private Text moneyText;
        private Text rankText;
        private Text customerText;
        private Text interactLabel;
        private Text toastText;
        private GameObject interactButton;
        private GameObject counterPanel;
        private Image crosshair;
        private CanvasGroup toastGroup;
        private float toastHideTime;

        public event Action InteractRequested;
        public event Action InventoryRequested;
        public event Action UpgradesRequested;
        public event Action TravelRequested;

        /// <summary>Raised with true when a panel opens and false when it closes.</summary>
        public event Action<bool> PanelVisibilityChanged;

        public bool IsPanelOpen => counterPanel != null && counterPanel.activeSelf;

        private void Awake()
        {
            UIFactory.EnsureEventSystem();
            Build();
            SetFocus(null);
        }

        private void Update()
        {
            if (toastGroup.alpha > 0f && Time.unscaledTime >= toastHideTime)
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime / ToastFadeSeconds);
            }
        }

        // ------------------------------------------------------------------ Public API

        public void SetMoney(long coins)
        {
            moneyText.text = coins.ToString("N0") + " coins";
        }

        public void SetRank(string shopName, int rank)
        {
            rankText.text = $"{shopName}  ·  Rank {rank}";
        }

        public void SetCustomerStatus(string status)
        {
            customerText.text = status;
        }

        /// <summary>Shows the interact button (labelled with the target's prompt) or hides it when null.</summary>
        public void SetFocus(IInteractable target)
        {
            if (interactButton == null || crosshair == null)
            {
                return; // HUD already destroyed (scene unloading).
            }

            bool hasTarget = target != null;
            interactButton.SetActive(hasTarget);
            if (hasTarget)
            {
                interactLabel.text = target.Prompt;
            }

            crosshair.color = hasTarget ? UITheme.Accent : UITheme.CrosshairIdle;
            crosshair.rectTransform.sizeDelta = hasTarget ? CrosshairFocusSize : CrosshairIdleSize;
        }

        public void ShowToast(string message, float seconds = 2.2f)
        {
            toastText.text = message;
            toastGroup.alpha = 1f;
            toastHideTime = Time.unscaledTime + seconds;
        }

        public void ShowCounterPanel()
        {
            if (IsPanelOpen)
            {
                return;
            }

            counterPanel.SetActive(true);
            PanelVisibilityChanged?.Invoke(true);
        }

        public void HideCounterPanel()
        {
            if (!IsPanelOpen)
            {
                return;
            }

            counterPanel.SetActive(false);
            PanelVisibilityChanged?.Invoke(false);
        }

        // ------------------------------------------------------------------ Layout

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("ShopHUDCanvas", transform, 10);

            RectTransform safeArea = UIFactory.CreateRect("SafeArea", canvas.transform);
            UIFactory.Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            float margin = UITheme.ScreenMargin;

            // Money and rank, top-left.
            RectTransform status = UIFactory.CreatePanel("StatusPanel", safeArea);
            UIFactory.Place(status, UIFactory.TopLeft, new Vector2(margin, -margin), new Vector2(540f, 150f));
            moneyText = UIFactory.CreateText("Money", status, string.Empty, UITheme.FontSizeTitle, UITheme.Text, TextAnchor.UpperLeft);
            UIFactory.Place(moneyText.rectTransform, UIFactory.TopLeft, new Vector2(28f, -18f), new Vector2(490f, 64f));
            rankText = UIFactory.CreateText("Rank", status, string.Empty, UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.UpperLeft);
            UIFactory.Place(rankText.rectTransform, UIFactory.TopLeft, new Vector2(28f, -90f), new Vector2(490f, 44f));

            // Customer status, top-centre.
            RectTransform customer = UIFactory.CreatePanel("CustomerPanel", safeArea);
            UIFactory.Place(customer, UIFactory.TopCentre, new Vector2(0f, -margin), new Vector2(600f, 90f));
            customerText = UIFactory.CreateText("CustomerStatus", customer, string.Empty, UITheme.FontSizeSmall, UITheme.Text, TextAnchor.MiddleCenter);
            UIFactory.Stretch(customerText.rectTransform, 12f);

            // Shortcuts, bottom-left.
            Vector2 navSize = new Vector2(270f, UITheme.ButtonHeight);
            float x = margin;
            CreateNavButton("InventoryButton", "Inventory", safeArea, ref x, navSize, () => InventoryRequested?.Invoke());
            CreateNavButton("UpgradesButton", "Upgrades", safeArea, ref x, navSize, () => UpgradesRequested?.Invoke());
            CreateNavButton("TravelButton", "Travel", safeArea, ref x, navSize, () => TravelRequested?.Invoke());

            // Contextual interact button, bottom-right (thumb position).
            Button interact = UIFactory.CreateButton("InteractButton", safeArea, "Use", () => InteractRequested?.Invoke());
            UIFactory.Place((RectTransform)interact.transform, UIFactory.BottomRight, new Vector2(-margin, margin), new Vector2(380f, 160f));
            interactButton = interact.gameObject;
            interactLabel = interact.GetComponentInChildren<Text>();

            // Crosshair: shows what the interact button will use.
            crosshair = UIFactory.CreateImage("Crosshair", safeArea, UITheme.CrosshairIdle);
            crosshair.raycastTarget = false;
            UIFactory.Place(crosshair.rectTransform, UIFactory.Centre, Vector2.zero, CrosshairIdleSize);

            // Toast message, under the customer status.
            RectTransform toast = UIFactory.CreatePanel("Toast", safeArea);
            UIFactory.Place(toast, UIFactory.TopCentre, new Vector2(0f, -150f), new Vector2(940f, 100f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            toastText = UIFactory.CreateText("Message", toast, string.Empty, UITheme.FontSizeSmall, UITheme.Text, TextAnchor.MiddleCenter);
            UIFactory.Stretch(toastText.rectTransform, 16f);

            // Counter panel, above everything else. The dim background covers the full screen
            // (not just the safe area) and blocks touches to the world behind it.
            counterPanel = BuildCounterPanel(canvas.transform);
            counterPanel.SetActive(false);
        }

        private static void CreateNavButton(string name, string label, Transform parent, ref float x, Vector2 size, Action onClick)
        {
            Button button = UIFactory.CreateButton(name, parent, label, onClick);
            UIFactory.Place((RectTransform)button.transform, UIFactory.BottomLeft, new Vector2(x, UITheme.ScreenMargin), size);
            x += size.x + UITheme.Gap;
        }

        private GameObject BuildCounterPanel(Transform parent)
        {
            Image dim = UIFactory.CreateImage("CounterPanel", parent, UITheme.Dim);
            UIFactory.Stretch(dim.rectTransform);

            RectTransform window = UIFactory.CreatePanel("Window", dim.transform);
            UIFactory.Place(window, UIFactory.Centre, Vector2.zero, new Vector2(1040f, 580f));

            Text title = UIFactory.CreateText("Title", window, "Shop Counter", UITheme.FontSizeTitle, UITheme.Text, TextAnchor.UpperLeft);
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, new Vector2(48f, -40f), new Vector2(940f, 70f));

            Text body = UIFactory.CreateText(
                "Body", window,
                "No customers are waiting.\n\nThis is where you will greet visitors, check what they want and agree on a price. " +
                "Customers start arriving in the next update.",
                UITheme.FontSizeBody, UITheme.TextMuted, TextAnchor.UpperLeft);
            UIFactory.Place(body.rectTransform, UIFactory.TopLeft, new Vector2(48f, -130f), new Vector2(940f, 260f));

            Button close = UIFactory.CreateButton("CloseButton", window, "Close", HideCounterPanel);
            UIFactory.Place((RectTransform)close.transform, UIFactory.BottomRight, new Vector2(-40f, 40f), new Vector2(280f, 120f));

            return dim.gameObject;
        }
    }
}
