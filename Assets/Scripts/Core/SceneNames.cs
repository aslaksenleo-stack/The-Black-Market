namespace BlackMarket.Core
{
    /// <summary>
    /// Every scene name in one place, so a typo cannot silently break scene loading.
    /// These must match the scene file names in Assets/Scenes exactly.
    /// </summary>
    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string Shop = "Shop";
        public const string OldWorldMarketplace = "OldWorldMarketplace";
        public const string UnknownMarketplace = "UnknownMarketplace";

        /// <summary>Scenes that must exist and be in Build Settings for Milestone 0.</summary>
        public static readonly string[] Milestone0Scenes = { Bootstrap, Shop };

        /// <summary>
        /// Which GameState a scene represents. Bootstrap has no state of its own, so it returns false.
        /// </summary>
        public static bool TryGetStateForScene(string sceneName, out GameState state)
        {
            switch (sceneName)
            {
                case MainMenu:
                    state = GameState.MainMenu;
                    return true;
                case Shop:
                    state = GameState.Shop;
                    return true;
                case OldWorldMarketplace:
                case UnknownMarketplace:
                    state = GameState.Marketplace;
                    return true;
                default:
                    state = default;
                    return false;
            }
        }
    }
}
