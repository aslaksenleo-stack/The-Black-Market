using System;
using System.Collections.Generic;
using BlackMarket.Items;

namespace BlackMarket.Customers
{
    public enum VisitorRole
    {
        /// <summary>Brings an item and wants the shop to buy it.</summary>
        Seller,
        /// <summary>Wants to buy an item from the shop.</summary>
        Buyer
    }

    /// <summary>A min/max pair used for randomised per-visitor values.</summary>
    public readonly struct FloatRange
    {
        public FloatRange(float min, float max)
        {
            Min = Math.Min(min, max);
            Max = Math.Max(min, max);
        }

        public float Min { get; }
        public float Max { get; }
    }

    /// <summary>
    /// How a type of visitor behaves when trading. All multipliers are relative to the item's value.
    /// Values here are the game's balance; they are covered by tests in PersonalityCatalogTests.
    /// </summary>
    public sealed class Personality
    {
        public string Id { get; set; }
        public VisitorRole Role { get; set; }

        /// <summary>Short trait shown on the visitor tag, e.g. "Desperate".</summary>
        public string Trait { get; set; }

        /// <summary>Hint shown to the player about how this visitor behaves.</summary>
        public string Hint { get; set; }

        // ---- Sellers
        /// <summary>Opening asking price as a multiple of what the item APPEARS to be worth.</summary>
        public FloatRange AskFactor { get; set; } = new FloatRange(1f, 1f);

        /// <summary>Lowest price they'll accept, as a multiple of what the item is REALLY worth.</summary>
        public FloatRange FloorFactor { get; set; } = new FloatRange(0.8f, 0.8f);

        /// <summary>Chance the item they bring has a hidden flaw they won't mention.</summary>
        public float FlawChance { get; set; }

        /// <summary>Weights for the rarity of items they bring (Common..Legendary). Null = shop default.</summary>
        public float[] RarityWeights { get; set; }

        // ---- Buyers
        /// <summary>Most they'd pay, as a multiple of the item's real value.</summary>
        public FloatRange Valuation { get; set; } = new FloatRange(1f, 1f);

        /// <summary>First bid as a fraction of the most they'd pay.</summary>
        public FloatRange OpeningBid { get; set; } = new FloatRange(0.75f, 0.75f);

        /// <summary>Total coins they carry. Caps what they can pay regardless of valuation.</summary>
        public FloatRange Budget { get; set; } = new FloatRange(500f, 5000f);

        /// <summary>Categories they pay extra for (+15%) and usually ask for.</summary>
        public ItemCategory[] Likes { get; set; } = Array.Empty<ItemCategory>();

        // ---- Both
        /// <summary>Rejected offers they'll tolerate before leaving.</summary>
        public int PatienceMin { get; set; } = 3;
        public int PatienceMax { get; set; } = 4;

        /// <summary>0..1. How far their counter-offer moves toward yours each round.</summary>
        public float Flexibility { get; set; } = 0.4f;

        /// <summary>Which character looks (see CharacterLibrary on the Unity side) can play this personality.</summary>
        public string[] LookIds { get; set; } = Array.Empty<string>();

        public DialogueLines Lines { get; set; } = new DialogueLines();

        public bool LikesCategory(ItemCategory category) => Array.IndexOf(Likes, category) >= 0;
    }

    /// <summary>
    /// What a personality says. {item}, {price} and {category} are replaced when spoken.
    /// Several lines per moment keep repeat visitors from sounding identical.
    /// </summary>
    public sealed class DialogueLines
    {
        public string[] Greeting { get; set; } = { "Good day." };

        /// <summary>Buyer greeting when they want anything from a category rather than one item.</summary>
        public string[] GreetingCategory { get; set; } =
        {
            "Got any {category}? Show me what you have.",
            "I'm after {category}. What have you got?",
            "I need some {category}. Anything on your shelves?"
        };
        public string[] Counter { get; set; } = { "How about {price}?" };
        public string[] FinalOffer { get; set; } = { "{price}. That's my final word." };
        public string[] Insulted { get; set; } = { "Is that a joke? {price}, and be serious." };
        public string[] Accept { get; set; } = { "Deal." };
        public string[] WalkAway { get; set; } = { "Forget it. I'll take my business elsewhere." };
        public string[] Declined { get; set; } = { "Your loss." };
        public string[] WrongItem { get; set; } = { "That's not what I'm after." };

        public static string Fill(string line, string item, long price, string category = null)
        {
            if (line == null) return string.Empty;
            return line.Replace("{item}", item ?? "it")
                       .Replace("{price}", ItemText.Coins(price) + " coins")
                       .Replace("{category}", category ?? "something");
        }

        public static IReadOnlyList<string> Pick(DialogueLines lines, DialogueMoment moment)
        {
            switch (moment)
            {
                case DialogueMoment.Greeting: return lines.Greeting;
                case DialogueMoment.GreetingCategory: return lines.GreetingCategory;
                case DialogueMoment.Counter: return lines.Counter;
                case DialogueMoment.FinalOffer: return lines.FinalOffer;
                case DialogueMoment.Insulted: return lines.Insulted;
                case DialogueMoment.Accept: return lines.Accept;
                case DialogueMoment.WalkAway: return lines.WalkAway;
                case DialogueMoment.Declined: return lines.Declined;
                case DialogueMoment.WrongItem: return lines.WrongItem;
                default: return lines.Greeting;
            }
        }
    }

    public enum DialogueMoment
    {
        Greeting,
        GreetingCategory,
        Counter,
        FinalOffer,
        Insulted,
        Accept,
        WalkAway,
        Declined,
        WrongItem
    }
}
