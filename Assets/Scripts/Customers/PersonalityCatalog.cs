using System.Collections.Generic;
using BlackMarket.Items;

namespace BlackMarket.Customers
{
    /// <summary>
    /// Every visitor personality, i.e. the game's trading balance in one place.
    ///
    /// Rule of thumb for sellers: FloorFactor below 1 = a profitable deal is possible;
    /// above 1 = they want more than the item is worth (a trap, unless a generous buyer turns up).
    /// For buyers: Valuation above 1 = they'll pay more than the item's value.
    /// </summary>
    public static class PersonalityCatalog
    {
        // Rarity weights Common, Uncommon, Rare, Epic, Legendary.
        private static readonly float[] TreasureWeights = { 0f, 2f, 5f, 2.5f, 0.6f };

        public static readonly Personality Desperate = new Personality
        {
            Id = "seller_desperate", Role = VisitorRole.Seller, Trait = "Desperate",
            Hint = "Needs coin fast. Will take a low offer.",
            AskFactor = new FloatRange(0.6f, 0.85f), FloorFactor = new FloatRange(0.35f, 0.55f),
            PatienceMin = 3, PatienceMax = 5, Flexibility = 0.6f, FlawChance = 0.05f,
            LookIds = new[] { "wanderer", "sailor", "peddler" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Please, I need coin tonight. Will you take this {item}? {price}, or whatever you can spare.",
                    "I have to sell my {item}. {price}? I'm not in a position to argue much.",
                    "Rent's due and I'm short. This {item} for {price}... please?"
                },
                Counter = new[] { "I... could do {price}.", "{price}? Please, I really need this.", "Alright, alright. {price}." },
                FinalOffer = new[] { "{price}. I can't go lower, I'm sorry.", "I'd be giving it away below {price}." },
                Insulted = new[] { "That's... that's cruel. {price}, please.", "I'm desperate, not a fool. {price}." },
                Accept = new[] { "Thank you. Truly.", "Oh, thank the stars. Deal.", "It's yours. Thank you." },
                WalkAway = new[] { "I'll try somewhere else. Somewhere kinder.", "Forget it. I'll manage somehow." },
                Declined = new[] { "I understand. Thank you anyway.", "Right. Of course." }
            }
        };

