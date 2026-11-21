using UnityEngine;

namespace BlackMarket.UI
{
    /// <summary>
    /// The game's UI design system: every colour, size and font comes from here so all screens match.
    ///
    /// Style: dark, warm, slightly translucent panels with soft rounded corners and a hairline light
    /// edge; parchment speech bubbles for what visitors say; one gold accent for money and titles;
    /// green for "do the deal", red only for "decline". No glows, gradients or heavy gold frames.
    /// Sizes are reference pixels on a 1920×1080 landscape screen.
    /// </summary>
    public static class UITheme
    {
        // Surfaces
        public static readonly Color Panel = new Color32(24, 19, 16, 232);
        public static readonly Color PanelSolid = new Color32(30, 24, 20, 250);
        public static readonly Color PanelEdge = new Color32(255, 226, 180, 34);
        public static readonly Color Surface = new Color32(52, 42, 35, 245);
        public static readonly Color SurfacePressed = new Color32(74, 60, 48, 255);
        public static readonly Color Inset = new Color32(14, 11, 9, 150);
        public static readonly Color Shadow = new Color32(0, 0, 0, 110);
        public static readonly Color Dim = new Color32(8, 6, 5, 170);

        // Text
        public static readonly Color Text = new Color32(246, 238, 224, 255);
        public static readonly Color TextMuted = new Color32(186, 172, 152, 255);
        public static readonly Color TextFaint = new Color32(140, 128, 112, 255);
        public static readonly Color Ink = new Color32(46, 34, 26, 255);
        public static readonly Color InkMuted = new Color32(110, 92, 74, 255);

        // Accents
        public static readonly Color Gold = new Color32(236, 192, 104, 255);
        public static readonly Color Positive = new Color32(78, 150, 70, 255);
        public static readonly Color PositiveEdge = new Color32(150, 214, 120, 255);
        public static readonly Color PositiveText = new Color32(134, 210, 112, 255);
        public static readonly Color Negative = new Color32(150, 52, 46, 255);
        public static readonly Color NegativeEdge = new Color32(222, 112, 98, 255);
        public static readonly Color NegativeText = new Color32(236, 120, 104, 255);
        public static readonly Color Warning = new Color32(240, 168, 72, 255);

        // Speech bubble (parchment)
        public static readonly Color Bubble = new Color32(248, 239, 220, 252);
        public static readonly Color NameTag = new Color32(40, 31, 25, 250);

        // Legacy names kept for code written against the first HUD
        public static readonly Color Background = Panel;
        public static readonly Color Border = PanelEdge;
        public static readonly Color Accent = Gold;
        public static readonly Color CrosshairIdle = new Color32(246, 238, 224, 120);

        public const float ScreenMargin = 36f;
        public const float Gap = 18f;
        public const float Radius = 22f;
        public const float BorderWidth = 2f;

        public const int FontSizeHuge = 64;
        public const int FontSizeTitle = 46;
        public const int FontSizeLarge = 40;
        public const int FontSizeBody = 34;
        public const int FontSizeSmall = 28;
        public const int FontSizeTiny = 24;

        /// <summary>Minimum touch target height (well above the 48dp guideline on phones).</summary>
        public const float ButtonHeight = 118f;

        private static Font bold;
        private static Font regular;
        private static Font title;

        /// <summary>Alegreya Sans Bold (SIL Open Font Licence). Labels, buttons, numbers.</summary>
        public static Font Font => bold != null ? bold : bold = Load("Fonts/AlegreyaSans-Bold");

        /// <summary>Alegreya Sans Medium. Longer text: descriptions, dialogue.</summary>
        public static Font BodyFont => regular != null ? regular : regular = Load("Fonts/AlegreyaSans-Medium");

        /// <summary>Alegreya SC Black. Titles and the shop name.</summary>
        public static Font TitleFont => title != null ? title : title = Load("Fonts/AlegreyaSC-Black");

        private static Font Load(string path)
        {
            var f = Resources.Load<Font>(path);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
