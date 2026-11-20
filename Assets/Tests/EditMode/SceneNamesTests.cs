using BlackMarket.Core;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public class SceneNamesTests
    {
        [TestCase(SceneNames.Shop, GameState.Shop)]
        [TestCase(SceneNames.MainMenu, GameState.MainMenu)]
        [TestCase(SceneNames.OldWorldMarketplace, GameState.Marketplace)]
        [TestCase(SceneNames.UnknownMarketplace, GameState.Marketplace)]
        public void PlayableScenes_MapToTheirState(string sceneName, GameState expected)
        {
            Assert.IsTrue(SceneNames.TryGetStateForScene(sceneName, out GameState state));
            Assert.AreEqual(expected, state);
        }

        [TestCase(SceneNames.Bootstrap)]
        [TestCase("SampleScene")]
        [TestCase("")]
        public void NonGameplayScenes_HaveNoState(string sceneName)
        {
            Assert.IsFalse(SceneNames.TryGetStateForScene(sceneName, out _));
        }

        [Test]
        public void Milestone0Scenes_StartWithBootstrap()
        {
            Assert.AreEqual(SceneNames.Bootstrap, SceneNames.Milestone0Scenes[0]);
        }
    }
}
