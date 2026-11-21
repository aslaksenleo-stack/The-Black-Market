using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackMarket.UI
{
    public enum Icon
    {
        Coin,
        Calendar,
        Box,
        ArrowUp,
        Book,
        Bell,
        Magnifier,
        Check,
        Cross,
        Chat,
        Clock,
        Star,
        Warning
    }

    /// <summary>
    /// UI sprites drawn in code: rounded panels (9-sliced so they stretch cleanly), a soft shadow,
    /// circles and a small set of simple, consistent line icons. Drawn with signed-distance shapes,
    /// so edges are anti-aliased. Built once and cached.
    /// </summary>
    public static class UISprites
    {
        private const int RoundSize = 64;
        private const float RoundRadius = 24f;
        private const int IconSize = 96;

        private static Sprite rounded;
        private static Sprite roundedEdge;
        private static Sprite shadow;
        private static Sprite circle;
        private static Sprite pill;
        private static readonly Dictionary<Icon, Sprite> Icons = new Dictionary<Icon, Sprite>();

        /// <summary>Rounded rectangle, 9-sliced. Corner radius ≈ 24 reference px.</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(false);

        /// <summary>Rounded outline only (2 px), for hairline borders.</summary>
        public static Sprite RoundedEdge => roundedEdge != null ? roundedEdge : roundedEdge = MakeRounded(true);

        /// <summary>Soft blurred rounded shape for drop shadows.</summary>
        public static Sprite Shadow => shadow != null ? shadow : shadow = MakeShadow();

        public static Sprite Circle => circle != null ? circle : circle = MakeSprite(IconSize, p => SdCircle(p, new Vector2(0.5f, 0.5f), 0.46f), Vector4.zero);

        /// <summary>Fully rounded ends (capsule), 9-sliced.</summary>
        public static Sprite Pill => pill != null ? pill : pill = MakeSprite(RoundSize, p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.47f, 0.47f), 0.47f),
            new Vector4(31, 31, 31, 31));

        public static Sprite Get(Icon icon)
        {
            if (!Icons.TryGetValue(icon, out Sprite s))
            {
                s = MakeSprite(IconSize, IconShape(icon), Vector4.zero);
                Icons[icon] = s;
            }

            return s;
        }

        // ------------------------------------------------------------------ Icon shapes (0..1 space, y up)

        private static Func<Vector2, float> IconShape(Icon icon)
        {
            const float stroke = 0.055f;
            switch (icon)
            {
                case Icon.Coin:
                    return Coin;
                case Icon.Calendar:
                    return p => Min(
                        Max(Box(p, C(0.5f, 0.45f), new Vector2(0.36f, 0.33f), 0.08f), -Box(p, C(0.5f, 0.38f), new Vector2(0.29f, 0.19f), 0.03f)),
                        Box(p, C(0.34f, 0.8f), new Vector2(0.045f, 0.09f), 0.04f),
                        Box(p, C(0.66f, 0.8f), new Vector2(0.045f, 0.09f), 0.04f));
                case Icon.Box:
                    return p => Min(
                        Max(Box(p, C(0.5f, 0.4f), new Vector2(0.32f, 0.26f), 0.05f), -Box(p, C(0.5f, 0.4f), new Vector2(0.25f, 0.19f), 0.02f)),
                        Box(p, C(0.5f, 0.74f), new Vector2(0.38f, 0.09f), 0.05f),
                        Box(p, C(0.5f, 0.52f), new Vector2(0.09f, 0.035f), 0.02f));
                case Icon.ArrowUp:
                    return p => Min(Triangle(p, C(0.5f, 0.88f), C(0.18f, 0.52f), C(0.82f, 0.52f)), Box(p, C(0.5f, 0.32f), new Vector2(0.1f, 0.24f), 0.04f));
                case Icon.Book:
                    return p => Min(
                        Max(Box(p, C(0.29f, 0.5f), new Vector2(0.21f, 0.33f), 0.05f), -Box(p, C(0.29f, 0.5f), new Vector2(0.15f, 0.27f), 0.01f)),
                        Max(Box(p, C(0.71f, 0.5f), new Vector2(0.21f, 0.33f), 0.05f), -Box(p, C(0.71f, 0.5f), new Vector2(0.15f, 0.27f), 0.01f)),
                        Box(p, C(0.29f, 0.58f), new Vector2(0.1f, 0.025f), 0.01f), Box(p, C(0.29f, 0.45f), new Vector2(0.1f, 0.025f), 0.01f),
                        Box(p, C(0.71f, 0.58f), new Vector2(0.1f, 0.025f), 0.01f), Box(p, C(0.71f, 0.45f), new Vector2(0.1f, 0.025f), 0.01f));
                case Icon.Bell:
                    return p => Min(
                        Min(SdCircle(p, C(0.5f, 0.55f), 0.27f), Box(p, C(0.5f, 0.4f), new Vector2(0.27f, 0.15f), 0.02f)),
                        Box(p, C(0.5f, 0.24f), new Vector2(0.38f, 0.05f), 0.04f),
                        SdCircle(p, C(0.5f, 0.13f), 0.07f),
                        SdCircle(p, C(0.5f, 0.86f), 0.05f));
                case Icon.Magnifier:
                    return p => Min(Ring(p, C(0.42f, 0.58f), 0.24f, stroke * 1.2f), Segment(p, C(0.6f, 0.4f), C(0.84f, 0.16f), 0.075f));
                case Icon.Check:
                    return p => Min(Segment(p, C(0.18f, 0.5f), C(0.4f, 0.27f), 0.08f), Segment(p, C(0.4f, 0.27f), C(0.84f, 0.74f), 0.08f));
                case Icon.Cross:
                    return p => Min(Segment(p, C(0.22f, 0.22f), C(0.78f, 0.78f), 0.08f), Segment(p, C(0.22f, 0.78f), C(0.78f, 0.22f), 0.08f));
                case Icon.Chat:
                    return p => Min(Box(p, C(0.5f, 0.58f), new Vector2(0.38f, 0.26f), 0.12f), Triangle(p, C(0.26f, 0.36f), C(0.46f, 0.36f), C(0.2f, 0.14f)));
                case Icon.Clock:
                    return p => Min(Ring(p, C(0.5f, 0.5f), 0.36f, stroke), Segment(p, C(0.5f, 0.5f), C(0.5f, 0.75f), 0.05f), Segment(p, C(0.5f, 0.5f), C(0.68f, 0.42f), 0.05f));
                case Icon.Star:
                    return p => Star(p);
                case Icon.Warning:
                    return p => Max(Triangle(p, C(0.5f, 0.9f), C(0.08f, 0.14f), C(0.92f, 0.14f)),
                        -Min(Box(p, C(0.5f, 0.52f), new Vector2(0.05f, 0.16f), 0.04f), SdCircle(p, C(0.5f, 0.27f), 0.055f)));
                default:
                    return p => SdCircle(p, C(0.5f, 0.5f), 0.4f);
            }
        }

        // A coin: filled disc with an engraved inner ring and a raised diamond in the middle.
        private static float Coin(Vector2 p)
        {
            Vector2 c = C(0.5f, 0.5f);
            float disc = Max(SdCircle(p, c, 0.42f), -Ring(p, c, 0.3f, 0.032f));
            float face = Max(disc, -SdCircle(p, c, 0.24f));
            return Min(face, Diamond(p, c, 0.15f));
        }

        private static float Star(Vector2 p)
        {
            Vector2 c = C(0.5f, 0.52f);
            float d = float.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI * 2f / 5f;
                float b = a + Mathf.PI / 5f;
                float b2 = a - Mathf.PI / 5f;
                Vector2 tip = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.42f;
                Vector2 l = c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 0.17f;
                Vector2 r = c + new Vector2(Mathf.Cos(b2), Mathf.Sin(b2)) * 0.17f;
                d = Mathf.Min(d, Triangle(p, tip, l, r));
            }

            return Mathf.Min(d, SdCircle(p, c, 0.18f));
        }

        // ------------------------------------------------------------------ SDF primitives (negative = inside)

        private static Vector2 C(float x, float y) => new Vector2(x, y);
        private static float Min(params float[] d) { float m = d[0]; for (int i = 1; i < d.Length; i++) m = Mathf.Min(m, d[i]); return m; }
        private static float Max(float a, float b) => Mathf.Max(a, b);

        private static float SdCircle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        private static float Ring(Vector2 p, Vector2 c, float r, float halfWidth) => Mathf.Abs((p - c).magnitude - r) - halfWidth;

        private static float Box(Vector2 p, Vector2 c, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(radius, radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Diamond(Vector2 p, Vector2 c, float r) => (Mathf.Abs(p.x - c.x) + Mathf.Abs(p.y - c.y) - r) * 0.7071f;

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Edge(Vector2 e0, Vector2 e1)
            {
                Vector2 e = e1 - e0;
                Vector2 n = new Vector2(e.y, -e.x).normalized;
                return Vector2.Dot(p - e0, n);
            }

            // Orientation-independent: inside when all edge distances share a sign.
            float d0 = Edge(a, b), d1 = Edge(b, c), d2 = Edge(c, a);
            bool clockwise = Vector2.Dot(new Vector2((b - a).y, -(b - a).x), c - a) < 0f;
            return clockwise ? Mathf.Max(d0, Mathf.Max(d1, d2)) : Mathf.Max(-d0, Mathf.Max(-d1, -d2));
        }

        // ------------------------------------------------------------------ Rasterising

        private static Sprite MakeRounded(bool edgeOnly)
        {
            float r = RoundRadius / RoundSize;
            Func<Vector2, float> shape = edgeOnly
                ? (Func<Vector2, float>)(p => Mathf.Abs(Box(p, C(0.5f, 0.5f), C(0.5f, 0.5f) - Vector2.one * (1.2f / RoundSize), r)) - 1f / RoundSize)
                : p => Box(p, C(0.5f, 0.5f), C(0.5f, 0.5f) - Vector2.one * (0.5f / RoundSize), r);
            float border = RoundRadius + 2f;
            return MakeSprite(RoundSize, shape, new Vector4(border, border, border, border));
        }

        private static Sprite MakeShadow()
        {
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d = Box(p, C(0.5f, 0.5f), C(0.18f, 0.18f), 0.12f);
                    float a = Mathf.Clamp01(1f - d / 0.3f);
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(46, 46, 46, 46));
        }

        private static Sprite MakeSprite(int size, Func<Vector2, float> sdf, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d = sdf(p) * size;
                    float a = Mathf.Clamp01(0.5f - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
