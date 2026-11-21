using System;
using BlackMarket.Items;

namespace BlackMarket.Economy
{
    /// <summary>
    /// The player's estimate of what an item is worth: a range, not an exact number.
    /// A better appraisal tool narrows the range; inspecting an item closely narrows it further
    /// and exposes hidden flaws. Until a flaw is found, the estimate is based on what the item
    /// APPEARS to be worth, which is exactly how a dishonest seller fools you.
    /// </summary>
    public readonly struct ValueEstimate
    {
        public ValueEstimate(long low, long high)
        {
            Low = Math.Min(low, high);
            High = Math.Max(low, high);
        }

        public long Low { get; }
        public long High { get; }
        public long Mid => (Low + High) / 2;

        public bool Contains(long value) => value >= Low && value <= High;

        public override string ToString() => $"{ItemText.Coins(Low)} – {ItemText.Coins(High)}";

        /// <summary>
        /// Builds an estimate around the value the player can perceive.
        /// </summary>
        /// <param name="spread">Half-width as a fraction of value (0.3 = roughly ±30%).</param>
        /// <param name="offset">-1..1. Where the real value sits inside the range (randomised per item so the
        /// middle of the range isn't always the exact answer). The perceived value is always inside.</param>
        public static ValueEstimate Create(long perceivedValue, float spread, float offset)
        {
            spread = Math.Max(0f, spread);
            offset = Math.Max(-1f, Math.Min(1f, offset)) * 0.5f;
            long low = ItemInstance.RoundValue(perceivedValue * (1f - spread * (1f + offset)));
            long high = ItemInstance.RoundValue(perceivedValue * (1f + spread * (1f - offset)));
            low = Math.Min(low, perceivedValue);
            high = Math.Max(high, perceivedValue);
            return new ValueEstimate(low, high);
        }

        /// <summary>The value the player can perceive: true value if the flaw has been found, otherwise the apparent value.</summary>
        public static long PerceivedValue(ItemInstance item)
        {
            return item.FlawRevealed ? item.TrueValue : item.ApparentValue;
        }
    }
}
