using BlackMarket.Items;
using UnityEngine;

namespace BlackMarket.Art
{
    /// <summary>
    /// Every colour used by 3D art, in one place, so the shop, characters and items share one look:
    /// warm woods and brass, muted cloth, with saturated colour reserved for rare items and glows.
    /// </summary>
    public static class Palette
    {
        // Woods (tinted onto the grain textures)
        public static readonly Color WoodLight = Hex(0xB07A4A);
        public static readonly Color WoodMid = Hex(0x8A5A34);
        public static readonly Color WoodDark = Hex(0x5A3620);
        public static readonly Color WoodDeep = Hex(0x3A2216);

        // Building
        public static readonly Color Brick = Hex(0x9A5A42);
        public static readonly Color Plaster = Hex(0xC9AE8A);
        public static readonly Color Stone = Hex(0x8A8078);
        public static readonly Color StoneDark = Hex(0x5E5650);

        // Metals
        public static readonly Color Brass = Hex(0xD9A649);
        public static readonly Color BrassDark = Hex(0x9C7330);
        public static readonly Color Gold = Hex(0xF2C14E);
        public static readonly Color Silver = Hex(0xC9CCD2);
        public static readonly Color Iron = Hex(0x55565E);
        public static readonly Color Pewter = Hex(0x8E929A);
        public static readonly Color Steel = Hex(0xA4ABB5);

        // Materials
        public static readonly Color Leather = Hex(0x7A3E26);
        public static readonly Color LeatherDark = Hex(0x4E2818);
        public static readonly Color Parchment = Hex(0xEADBB4);
        public static readonly Color Bone = Hex(0xE8DEC6);
        public static readonly Color Wax = Hex(0xB3262E);
        public static readonly Color Velvet = Hex(0x7A1E36);
        public static readonly Color Clay = Hex(0x8C5A44);
        public static readonly Color Cork = Hex(0xA0784E);

        // Cloth
        public static readonly Color ClothRed = Hex(0x8E2A34);
        public static readonly Color ClothPurple = Hex(0x5A3270);
        public static readonly Color ClothBlue = Hex(0x2F4D7A);
        public static readonly Color ClothGreen = Hex(0x4A6B3A);
        public static readonly Color ClothBrown = Hex(0x6B4A30);
        public static readonly Color ClothGrey = Hex(0x6E6A66);
        public static readonly Color ClothCream = Hex(0xE6D8BC);
        public static readonly Color Fur = Hex(0xEDE3D0);

        // Light and magic
        public static readonly Color Flame = Hex(0xFFC46B);
        public static readonly Color LampGlass = Hex(0xFFD28A);
        public static readonly Color Crystal = Hex(0x5CB8FF);
        public static readonly Color Ember = Hex(0xFF7A2E);
        public static readonly Color Arcane = Hex(0x9B6BFF);
        public static readonly Color SeaGlass = Hex(0x5FD6C0);
        public static readonly Color Potion = Hex(0xE0303C);
        public static readonly Color Ink = Hex(0x1B1612);
        public static readonly Color NightSky = Hex(0x2A2448);

        // Skin tones for characters
        public static readonly Color[] Skin =
        {
            Hex(0xF2C8A2), Hex(0xE0A982), Hex(0xC68862), Hex(0x9C6644), Hex(0x6E4630)
        };

        public static readonly Color[] Hair =
        {
            Hex(0x2B1D14), Hex(0x4A2E1C), Hex(0x7A4A26), Hex(0xB86A2E), Hex(0xC8C2B8), Hex(0x1C1C22)
        };

        /// <summary>Rarity colours, shared by the 3D glow accents and the UI badges.</summary>
        public static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return Hex(0x6CC24A);
                case Rarity.Rare: return Hex(0x4A9DF0);
                case Rarity.Epic: return Hex(0xB46CF0);
                case Rarity.Legendary: return Hex(0xF0A030);
                default: return Hex(0xB8B0A2);
            }
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        /// <summary>Multiplies brightness, keeps hue.</summary>
        public static Color Shade(Color c, float factor)
        {
            return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor), Mathf.Clamp01(c.b * factor), c.a);
        }

        public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);
    }
}
