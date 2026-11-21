using System.Collections.Generic;
using BlackMarket.Core;
using BlackMarket.Items;
using BlackMarket.Progression;
using BlackMarket.Trading;

namespace BlackMarket.Save
{
    /// <summary>
    /// Checks a save before it is trusted. A save that fails is never loaded (the backup is tried instead),
    /// so a corrupt or tampered file can't put the game into an impossible state.
    /// </summary>
    public static class SaveValidator
    {
        /// <summary>Most ledger lines kept in a save, so the file can't grow forever.</summary>
        public const int MaxLedgerRecords = 300;

        public static List<string> Validate(SaveData data)
        {
            var problems = new List<string>();
            if (data == null)
            {
                problems.Add("Save is empty.");
                return problems;
            }

            if (data.version < 1 || data.version > SaveData.CurrentVersion) problems.Add($"Unknown save version {data.version}.");
            if (data.coins < 0) problems.Add("Negative coins.");
            if (data.day < 1) problems.Add("Day before day 1.");
            if (data.minuteOfDay < DayClock.OpeningMinute || data.minuteOfDay > DayClock.ClosingMinute) problems.Add("Time outside opening hours.");
            if (data.visitCount < 0) problems.Add("Negative visit count.");

            CheckLevel(problems, "Appraisal", data.appraisalLevel, ShopUpgrades.AppraisalLens.MaxLevel);
            CheckLevel(problems, "Storage", data.storageLevel, ShopUpgrades.Storage.MaxLevel);
            CheckLevel(problems, "Reputation", data.reputationLevel, ShopUpgrades.Reputation.MaxLevel);

            if (data.items == null) problems.Add("Missing item list.");
            else
            {
                var upgrades = new ShopUpgrades();
                upgrades.SetLevel(UpgradeId.Storage, data.storageLevel);
                if (data.items.Count > upgrades.InventoryCapacity) problems.Add("More items than storage holds.");

                foreach (SavedItem item in data.items)
                {
                    if (item == null || !ItemCatalog.TryGet(item.id, out _)) problems.Add($"Unknown item '{item?.id}'.");
                    else if (item.condition < 0 || item.condition > (int)ItemCondition.Pristine) problems.Add($"Bad condition for '{item.id}'.");
                    else if (item.paid < 0) problems.Add($"Negative price for '{item.id}'.");
                }
            }

            if (data.ledger == null) problems.Add("Missing ledger.");
            else
            {
                foreach (SavedRecord r in data.ledger)
                {
                    if (r == null || r.kind < 0 || r.kind > (int)TradeKind.Upgrade || r.amount < 0) problems.Add("Bad ledger line.");
                }
            }

            return problems;
        }

        private static void CheckLevel(List<string> problems, string name, int level, int max)
        {
            if (level < 0 || level > max) problems.Add($"{name} level {level} out of range.");
        }
    }
}
