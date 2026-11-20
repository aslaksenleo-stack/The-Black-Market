using BlackMarket.Core;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public class GameStateManagerTests
    {
        [Test]
        public void StartsInBooting_ByDefault()
        {
            var manager = new GameStateManager();
            Assert.AreEqual(GameState.Booting, manager.Current);
        }

        [Test]
        public void AllowedTransition_ChangesState_AndRaisesEvent()
        {
            var manager = new GameStateManager();
            GameState? previous = null, next = null;
            manager.StateChanged += (p, n) => { previous = p; next = n; };

            bool changed = manager.TryChangeState(GameState.Shop);

            Assert.IsTrue(changed);
            Assert.AreEqual(GameState.Shop, manager.Current);
            Assert.AreEqual(GameState.Booting, previous);
            Assert.AreEqual(GameState.Shop, next);
        }

        [Test]
        public void ShopToMarketplace_WithoutTraveling_IsRejected()
        {
            var manager = new GameStateManager(GameState.Shop);

            Assert.IsFalse(manager.TryChangeState(GameState.Marketplace));
            Assert.AreEqual(GameState.Shop, manager.Current);
        }

        [Test]
        public void FullTravelRoundTrip_IsAllowed()
        {
            var manager = new GameStateManager(GameState.Shop);

            Assert.IsTrue(manager.TryChangeState(GameState.Traveling));
            Assert.IsTrue(manager.TryChangeState(GameState.Marketplace));
            Assert.IsTrue(manager.TryChangeState(GameState.Traveling));
            Assert.IsTrue(manager.TryChangeState(GameState.Shop));
        }

        [Test]
        public void ChangingToSameState_ReturnsFalse_AndDoesNotRaiseEvent()
        {
            var manager = new GameStateManager(GameState.Shop);
            int eventCount = 0;
            manager.StateChanged += (p, n) => eventCount++;

            Assert.IsFalse(manager.TryChangeState(GameState.Shop));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RejectedTransition_DoesNotRaiseEvent()
        {
            var manager = new GameStateManager(GameState.MainMenu);
            int eventCount = 0;
            manager.StateChanged += (p, n) => eventCount++;

            Assert.IsFalse(manager.TryChangeState(GameState.Marketplace));
            Assert.AreEqual(0, eventCount);
        }
    }
}
