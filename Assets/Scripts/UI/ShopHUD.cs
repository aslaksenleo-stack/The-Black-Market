using System;
using BlackMarket.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMarket.UI
{
    /// <summary>
    /// The shop screen: day and time (top left), coins and shop level (top right), the shortcuts
    /// (bottom left), a status line, short messages, the look-at prompt, and all trading panels.
    ///
    /// It only displays things and reports button presses through events. It never changes game data;
    /// the shop controllers connect it to the game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopHUD : MonoBehaviour
    {
        private const float ToastFadeSeconds = 0.3f;

        private RectTransform safeArea;
        private Text dayText;
        private Text timeText;
        private Text coinsText;
        private Pulse coinsPulse;
        private RectTransform coinsPill;
        private Text levelText;
        private Text statusText;
        private RectTransform statusPill;
        private Text toastText;
        private CanvasGroup toastGroup;
        private float toastHideTime;
        private Image crosshair;
        private Button interactButton;
        private Text interactLabel;
        private GameObject nav;
        private int openModals;

        public TradePanel Trade { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public UpgradesPanel Upgrades { get; private set; }
        public LedgerPanel Ledger { get; private set; }
        public DaySummaryPanel DaySummary { get; private set; }
        public InspectOverlay Inspect { get; private set; }

        public event Action InteractRequested;
        public event Action InventoryRequested;
        public event Action UpgradesRequested;
        public event Action LedgerRequested;

        /// <summary>Raised with true when a full-screen panel opens and false when the last one closes.</summary>
        public event Action<bool> ModalChanged;

        public bool IsModalOpen => openModals > 0;

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

        public void SetDay(int day, string time)
        {
            dayText.text = "Day " + day;
            timeText.text = time;
        }

        public void SetCoins(long coins)
        {
            coinsText.text = Items.ItemText.Coins(coins);
        }

        /// <summary>Coin counter bounces and a "+600" / "−400" floats beside it.</summary>
        public void ShowCoinChange(long delta)
        {
            if (delta == 0) return;
            coinsPulse.Play(delta > 0 ? 0.14f : 0.08f);
            Text pop = UIFactory.CreateText("CoinPop", safeArea, (delta > 0 ? "+" : "−") + Items.ItemText.Coins(Math.Abs(delta)),
                UITheme.FontSizeLarge, delta > 0 ? UITheme.PositiveText : UITheme.NegativeText, TextAnchor.MiddleRight);
            UIFactory.Place(pop.rectTransform, UIFactory.TopRight, new Vector2(-UITheme.ScreenMargin - 20f, -UITheme.ScreenMargin - 170f), new Vector2(360f, 60f));
            var outline = pop.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);
            FloatAway.Attach(pop.gameObject);
        }

        public void SetShopInfo(int level, int stock, int capacity)
        {
            levelText.text = $"Shop Lv. {level}   ·   Stock {stock}/{capacity}";
        }

        public void SetStatus(string status)
        {
            statusText.text = status ?? string.Empty;
            statusPill.gameObject.SetActive(!string.IsNullOrEmpty(status));
        }

        public void ShowToast(string message, float seconds = 2.6f)
        {
            toastText.text = message;
            toastGroup.alpha = 1f;
            toastHideTime = Time.unscaledTime + seconds;
        }

        /// <summary>Shows the look-at prompt (labelled with the target's action), or hides it when null.</summary>
        public void SetFocus(IInteractable target)
        {
            if (interactButton == null || crosshair == null) return; // HUD already destroyed (scene unloading)

            bool hasTarget = target != null;
            interactButton.gameObject.SetActive(hasTarget);
            if (hasTarget) interactLabel.text = target.Prompt;
            crosshair.color = hasTarget ? UITheme.Gold : UITheme.CrosshairIdle;
            crosshair.rectTransform.sizeDelta = hasTarget ? new Vector2(18f, 18f) : new Vector2(10f, 10f);
        }

        /// <summary>Hides the crosshair, prompt and shortcuts (e.g. while inspecting an item).</summary>
        public void SetWorldControlsVisible(bool visible)
        {
            crosshair.gameObject.SetActive(visible);
            nav.SetActive(visible);
            if (!visible) interactButton.gameObject.SetActive(false);
        }

        public void SetNavigationEnabled(bool enabled)
        {
            foreach (Button b in nav.GetComponentsInChildren<Button>(true))
            {
                UIFactory.SetInteractable(b, enabled);
            }
        }

        // ------------------------------------------------------------------ Layout

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("ShopHUDCanvas", transform, 10);

            safeArea = UIFactory.CreateRect("SafeArea", canvas.transform);
            UIFactory.Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            float m = UITheme.ScreenMargin;

            // Crosshair (behind everything else)
            crosshair = UIFactory.CreateImage("Crosshair", safeArea, UITheme.CrosshairIdle, UISprites.Circle);
            crosshair.raycastTarget = false;
            UIFactory.Place(crosshair.rectTransform, UIFactory.Centre, Vector2.zero, new Vector2(10f, 10f));

            // Day and time, top left
            RectTransform dayPill = UIFactory.CreatePanel("DayPill", safeArea);
            UIFactory.Place(dayPill, UIFactory.TopLeft, new Vector2(m, -m), new Vector2(380f, 86f));
            Image calendar = UIFactory.CreateIcon("Calendar", dayPill, Icon.Calendar, UITheme.Gold);
            UIFactory.Place(calendar.rectTransform, UIFactory.MiddleLeft, new Vector2(26f, 0f), new Vector2(46f, 46f));
            calendar.rectTransform.pivot = new Vector2(0f, 0.5f);
            dayText = UIFactory.CreateText("Day", dayPill, "Day 1", UITheme.FontSizeBody, UITheme.Text, TextAnchor.MiddleLeft);
            UIFactory.Place(dayText.rectTransform, UIFactory.MiddleLeft, new Vector2(88f, 0f), new Vector2(150f, 60f));
            dayText.rectTransform.pivot = new Vector2(0f, 0.5f);
            Image divider = UIFactory.CreateImage("Divider", dayPill, UITheme.PanelEdge);
            UIFactory.Place(divider.rectTransform, UIFactory.MiddleLeft, new Vector2(232f, 0f), new Vector2(2f, 46f));
            timeText = UIFactory.CreateText("Time", dayPill, "09:00", UITheme.FontSizeBody, UITheme.Text, TextAnchor.MiddleRight);
            UIFactory.Place(timeText.rectTransform, UIFactory.MiddleRight, new Vector2(-28f, 0f), new Vector2(120f, 60f));
            timeText.rectTransform.pivot = new Vector2(1f, 0.5f);

            // Coins, top right, with shop level and stock underneath
            coinsPill = UIFactory.CreatePanel("CoinsPill", safeArea);
            UIFactory.Place(coinsPill, UIFactory.TopRight, new Vector2(-m, -m), new Vector2(380f, 86f));
            coinsPulse = coinsPill.gameObject.AddComponent<Pulse>();
            Image coin = UIFactory.CreateIcon("Coin", coinsPill, Icon.Coin, UITheme.Gold);
            UIFactory.Place(coin.rectTransform, UIFactory.MiddleLeft, new Vector2(24f, 0f), new Vector2(54f, 54f));
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coinsText = UIFactory.CreateText("Coins", coinsPill, "0", UITheme.FontSizeLarge + 4, UITheme.Text, TextAnchor.MiddleRight);
            UIFactory.Place(coinsText.rectTransform, UIFactory.MiddleRight, new Vector2(-28f, 0f), new Vector2(270f, 70f));
            coinsText.rectTransform.pivot = new Vector2(1f, 0.5f);

            RectTransform levelChip = UIFactory.CreatePanel("ShopInfo", safeArea, UITheme.Panel, false);
            UIFactory.Place(levelChip, UIFactory.TopRight, new Vector2(-m, -m - 96f), new Vector2(380f, 54f));
            levelText = UIFactory.CreateText("Text", levelChip, string.Empty, UITheme.FontSizeTiny, UITheme.TextMuted, TextAnchor.MiddleCenter);
            UIFactory.Stretch(levelText.rectTransform, 4f);

            // Status line, top centre
            statusPill = UIFactory.CreatePanel("Status", safeArea, UITheme.Panel, false);
            UIFactory.Place(statusPill, UIFactory.TopCentre, new Vector2(0f, -m), new Vector2(640f, 64f));
            statusText = UIFactory.CreateText("Text", statusPill, string.Empty, UITheme.FontSizeSmall, UITheme.TextMuted, TextAnchor.MiddleCenter, UITheme.BodyFont);
            UIFactory.Stretch(statusText.rectTransform, 8f);

            // Shortcuts, bottom left
            nav = UIFactory.CreateRect("Navigation", safeArea).gameObject;
            var navRect = (RectTransform)nav.transform;
            UIFactory.Place(navRect, UIFactory.BottomLeft, new Vector2(m, m), new Vector2(3f * 196f + 2f * 16f, 132f));
            CreateNav("Inventory", Icon.Box, 0, () => InventoryRequested?.Invoke());
            CreateNav("Upgrades", Icon.ArrowUp, 1, () => UpgradesRequested?.Invoke());
            CreateNav("Ledger", Icon.Book, 2, () => LedgerRequested?.Invoke());

            // Look-at prompt, just below the crosshair
            interactButton = UIFactory.CreateButton("Interact", safeArea, "Use", () => InteractRequested?.Invoke(), ButtonStyle.Ghost, null, UITheme.FontSizeSmall);
            UIFactory.Place((RectTransform)interactButton.transform, UIFactory.Centre, new Vector2(0f, -86f), new Vector2(380f, 76f));
            interactLabel = interactButton.GetComponentInChildren<Text>();

            // Trade interface
            Trade = gameObject.AddComponent<TradePanel>();
            Trade.Build(safeArea);

            Inspect = gameObject.AddComponent<InspectOverlay>();
            Inspect.Build(safeArea);

            // Toast, under the status line
            RectTransform toast = UIFactory.CreatePanel("Toast", safeArea, UITheme.PanelSolid);
            UIFactory.Place(toast, UIFactory.TopCentre, new Vector2(0f, -m - 80f), new Vector2(900f, 92f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            toastText = UIFactory.CreateText("Message", toast, string.Empty, UITheme.FontSizeSmall + 2, UITheme.Text, TextAnchor.MiddleCenter);
            UIFactory.Stretch(toastText.rectTransform, 16f);

            // Full-screen panels (full screen, not limited to the safe area, so the dim covers the notch)
            Inventory = AddModal<InventoryPanel>(canvas, new Vector2(1500f, 880f));
            Upgrades = AddModal<UpgradesPanel>(canvas, new Vector2(1500f, 760f));
            Ledger = AddModal<LedgerPanel>(canvas, new Vector2(1320f, 840f));
            DaySummary = AddModal<DaySummaryPanel>(canvas, new Vector2(960f, 720f), false);
        }

        private void CreateNav(string label, Icon icon, int index, Action onClick)
        {
            Button b = UIFactory.CreateTileButton(label + "Button", nav.transform, label, icon, onClick);
            UIFactory.Place((RectTransform)b.transform, UIFactory.BottomLeft, new Vector2(index * (196f + 16f), 0f), new Vector2(196f, 132f));
        }

        private T AddModal<T>(Canvas canvas, Vector2 size, bool closable = true) where T : ModalPanel
        {
            var panel = gameObject.AddComponent<T>();
            panel.Build((RectTransform)canvas.transform, size, closable);
            panel.VisibilityChanged += OnModalVisibility;
            return panel;
        }

        private void OnModalVisibility(ModalPanel panel, bool open)
        {
            int before = openModals;
            openModals = Mathf.Max(0, openModals + (open ? 1 : -1));
            if ((before == 0) != (openModals == 0))
            {
                ModalChanged?.Invoke(openModals > 0);
            }
        }
    }
}
