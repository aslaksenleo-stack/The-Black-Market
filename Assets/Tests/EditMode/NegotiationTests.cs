using System;
using BlackMarket.Trading;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public sealed class NegotiationTests
    {
        // Seller asks 1000, would accept 700, 3 patience, moves halfway each round.
        private static Negotiation Seller() => new Negotiation(TradeSide.PlayerBuys, 1000, 700, 3, 0.5f);

        // Buyer bids 600, would pay up to 900.
        private static Negotiation Buyer() => new Negotiation(TradeSide.PlayerSells, 600, 900, 3, 0.5f);

        [Test]
        public void Buying_OfferAtOrAboveLimit_IsAcceptedAtPlayersPrice()
        {
            Negotiation n = Seller();
            NegotiationResult r = n.Propose(750);
            Assert.AreEqual(NegotiationOutcome.Accepted, r.Outcome);
            Assert.AreEqual(750, r.Price);
            Assert.AreEqual(750, n.AgreedPrice);
        }

        [Test]
        public void Buying_OfferAboveAsk_PaysOnlyTheAsk()
        {
            NegotiationResult r = Seller().Propose(5000);
            Assert.AreEqual(NegotiationOutcome.Accepted, r.Outcome);
            Assert.AreEqual(1000, r.Price);
        }

        [Test]
        public void Buying_LowOffer_GetsCounterBetweenOfferAndAsk_AndCostsPatience()
        {
            Negotiation n = Seller();
            NegotiationResult r = n.Propose(600); // below 700 limit, not insulting
            Assert.AreEqual(NegotiationOutcome.Countered, r.Outcome);
            Assert.Less(r.Price, 1000);
            Assert.GreaterOrEqual(r.Price, 700);
            Assert.AreEqual(2, n.PatienceLeft);
            Assert.IsFalse(r.Insulted);
        }

        [Test]
        public void Buying_CounterNeverGoesBelowLimit()
        {
            Negotiation n = new Negotiation(TradeSide.PlayerBuys, 1000, 700, 10, 1f);
            NegotiationResult r = n.Propose(650);
            Assert.AreEqual(700, r.Price);
            Assert.AreEqual(NegotiationOutcome.FinalOffer, r.Outcome);
            Assert.IsTrue(n.AtLimit);
        }

        [Test]
        public void Buying_InsultingOffer_CostsTwoPatience()
        {
            Negotiation n = Seller();
            NegotiationResult r = n.Propose(100); // < 700 * 0.6
            Assert.IsTrue(r.Insulted);
            Assert.AreEqual(1, n.PatienceLeft);
        }

        [Test]
        public void RunningOutOfPatience_MakesThemWalkAway()
        {
            Negotiation n = Seller();
            n.Propose(600);
            n.Propose(600);
            NegotiationResult r = n.Propose(600);
            Assert.AreEqual(NegotiationOutcome.WalkedAway, r.Outcome);
            Assert.IsTrue(n.IsOver);
            Assert.IsNull(n.AgreedPrice);
        }

        [Test]
        public void EndedNegotiation_RejectsFurtherOffers()
        {
            Negotiation n = Seller();
            n.AcceptCurrentPrice();
            Assert.Throws<InvalidOperationException>(() => n.Propose(800));
            Assert.Throws<InvalidOperationException>(() => n.AcceptCurrentPrice());
        }

        [Test]
        public void Selling_AskAtOrBelowLimit_IsAccepted()
        {
            NegotiationResult r = Buyer().Propose(850);
            Assert.AreEqual(NegotiationOutcome.Accepted, r.Outcome);
            Assert.AreEqual(850, r.Price);
        }

        [Test]
        public void Selling_AskBelowTheirBid_SellsAtTheirBid()
        {
            NegotiationResult r = Buyer().Propose(300);
            Assert.AreEqual(NegotiationOutcome.Accepted, r.Outcome);
            Assert.AreEqual(600, r.Price);
        }

        [Test]
        public void Selling_HighAsk_RaisesTheirBid_ButNotAboveLimit()
        {
            Negotiation n = Buyer();
            NegotiationResult r = n.Propose(1100);
            Assert.AreEqual(NegotiationOutcome.Countered, r.Outcome);
            Assert.Greater(r.Price, 600);
            Assert.LessOrEqual(r.Price, 900);
        }

        [Test]
        public void SameOffers_GiveSameAnswers()
        {
            Negotiation a = Seller();
            Negotiation b = Seller();
            Assert.AreEqual(a.Propose(610).Price, b.Propose(610).Price);
            Assert.AreEqual(a.Propose(640).Price, b.Propose(640).Price);
        }

        [Test]
        public void SellerOpeningBelowLimit_IsRaisedToLimit()
        {
            var n = new Negotiation(TradeSide.PlayerBuys, 500, 700, 3, 0.5f);
            Assert.AreEqual(700, n.CurrentPrice);
        }

        [Test]
        public void CarriedOverPatience_IsRespected()
        {
            var n = new Negotiation(TradeSide.PlayerSells, 600, 900, 4, 0.5f, patienceLeft: 1);
            Assert.AreEqual(1, n.PatienceLeft);
            Assert.AreEqual(NegotiationOutcome.WalkedAway, n.Propose(2000).Outcome);
        }
    }
}
