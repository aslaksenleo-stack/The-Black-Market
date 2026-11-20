using UnityEngine;

namespace BlackMarket.Player
{
    /// <summary>
    /// What gameplay code is allowed to know about input. The camera and movement code read
    /// this, never the touchscreen or mouse directly, so controls can change in one place.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Look movement this frame, in screen pixels (x = right, y = up).</summary>
        Vector2 LookDelta { get; }

        /// <summary>Movement direction, length 0..1 (x = strafe, y = forward). Marketplaces only.</summary>
        Vector2 Move { get; }

        /// <summary>True on the single frame the player asked to interact.</summary>
        bool InteractPressed { get; }
    }
}
