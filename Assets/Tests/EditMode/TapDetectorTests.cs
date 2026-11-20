using BlackMarket.Player;
using NUnit.Framework;
using UnityEngine;

namespace BlackMarket.Tests
{
    public class TapDetectorTests
    {
        private TapDetector detector;

        [SetUp]
        public void SetUp()
        {
            detector = new TapDetector { MaxDurationSeconds = 0.3f, MaxMovePixels = 20f };
        }

        [Test]
        public void QuickStillPress_IsTap()
        {
            detector.Begin(new Vector2(100f, 100f), 1f);
            Assert.IsTrue(detector.End(new Vector2(105f, 102f), 1.2f));
        }

        [Test]
        public void LongPress_IsNotTap()
        {
            detector.Begin(new Vector2(100f, 100f), 1f);
            Assert.IsFalse(detector.End(new Vector2(100f, 100f), 1.5f));
        }

        [Test]
        public void Drag_IsNotTap_EvenIfFingerReturnsToStart()
        {
            detector.Begin(new Vector2(100f, 100f), 1f);
            detector.Move(new Vector2(160f, 100f));
            Assert.IsFalse(detector.End(new Vector2(100f, 100f), 1.1f));
        }

        [Test]
        public void ReleaseFarFromStart_IsNotTap()
        {
            detector.Begin(new Vector2(100f, 100f), 1f);
            Assert.IsFalse(detector.End(new Vector2(150f, 100f), 1.1f));
        }

        [Test]
        public void EndWithoutBegin_IsNotTap()
        {
            Assert.IsFalse(detector.End(Vector2.zero, 1f));
        }

        [Test]
        public void Cancel_StopsTracking()
        {
            detector.Begin(Vector2.zero, 1f);
            detector.Cancel();
            Assert.IsFalse(detector.IsTracking);
            Assert.IsFalse(detector.End(Vector2.zero, 1.1f));
        }

        [Test]
        public void SecondEnd_DoesNotCountAsAnotherTap()
        {
            detector.Begin(Vector2.zero, 1f);
            Assert.IsTrue(detector.End(Vector2.zero, 1.1f));
            Assert.IsFalse(detector.End(Vector2.zero, 1.15f));
        }
    }
}
