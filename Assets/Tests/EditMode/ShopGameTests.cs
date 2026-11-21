using BlackMarket.Customers;
using BlackMarket.Economy;
using BlackMarket.Items;
using BlackMarket.Progression;
using BlackMarket.Trading;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    /// <summary>Buying, selling, profit and upgrades through the ShopGame facade.</summary>
    public sealed class ShopGameTests
    {
        private static ItemDefinition Astrolabe => ItemCatalog.Get("brass_astrolabe");

        private static ShopGame NewGame(long coins = 2000) => new ShopGame(new SeededRandom(42), coins);

        private static Visitor SellerOf(ItemInstance item, long ask, long minimum) =>
            Visitor.CreateSeller(PersonalityCatalog.FairTrader, "peddler", 3, item, ask, minimum);

        private static Visitor BuyerOf(ItemDefinition wanted, long budget = 10000, float valuation = 1.2f) =>
            Visitor.CreateBuyer(PersonalityCatalog.Collector, "elder", 3, BuyerRequest.ForItem(wanted), budget, valuation, 0.7f);

        private static ItemInstance BuyOne(ShopGame game, long price)
        {
            var item = new ItemInstance(Astrolabe, ItemCondition.Good);
            game.BeginVisit(SellerOf(item, price, price));
            Assert.IsTrue(game.AcceptCurrentPrice().Success);
            game.EndVisit();
            return item;
        }

        [Test]
        public void AcceptingAsk_MovesCoinsAndItem_Together()
        {
            ShopGame game = NewGame();
            var item = new ItemInstance(Astrolabe, ItemCondition.Good);
            game.BeginVisit(SellerOf(item, 400, 300));

            TradeResult result = game.AcceptCurrentPrice();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1600, game.Wallet.Coins);
            Assert.IsTrue(game.Inventory.Contains(item));
            Assert.AreEqual(400, item.PurchasePrice);
            Assert.AreEqual(1, game.Ledger.TotalPurchases);
        }

        [Test]
        public void ExactMoney_CanBuy()
        {
            ShopGame game = NewGame(400);
            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 400, 300));
            Assert.IsTrue(game.AcceptCurrentPrice().Success);
            Assert.AreEqual(0, game.Wallet.Coins);
        }

        [Test]
        public void OneCoinShort_ChangesNothing()
        {
            ShopGame game = NewGame(399);
            var item = new ItemInstance(Astrolabe, ItemCondition.Good);
            game.BeginVisit(SellerOf(item, 400, 300));

            TradeResult result = game.AcceptCurrentPrice();

            Assert.AreEqual(TradeError.NotEnoughCoins, result.Error);
            Assert.AreEqual(399, game.Wallet.Coins);
            Assert.AreEqual(0, game.Inventory.Count);
            Assert.IsFalse(game.CurrentNegotiation.IsOver, "A refused purchase must not end the haggle.");
        }

        [Test]
        public void UnaffordableOffer_IsRefusedBeforeTheSellerHearsIt()
        {
            ShopGame game = NewGame(100);
            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 400, 300));

            ProposalResult r = game.Propose(350);

            Assert.AreEqual(TradeError.NotEnoughCoins, r.Trade.Error);
            Assert.AreEqual(3, game.CurrentNegotiation.PatienceLeft, "No patience should be spent.");
        }

        [Test]
        public void RepeatedBuy_OnlyBuysOnce()
        {
            ShopGame game = NewGame();
            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 400, 300));

            Assert.IsTrue(game.AcceptCurrentPrice().Success);
            Assert.IsFalse(game.AcceptCurrentPrice().Success);
            Assert.AreEqual(1600, game.Wallet.Coins);
            Assert.AreEqual(1, game.Inventory.Count);
        }

        [Test]
        public void FullInventory_BlocksPurchase()
        {
            ShopGame game = NewGame(100000);
            for (int i = 0; i < game.Inventory.Capacity; i++) BuyOne(game, 10);

            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 10, 10));
            Assert.AreEqual(TradeError.InventoryFull, game.AcceptCurrentPrice().Error);
        }

        [Test]
        public void HaggledPurchase_UsesAgreedPrice()
        {
            ShopGame game = NewGame();
            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 600, 450));

            ProposalResult r = game.Propose(470);

            Assert.AreEqual(NegotiationOutcome.Accepted, r.Answer.Outcome);
            Assert.IsTrue(r.Trade.Success);
            Assert.AreEqual(2000 - 470, game.Wallet.Coins);
        }

        [Test]
        public void RejectedOffer_ReportsNoDeal_NotSuccess()
        {
            ShopGame game = NewGame();
            game.BeginVisit(SellerOf(new ItemInstance(Astrolabe, ItemCondition.Good), 600, 450));

            ProposalResult r = game.Propose(400);

            Assert.AreNotEqual(NegotiationOutcome.Accepted, r.Answer.Outcome);
            Assert.AreEqual(TradeError.NoDeal, r.Trade.Error);
            Assert.AreEqual(2000, game.Wallet.Coins);
        }

        [Test]
        public void FullLoop_BuyLowSellHigh_RecordsProfit()
        {
            ShopGame game = NewGame();
            ItemInstance item = BuyOne(game, 300);

            game.BeginVisit(BuyerOf(Astrolabe));
            Assert.IsTrue(game.OfferItemToBuyer(item).Success);
            ProposalResult r = game.Propose(500); // value 520 * 1.2 * 1.0 = 624 max: accepted

            Assert.IsTrue(r.Trade.Success);
            Assert.AreEqual(500, r.Trade.Price);
            Assert.AreEqual(200, r.Trade.Profit);
            Assert.AreEqual(2000 - 300 + 500, game.Wallet.Coins);
            Assert.IsFalse(game.Inventory.Contains(item));
            Assert.AreEqual(200, game.Ledger.TotalProfit);
        }

        [Test]
        public void SellingAtALoss_RecordsNegativeProfit()
        {
            ShopGame game = NewGame();
            ItemInstance item = BuyOne(game, 600);
            game.BeginVisit(BuyerOf(Astrolabe));
            game.OfferItemToBuyer(item);

            TradeResult r = game.AcceptCurrentPrice();

            Assert.IsTrue(r.Success);
            Assert.Less(r.Profit, 0);
        }

        [Test]
        public void CannotOfferItemTheBuyerDoesNotWant()
        {
            ShopGame game = NewGame();
            ItemInstance item = BuyOne(game, 100);
            game.BeginVisit(BuyerOf(ItemCatalog.Get("drowned_crown")));
            Assert.AreEqual(TradeError.BuyerDoesNotWant, game.OfferItemToBuyer(item).Error);
        }

        [Test]
        public void CannotSellItemNotInInventory()
        {
            ShopGame game = NewGame();
            game.BeginVisit(BuyerOf(Astrolabe));
            Assert.AreEqual(TradeError.ItemNotOwned, game.OfferItemToBuyer(new ItemInstance(Astrolabe, ItemCondition.Good)).Error);
        }

        [Test]
        public void BuyerPays_AtMostTheirBudget()
        {
            ShopGame game = NewGame();
            ItemInstance item = BuyOne(game, 100);
            game.BeginVisit(BuyerOf(Astrolabe, budget: 250, valuation: 2f));
            game.OfferItemToBuyer(item);

            Assert.AreNotEqual(NegotiationOutcome.Accepted, game.Propose(260).Answer.Outcome);
            Assert.AreEqual(NegotiationOutcome.Accepted, game.Propose(250).Answer.Outcome);
        }

        [Test]
        public void SecondVisitorCannotArrive_WhileOneIsAtTheCounter()
        {
            ShopGame game = NewGame();
            Assert.IsNotNull(game.BeginVisit());
            Assert.IsNull(game.BeginVisit());
            game.EndVisit();
            Assert.IsNotNull(game.BeginVisit());
        }

        [Test]
        public void FirstVisitor_IsAnAffordableHonestSeller()
        {
            ShopGame game = NewGame(ShopGame.DefaultStartingCoins);
            Visitor v = game.BeginVisit();
            Assert.AreEqual(VisitorRole.Seller, v.Role);
            Assert.IsFalse(v.OfferedItem.HasHiddenFlaw);
            Assert.LessOrEqual(v.AskingPrice, ShopGame.DefaultStartingCoins);
        }

        [Test]
        public void Upgrade_TakesCoins_AndChangesRealValues()
        {
            ShopGame game = NewGame(10000);
            int capacity = game.Inventory.Capacity;
            float spread = game.Upgrades.EstimateSpread;

            Assert.IsTrue(game.TryBuyUpgrade(UpgradeId.Storage).Success);
            Assert.IsTrue(game.TryBuyUpgrade(UpgradeId.AppraisalLens).Success);

            Assert.Greater(game.Inventory.Capacity, capacity);
            Assert.Less(game.Upgrades.EstimateSpread, spread);
            Assert.AreEqual(10000 - ShopUpgrades.Storage.Costs[0] - ShopUpgrades.AppraisalLens.Costs[0], game.Wallet.Coins);
        }

        [Test]
        public void UnaffordableUpgrade_ChangesNothing()
        {
            ShopGame game = NewGame(10);
            Assert.AreEqual(TradeError.NotEnoughCoins, game.TryBuyUpgrade(UpgradeId.Reputation).Error);
            Assert.AreEqual(0, game.Upgrades.Level(UpgradeId.Reputation));
            Assert.AreEqual(10, game.Wallet.Coins);
        }

        [Test]
        public void MaxedUpgrade_CannotBeBought()
        {
            ShopGame game = NewGame(1000000);
            for (int i = 0; i < ShopUpgrades.Storage.MaxLevel; i++) game.TryBuyUpgrade(UpgradeId.Storage);
            Assert.AreEqual(TradeError.MaxLevel, game.TryBuyUpgrade(UpgradeId.Storage).Error);
        }

        [Test]
        public void Inspection_RevealsHiddenFlaw_AndLowersEstimate()
        {
            ShopGame game = NewGame();
            var item = new ItemInstance(ItemCatalog.Get("azure_crystal"), ItemCondition.Good, hasHiddenFlaw: true);
            ValueEstimate before = game.Estimate(item);

            Assert.IsTrue(game.Inspect(item));
            ValueEstimate after = game.Estimate(item);

            Assert.IsTrue(before.Contains(item.ApparentValue));
            Assert.IsTrue(after.Contains(item.TrueValue));
            Assert.Less(after.High, before.Low);
            Assert.IsFalse(game.Inspect(item), "A flaw is only 'found' once.");
        }

        [Test]
        public void Estimate_IsStable_AndNarrowerAfterInspection()
        {
            ShopGame game = NewGame();
            var item = new ItemInstance(ItemCatalog.Get("azure_crystal"), ItemCondition.Good);
            ValueEstimate a = game.Estimate(item);
            Assert.AreEqual(a.Low, game.Estimate(item).Low);

            game.Inspect(item);
            ValueEstimate b = game.Estimate(item);
            Assert.Less(b.High - b.Low, a.High - a.Low);
        }

        [Test]
        public void CloseDay_SummarisesAndStartsNextDay()
        {
            ShopGame game = NewGame();
            ItemInstance item = BuyOne(game, 300);
            game.BeginVisit(BuyerOf(Astrolabe));
            game.OfferItemToBuyer(item);
            game.Propose(450);
            game.EndVisit();

            DaySummary summary = game.CloseDay();

            Assert.AreEqual(1, summary.Day);
            Assert.AreEqual(1, summary.Purchases);
            Assert.AreEqual(1, summary.Sales);
            Assert.AreEqual(150, summary.Profit);
            Assert.AreEqual(150, summary.NetCoins);
            Assert.AreEqual(2, game.Clock.Day);
        }

        [Test]
        public void SameSeed_GivesSameVisitors()
        {
            var a = new ShopGame(new SeededRandom(7));
            var b = new ShopGame(new SeededRandom(7));
            for (int i = 0; i < 10; i++)
            {
                Visitor va = a.BeginVisit();
                Visitor vb = b.BeginVisit();
                Assert.AreEqual(va.Personality.Id, vb.Personality.Id);
                Assert.AreEqual(va.LookId, vb.LookId);
                a.EndVisit();
                b.EndVisit();
            }
        }
    }
}
