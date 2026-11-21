using System;
using BlackMarket.Items;
using BlackMarket.Trading;

namespace BlackMarket.Customers
{
    /// <summary>What a buyer is looking for: one specific item, or anything from a category.</summary>
    public sealed class BuyerRequest
    {
        private BuyerRequest(ItemDefinition item, ItemCategory category)
        {
            SpecificItem = item;
            Category = category;
        }

        /// <summary>Set when the buyer wants exactly this item; null when any item of Category will do.</summary>
        public ItemDefinition SpecificItem { get; }
        public ItemCategory Category { get; }

        public static BuyerRequest ForItem(ItemDefinition item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return new BuyerRequest(item, item.Category);
        }

        public static BuyerRequest ForCategory(ItemCategory category) => new BuyerRequest(null, category);

        public bool Matches(ItemDefinition item)
        {
            if (item == null) return false;
            return SpecificItem != null ? item.Id == SpecificItem.Id : item.Category == Category;
        }

        /// <summary>"Brass Astrolabe" or "anything in Jewelry".</summary>
        public string Describe()
        {
            return SpecificItem != null ? SpecificItem.Name : "any " + ItemText.CategoryName(Category).ToLowerInvariant();
        }
    }

    /// <summary>
    /// One person visiting the shop. A Seller carries OfferedItem; a Buyer carries a Request and a budget.
    /// Their hidden limits are decided when they are created, so their behaviour is consistent all visit.
    /// </summary>
    public sealed class Visitor
    {
        /// <summary>Extra a buyer pays for a category they love.</summary>
        public const float LikedCategoryBonus = 1.15f;

        private Visitor(Personality personality, string lookId, int patience)
        {
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
            LookId = lookId;
            DisplayName = PersonalityCatalog.LookDisplayName(lookId);
            Patience = Math.Max(1, patience);
        }

        public Personality Personality { get; }
        public VisitorRole Role => Personality.Role;
        public string LookId { get; }
        public string DisplayName { get; }
        public int Patience { get; }

        // ---- Seller
        public ItemInstance OfferedItem { get; private set; }
        public long AskingPrice { get; private set; }

        /// <summary>Hidden: the lowest price this seller accepts.</summary>
        public long MinimumPrice { get; private set; }

        // ---- Buyer
        public BuyerRequest Request { get; private set; }
        public long Budget { get; private set; }

        /// <summary>Hidden: how much above/below value this buyer pays (before budget and likes).</summary>
        public float Valuation { get; private set; }

        /// <summary>Hidden: first bid as a fraction of what they'd pay at most.</summary>
        public float OpeningBidFraction { get; private set; }

        public static Visitor CreateSeller(Personality personality, string lookId, int patience,
            ItemInstance item, long askingPrice, long minimumPrice)
        {
            if (personality.Role != VisitorRole.Seller) throw new ArgumentException("Personality is not a seller.", nameof(personality));
            if (item == null) throw new ArgumentNullException(nameof(item));

            minimumPrice = Math.Max(5, minimumPrice);
            return new Visitor(personality, lookId, patience)
            {
                OfferedItem = item,
                MinimumPrice = minimumPrice,
                AskingPrice = Math.Max(askingPrice, minimumPrice)
            };
        }

        public static Visitor CreateBuyer(Personality personality, string lookId, int patience,
            BuyerRequest request, long budget, float valuation, float openingBidFraction)
        {
            if (personality.Role != VisitorRole.Buyer) throw new ArgumentException("Personality is not a buyer.", nameof(personality));
            return new Visitor(personality, lookId, patience)
            {
                Request = request ?? throw new ArgumentNullException(nameof(request)),
                Budget = Math.Max(10, budget),
                Valuation = valuation,
                OpeningBidFraction = Math.Max(0.3f, Math.Min(1f, openingBidFraction))
            };
        }

        public bool Wants(ItemInstance item) => Role == VisitorRole.Buyer && item != null && Request.Matches(item.Definition);

        /// <summary>Hidden: the most this buyer will pay for this item (0 if they don't want it).</summary>
        public long MaximumPriceFor(ItemInstance item)
        {
            if (!Wants(item)) return 0;
            double value = item.TrueValue * Valuation;
            if (Personality.LikesCategory(item.Definition.Category)) value *= LikedCategoryBonus;
            return Math.Min(Budget, Math.Max(5, ItemInstance.RoundValue(value)));
        }

        /// <summary>First price a buyer offers for an item they want.</summary>
        public long OpeningBidFor(ItemInstance item)
        {
            return Math.Max(5, ItemInstance.RoundValue(MaximumPriceFor(item) * OpeningBidFraction));
        }

        /// <summary>Starts haggling over the offered item (seller) or the given item (buyer).</summary>
        /// <param name="patienceLeft">Carry over patience from an earlier haggle this visit (0 = full patience).</param>
        public Negotiation StartNegotiation(ItemInstance itemForBuyer = null, int patienceLeft = 0)
        {
            if (Role == VisitorRole.Seller)
            {
                return new Negotiation(TradeSide.PlayerBuys, AskingPrice, MinimumPrice, Patience, Personality.Flexibility, patienceLeft);
            }

            if (!Wants(itemForBuyer)) throw new InvalidOperationException("This buyer doesn't want that item.");
            return new Negotiation(TradeSide.PlayerSells, OpeningBidFor(itemForBuyer), MaximumPriceFor(itemForBuyer),
                Patience, Personality.Flexibility, patienceLeft);
        }

        /// <summary>The item name to use in dialogue.</summary>
        public string SubjectName => Role == VisitorRole.Seller ? OfferedItem.Definition.Name : Request.Describe();

        public string CategoryName => Role == VisitorRole.Seller
            ? ItemText.CategoryName(OfferedItem.Definition.Category).ToLowerInvariant()
            : ItemText.CategoryName(Request.Category).ToLowerInvariant();
    }
}
