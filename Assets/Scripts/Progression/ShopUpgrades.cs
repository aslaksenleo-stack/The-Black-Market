using System;
using System.Collections.Generic;

namespace BlackMarket.Progression
{
    public enum UpgradeId
    {
        /// <summary>Narrows value estimates.</summary>
        AppraisalLens,
        /// <summary>More inventory space.</summary>
        Storage,
        /// <summary>Attracts rarer sellers and richer buyers.</summary>
        Reputation
    }

    /// <summary>One upgrade track: name, description per level, and cost of each next level.</summary>
    public sealed class UpgradeDefinition
    {
        public UpgradeDefinition(UpgradeId id, string name, string[] levelEffects, long[] costs)
        {
            if (levelEffects.Length != costs.Length + 1)
            {
                throw new ArgumentException("Need one effect description per level (including level 0).");
            }

            Id = id;
            Name = name;
            LevelEffects = levelEffects;
            Costs = costs;
        }

        public UpgradeId Id { get; }
        public string Name { get; }

        /// <summary>LevelEffects[n] describes what level n gives.</summary>
        public IReadOnlyList<string> LevelEffects { get; }

        /// <summary>Costs[n] is the price of going from level n to n+1.</summary>
        public IReadOnlyList<long> Costs { get; }

        public int MaxLevel => Costs.Count;
    }

    /// <summary>
    /// The shop's upgrade levels and what they actually do. Every upgrade changes a real gameplay value
    /// (estimate spread, inventory capacity, visitor quality), never just a number on screen.
    /// </summary>
    public sealed class ShopUpgrades
    {
        public static readonly UpgradeDefinition AppraisalLens = new UpgradeDefinition(
            UpgradeId.AppraisalLens, "Appraiser's Loupe",
            new[] { "Estimates ±35%", "Estimates ±24%", "Estimates ±15%", "Estimates ±8%" },
            new long[] { 900, 2400, 5500 });

        public static readonly UpgradeDefinition Storage = new UpgradeDefinition(
            UpgradeId.Storage, "Back Room Storage",
            new[] { "6 item slots", "9 item slots", "13 item slots", "18 item slots" },
            new long[] { 700, 1800, 4200 });

        public static readonly UpgradeDefinition Reputation = new UpgradeDefinition(
            UpgradeId.Reputation, "Shop Reputation",
            new[] { "Unknown shop", "Local name: rarer finds", "Talk of the town: rich buyers", "Legendary: treasures walk in" },
            new long[] { 1200, 3200, 8000 });

        public static readonly IReadOnlyList<UpgradeDefinition> All = new[] { AppraisalLens, Storage, Reputation };

        private static readonly float[] SpreadByLevel = { 0.35f, 0.24f, 0.15f, 0.08f };
        private static readonly int[] CapacityByLevel = { 6, 9, 13, 18 };

        private readonly Dictionary<UpgradeId, int> levels = new Dictionary<UpgradeId, int>
        {
            { UpgradeId.AppraisalLens, 0 },
            { UpgradeId.Storage, 0 },
            { UpgradeId.Reputation, 0 }
        };

        public event Action<UpgradeId, int> LevelChanged;

        public static UpgradeDefinition Definition(UpgradeId id)
        {
            foreach (UpgradeDefinition d in All)
            {
                if (d.Id == id) return d;
            }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public int Level(UpgradeId id) => levels[id];

        public bool IsMaxed(UpgradeId id) => Level(id) >= Definition(id).MaxLevel;

        /// <summary>Price of the next level, or -1 when maxed.</summary>
        public long NextCost(UpgradeId id) => IsMaxed(id) ? -1 : Definition(id).Costs[Level(id)];

        /// <summary>Shop level shown in the HUD: 1 + all upgrade levels.</summary>
        public int ShopLevel
        {
            get
            {
                int total = 1;
                foreach (int l in levels.Values) total += l;
                return total;
            }
        }

        /// <summary>Half-width of value estimates as a fraction (0.35 = ±35%).</summary>
        public float EstimateSpread => SpreadByLevel[Level(UpgradeId.AppraisalLens)];

        public int InventoryCapacity => CapacityByLevel[Level(UpgradeId.Storage)];

        public int ReputationLevel => Level(UpgradeId.Reputation);

        /// <summary>Raises the level by one. Does NOT take payment; ShopGame does that atomically.</summary>
        internal void Increase(UpgradeId id)
        {
            if (IsMaxed(id)) throw new InvalidOperationException($"{id} is already at max level.");
            levels[id]++;
            LevelChanged?.Invoke(id, levels[id]);
        }

        /// <summary>For loading saves and tests.</summary>
        public void SetLevel(UpgradeId id, int level)
        {
            levels[id] = Math.Max(0, Math.Min(level, Definition(id).MaxLevel));
            LevelChanged?.Invoke(id, levels[id]);
        }
    }
}
