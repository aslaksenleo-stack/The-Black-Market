using System;

namespace BlackMarket.Economy
{
    /// <summary>The shop's coins. Integer only; the balance can never go negative.</summary>
    public sealed class Wallet
    {
        public Wallet(long startingCoins)
        {
            if (startingCoins < 0) throw new ArgumentOutOfRangeException(nameof(startingCoins));
            Coins = startingCoins;
        }

        public long Coins { get; private set; }

        /// <summary>Raised with (newBalance, change) after every change.</summary>
        public event Action<long, long> Changed;

        public bool CanAfford(long amount) => amount >= 0 && amount <= Coins;

        public bool TrySpend(long amount)
        {
            if (amount < 0 || amount > Coins)
            {
                return false;
            }

            Coins -= amount;
            Changed?.Invoke(Coins, -amount);
            return true;
        }

        public void Add(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to remove coins.");
            Coins += amount;
            Changed?.Invoke(Coins, amount);
        }
    }
}
