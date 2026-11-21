using System;
using System.Collections.Generic;
using BlackMarket.Core;
using BlackMarket.Customers;
using BlackMarket.Economy;
using BlackMarket.Inventory;
using BlackMarket.Items;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public sealed class WalletTests
    {
        [Test]
        public void Spend_And_Add()
        {
            var w = new Wallet(100);
            Assert.IsTrue(w.TrySpend(100));
            Assert.AreEqual(0, w.Coins);
            w.Add(50);
            Assert.AreEqual(50, w.Coins);
        }

        [Test]
        public void CannotOverspend_OrSpendNegative()
        {
            var w = new Wallet(100);
            Assert.IsFalse(w.TrySpend(101));
            Assert.IsFalse(w.TrySpend(-5));
            Assert.AreEqual(100, w.Coins);
            Assert.Throws<ArgumentOutOfRangeException>(() => w.Add(-1));
        }

        [Test]
        public void Changed_ReportsBalanceAndDelta()
        {
            var w = new Wallet(100);
            long balance = 0, delta = 0;
            w.Changed += (b, d) => { balance = b; delta = d; };
            w.TrySpend(30);
            Assert.AreEqual(70, balance);
            Assert.AreEqual(-30, delta);
        }
    }

    public sealed class InventoryTests
    {
        private static ItemInstance NewItem() => new ItemInstance(ItemCatalog.Get("pewter_tankard"), ItemCondition.Good);

        [Test]
        public void AddRemove()
        {
            var inv = new ShopInventory(2);
            ItemInstance a = NewItem();
            Assert.IsTrue(inv.TryAdd(a));
            Assert.IsTrue(inv.Contains(a));
            Assert.IsTrue(inv.Remove(a));
            Assert.AreEqual(0, inv.Count);
        }

        [Test]
        public void Full_RejectsMore()
        {
            var inv = new ShopInventory(1);
            Assert.IsTrue(inv.TryAdd(NewItem()));
            Assert.IsTrue(inv.IsFull);
            Assert.IsFalse(inv.TryAdd(NewItem()));
        }

        [Test]
        public void SameItemTwice_And_DoubleRemove_AreRejected()
        {
            var inv = new ShopInventory(5);
            ItemInstance a = NewItem();
            inv.TryAdd(a);
            Assert.IsFalse(inv.TryAdd(a));
            inv.Remove(a);
            Assert.IsFalse(inv.Remove(a));
            Assert.AreEqual(0, inv.Count);
        }

        [Test]
        public void Capacity_OnlyGrows()
        {
            var inv = new ShopInventory(6);
            inv.IncreaseCapacity(4);
            Assert.AreEqual(6, inv.Capacity);
            inv.IncreaseCapacity(9);
            Assert.AreEqual(9, inv.Capacity);
        }
    }

    public sealed class ItemTests
    {
        [Test]
        public void Catalog_IdsUnique_AndValuesPositive()
        {
            var ids = new HashSet<string>();
            foreach (ItemDefinition d in ItemCatalog.All)
            {
                Assert.IsTrue(ids.Add(d.Id), d.Id);
                Assert.Greater(d.BaseValue, 0);
                Assert.IsFalse(string.IsNullOrEmpty(d.Description), d.Id);
            }
        }

        [Test]
        public void Catalog_ModelIdsUnique_SoNoTwoItemsLookTheSame()
        {
            var models = new HashSet<string>();
            foreach (ItemDefinition d in ItemCatalog.All) Assert.IsTrue(models.Add(d.ModelId), d.ModelId);
        }

        [Test]
        public void EveryRarityBelowLegendary_HasItems()
        {
            foreach (Rarity r in new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary })
            {
                Assert.IsTrue(Array.Exists(ToArray(), d => d.Rarity == r), r.ToString());
            }
        }

        [Test]
        public void Condition_And_Flaw_ChangeValue()
        {
            ItemDefinition d = ItemCatalog.Get("azure_crystal");
            Assert.Greater(new ItemInstance(d, ItemCondition.Pristine).TrueValue, new ItemInstance(d, ItemCondition.Good).TrueValue);
            Assert.Less(new ItemInstance(d, ItemCondition.Worn).TrueValue, new ItemInstance(d, ItemCondition.Good).TrueValue);

            var flawed = new ItemInstance(d, ItemCondition.Good, hasHiddenFlaw: true);
            Assert.Less(flawed.TrueValue, flawed.ApparentValue);
        }

        [TestCase(0, 0)]
        [TestCase(42, 40)]
        [TestCase(43, 45)]
        [TestCase(1234, 1230)]
        [TestCase(1235, 1240)]
        public void RoundValue(double input, long expected)
        {
            Assert.AreEqual(expected, ItemInstance.RoundValue(input));
        }

        [Test]
        public void Estimate_AlwaysContainsPerceivedValue()
        {
            for (float offset = -1f; offset <= 1f; offset += 0.25f)
            {
                ValueEstimate e = ValueEstimate.Create(1000, 0.3f, offset);
                Assert.IsTrue(e.Contains(1000), $"offset {offset}: {e}");
                Assert.Less(e.Low, e.High);
            }
        }

        private static ItemDefinition[] ToArray()
        {
            var list = new List<ItemDefinition>(ItemCatalog.All);
            return list.ToArray();
        }
    }

    public sealed class VisitorTests
    {
        [Test]
        public void Personalities_AreValid()
        {
            foreach (Personality p in PersonalityCatalog.Sellers)
            {
                Assert.AreEqual(VisitorRole.Seller, p.Role, p.Id);
                Assert.Greater(p.LookIds.Length, 0, p.Id);
                Assert.LessOrEqual(p.PatienceMin, p.PatienceMax, p.Id);
            }

            foreach (Personality p in PersonalityCatalog.Buyers)
            {
                Assert.AreEqual(VisitorRole.Buyer, p.Role, p.Id);
                Assert.Greater(p.LookIds.Length, 0, p.Id);
            }

            Assert.AreEqual(PersonalityCatalog.Sellers.Count, PersonalityCatalog.SellerWeights.Length);
            Assert.AreEqual(PersonalityCatalog.Buyers.Count, PersonalityCatalog.BuyerWeights.Length);
        }

        [Test]
        public void Desperate_IsUsuallyAGoodDeal_Greedy_IsUsuallyABadOne()
        {
            var gen = new VisitorGenerator(new SeededRandom(1));
            int desperateGood = 0, greedyBad = 0;
            for (int i = 0; i < 200; i++)
            {
                Visitor d = gen.CreateSeller(PersonalityCatalog.Desperate, 0, new[] { 1f, 1f, 1f, 0f, 0f }, false);
                if (d.MinimumPrice < d.OfferedItem.TrueValue) desperateGood++;
                Visitor g = gen.CreateSeller(PersonalityCatalog.Greedy, 0, new[] { 1f, 1f, 1f, 0f, 0f }, false);
                if (g.MinimumPrice > g.OfferedItem.TrueValue) greedyBad++;
            }

            Assert.Greater(desperateGood, 190);
            Assert.Greater(greedyBad, 190);
        }

        [Test]
        public void EmptyShop_OnlyGetsSellers()
        {
            var gen = new VisitorGenerator(new SeededRandom(3));
            var inv = new ShopInventory(6);
            for (int i = 1; i < 50; i++)
            {
                Assert.AreEqual(VisitorRole.Seller, gen.Next(inv, 0, i).Role);
            }
        }

        [Test]
        public void Buyers_MostlyWantSomethingInStock()
        {
            var gen = new VisitorGenerator(new SeededRandom(5));
            var inv = new ShopInventory(6);
            inv.TryAdd(new ItemInstance(ItemCatalog.Get("brass_astrolabe"), ItemCondition.Good));
            inv.TryAdd(new ItemInstance(ItemCatalog.Get("signet_ring"), ItemCondition.Good));

            int buyers = 0, matching = 0;
            for (int i = 1; i < 400; i++)
            {
                Visitor v = gen.Next(inv, 0, i);
                if (v.Role != VisitorRole.Buyer) continue;
                buyers++;
                foreach (ItemInstance item in inv.Items)
                {
                    if (v.Wants(item)) { matching++; break; }
                }
            }

            Assert.Greater(buyers, 100);
            Assert.Greater(matching / (float)buyers, 0.65f);
        }

        [Test]
        public void CategoryRequest_MatchesAnyItemInCategory()
        {
            BuyerRequest r = BuyerRequest.ForCategory(ItemCategory.Jewelry);
            Assert.IsTrue(r.Matches(ItemCatalog.Get("signet_ring")));
            Assert.IsTrue(r.Matches(ItemCatalog.Get("drowned_crown")));
            Assert.IsFalse(r.Matches(ItemCatalog.Get("pewter_tankard")));
        }

        [Test]
        public void DialogueFill_ReplacesPlaceholders()
        {
            string line = DialogueLines.Fill("A {item} for {price}? Got {category}?", "Ring", 1200, "jewelry");
            Assert.AreEqual("A Ring for 1,200 coins? Got jewelry?", line);
        }
    }

    public sealed class DayClockTests
    {
        [Test]
        public void StartsAtOpening_AndCloses()
        {
            var clock = new DayClock();
            Assert.AreEqual("09:00", clock.TimeText);
            Assert.IsFalse(clock.IsClosingTime);
            clock.Advance(10 * 60);
            Assert.IsTrue(clock.IsClosingTime);
            Assert.AreEqual("18:00", clock.TimeText);
        }

        [Test]
        public void NextDay_ResetsTime()
        {
            var clock = new DayClock();
            clock.Advance(100);
            clock.StartNextDay();
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(DayClock.OpeningMinute, clock.MinuteOfDay);
        }
    }
}
