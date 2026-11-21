using System;
using System.IO;
using System.Linq;
using BlackMarket.Customers;
using BlackMarket.Economy;
using BlackMarket.Items;
using BlackMarket.Progression;
using BlackMarket.Save;
using BlackMarket.Trading;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public sealed class SaveTests
    {
        private string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "bm_save_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        // A shop with some history: one item bought and sold, one in stock, an upgrade, day 2.
        private static ShopGame PlayedGame()
        {
            var game = new ShopGame(new SeededRandom(11), 5000);
            var astrolabe = ItemCatalog.Get("brass_astrolabe");

            var sold = new ItemInstance(astrolabe, ItemCondition.Good);
            game.BeginVisit(Visitor.CreateSeller(PersonalityCatalog.FairTrader, "peddler", 3, sold, 300, 300));
            game.AcceptCurrentPrice();
            game.EndVisit();
            game.BeginVisit(Visitor.CreateBuyer(PersonalityCatalog.Collector, "elder", 3, BuyerRequest.ForItem(astrolabe), 9000, 1.2f, 0.7f));
            game.OfferItemToBuyer(sold);
            game.Propose(450);
            game.EndVisit();

            var kept = new ItemInstance(ItemCatalog.Get("azure_crystal"), ItemCondition.Worn, hasHiddenFlaw: true);
            game.BeginVisit(Visitor.CreateSeller(PersonalityCatalog.Shady, "wanderer", 3, kept, 500, 400));
            game.AcceptCurrentPrice();
            game.EndVisit();

            game.TryBuyUpgrade(UpgradeId.Storage);
            game.CloseDay();
            return game;
        }

        [Test]
        public void Snapshot_RoundTrip_RestoresEverything()
        {
            ShopGame original = PlayedGame();
            ShopGame loaded = ShopGame.FromSave(original.CreateSave(), new SeededRandom(1));

            Assert.AreEqual(original.Wallet.Coins, loaded.Wallet.Coins);
            Assert.AreEqual(original.Clock.Day, loaded.Clock.Day);
            Assert.AreEqual(original.Clock.MinuteOfDay, loaded.Clock.MinuteOfDay);
            Assert.AreEqual(original.VisitCount, loaded.VisitCount);
            Assert.AreEqual(1, loaded.Upgrades.Level(UpgradeId.Storage));
            Assert.AreEqual(original.Inventory.Capacity, loaded.Inventory.Capacity);
            Assert.AreEqual(original.Ledger.TotalProfit, loaded.Ledger.TotalProfit);
            Assert.AreEqual(original.Ledger.Records.Count, loaded.Ledger.Records.Count);

            ItemInstance item = loaded.Inventory.Items.Single();
            Assert.AreEqual("azure_crystal", item.Definition.Id);
            Assert.AreEqual(ItemCondition.Worn, item.Condition);
            Assert.IsTrue(item.HasHiddenFlaw);
            Assert.AreEqual(500, item.PurchasePrice);
            Assert.AreEqual(original.Inventory.Items.Single().TrueValue, item.TrueValue);
        }

        [Test]
        public void SaveIsValid_AndStopsGrowing()
        {
            ShopGame game = PlayedGame();
            Assert.IsEmpty(SaveValidator.Validate(game.CreateSave()));
        }

        [Test]
        public void Validator_RejectsImpossibleSaves()
        {
            SaveData good = PlayedGame().CreateSave();

            SaveData negative = PlayedGame().CreateSave();
            negative.coins = -1;
            SaveData unknownItem = PlayedGame().CreateSave();
            unknownItem.items[0].id = "made_up";
            SaveData future = PlayedGame().CreateSave();
            future.version = SaveData.CurrentVersion + 1;
            SaveData tooMany = PlayedGame().CreateSave();
            for (int i = 0; i < 30; i++) tooMany.items.Add(new SavedItem { id = "pewter_tankard", condition = 2 });
            SaveData badLevel = PlayedGame().CreateSave();
            badLevel.reputationLevel = 99;

            Assert.IsEmpty(SaveValidator.Validate(good));
            foreach (SaveData bad in new[] { negative, unknownItem, future, tooMany, badLevel, null })
            {
                Assert.IsNotEmpty(SaveValidator.Validate(bad));
            }

            Assert.Throws<ArgumentException>(() => ShopGame.FromSave(negative, new SeededRandom(1)));
        }

        // ---- File store (with a tiny test format, since Unity's JsonUtility isn't available outside Unity)

        private SaveStore Store() => new SaveStore(folder, MiniFormat.Write, MiniFormat.Read);

        [Test]
        public void Store_WriteThenRead()
        {
            SaveData data = PlayedGame().CreateSave();
            Assert.IsTrue(Store().Write(data, out string error), error);

            SaveData read = Store().Read(out string note);
            Assert.IsNull(note);
            Assert.AreEqual(data.coins, read.coins);
            Assert.AreEqual(data.items[0].id, read.items[0].id);
        }

        [Test]
        public void Store_NoFile_MeansNewGame()
        {
            Assert.IsNull(Store().Read(out string note));
            Assert.IsNull(note);
            Assert.IsFalse(Store().Exists);
        }

        [Test]
        public void Store_SecondSave_KeepsBackupOfFirst()
        {
            SaveData first = PlayedGame().CreateSave();
            SaveData second = PlayedGame().CreateSave();
            second.coins = first.coins + 100;
            Store().Write(first, out _);
            Store().Write(second, out _);

            Assert.AreEqual(second.coins, MiniFormat.Read(File.ReadAllText(Store().FilePath)).coins);
            Assert.AreEqual(first.coins, MiniFormat.Read(File.ReadAllText(Store().BackupPath)).coins);
        }

        [Test]
        public void Store_CorruptSave_FallsBackToBackup()
        {
            SaveData first = PlayedGame().CreateSave();
            Store().Write(first, out _);
            Store().Write(PlayedGame().CreateSave(), out _);
            File.WriteAllText(Store().FilePath, "garbage{{{");

            SaveData read = Store().Read(out string note);
            Assert.IsNotNull(read);
            Assert.AreEqual(first.coins, read.coins);
            StringAssert.Contains("backup", note);
        }

        [Test]
        public void Store_InvalidData_NeverOverwritesGoodSave()
        {
            SaveData good = PlayedGame().CreateSave();
            Store().Write(good, out _);
            SaveData bad = PlayedGame().CreateSave();
            bad.coins = -50;

            Assert.IsFalse(Store().Write(bad, out string error));
            Assert.IsNotNull(error);
            Assert.AreEqual(good.coins, Store().Read(out _).coins);
            Assert.IsFalse(File.Exists(Store().FilePath + ".tmp"));
        }

        /// <summary>Minimal line-based format covering the fields these tests use.</summary>
        private static class MiniFormat
        {
            public static string Write(SaveData d)
            {
                string items = string.Join(";", d.items.Select(i => $"{i.id},{i.condition},{i.flawed},{i.paid}"));
                return $"BM|{d.version}|{d.coins}|{d.day}|{d.minuteOfDay}|{d.visitCount}|{d.appraisalLevel}|{d.storageLevel}|{d.reputationLevel}|{items}";
            }

            public static SaveData Read(string text)
            {
                string[] p = text.Split('|');
                if (p.Length != 10 || p[0] != "BM") throw new FormatException("Not a save.");
                var d = new SaveData
                {
                    version = int.Parse(p[1]), coins = long.Parse(p[2]), day = int.Parse(p[3]), minuteOfDay = int.Parse(p[4]),
                    visitCount = int.Parse(p[5]), appraisalLevel = int.Parse(p[6]), storageLevel = int.Parse(p[7]), reputationLevel = int.Parse(p[8])
                };
                foreach (string item in p[9].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] f = item.Split(',');
                    d.items.Add(new SavedItem { id = f[0], condition = int.Parse(f[1]), flawed = bool.Parse(f[2]), paid = long.Parse(f[3]), inspected = true });
                }

                return d;
            }
        }
    }
}
