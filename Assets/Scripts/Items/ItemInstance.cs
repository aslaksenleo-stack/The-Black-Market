using System;

namespace BlackMarket.Items
{
    /// <summary>
    /// One specific physical item: a definition plus its own condition, any hidden flaw, and what the
    /// player paid for it. Runtime state, unlike the shared ItemDefinition.
    /// </summary>
    public sealed class ItemInstance
    {
        private static int nextId = 1;

        public ItemInstance(ItemDefinition definition, ItemCondition condition, bool hasHiddenFlaw = false)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Condition = condition;
            HasHiddenFlaw = hasHiddenFlaw;
            Uid = nextId++;
        }

        /// <summary>Unique within a play session. Used to tell two copies of the same item apart.</summary>
        public int Uid { get; }

        public ItemDefinition Definition { get; }
        public ItemCondition Condition { get; }

        /// <summary>
        /// A defect the seller didn't mention (crack, fake stone...). Lowers the true value.
        /// The player finds it by inspecting the item closely.
        /// </summary>
        public bool HasHiddenFlaw { get; }

        /// <summary>True once any hidden flaw is known: after a close inspection, or after buying it.</summary>
        public bool FlawRevealed { get; private set; }

        /// <summary>True once the player has examined it closely. Inspected items get tighter estimates.</summary>
        public bool IsInspected { get; private set; }

        /// <summary>What the shop paid. 0 until bought.</summary>
        public long PurchasePrice { get; private set; }

        public const float FlawMultiplier = 0.45f;

        /// <summary>What the item is really worth to buyers.</summary>
        public long TrueValue => RoundValue(Definition.BaseValue * ItemText.ConditionMultiplier(Condition) *
                                            (HasHiddenFlaw ? FlawMultiplier : 1f));

        /// <summary>What it would be worth without the hidden flaw: what a dishonest seller prices from.</summary>
        public long ApparentValue => RoundValue(Definition.BaseValue * ItemText.ConditionMultiplier(Condition));

        /// <summary>Close inspection: tightens the estimate and exposes any hidden flaw.</summary>
        public void Inspect()
        {
            IsInspected = true;
            FlawRevealed = true;
        }

        internal void MarkPurchased(long price)
        {
            PurchasePrice = price;
            Inspect(); // Once it's in your back room you know exactly what you bought.
        }

        /// <summary>Rounds to a "shop price": nearest 5 below 100, nearest 10 above.</summary>
        public static long RoundValue(double value)
        {
            if (value <= 0) return 0;
            long step = value < 100 ? 5 : 10;
            long rounded = (long)Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
            return Math.Max(step, rounded);
        }

        public override string ToString() => $"#{Uid} {Definition.Name} [{Condition}{(HasHiddenFlaw ? ", flawed" : "")}]";
    }
}
