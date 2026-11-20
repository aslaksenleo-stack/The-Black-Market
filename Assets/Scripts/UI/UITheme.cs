using UnityEngine;

namespace BlackMarket.UI
{
    /// <summary>
    /// The game's visual design system: every UI colour and size comes from here, so screens
    /// stay consistent and the whole look can be changed in one place.
    ///
    /// Style: "merchant's ledger": near-black walnut panels, thin brass borders, warm parchment text.
    /// Flat colours only: no gradients, no glow, square corners (GDD Part 8).
    /// Sizes are in reference pixels for a 1920×1080 landscape screen.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color32(18, 15, 12, 235);
        public static readonly Color Surface = new Color32(38, 32, 26, 255);
        public static readonly Color SurfacePressed = new Color32(66, 54, 40, 255);
        public static readonly Color Border = new Color32(176, 141, 87, 255);  // brass
        public static readonly Color Text = new Color32(237, 230, 214, 255);   // parchment
        public static readonly Color TextMuted = new Color32(163, 154, 136, 255);
        public static readonly Color Accent = new Color32(214, 177, 110, 255); // bright brass
        public static readonly Color Dim = new Color32(0, 0, 0, 150);
        public static readonly Color CrosshairIdle = new Color32(237, 230, 214, 140);

        public const float BorderWidth = 3f;
        public const float ScreenMargin = 32f;
        public const float Gap = 20f;

        public const int FontSizeTitle = 48;
        public const int FontSizeBody = 34;
        public const int FontSizeSmall = 26;

        /// <summary>
        /// Minimum touch target height. At 1080 reference height on a typical phone this is
        /// comfortably above the 48dp accessibility guideline.
        /// </summary>
        public const float ButtonHeight = 140f;

        private static Font font;

        /// <summary>Unity's built-in font. Will be replaced by a licensed game font (see ASSET_CHECKLIST).</summary>
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return font;
            }
        }
    }
}
