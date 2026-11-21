namespace BlackMarket.Items
{
    /// <summary>How hard an item is to find. Drives base value and which customers want it.</summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemCategory
    {
        Artifacts,
        Jewelry,
        Tools,
        Materials,
        Collectibles,
        Technology,
        Specimens
    }

    /// <summary>Physical state of one specific item. Multiplies its market value.</summary>
    public enum ItemCondition
    {
        Damaged,
        Worn,
        Good,
        Pristine
    }

    /// <summary>Display names and value multipliers. Plain C#, no Unity types.</summary>
    public static class ItemText
    {
        public static float ConditionMultiplier(ItemCondition condition)
        {
            switch (condition)
            {
                case ItemCondition.Damaged: return 0.55f;
                case ItemCondition.Worn: return 0.8f;
                case ItemCondition.Pristine: return 1.3f;
                default: return 1f;
            }
        }

        public static string ConditionName(ItemCondition condition)
        {
            return condition.ToString();
        }

        public static string RarityName(Rarity rarity)
        {
            return rarity.ToString();
        }

        public static string CategoryName(ItemCategory category)
        {
            return category == ItemCategory.Specimens ? "Specimens" : category.ToString();
        }

        /// <summary>Formats coins the same way everywhere: "12,680".</summary>
        public static string Coins(long amount)
        {
            return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
