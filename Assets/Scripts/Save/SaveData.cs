using System;
using System.Collections.Generic;

namespace BlackMarket.Save
{
    /// <summary>
    /// Everything needed to restore the shop, as plain public fields so Unity's JsonUtility can write it.
    /// Bump <see cref="CurrentVersion"/> when the format changes and teach <see cref="SaveValidator"/> to migrate.
    /// A visitor standing at the counter is not saved: deals are atomic, so the shop is always consistent
    /// between visits, and an unfinished conversation simply doesn't resume.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long coins;
        public int day = 1;
        public int minuteOfDay;
        public int visitCount;
        public int appraisalLevel;
        public int storageLevel;
        public int reputationLevel;
        public List<SavedItem> items = new List<SavedItem>();
        public List<SavedRecord> ledger = new List<SavedRecord>();

        // All-time totals (the ledger list itself is trimmed to the latest records)
        public long totalProfit;
        public int totalSales;
        public int totalPurchases;
    }

    [Serializable]
    public sealed class SavedItem
    {
        public string id;
        public int condition;
        public bool flawed;
        public bool inspected;
        public long paid;
    }

    [Serializable]
    public sealed class SavedRecord
    {
        public int day;
        public string time;
        public int kind;
        public string description;
        public long amount;
        public long profit;
    }
}
