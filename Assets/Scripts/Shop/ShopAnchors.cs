using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>Named points in the shop that visitors and items move between. Filled in by the scene builder.</summary>
    [DisallowMultipleComponent]
    public sealed class ShopAnchors : MonoBehaviour
    {
        public Vector3 CustomerStart = new Vector3(0.6f, 0f, 11f);
        public Vector3 DoorOutside = new Vector3(0f, 0f, 6.4f);
        public Vector3 DoorInside = new Vector3(0f, 0f, 4.7f);
        public Vector3 CustomerCounter = new Vector3(0f, 0f, 2f);
        public Vector3 ItemSpot = new Vector3(0f, 1.014f, 0.83f);
        public Vector3 StockDrop = new Vector3(0.85f, 0.62f, 0.3f);
    }
}
