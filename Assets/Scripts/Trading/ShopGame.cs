using System;
using BlackMarket.Core;
using BlackMarket.Customers;
using BlackMarket.Economy;
using BlackMarket.Inventory;
using BlackMarket.Items;
using BlackMarket.Progression;

namespace BlackMarket.Trading
{
    public enum TradeError
    {
        None,
        NoVisitor,
        WrongVisitor,
        NoNegotiation,
        NegotiationOver,
        NotEnoughCoins,
        InventoryFull,
        ItemNotOwned,
        BuyerDoesNotWant,
        InvalidPrice,
        MaxLevel,
        /// <summary>The visitor answered but didn't agree (yet). Not an error the player caused.</summary>
        NoDeal
    }

    /// <summary>Outcome of an action that may move coins or items. Nothing changes unless Success.</summary>
    public readonly struct TradeResult
    {
        private TradeResult(TradeError error, long price, long profit)
        {
            Error = error;
            Price = price;
            Profit = profit;
        }

        public TradeError Error { get; }
        public bool Success => Error == TradeError.None;
        public long Price { get; }
        public long Profit { get; }

        public static TradeResult Ok(long price, long profit = 0) => new TradeResult(TradeError.None, price, profit);
        public static TradeResult Fail(TradeError error) => new TradeResult(error, 0, 0);

        public string ErrorMessage
        {
            get
            {
                switch (Error)
                {
                    case TradeError.NotEnoughCoins: return "You don't have enough coins.";
                    case TradeError.InventoryFull: return "Your storage is full. Sell something or upgrade storage.";
                    case TradeError.ItemNotOwned: return "That item isn't in your inventory.";
                    case TradeError.BuyerDoesNotWant: return "They aren't looking for that.";
                    case TradeError.InvalidPrice: return "That price isn't valid.";
                    case TradeError.MaxLevel: return "Already at the highest level.";
                    case TradeError.NegotiationOver: return "This conversation is over.";
                    default: return "That can't be done right now.";
                }
            }
        }
    }

    /// <summary>Result of the player proposing a price: how the visitor answered, and the deal if they agreed.</summary>
    public readonly struct ProposalResult
    {
        public ProposalResult(NegotiationResult answer, TradeResult trade)
        {
            Answer = answer;
            Trade = trade;
        }

        public NegotiationResult Answer { get; }

        /// <summary>The completed deal when Answer.Outcome is Accepted; otherwise a failure (NoDeal, or why the offer was refused before the visitor heard it).</summary>
        public TradeResult Trade { get; }
    }

    /// <summary>
    /// The shop's whole trading state and the only place that changes it: coins, stock, upgrades,
    /// the clock, the ledger and the visitor at the counter. Plain C#, created once by GameManager.
    ///
    /// Every money/stock change is validated completely first, then applied in one step, so a failed
    /// or repeated action can never leave the shop half-changed (e.g. coins taken but no item).
    /// </summary>
    public sealed class ShopGame
    {
        public const long DefaultStartingCoins = 2500;

        private readonly IRandom random;
        private readonly VisitorGenerator generator;

        public ShopGame(IRandom random, long startingCoins = DefaultStartingCoins)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            generator = new VisitorGenerator(random);
            Wallet = new Wallet(startingCoins);
            Upgrades = new ShopUpgrades();
            Inventory = new ShopInventory(Upgrades.InventoryCapacity);
            Ledger = new TradeLedger();
            Clock = new DayClock();
        }

        public Wallet Wallet { get; }
        public ShopInventory Inventory { get; }
        public ShopUpgrades Upgrades { get; }
        public TradeLedger Ledger { get; }
        public DayClock Clock { get; }

        public Visitor CurrentVisitor { get; private set; }

        /// <summary>The haggle in progress with the current visitor (null before an item is on the table).</summary>
        public Negotiation CurrentNegotiation { get; private set; }

        /// <summary>For buyers: the item the player put on the counter.</summary>
        public ItemInstance ItemOnOffer { get; private set; }

        /// <summary>Number of visitors so far this game.</summary>
        public int VisitCount { get; private set; }

        /// <summary>True when the current visitor's deal has been completed.</summary>
        public bool DealDone { get; private set; }

        // ------------------------------------------------------------------ Visits

        /// <summary>Brings in the next visitor. Returns null if someone is already at the counter or the shop is closed.</summary>
        public Visitor BeginVisit()
        {
            if (CurrentVisitor != null || Clock.IsClosingTime) return null;
            return BeginVisit(generator.Next(Inventory, Upgrades.ReputationLevel, VisitCount));
        }