        public static readonly Personality FairTrader = new Personality
        {
            Id = "seller_fair", Role = VisitorRole.Seller, Trait = "Honest",
            Hint = "Asks a fair price. Doesn't budge much.",
            AskFactor = new FloatRange(0.9f, 1.08f), FloorFactor = new FloatRange(0.7f, 0.85f),
            PatienceMin = 3, PatienceMax = 4, Flexibility = 0.35f, FlawChance = 0f,
            LookIds = new[] { "peddler", "tinker", "explorer", "elder" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Got a {item} here. I'm asking {price}, which is fair.",
                    "This {item} needs a new home. {price} and it's yours.",
                    "Fair price for a fair {item}: {price}."
                },
                Counter = new[] { "I could meet you at {price}.", "Let's say {price}.", "{price}, and we both walk away happy." },
                FinalOffer = new[] { "{price} is as low as I go.", "No lower than {price}, friend." },
                Insulted = new[] { "Come on now. {price}.", "Let's keep this honest. {price}." },
                Accept = new[] { "Pleasure doing business.", "Done. Take good care of it.", "A fair deal." },
                WalkAway = new[] { "We're too far apart. Good day.", "I'll find another buyer." },
                Declined = new[] { "Fair enough.", "Maybe next time." }
            }
        };

        public static readonly Personality Haggler = new Personality
        {
            Id = "seller_haggler", Role = VisitorRole.Seller, Trait = "Haggler",
            Hint = "Opens high, loves to bargain.",
            AskFactor = new FloatRange(1.25f, 1.6f), FloorFactor = new FloatRange(0.65f, 0.85f),
            PatienceMin = 4, PatienceMax = 6, Flexibility = 0.45f, FlawChance = 0.1f,
            LookIds = new[] { "explorer", "peddler", "sailor", "tinker" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "I've got some interesting things from my last trip. This {item}: {price}. A bargain!",
                    "Feast your eyes on this {item}! Yours for just {price}.",
                    "Rare find, this {item}. {price}, and I'm robbing myself."
                },
                Counter = new[] { "Ha! You drive a hard bargain. {price}.", "Ooh, you're good. {price}?", "Let's meet in the middle: {price}." },
                FinalOffer = new[] { "{price}! Not a copper less.", "You've squeezed me dry. {price}, final." },
                Insulted = new[] { "Ha! Nice try. {price}.", "You wound me! {price}, and stop joking." },
                Accept = new[] { "Done! You'd make a fine pirate.", "Ha! Deal, you shark.", "Sold! Pleasure haggling with you." },
                WalkAway = new[] { "Bah! You're no fun. I'm off.", "My patience has limits, friend." },
                Declined = new[] { "Your loss! Someone else will snap it up.", "Pfft. Fine." }
            }
        };

        public static readonly Personality Greedy = new Personality
        {
            Id = "seller_greedy", Role = VisitorRole.Seller, Trait = "Greedy",
            Hint = "Wants far more than it's worth.",
            AskFactor = new FloatRange(1.7f, 2.3f), FloorFactor = new FloatRange(1.1f, 1.4f),
            PatienceMin = 2, PatienceMax = 3, Flexibility = 0.25f, FlawChance = 0.1f,
            LookIds = new[] { "noble", "elder" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "A {item} of the finest quality. {price}. I won't haggle.",
                    "You won't see a {item} like this again. {price}.",
                    "{price} for this {item}. Frankly you should thank me."
                },
                Counter = new[] { "Hmph. {price}.", "{price}, and that's generous." },
                FinalOffer = new[] { "{price}. I don't negotiate with shopkeepers.", "Not one coin under {price}." },
                Insulted = new[] { "How dare you. {price}.", "Do you know who I am? {price}." },
                Accept = new[] { "Finally, someone with taste.", "Hmph. Acceptable." },
                WalkAway = new[] { "I've wasted enough time here.", "Peasant prices. Good day." },
                Declined = new[] { "Your shop is beneath this {item} anyway.", "Hmph." }
            }
        };

        public static readonly Personality Shady = new Personality
        {
            Id = "seller_shady", Role = VisitorRole.Seller, Trait = "Shifty",
            Hint = "Avoids eye contact. Inspect before you buy.",
            AskFactor = new FloatRange(0.85f, 1.15f), FloorFactor = new FloatRange(0.85f, 1.2f),
            PatienceMin = 2, PatienceMax = 4, Flexibility = 0.5f, FlawChance = 0.75f,
            LookIds = new[] { "wanderer" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "I want to sell this {item}. {price}. Don't ask where it's from.",
                    "Psst. {item}. Perfect condition. {price}, quick, before anyone sees.",
                    "This {item}... it's genuine. {price}. No questions."
                },
                Counter = new[] { "Fine. {price}. Quickly now.", "{price}. Hurry up." },
                FinalOffer = new[] { "{price}. Last offer, I have to go.", "{price} or I walk." },
                Insulted = new[] { "Don't play games with me. {price}.", "Tsk. {price}." },
                Accept = new[] { "Good. We never met.", "Pleasure. Forget my face." },
                WalkAway = new[] { "Too many questions. I'm gone.", "Forget it." },
                Declined = new[] { "Suit yourself.", "Your loss. Don't follow me." }
            }
        };

        public static readonly Personality TreasureHunter = new Personality
        {
            Id = "seller_treasure", Role = VisitorRole.Seller, Trait = "Treasure Hunter",
            Hint = "Brings rare finds. Knows what they're worth.",
            AskFactor = new FloatRange(1.05f, 1.35f), FloorFactor = new FloatRange(0.75f, 0.92f),
            PatienceMin = 3, PatienceMax = 4, Flexibility = 0.35f, FlawChance = 0.05f,
            RarityWeights = TreasureWeights,
            LookIds = new[] { "explorer", "sailor" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Dug this {item} out of a collapsed vault. {price}. It's worth every coin.",
                    "Three weeks in the marshes for this {item}. {price}.",
                    "Not many of these left. {item}, {price}."
                },
                Counter = new[] { "I can do {price}.", "For you, {price}." },
                FinalOffer = new[] { "{price}. I nearly died for this.", "{price}, final." },
                Insulted = new[] { "I nearly died for this. {price}.", "You don't know what you're looking at. {price}." },
                Accept = new[] { "Deal. Treat it with respect.", "Good. Now I can afford new boots." },
                WalkAway = new[] { "I'll find a collector who appreciates it.", "Not worth my time." },
                Declined = new[] { "Plenty of collectors in town.", "Your loss." }
            }
        };

        // ------------------------------------------------------------------ Buyers

        public static readonly Personality Collector = new Personality
        {
            Id = "buyer_collector", Role = VisitorRole.Buyer, Trait = "Collector",
            Hint = "Pays well for rare and curious things.",
            Valuation = new FloatRange(1.15f, 1.45f), OpeningBid = new FloatRange(0.68f, 0.82f),
            Budget = new FloatRange(2500f, 14000f), PatienceMin = 3, PatienceMax = 5, Flexibility = 0.45f,
            Likes = new[] { ItemCategory.Collectibles, ItemCategory.Artifacts, ItemCategory.Jewelry },
            LookIds = new[] { "elder", "noble", "scholar" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "I collect {category}. Do you have a {item}?",
                    "I'm looking for a {item} for my collection. Anything?",
                    "I hear you deal in {category}. I'm after a {item}."
                },
                Counter = new[] { "Mm. I could stretch to {price}.", "{price}, for the collection's sake." },
                FinalOffer = new[] { "{price}. My collection has a budget, sadly.", "{price} is my limit." },
                Insulted = new[] { "I'm a collector, not a fool. {price}.", "Absurd. {price}." },
                Accept = new[] { "Marvellous. It'll have pride of place.", "Splendid! Wrap it carefully." },
                WalkAway = new[] { "Too rich for me. Good day.", "I'll wait for an auction." },
                Declined = new[] { "A pity. Do keep an eye out.", "Ah well. Another time." },
                WrongItem = new[] { "Charming, but not what I collect.", "No, no. That won't do." }
            }
        };

        public static readonly Personality BargainHunter = new Personality
        {
            Id = "buyer_bargain", Role = VisitorRole.Buyer, Trait = "Penny-pincher",
            Hint = "Small budget. Haggles hard.",
            Valuation = new FloatRange(0.85f, 1.05f), OpeningBid = new FloatRange(0.55f, 0.7f),
            Budget = new FloatRange(150f, 1800f), PatienceMin = 4, PatienceMax = 6, Flexibility = 0.35f,
            Likes = new[] { ItemCategory.Tools, ItemCategory.Materials },
            LookIds = new[] { "peddler", "tinker", "sailor" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Need a {item}. Something cheap, mind.",
                    "Got any {category}? A {item}, ideally. I'm not made of coin.",
                    "Looking for a {item}. Don't go robbing me now."
                },
                Counter = new[] { "{price}. That's a lot of coin for me.", "Ugh. {price}?", "I could scrape together {price}." },
                FinalOffer = new[] { "{price}. That's everything in my purse.", "{price}. I'll eat soup all week." },
                Insulted = new[] { "Are you mad? {price}.", "Ha! No. {price}." },
                Accept = new[] { "Fine, fine. Deal.", "Daylight robbery, but deal." },
                WalkAway = new[] { "Too steep. I'll make do.", "Forget it." },
                Declined = new[] { "Huh. Alright.", "Never mind then." },
                WrongItem = new[] { "That's not what I asked for.", "Nah, I need the other thing." }
            }
        };

        public static readonly Personality Noble = new Personality
        {
            Id = "buyer_noble", Role = VisitorRole.Buyer, Trait = "Wealthy",
            Hint = "Rich and impatient. Pays well, haggles little.",
            Valuation = new FloatRange(1.25f, 1.6f), OpeningBid = new FloatRange(0.8f, 0.9f),
            Budget = new FloatRange(5000f, 25000f), PatienceMin = 1, PatienceMax = 2, Flexibility = 0.6f,
            Likes = new[] { ItemCategory.Jewelry, ItemCategory.Technology },
            LookIds = new[] { "noble" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "I require a {item}. Quickly, please.",
                    "My steward says you might have a {item}. Do you?",
                    "A {item}. Have you one or not?"
                },
                Counter = new[] { "Hmm. {price}, and be quick.", "{price}. I haven't all day." },
                FinalOffer = new[] { "{price}. Take it or leave it.", "{price}. My patience is ending." },
                Insulted = new[] { "Do I look like a fool? {price}.", "Outrageous. {price}." },
                Accept = new[] { "Good. Have it delivered.", "Acceptable. Here." },
                WalkAway = new[] { "I've no time for this.", "Tiresome. Good day." },
                Declined = new[] { "Then why am I here?", "Hmph." },
                WrongItem = new[] { "That is clearly not a {item}.", "Are you deliberately wasting my time?" }
            }
        };

        public static readonly Personality Scholar = new Personality
        {
            Id = "buyer_scholar", Role = VisitorRole.Buyer, Trait = "Scholar",
            Hint = "Wants specimens and artefacts for study.",
            Valuation = new FloatRange(1.0f, 1.25f), OpeningBid = new FloatRange(0.65f, 0.8f),
            Budget = new FloatRange(800f, 6000f), PatienceMin = 3, PatienceMax = 4, Flexibility = 0.4f,
            Likes = new[] { ItemCategory.Specimens, ItemCategory.Artifacts, ItemCategory.Materials, ItemCategory.Technology },
            LookIds = new[] { "scholar", "elder", "tinker" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Fascinating shop. I'm researching {category}. Would you have a {item}?",
                    "For my studies I need a {item}. Any chance?",
                    "The academy needs a {item}. Do you stock {category}?"
                },
                Counter = new[] { "The grant allows {price}.", "Mm. {price}, for science." },
                FinalOffer = new[] { "{price}. The academy won't pay more.", "{price} is my whole grant." },
                Insulted = new[] { "That's... statistically absurd. {price}.", "Please. {price}." },
                Accept = new[] { "Excellent! This will be most useful.", "Splendid. For science!" },
                WalkAway = new[] { "I'll have to rethink my research.", "Too costly, I'm afraid." },
                Declined = new[] { "A shame. I'll check the library.", "Ah. Never mind." },
                WrongItem = new[] { "No, that isn't what I'm studying.", "Interesting, but irrelevant." }
            }
        };

        public static readonly Personality Adventurer = new Personality
        {
            Id = "buyer_adventurer", Role = VisitorRole.Buyer, Trait = "Adventurer",
            Hint = "Needs gear before a trip.",
            Valuation = new FloatRange(0.95f, 1.25f), OpeningBid = new FloatRange(0.65f, 0.8f),
            Budget = new FloatRange(400f, 5000f), PatienceMin = 2, PatienceMax = 4, Flexibility = 0.45f,
            Likes = new[] { ItemCategory.Tools, ItemCategory.Artifacts },
            LookIds = new[] { "explorer", "sailor" },
            Lines = new DialogueLines
            {
                Greeting = new[]
                {
                    "Heading out tomorrow. Got a {item}?",
                    "I need a {item} before the caravan leaves. Anything?",
                    "Know where I can find a {item}? Hoping you've got one."
                },
                Counter = new[] { "I can manage {price}.", "{price}, and I'll mention you in my tales." },
                FinalOffer = new[] { "{price}. I still need to buy rope.", "{price}. That's my travel money." },
                Insulted = new[] { "Ha, I'm not that green. {price}.", "Come on. {price}." },
                Accept = new[] { "Perfect. Wish me luck!", "Deal! This'll save my neck." },
                WalkAway = new[] { "Too rich. I'll improvise.", "Never mind, I'm off." },
                Declined = new[] { "No luck then. Cheers anyway.", "Right, I'll look elsewhere." },
                WrongItem = new[] { "Nah, that won't help where I'm going.", "Not quite what I need." }
            }
        };

        public static readonly IReadOnlyList<Personality> Sellers = new[] { Desperate, FairTrader, Haggler, Greedy, Shady, TreasureHunter };
        public static readonly IReadOnlyList<Personality> Buyers = new[] { Collector, BargainHunter, Noble, Scholar, Adventurer };

        /// <summary>How often each seller type appears (same order as Sellers).</summary>
        public static readonly float[] SellerWeights = { 1.2f, 1.6f, 1.6f, 0.8f, 0.9f, 0.7f };

        /// <summary>How often each buyer type appears (same order as Buyers).</summary>
        public static readonly float[] BuyerWeights = { 1f, 1.3f, 0.6f, 1f, 1.1f };

        /// <summary>Display name for a look id, used as the visitor's name tag.</summary>
        public static string LookDisplayName(string lookId)
        {
            switch (lookId)
            {
                case "explorer": return "Explorer";
                case "wanderer": return "Wanderer";
                case "noble": return "Noble";
                case "scholar": return "Scholar";
                case "peddler": return "Peddler";
                case "elder": return "Old Collector";
                case "tinker": return "Tinkerer";
                case "sailor": return "Sailor";
                default: return "Visitor";
            }
        }
    }
}
