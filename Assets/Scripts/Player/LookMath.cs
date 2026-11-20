using UnityEngine;

namespace BlackMarket.Player
{
    /// <summary>
    /// Camera look calculations, kept separate from Unity components so they can be unit-tested.
    /// Look is stored as (yaw offset, pitch) in degrees. Positive pitch looks down.
    /// </summary>
    public static class LookMath
    {
        /// <summary>Used when the phone does not report its screen density (Screen.dpi = 0).</summary>
        public const float FallbackDpi = 160f;

        public static float EffectiveDpi(float reportedDpi)
        {
            return reportedDpi > 1f ? reportedDpi : FallbackDpi;
        }

        /// <summary>
        /// Applies a finger/mouse movement to the current look angles.
        /// Movement is converted from pixels to inches first, so the same finger movement turns the
        /// camera the same amount on every phone regardless of resolution.
        /// Dragging up looks up (unless invertY).
        /// </summary>
        public static Vector2 Apply(
            Vector2 current,
            Vector2 deltaPixels,
            float dpi,
            float degreesPerInch,
            float yawLimit,
            float minPitch,
            float maxPitch,
            bool invertY = false)
        {
            float degreesPerPixel = degreesPerInch / EffectiveDpi(dpi);

            float yaw = current.x + deltaPixels.x * degreesPerPixel;
            float pitchChange = deltaPixels.y * degreesPerPixel;
            float pitch = current.y + (invertY ? pitchChange : -pitchChange);

            return new Vector2(
                Mathf.Clamp(yaw, -yawLimit, yawLimit),
                Mathf.Clamp(pitch, minPitch, maxPitch));
        }
    }
}
