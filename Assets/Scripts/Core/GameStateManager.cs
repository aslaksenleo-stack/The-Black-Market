using System;
using System.Collections.Generic;

namespace BlackMarket.Core
{
    /// <summary>The high-level mode the game is in.</summary>
    public enum GameState
    {
        /// <summary>Systems are starting up. The game can boot into any playable scene.</summary>
        Booting,
        MainMenu,
        /// <summary>The player is in their shop, behind the counter.</summary>
        Shop,
        /// <summary>The portal is active and a scene change is in progress.</summary>
        Traveling,
        /// <summary>The player is walking around a dimensional marketplace.</summary>
        Marketplace
    }

    /// <summary>
    /// Tracks the current GameState and only allows sensible transitions.
    /// Plain C# (no MonoBehaviour), so it is fully covered by edit-mode tests.
    ///
    /// Travel always goes through Traveling (Shop -> Traveling -> Marketplace and back).
    /// That gives the portal effect, saving and loading one clear moment to happen in.
    /// </summary>
    public sealed class GameStateManager
    {
        private static readonly Dictionary<GameState, GameState[]> AllowedTransitions =
            new Dictionary<GameState, GameState[]>
            {
                { GameState.Booting, new[] { GameState.MainMenu, GameState.Shop, GameState.Marketplace } },
                { GameState.MainMenu, new[] { GameState.Shop } },
                { GameState.Shop, new[] { GameState.Traveling, GameState.MainMenu } },
                { GameState.Traveling, new[] { GameState.Marketplace, GameState.Shop } },
                { GameState.Marketplace, new[] { GameState.Traveling, GameState.MainMenu } }
            };

        public GameStateManager(GameState initialState = GameState.Booting)
        {
            Current = initialState;
        }

        public GameState Current { get; private set; }

        /// <summary>Raised with (previousState, newState) after every successful change.</summary>
        public event Action<GameState, GameState> StateChanged;

        public bool CanChangeTo(GameState next)
        {
            return AllowedTransitions.TryGetValue(Current, out GameState[] targets)
                   && Array.IndexOf(targets, next) >= 0;
        }

        /// <summary>
        /// Changes state if the transition is allowed. Returns false (and changes nothing)
        /// when the transition is not allowed or the game is already in that state.
        /// </summary>
        public bool TryChangeState(GameState next)
        {
            if (next == Current || !CanChangeTo(next))
            {
                return false;
            }

            GameState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
