using BlackMarket.Player;
using NUnit.Framework;
using UnityEngine;

namespace BlackMarket.Tests
{
    public class LookMathTests
    {
        private const float Dpi = 400f;
        private const float DegreesPerInch = 90f;
        private const float YawLimit = 100f;
        private const float MinPitch = -35f;
        private const float MaxPitch = 60f;

        private static Vector2 Apply(Vector2 current, Vector2 deltaPixels, float dpi = Dpi, bool invertY = false)
        {
            return LookMath.Apply(current, deltaPixels, dpi, DegreesPerInch, YawLimit, MinPitch, MaxPitch, invertY);
        }

        [Test]
        public void OneInchSideways_TurnsDegreesPerInch()
        {
            Vector2 result = Apply(Vector2.zero, new Vector2(Dpi, 0f));
            Assert.AreEqual(90f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
        }

        [Test]
        public void SameFingerDistance_TurnsSameAmount_OnDifferentScreenDensities()
        {
            // Half an inch on a 200 dpi and a 500 dpi screen.
            Vector2 lowDensity = Apply(Vector2.zero, new Vector2(100f, 0f), 200f);
            Vector2 highDensity = Apply(Vector2.zero, new Vector2(250f, 0f), 500f);
            Assert.AreEqual(lowDensity.x, highDensity.x, 0.001f);
        }

        [Test]
        public void DraggingUp_LooksUp()
        {
            Vector2 result = Apply(new Vector2(0f, 10f), new Vector2(0f, Dpi * 0.1f));
            Assert.Less(result.y, 10f);
        }

        [Test]
        public void InvertY_ReversesPitchDirection()
        {
            Vector2 result = Apply(new Vector2(0f, 10f), new Vector2(0f, Dpi * 0.1f), invertY: true);
            Assert.Greater(result.y, 10f);
        }

        [Test]
        public void Yaw_IsClampedBothWays()
        {
            Assert.AreEqual(YawLimit, Apply(Vector2.zero, new Vector2(Dpi * 10f, 0f)).x, 0.001f);
            Assert.AreEqual(-YawLimit, Apply(Vector2.zero, new Vector2(-Dpi * 10f, 0f)).x, 0.001f);
        }

        [Test]
        public void Pitch_IsClampedBothWays()
        {
            Assert.AreEqual(MinPitch, Apply(Vector2.zero, new Vector2(0f, Dpi * 10f)).y, 0.001f);
            Assert.AreEqual(MaxPitch, Apply(Vector2.zero, new Vector2(0f, -Dpi * 10f)).y, 0.001f);
        }

        [Test]
        public void UnknownDpi_UsesFallback()
        {
            Assert.AreEqual(LookMath.FallbackDpi, LookMath.EffectiveDpi(0f));
            Vector2 result = Apply(Vector2.zero, new Vector2(LookMath.FallbackDpi, 0f), 0f);
            Assert.AreEqual(90f, result.x, 0.001f);
        }
    }
}
