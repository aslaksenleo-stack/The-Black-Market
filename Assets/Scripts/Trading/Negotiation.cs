using System;
using BlackMarket.Items;

namespace BlackMarket.Trading
{
    /// <summary>Which side of the deal the player is on.</summary>
    public enum TradeSide
    {
        /// <summary>The player is buying from a seller: lower is better for the player.</summary>
        PlayerBuys,
        /// <summary>The player is selling to a buyer: higher is better for the player.</summary>
        PlayerSells
    }

    public enum NegotiationOutcome
    {
        Accepted,
        Countered,
        /// <summary>Countered, but the price can't move any more in the player's favour.</summary>
        FinalOffer,
        WalkedAway
    }

    public readonly struct NegotiationResult
    {
        public NegotiationResult(NegotiationOutcome outcome, long price, bool insulted)
        {
            Outcome = outcome;
            Price = price;
            Insulted = insulted;
        }

        public NegotiationOutcome Outcome { get; }

        /// <summary>Agreed price when Accepted; the counterpart's new price when Countered/FinalOffer.</summary>
        public long Price { get; }

        /// <summary>The player's offer was so far off that it cost extra patience.</summary>
        public bool Insulted { get; }
    }

    /// <summary>
    /// One haggling session with one visitor. Deterministic: the same offers always give the same answers.
    ///
    /// The counterpart has a hidden limit (the lowest a seller accepts / the most a buyer pays) and a
    /// visible current price. Each offer:
    /// - meets their current price → deal at their price;
    /// - is on the right side of their hidden limit → deal at the player's price;
    /// - otherwise costs 1 patience (2 if insulting) and they move part of the way toward the offer,
    ///   never past their limit. At 0 patience they walk away.
    /// </summary>
    public sealed class Negotiation
    {
        /// <summary>Offers this far past the limit (as a fraction of it) are insulting.</summary>
        public const float InsultMargin = 0.4f;

        private readonly long limit;
        private readonly float flexibility;

        /// <param name="patienceLeft">Start with less than full patience (0 = full).</param>
        public Negotiation(TradeSide side, long openingPrice, long limit, int patience, float flexibility, int patienceLeft = 0)
        {
            if (openingPrice <= 0) throw new ArgumentOutOfRangeException(nameof(openingPrice));
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
            if (patience <= 0) throw new ArgumentOutOfRangeException(nameof(patience));

            Side = side;
            // A seller never opens below their own limit, a buyer never opens above theirs.
            CurrentPrice = side == TradeSide.PlayerBuys ? Math.Max(openingPrice, limit) : Math.Min(openingPrice, limit);
            OpeningPrice = CurrentPrice;
            this.limit = limit;
            this.flexibility = Math.Max(0.05f, Math.Min(1f, flexibility));
            MaxPatience = patience;
            PatienceLeft = patienceLeft > 0 ? Math.Min(patienceLeft, patience) : patience;
        }

        public TradeSide Side { get; }
        public long OpeningPrice { get; }

        /// <summary>The counterpart's current price: the seller's ask, or the buyer's bid.</summary>
        public long CurrentPrice { get; private set; }

        public int MaxPatience { get; }
        public int PatienceLeft { get; private set; }
        public int Rounds { get; private set; }

        public bool IsOver => AgreedPrice.HasValue || WalkedAway;
        public bool WalkedAway { get; private set; }
        public long? AgreedPrice { get; private set; }

        /// <summary>True once the counterpart's price has reached their limit.</summary>
        public bool AtLimit => CurrentPrice == limit;

        /// <summary>Player accepts the counterpart's current price.</summary>
        public long AcceptCurrentPrice()
        {
            ThrowIfOver();
            AgreedPrice = CurrentPrice;
            return CurrentPrice;
        }

        /// <summary>Player proposes a price. Returns how the counterpart responds.</summary>
        public NegotiationResult Propose(long playerPrice)
        {
            ThrowIfOver();
            if (playerPrice <= 0) throw new ArgumentOutOfRangeException(nameof(playerPrice), "Offers must be positive.");

            Rounds++;
            bool playerBuys = Side == TradeSide.PlayerBuys;

            // Meets or beats their price: deal at their price (nobody refuses a better deal than they asked for).
            if (playerBuys ? playerPrice >= CurrentPrice : playerPrice <= CurrentPrice)
            {
                AgreedPrice = CurrentPrice;
                return new NegotiationResult(NegotiationOutcome.Accepted, CurrentPrice, false);
            }

            // Within their hidden limit: deal at the player's price.
            if (playerBuys ? playerPrice >= limit : playerPrice <= limit)
            {
                AgreedPrice = playerPrice;
                return new NegotiationResult(NegotiationOutcome.Accepted, playerPrice, false);
            }

            bool insulted = playerBuys
                ? playerPrice < limit * (1f - InsultMargin)
                : playerPrice > limit * (1f + InsultMargin);

            PatienceLeft = Math.Max(0, PatienceLeft - (insulted ? 2 : 1));
            if (PatienceLeft == 0)
            {
                WalkedAway = true;
                return new NegotiationResult(NegotiationOutcome.WalkedAway, CurrentPrice, insulted);
            }

            // Move toward the offer. Insulting offers earn a much smaller step.
            double step = (playerPrice - CurrentPrice) * flexibility * (insulted ? 0.25 : 1.0);
            long next = ItemInstance.RoundValue(CurrentPrice + step);
            next = playerBuys
                ? Math.Max(limit, Math.Min(next, CurrentPrice))   // seller: only comes down, never below limit
                : Math.Min(limit, Math.Max(next, CurrentPrice));  // buyer: only goes up, never above limit

            CurrentPrice = next;
            return new NegotiationResult(AtLimit ? NegotiationOutcome.FinalOffer : NegotiationOutcome.Countered, CurrentPrice, insulted);
        }

        /// <summary>The player ends the conversation without a deal.</summary>
        public void Decline()
        {
            if (!IsOver)
            {
                WalkedAway = true;
            }
        }

        private void ThrowIfOver()
        {
            if (IsOver) throw new InvalidOperationException("This negotiation has already ended.");
        }
    }
}
