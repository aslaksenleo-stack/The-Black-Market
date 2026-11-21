using System; using System.Linq; using BlackMarket.Trading; using BlackMarket.Economy; using BlackMarket.Customers; using BlackMarket.Items; using BlackMarket.Progression;
static class P {
  static (long coins, long profit, int deals, int walk) Play(int seed, bool smart, int days) {
    var g = new ShopGame(new SeededRandom(seed)); int deals = 0, walk = 0;
    for (int d = 0; d < days; d++) {
      while (!g.Clock.IsClosingTime) {
        var v = g.BeginVisit(); if (v == null) break;
        if (v.Role == VisitorRole.Seller) {
          var item = v.OfferedItem;
          if (!smart) { if (g.AcceptCurrentPrice().Success) deals++; }
          else {
            g.Inspect(item); var est = g.Estimate(item);
            long target = (long)(est.Mid * 0.72); long offer = (long)(est.Mid * 0.5);
            while (!g.CurrentNegotiation.IsOver && offer <= target) {
              var r = g.Propose(offer); if (r.Answer.Outcome == NegotiationOutcome.Accepted && r.Trade.Success) { deals++; break; }
              if (r.Answer.Outcome == NegotiationOutcome.WalkedAway) { walk++; break; }
              if (r.Trade.Error != TradeError.NoDeal && r.Trade.Error != TradeError.None) break;
              if (r.Answer.Price <= target) { if (g.AcceptCurrentPrice().Success) deals++; break; }
              offer = (long)(offer * 1.12);
            }
          }
        } else {
          var options = g.Inventory.FindAll(v.Wants);
          if (options.Count > 0) {
            var item = options.OrderByDescending(i => i.TrueValue).First(); g.OfferItemToBuyer(item);
            if (!smart) { g.AcceptCurrentPrice(); deals++; }
            else {
              var est = g.Estimate(item); long ask = (long)Math.Max(est.Mid * 1.35, item.PurchasePrice * 1.2);
              while (!g.CurrentNegotiation.IsOver) {
                var r = g.Propose(ask); if (r.Answer.Outcome == NegotiationOutcome.Accepted) { deals++; break; }
                if (r.Answer.Outcome == NegotiationOutcome.WalkedAway) { walk++; break; }
                if (r.Answer.Price >= item.PurchasePrice * 1.1) { g.AcceptCurrentPrice(); deals++; break; }
                ask = (long)(ask * 0.9); if (ask < item.PurchasePrice) { break; }
              }
            }
          }
        }
        g.EndVisit();
      }
      if (smart) { foreach (var u in new[]{UpgradeId.AppraisalLens, UpgradeId.Storage, UpgradeId.Reputation}) if (!g.Upgrades.IsMaxed(u) && g.Wallet.Coins > g.Upgrades.NextCost(u) * 3) g.TryBuyUpgrade(u); }
      g.CloseDay();
    }
    long stock = g.Inventory.Items.Sum(i => i.PurchasePrice);
    return (g.Wallet.Coins + stock, g.Ledger.TotalProfit, deals, walk);
  }
  static void Main() {
    foreach (bool smart in new[]{true,false}) {
      var res = Enumerable.Range(1, 40).Select(s => Play(s, smart, 7)).ToArray();
      Console.WriteLine($"{(smart?"smart":"naive"),-6} 7 days: worth avg {res.Average(r=>r.coins):N0} (min {res.Min(r=>r.coins):N0}, max {res.Max(r=>r.coins):N0}); profit on sales avg {res.Average(r=>r.profit):N0}; deals {res.Average(r=>r.deals):N1}; walkouts {res.Average(r=>r.walk):N1}");
    }
    var g2 = new ShopGame(new SeededRandom(3)); int visits = 0; while (!g2.Clock.IsClosingTime) { if (g2.BeginVisit()==null) break; visits++; g2.EndVisit(); } Console.WriteLine($"visits per day (no real-time ticking): {visits}");
  }
}
