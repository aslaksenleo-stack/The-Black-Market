using System;
using System.Collections.Generic;

namespace BlackMarket.Items
{
    /// <summary>
    /// Every item the game knows about. Each id must be unique and each item has its own 3D model
    /// recipe (see ItemModelBuilder), so no two items look like recolours of each other.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly ItemDefinition[] Items =
        {
            new ItemDefinition("azure_crystal", "Azure Crystal", ItemCategory.Materials, Rarity.Rare, 1600,
                "A cluster of blue crystal that hums faintly when held. Alchemists grind it for focus tinctures.", "crystal"),
            new ItemDefinition("brass_astrolabe", "Brass Astrolabe", ItemCategory.Tools, Rarity.Uncommon, 520,
                "Nested brass rings for reading the stars. One ring is engraved with constellations nobody recognises.", "astrolabe"),
            new ItemDefinition("signet_ring", "Gilded Signet Ring", ItemCategory.Jewelry, Rarity.Uncommon, 450,
                "A heavy gold ring bearing a minor noble house's crest. Someone, somewhere, misses it.", "ring"),
            new ItemDefinition("scrying_orb", "Scrying Orb", ItemCategory.Artifacts, Rarity.Rare, 1400,
                "Smoky violet glass on a bronze stand. Stare long enough and the smoke stares back.", "orb"),
            new ItemDefinition("alchemist_retort", "Alchemist's Retort", ItemCategory.Tools, Rarity.Common, 140,
                "A long-necked glass flask, stained green inside. Still smells of sulphur.", "retort"),
            new ItemDefinition("dragonbone_dagger", "Dragonbone Dagger", ItemCategory.Artifacts, Rarity.Rare, 1900,
                "A pale blade carved from a single dragon tooth. It is warm to the touch.", "dagger"),
            new ItemDefinition("bestiary", "Illuminated Bestiary", ItemCategory.Collectibles, Rarity.Rare, 1150,
                "A hand-painted book of monsters. Several pages have been chewed by one of them.", "book"),
            new ItemDefinition("pewter_tankard", "Pewter Tankard", ItemCategory.Collectibles, Rarity.Common, 90,
                "A dented tavern tankard with a hinged lid. Holds exactly one bad decision.", "tankard"),
            new ItemDefinition("reliquary_locket", "Silver Reliquary Locket", ItemCategory.Jewelry, Rarity.Rare, 1250,
                "A silver locket that won't open. Whatever is inside rattles when nobody is looking.", "locket"),
            new ItemDefinition("clockwork_songbird", "Clockwork Songbird", ItemCategory.Technology, Rarity.Epic, 3400,
                "A brass bird that sings a different song every dawn. Nobody knows who wound it first.", "songbird"),
            new ItemDefinition("iron_lantern", "Watchman's Iron Lantern", ItemCategory.Tools, Rarity.Common, 120,
                "A sturdy city-watch lantern. The glass is sooty but whole.", "lantern"),
            new ItemDefinition("royal_charter", "Wax-Sealed Royal Charter", ItemCategory.Collectibles, Rarity.Uncommon, 600,
                "A rolled charter granting rights to a city that no longer exists. The seal is unbroken.", "scroll"),
            new ItemDefinition("ember_salts", "Ember Salt Crystals", ItemCategory.Materials, Rarity.Common, 180,
                "Orange salt crystals that stay warm for years. Popular with smiths and cold-footed scholars.", "salts"),
            new ItemDefinition("knight_helm", "Knight's Visored Helm", ItemCategory.Artifacts, Rarity.Uncommon, 680,
                "A battered steel helm with a working visor. A name is scratched inside: 'Ser Pell'.", "helm"),
            new ItemDefinition("mandrake_jar", "Jarred Mandrake Root", ItemCategory.Specimens, Rarity.Uncommon, 380,
                "A mandrake root floating in brine. Do NOT open the jar.", "jar"),
            new ItemDefinition("drowned_crown", "Crown of the Drowned King", ItemCategory.Jewelry, Rarity.Legendary, 9500,
                "A crown of tarnished gold and sea-glass, pulled from a sunken throne room. Always slightly damp.", "crown"),
            new ItemDefinition("ram_skull", "Horned Ram Skull", ItemCategory.Specimens, Rarity.Common, 160,
                "Bleached skull of a mountain ram, curled horns intact. Good for scaring off cheap customers.", "skull"),
            new ItemDefinition("potion_vigour", "Potion of Vigour", ItemCategory.Materials, Rarity.Uncommon, 300,
                "A stoppered bottle of bubbling red draught. Tastes like pennies and courage.", "potion")
        };

        private static readonly Dictionary<string, ItemDefinition> ById = BuildIndex();

        public static IReadOnlyList<ItemDefinition> All => Items;

        public static ItemDefinition Get(string id)
        {
            if (id != null && ById.TryGetValue(id, out ItemDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"No item with id '{id}'.");
        }

        public static bool TryGet(string id, out ItemDefinition definition)
        {
            definition = null;
            return id != null && ById.TryGetValue(id, out definition);
        }

        private static Dictionary<string, ItemDefinition> BuildIndex()
        {
            var index = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            foreach (ItemDefinition item in Items)
            {
                if (index.ContainsKey(item.Id))
                {
                    throw new InvalidOperationException($"Duplicate item id '{item.Id}' in ItemCatalog.");
                }

                index.Add(item.Id, item);
            }

            return index;
        }
    }
}