        /// <summary>Brings in a specific visitor (scripted moments and tests). Returns null if someone is already here.</summary>
        public Visitor BeginVisit(Visitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));
            if (CurrentVisitor != null) return null;

            CurrentVisitor = visitor;
            VisitCount++;
            CurrentNegotiation = null;
            ItemOnOffer = null;
            DealDone = false;

            if (CurrentVisitor.Role == VisitorRole.Seller)
            {
                CurrentNegotiation = CurrentVisitor.StartNegotiation();
            }

            return CurrentVisitor;
        }

        /// <summary>The visitor leaves. Time passes.</summary>
        public void EndVisit()
        {
            if (CurrentVisitor == null) return;
            CurrentNegotiation?.Decline();
            CurrentVisitor = null;
            CurrentNegotiation = null;
            ItemOnOffer = null;
            Clock.Advance(random.Range(20, 45));
        }

        /// <summary>Player turns the visitor away without a deal.</summary>
        public void DeclineVisitor()
        {
            CurrentNegotiation?.Decline();
        }

        /// <summary>Picks a line for the current visitor to say.</summary>
        public string Say(DialogueMoment moment, long price)
        {
            Visitor v = CurrentVisitor;
            if (v == null) return string.Empty;

            if (moment == DialogueMoment.Greeting && v.Role == VisitorRole.Buyer && v.Request.SpecificItem == null)
            {
                moment = DialogueMoment.GreetingCategory;
            }

            var lines = DialogueLines.Pick(v.Personality.Lines, moment);
            string line = lines.Count > 0 ? lines[random.Range(0, lines.Count)] : string.Empty;
            return DialogueLines.Fill(line, v.SubjectName, price, v.CategoryName);
        }

        // ------------------------------------------------------------------ Appraisal

        /// <summary>The player's current estimate of an item's value.</summary>
        public ValueEstimate Estimate(ItemInstance item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            float spread = Upgrades.EstimateSpread * (item.IsInspected ? 0.5f : 1f);
            return ValueEstimate.Create(ValueEstimate.PerceivedValue(item), spread, StableOffset(item));
        }

        /// <summary>Inspects an item closely. Returns true if this revealed a hidden flaw.</summary>
        public bool Inspect(ItemInstance item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            bool newlyFound = item.HasHiddenFlaw && !item.FlawRevealed;
            item.Inspect();
            return newlyFound;
        }

        // Each item's estimate is off-centre by its own fixed amount, so re-opening the panel never changes it.
        private static float StableOffset(ItemInstance item)
        {
            unchecked
            {
                int h = item.Uid * 73856093 ^ item.Definition.Id.Length * 19349663;
                return ((h & 0x7fffffff) % 2001) / 1000f - 1f;
            }
        }

        // ------------------------------------------------------------------ Buying from sellers

        /// <summary>Checks the player could pay this price for the seller's item right now.</summary>
        public TradeError CanBuyAt(long price)
        {
            if (CurrentVisitor == null) return TradeError.NoVisitor;
            if (CurrentVisitor.Role != VisitorRole.Seller) return TradeError.WrongVisitor;
            if (CurrentNegotiation == null) return TradeError.NoNegotiation;
            if (CurrentNegotiation.IsOver || DealDone) return TradeError.NegotiationOver;
            if (price <= 0) return TradeError.InvalidPrice;
            if (!Wallet.CanAfford(price)) return TradeError.NotEnoughCoins;
            if (Inventory.IsFull) return TradeError.InventoryFull;
            return TradeError.None;
        }

        // ------------------------------------------------------------------ Selling to buyers

        /// <summary>Puts an item from stock on the counter for the current buyer and starts haggling over it.</summary>
        public TradeResult OfferItemToBuyer(ItemInstance item)
        {
            if (CurrentVisitor == null) return TradeResult.Fail(TradeError.NoVisitor);
            if (CurrentVisitor.Role != VisitorRole.Buyer) return TradeResult.Fail(TradeError.WrongVisitor);
            if (DealDone) return TradeResult.Fail(TradeError.NegotiationOver);
            if (!Inventory.Contains(item)) return TradeResult.Fail(TradeError.ItemNotOwned);
            if (!CurrentVisitor.Wants(item)) return TradeResult.Fail(TradeError.BuyerDoesNotWant);

            // Showing a different item doesn't reset the buyer's patience.
            int patienceLeft = CurrentNegotiation != null ? CurrentNegotiation.PatienceLeft : 0;
            ItemOnOffer = item;
            CurrentNegotiation = CurrentVisitor.StartNegotiation(item, patienceLeft);
            return TradeResult.Ok(CurrentNegotiation.CurrentPrice);
        }

        public TradeError CanSellAt(long price)
        {
            if (CurrentVisitor == null) return TradeError.NoVisitor;
            if (CurrentVisitor.Role != VisitorRole.Buyer) return TradeError.WrongVisitor;
            if (CurrentNegotiation == null || ItemOnOffer == null) return TradeError.NoNegotiation;
            if (CurrentNegotiation.IsOver || DealDone) return TradeError.NegotiationOver;
            if (price <= 0) return TradeError.InvalidPrice;
            if (!Inventory.Contains(ItemOnOffer)) return TradeError.ItemNotOwned;
            return TradeError.None;
        }

        // ------------------------------------------------------------------ Both directions

        /// <summary>Player accepts the visitor's current price. Validates, then completes the deal.</summary>
        public TradeResult AcceptCurrentPrice()
        {
            if (CurrentNegotiation == null) return TradeResult.Fail(TradeError.NoNegotiation);
            long price = CurrentNegotiation.CurrentPrice;

            TradeError error = IsBuying ? CanBuyAt(price) : CanSellAt(price);
            if (error != TradeError.None) return TradeResult.Fail(error);

            CurrentNegotiation.AcceptCurrentPrice();
            return Complete(price);
        }

        /// <summary>
        /// Player proposes a price. It is validated BEFORE the visitor hears it (so they can't accept an
        /// offer the player can't pay), then the visitor answers; if they accept, the deal completes.
        /// </summary>
        public ProposalResult Propose(long price)
        {
            if (CurrentNegotiation == null)
            {
                return new ProposalResult(default, TradeResult.Fail(TradeError.NoNegotiation));
            }

            TradeError error = IsBuying ? CanBuyAt(price) : CanSellAt(price);
            if (error != TradeError.None)
            {
                return new ProposalResult(new NegotiationResult(NegotiationOutcome.Countered, CurrentNegotiation.CurrentPrice, false),
                    TradeResult.Fail(error));
            }

            NegotiationResult answer = CurrentNegotiation.Propose(price);
            TradeResult trade = answer.Outcome == NegotiationOutcome.Accepted
                ? Complete(answer.Price)
                : TradeResult.Fail(TradeError.NoDeal);

            return new ProposalResult(answer, trade);
        }

        private bool IsBuying => CurrentVisitor != null && CurrentVisitor.Role == VisitorRole.Seller;

        // Applies an agreed deal. Called only after validation, so every step here is known to succeed.
        private TradeResult Complete(long price)
        {
            Visitor visitor = CurrentVisitor;
            if (IsBuying)
            {
                ItemInstance item = visitor.OfferedItem;
                Wallet.TrySpend(price);
                Inventory.TryAdd(item);
                item.MarkPurchased(price);
                DealDone = true;
                Ledger.Record(new TradeRecord(Clock.Day, Clock.TimeText, TradeKind.Bought, item.Definition.Name, price, 0));
                return TradeResult.Ok(price);
            }
            else
            {
                ItemInstance item = ItemOnOffer;
                long profit = price - item.PurchasePrice;
                Inventory.Remove(item);
                Wallet.Add(price);
                DealDone = true;
                Ledger.Record(new TradeRecord(Clock.Day, Clock.TimeText, TradeKind.Sold, item.Definition.Name, price, profit));
                return TradeResult.Ok(price, profit);
            }
        }

        // ------------------------------------------------------------------ Upgrades

        public TradeResult TryBuyUpgrade(UpgradeId id)
        {
            if (Upgrades.IsMaxed(id)) return TradeResult.Fail(TradeError.MaxLevel);
            long cost = Upgrades.NextCost(id);
            if (!Wallet.CanAfford(cost)) return TradeResult.Fail(TradeError.NotEnoughCoins);

            Wallet.TrySpend(cost);
            Upgrades.Increase(id);
            Inventory.IncreaseCapacity(Upgrades.InventoryCapacity);
            Ledger.Record(new TradeRecord(Clock.Day, Clock.TimeText, TradeKind.Upgrade,
                $"{ShopUpgrades.Definition(id).Name} level {Upgrades.Level(id)}", cost, 0));
            return TradeResult.Ok(cost);
        }

        // ------------------------------------------------------------------ Days

        /// <summary>Closes the shop for the day (only when no one is at the counter) and starts the next one.</summary>
        public DaySummary CloseDay()
        {
            if (CurrentVisitor != null) throw new InvalidOperationException("Can't close while a visitor is at the counter.");
            DaySummary summary = Ledger.SummaryForDay(Clock.Day);
            Clock.StartNextDay();
            return summary;
        }
    }
}
