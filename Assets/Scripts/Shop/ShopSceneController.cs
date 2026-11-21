using System.Collections.Generic;
using BlackMarket.Audio;
using BlackMarket.Core;
using BlackMarket.Economy;
using BlackMarket.Interaction;
using BlackMarket.Items;
using BlackMarket.Player;
using BlackMarket.Progression;
using BlackMarket.Trading;
using BlackMarket.UI;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Connects the shop scene's pieces: the player's camera and interaction, the HUD and its panels,
    /// the counter objects (bell, ledger), the visit director, and the persistent ShopGame.
    /// Holds no game data itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopSceneController : MonoBehaviour
    {
        [SerializeField] private MobileInputController input;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private InteractionController interaction;
        [SerializeField] private ItemInspector inspector;
        [SerializeField] private ShopHUD hud;
        [SerializeField] private VisitDirector director;
        [SerializeField] private ShopAnchors anchors;
        [SerializeField] private ShopDoor door;
        [SerializeField] private CounterInteractable bell;
        [SerializeField] private CounterInteractable ledger;

        private ShopGame game;
        private bool wired;
        private bool modalOpen;
        private bool inspecting;

        private void Start()
        {
            if (input == null) input = FindFirstObjectByType<MobileInputController>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (interaction == null) interaction = FindFirstObjectByType<InteractionController>();
            if (inspector == null) inspector = FindFirstObjectByType<ItemInspector>();
            if (hud == null) hud = FindFirstObjectByType<ShopHUD>();
            if (director == null) director = FindFirstObjectByType<VisitDirector>();
            if (anchors == null) anchors = FindFirstObjectByType<ShopAnchors>();
            if (door == null) door = FindFirstObjectByType<ShopDoor>();
            FindCounterObjects();

            if (input == null || cameraController == null || interaction == null || inspector == null || hud == null ||
                director == null || anchors == null || door == null)
            {
                Debug.LogError("[ShopSceneController] The shop scene is missing a required object. Rebuild it with " +
                               "'The Black Market > Setup > 1. Create Scenes'.");
                enabled = false;
                return;
            }

            ConfigureQuality();

            if (!ServiceRegistry.TryGet(out game))
            {
                // Only happens if the scene runs without the GameManager (shouldn't in normal play).
                game = new ShopGame(new SeededRandom(System.Environment.TickCount));
            }

            hud.InteractRequested += input.RequestInteract;
            hud.InventoryRequested += OpenInventory;
            hud.UpgradesRequested += OpenUpgrades;
            hud.LedgerRequested += OpenLedger;
            hud.ModalChanged += OnModalChanged;
            interaction.FocusChanged += hud.SetFocus;
            director.InspectingChanged += OnInspectingChanged;
            if (bell != null) bell.Used += director.RingBell;
            if (ledger != null) ledger.Used += OpenLedger;
            UIFactory.Clicked += PlayClick;

            game.Wallet.Changed += OnCoinsChanged;
            game.Clock.Changed += RefreshClock;
            game.Inventory.Changed += RefreshShopInfo;
            game.Upgrades.LevelChanged += OnUpgradeChanged;
            wired = true;

            hud.SetCoins(game.Wallet.Coins);
            RefreshClock();
            RefreshShopInfo();
            hud.SetFocus(interaction.Focused);

            director.Initialise(game, hud, anchors, door, inspector, cameraController, cameraController.GetComponentInChildren<Camera>().transform);
        }

        private void OnDestroy()
        {
            UIFactory.Clicked -= PlayClick;
            if (!wired) return;

            // ShopGame outlives the scene, so its events must be released.
            game.Wallet.Changed -= OnCoinsChanged;
            game.Clock.Changed -= RefreshClock;
            game.Inventory.Changed -= RefreshShopInfo;
            game.Upgrades.LevelChanged -= OnUpgradeChanged;

            if (hud != null)
            {
                hud.InteractRequested -= input.RequestInteract;
                hud.InventoryRequested -= OpenInventory;
                hud.UpgradesRequested -= OpenUpgrades;
                hud.LedgerRequested -= OpenLedger;
                hud.ModalChanged -= OnModalChanged;
            }

            if (interaction != null && hud != null) interaction.FocusChanged -= hud.SetFocus;
            if (director != null) director.InspectingChanged -= OnInspectingChanged;
            if (bell != null && director != null) bell.Used -= director.RingBell;
            if (ledger != null) ledger.Used -= OpenLedger;
        }

        private void FindCounterObjects()
        {
            foreach (CounterInteractable c in FindObjectsByType<CounterInteractable>(FindObjectsSortMode.None))
            {
                if (bell == null && c.name == "Bell") bell = c;
                if (ledger == null && c.name == "Ledger") ledger = c;
            }
        }

        private static void ConfigureQuality()
        {
            // The lantern lights are what make the shop feel warm, so let a few of them light per-pixel.
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            QualitySettings.shadowDistance = 14f;
            Application.targetFrameRate = 60;
        }

        // ------------------------------------------------------------------ Pausing the world

        private void OnModalChanged(bool open)
        {
            modalOpen = open;
            ApplyWorldInput();
        }

        private void OnInspectingChanged(bool active)
        {
            inspecting = active;
            ApplyWorldInput();
        }

        // Looking around and tapping the world are paused while a full-screen panel is open or an item is being inspected.
        private void ApplyWorldInput()
        {
            cameraController.enabled = !modalOpen && !inspecting;
            interaction.enabled = !modalOpen && !inspecting;
        }

        // ------------------------------------------------------------------ HUD refresh

        private void OnCoinsChanged(long balance, long delta)
        {
            hud.SetCoins(balance);
        }

        private void RefreshClock()
        {
            hud.SetDay(game.Clock.Day, game.Clock.TimeText);
        }

        private void RefreshShopInfo()
        {
            hud.SetShopInfo(game.Upgrades.ShopLevel, game.Inventory.Count, game.Inventory.Capacity);
        }

        private void OnUpgradeChanged(UpgradeId id, int level)
        {
            RefreshShopInfo();
        }

        private static void PlayClick()
        {
            SoundKit.Play(Sound.Click, 0.6f);
        }

        // ------------------------------------------------------------------ Panels

        private void OpenInventory()
        {
            var entries = new List<InventoryEntryView>();
            foreach (ItemInstance item in game.Inventory.Items) entries.Add(director.EntryFor(item));

            long invested = 0;
            foreach (ItemInstance item in game.Inventory.Items) invested += item.PurchasePrice;

            hud.Inventory.Open("Storage",
                $"{game.Inventory.Count} of {game.Inventory.Capacity} slots used  ·  {ItemText.Coins(invested)} coins of stock",
                entries, null, "Your storage is empty.\nBuy items from sellers who visit the counter, then sell them to buyers for a profit.");
        }

        private void OpenUpgrades()
        {
            hud.Upgrades.Open(CoinsText(), BuildUpgradeViews());
        }

        private string CoinsText() => ItemText.Coins(game.Wallet.Coins) + " coins";

        private List<UpgradeView> BuildUpgradeViews()
        {
            var views = new List<UpgradeView>();
            foreach (UpgradeDefinition def in ShopUpgrades.All)
            {
                int level = game.Upgrades.Level(def.Id);
                bool maxed = game.Upgrades.IsMaxed(def.Id);
                long cost = game.Upgrades.NextCost(def.Id);
                UpgradeId id = def.Id;
                views.Add(new UpgradeView
                {
                    Name = def.Name,
                    Icon = id == UpgradeId.AppraisalLens ? Icon.Magnifier : id == UpgradeId.Storage ? Icon.Box : Icon.Star,
                    Level = level,
                    MaxLevel = def.MaxLevel,
                    Current = def.LevelEffects[level],
                    Next = maxed ? null : def.LevelEffects[level + 1],
                    ButtonLabel = maxed ? "Fully upgraded" : game.Wallet.CanAfford(cost) ? "Upgrade  ·  " + ItemText.Coins(cost) : "Need " + ItemText.Coins(cost),
                    CanBuy = !maxed && game.Wallet.CanAfford(cost),
                    Buy = () => BuyUpgrade(id)
                });
            }

            return views;
        }

        private void BuyUpgrade(UpgradeId id)
        {
            TradeResult result = game.TryBuyUpgrade(id);
            if (!result.Success)
            {
                hud.ShowToast(result.ErrorMessage);
                return;
            }

            SoundKit.Play(Sound.Upgrade);
            hud.ShowCoinChange(-result.Price);
            hud.ShowToast($"{ShopUpgrades.Definition(id).Name} upgraded: {ShopUpgrades.Definition(id).LevelEffects[game.Upgrades.Level(id)]}.");
            hud.Upgrades.Refresh(CoinsText(), BuildUpgradeViews());
        }

        private void OpenLedger()
        {
            DaySummary today = game.Ledger.SummaryForDay(game.Clock.Day);
            string profitToday = today.Profit >= 0 ? UIFactory.Colored("+" + ItemText.Coins(today.Profit), UITheme.PositiveText)
                : UIFactory.Colored("−" + ItemText.Coins(-today.Profit), UITheme.NegativeText);
            string profitAll = game.Ledger.TotalProfit >= 0 ? UIFactory.Colored("+" + ItemText.Coins(game.Ledger.TotalProfit), UITheme.PositiveText)
                : UIFactory.Colored("−" + ItemText.Coins(-game.Ledger.TotalProfit), UITheme.NegativeText);

            string summary =
                UIFactory.Gold("TODAY") + "\n" +
                $"Bought  {today.Purchases}   ·   Sold  {today.Sales}\n" +
                $"Spent  {ItemText.Coins(today.Spent)}\n" +
                $"Earned  {ItemText.Coins(today.Earned)}\n" +
                $"Profit  {profitToday}\n\n" +
                UIFactory.Gold("ALL TIME") + "\n" +
                $"Deals  {game.Ledger.TotalPurchases + game.Ledger.TotalSales}\n" +
                $"Profit  {profitAll}";

            var lines = new List<LedgerLine>();
            IReadOnlyList<TradeRecord> records = game.Ledger.Records;
            for (int i = records.Count - 1; i >= 0 && lines.Count < 9; i--)
            {
                TradeRecord r = records[i];
                string verb = r.Kind == TradeKind.Bought ? "Bought" : r.Kind == TradeKind.Sold ? "Sold" : "Upgrade:";
                string note = r.Kind == TradeKind.Sold ? $"  (profit {(r.Profit >= 0 ? "+" : "−")}{ItemText.Coins(System.Math.Abs(r.Profit))})" : string.Empty;
                bool incoming = r.Kind == TradeKind.Sold;
                lines.Add(new LedgerLine
                {
                    Time = $"Day {r.Day}  {r.Time}",
                    Text = $"{verb} {r.Description}{note}",
                    Amount = (incoming ? "+" : "−") + ItemText.Coins(r.Amount),
                    AmountColor = incoming ? UITheme.PositiveText : UITheme.TextMuted
                });
            }

            hud.Ledger.Open($"Day {game.Clock.Day}  ·  {CoinsText()}", summary, lines);
        }
    }
}
