using System;
using System.Collections.Generic;

namespace BlackMarket.Trading
{
    public enum TradeKind
    {
        Bought,
        Sold,
        Upgrade
    }

    /// <summary>One line in the ledger.</summary>
    public sealed class TradeRecord
    {
        public TradeRecord(int day, string time, TradeKind kind, string description, long amount, long profit)
        {
            Day = day;
            Time = time;
            Kind = kind;
            Description = description;
            Amount = amount;
            Profit = profit;
        }

        public int Day { get; }
        public string Time { get; }
        public TradeKind Kind { get; }
        public string Description { get; }

        /// <summary>Coins that changed hands (always positive).</summary>
        public long Amount { get; }

        /// <summary>For sales: sale price minus what the shop paid. 0 otherwise.</summary>
        public long Profit { get; }
    }

    /// <summary>Running record of every deal, with totals for the day and all time.</summary>
    public sealed class TradeLedger
    {
        private readonly List<TradeRecord> records = new List<TradeRecord>();

        public IReadOnlyList<TradeRecord> Records => records;

        public long TotalProfit { get; private set; }
        public int TotalSales { get; private set; }
        public int TotalPurchases { get; private set; }

        public event Action<TradeRecord> Recorded;

        public void Record(TradeRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            records.Add(record);
            if (record.Kind == TradeKind.Sold)
            {
                TotalSales++;
                TotalProfit += record.Profit;
            }
            else if (record.Kind == TradeKind.Bought)
            {
                TotalPurchases++;
            }

            Recorded?.Invoke(record);
        }

        /// <summary>Replaces the ledger with saved records and all-time totals (used when loading).</summary>
        public void Restore(IEnumerable<TradeRecord> saved, long totalProfit, int totalSales, int totalPurchases)
        {
            records.Clear();
            records.AddRange(saved);
            TotalProfit = totalProfit;
            TotalSales = totalSales;
            TotalPurchases = totalPurchases;
        }

        public DaySummary SummaryForDay(int day)
        {
            var summary = new DaySummary { Day = day };
            foreach (TradeRecord r in records)
            {
                if (r.Day != day) continue;
                switch (r.Kind)
                {
                    case TradeKind.Bought:
                        summary.Purchases++;
                        summary.Spent += r.Amount;
                        break;
                    case TradeKind.Sold:
                        summary.Sales++;
                        summary.Earned += r.Amount;
                        summary.Profit += r.Profit;
                        break;
                    case TradeKind.Upgrade:
                        summary.Invested += r.Amount;
                        break;
                }
            }

            return summary;
        }
    }

    public sealed class DaySummary
    {
        public int Day;
        public int Purchases;
        public int Sales;
        public long Spent;
        public long Earned;
        public long Invested;

        /// <summary>Sum of (sale price - purchase price) over items sold today.</summary>
        public long Profit;

        /// <summary>Net change in coins from trading and upgrades.</summary>
        public long NetCoins => Earned - Spent - Invested;
    }
}
