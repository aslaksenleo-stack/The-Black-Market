using UnityEngine;

namespace BlackMarket.Player
{
    /// <summary>
    /// What gameplay code is allowed to know about input. The camera, movement and interaction
    /// code read this, never the touchscreen or mouse directly, so controls can change in one place.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Look movement this frame, in screen pixels (x = right, y = up).</summary>
        Vector2 LookDelta { get; }

        /// <summary>Movement direction, length 0..1 (x = strafe, y = forward). Marketplaces only.</summary>
        Vector2 Move { get; }

        /// <summary>True on the single frame the player pressed the interact button (or E in the editor).</summary>
        bool InteractPressed { get; }

        /// <summary>True on the single frame a quick tap on the 3D world (not on UI) finished.</summary>
        bool Tapped { get; }

        /// <summary>Screen position of the last tap. Only meaningful when Tapped is true.</summary>
        Vector2 TapPosition { get; }
    }
}
