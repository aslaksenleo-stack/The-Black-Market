using System;
using System.Collections.Generic;
using BlackMarket.Items;

namespace BlackMarket.Inventory
{
    /// <summary>
    /// Items the shop owns, with a capacity limit (raised by the Storage upgrade).
    /// Each physical item is one entry, so two copies of the same item can differ in condition.
    /// </summary>
    public sealed class ShopInventory
    {
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        public ShopInventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public int Capacity { get; private set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public IReadOnlyList<ItemInstance> Items => items;

        public event Action Changed;

        public bool Contains(ItemInstance item) => item != null && items.Contains(item);

        public bool TryAdd(ItemInstance item)
        {
            if (item == null || IsFull || items.Contains(item))
            {
                return false;
            }

            items.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(ItemInstance item)
        {
            if (item == null || !items.Remove(item))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Capacity can only grow; shrinking would silently destroy items.</summary>
        public void IncreaseCapacity(int newCapacity)
        {
            if (newCapacity <= Capacity) return;
            Capacity = newCapacity;
            Changed?.Invoke();
        }

        public List<ItemInstance> FindAll(Predicate<ItemInstance> match)
        {
            return items.FindAll(match);
        }
    }
}
