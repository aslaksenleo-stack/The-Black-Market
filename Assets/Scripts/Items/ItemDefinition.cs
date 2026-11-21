using System;

namespace BlackMarket.Items
{
    /// <summary>
    /// What an item IS (shared by every copy): name, rarity, category, base value, description,
    /// and which 3D model recipe draws it. Immutable.
    ///
    /// Definitions live in code (ItemCatalog) rather than ScriptableObject assets because the
    /// project is built in the cloud without anyone opening the Editor; code keeps them reviewable
    /// and unit-testable. Moving them to ScriptableObjects later only changes ItemCatalog.
    /// </summary>
    public sealed class ItemDefinition
    {
        public ItemDefinition(string id, string name, ItemCategory category, Rarity rarity, long baseValue,
            string description, string modelId)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Item id is required.", nameof(id));
            if (baseValue <= 0) throw new ArgumentOutOfRangeException(nameof(baseValue), "Base value must be positive.");

            Id = id;
            Name = name;
            Category = category;
            Rarity = rarity;
            BaseValue = baseValue;
            Description = description;
            ModelId = string.IsNullOrEmpty(modelId) ? id : modelId;
        }

        public string Id { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public Rarity Rarity { get; }

        /// <summary>Fair market value of a copy in Good condition, in coins.</summary>
        public long BaseValue { get; }

        public string Description { get; }

        /// <summary>Key for ItemModelBuilder (the Unity side) to pick the 3D model.</summary>
        public string ModelId { get; }

        public override string ToString() => $"{Name} ({Rarity})";
    }
}
