using System;
using System.Collections.Generic;
using BlackMarket.Economy;
using BlackMarket.Inventory;
using BlackMarket.Items;

namespace BlackMarket.Customers
{
    /// <summary>
    /// Decides who walks in next. Keeps the core loop flowing: sellers when the shop has nothing to
    /// sell, mostly buyers when it's full, and buyers usually want something the shop actually has.
    /// Deterministic for a given IRandom.
    /// </summary>
    public sealed class VisitorGenerator
    {
        /// <summary>Chance a buyer asks for something the shop has in stock.</summary>
        public const float InStockRequestChance = 0.75f;

        // Rarity weights (Common..Legendary) for ordinary sellers, by reputation level.
        private static readonly float[][] RarityWeightsByReputation =
        {
            new[] { 5f, 3f, 1.2f, 0.25f, 0.03f },
            new[] { 4f, 3.5f, 2f, 0.5f, 0.06f },
            new[] { 3f, 3.5f, 2.6f, 0.9f, 0.12f },
            new[] { 2f, 3f, 3f, 1.4f, 0.3f }
        };

        // Damaged, Worn, Good, Pristine.
        private static readonly float[] ConditionWeights = { 1f, 3f, 4f, 1.2f };

        private readonly IRandom random;

        public VisitorGenerator(IRandom random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public static float BuyerChance(int itemsInStock, int capacity)
        {
            if (itemsInStock <= 0) return 0f;
            if (itemsInStock >= capacity) return 0.85f;
            if (itemsInStock >= capacity - 1) return 0.7f;
            return itemsInStock == 1 ? 0.45f : 0.55f;
        }

        /// <param name="visitNumber">0 for the very first visitor of a new game.</param>
        public Visitor Next(ShopInventory inventory, int reputationLevel, int visitNumber)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            reputationLevel = Math.Max(0, Math.Min(reputationLevel, RarityWeightsByReputation.Length - 1));

            if (visitNumber == 0)
            {
                // A friendly first deal: a desperate seller with something affordable and honest.
                return CreateSeller(PersonalityCatalog.Desperate, reputationLevel, new[] { 3f, 2f, 0f, 0f, 0f }, allowFlaw: false);
            }

            bool buyer = random.Chance(BuyerChance(inventory.Count, inventory.Capacity));
            if (buyer)
            {
                Personality p = PersonalityCatalog.Buyers[random.PickWeighted(PersonalityCatalog.BuyerWeights)];
                return CreateBuyer(p, inventory, reputationLevel);
            }

            Personality seller = PersonalityCatalog.Sellers[random.PickWeighted(PersonalityCatalog.SellerWeights)];
            return CreateSeller(seller, reputationLevel, seller.RarityWeights ?? RarityWeightsByReputation[reputationLevel], allowFlaw: true);
        }

        public Visitor CreateSeller(Personality personality, int reputationLevel, IReadOnlyList<float> rarityWeights, bool allowFlaw)
        {
            ItemDefinition definition = PickItemOfRarity((Rarity)random.PickWeighted(rarityWeights));
            var condition = (ItemCondition)random.PickWeighted(ConditionWeights);
            bool flawed = allowFlaw && random.Chance(personality.FlawChance);
            var item = new ItemInstance(definition, condition, flawed);

            long ask = ItemInstance.RoundValue(item.ApparentValue * random.Range(personality.AskFactor.Min, personality.AskFactor.Max));
            long minimum = ItemInstance.RoundValue(item.TrueValue * random.Range(personality.FloorFactor.Min, personality.FloorFactor.Max));
            int patience = random.Range(personality.PatienceMin, personality.PatienceMax + 1);

            return Visitor.CreateSeller(personality, random.Pick(personality.LookIds), patience, item, ask, minimum);
        }

        public Visitor CreateBuyer(Personality personality, ShopInventory inventory, int reputationLevel)
        {
            BuyerRequest request = PickRequest(personality, inventory);

            float wealth = 1f + 0.25f * reputationLevel;
            long budget = ItemInstance.RoundValue(random.Range(personality.Budget.Min, personality.Budget.Max) * wealth);
            float valuation = random.Range(personality.Valuation.Min, personality.Valuation.Max) + 0.04f * reputationLevel;
            float opening = random.Range(personality.OpeningBid.Min, personality.OpeningBid.Max);
            int patience = random.Range(personality.PatienceMin, personality.PatienceMax + 1);

            return Visitor.CreateBuyer(personality, random.Pick(personality.LookIds), patience, request, budget, valuation, opening);
        }

        private BuyerRequest PickRequest(Personality personality, ShopInventory inventory)
        {
            if (inventory.Count > 0 && random.Chance(InStockRequestChance))
            {
                // Prefer something in a category this buyer loves, if the shop has one.
                List<ItemInstance> liked = inventory.FindAll(i => personality.LikesCategory(i.Definition.Category));
                IReadOnlyList<ItemInstance> pool = liked.Count > 0 && random.Chance(0.6f) ? liked : inventory.Items;
                ItemInstance target = random.Pick(pool);
                return random.Chance(0.6f)
                    ? BuyerRequest.ForItem(target.Definition)
                    : BuyerRequest.ForCategory(target.Definition.Category);
            }

            // Something the shop may not have: the player has to say no (or got lucky).
            if (personality.Likes.Length > 0 && random.Chance(0.4f))
            {
                return BuyerRequest.ForCategory(random.Pick(personality.Likes));
            }

            return BuyerRequest.ForItem(random.Pick(ItemCatalog.All));
        }

        private ItemDefinition PickItemOfRarity(Rarity rarity)
        {
            var matches = new List<ItemDefinition>();
            foreach (ItemDefinition d in ItemCatalog.All)
            {
                if (d.Rarity == rarity) matches.Add(d);
            }

            return matches.Count > 0 ? random.Pick(matches) : random.Pick(ItemCatalog.All);
        }
    }
}
