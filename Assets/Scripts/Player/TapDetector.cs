using UnityEngine;

namespace BlackMarket.Player
{
    /// <summary>
    /// Decides whether a press-and-release was a tap (short and nearly still) or a drag.
    /// Plain C# so it can be unit-tested; MobileInputController feeds it positions and times.
    /// </summary>
    public sealed class TapDetector
    {
        private Vector2 startPosition;
        private float startTime;
        private bool movedTooFar;

        /// <summary>Longest press that still counts as a tap.</summary>
        public float MaxDurationSeconds { get; set; } = 0.3f;

        /// <summary>How far the finger may wander (in pixels) and still count as a tap.</summary>
        public float MaxMovePixels { get; set; } = 20f;

        public bool IsTracking { get; private set; }

        public void Begin(Vector2 position, float time)
        {
            startPosition = position;
            startTime = time;
            movedTooFar = false;
            IsTracking = true;
        }

        public void Move(Vector2 position)
        {
            if (IsTracking && (position - startPosition).sqrMagnitude > MaxMovePixels * MaxMovePixels)
            {
                movedTooFar = true;
            }
        }

        /// <summary>Finishes tracking. Returns true if the press was a tap.</summary>
        public bool End(Vector2 position, float time)
        {
            if (!IsTracking)
            {
                return false;
            }

            Move(position);
            IsTracking = false;
            return !movedTooFar && time - startTime <= MaxDurationSeconds;
        }

        public void Cancel()
        {
            IsTracking = false;
        }
    }
}
