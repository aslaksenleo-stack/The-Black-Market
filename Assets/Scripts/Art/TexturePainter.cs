using System;

namespace BlackMarket.Art
{
    /// <summary>
    /// Paints the shop's tileable surface textures in code (RGBA bytes), so no image files are needed:
    /// floor planks, wood grain, brick, plaster, cobblestones and woven cloth. Textures are painted
    /// for 1 metre per tile (MeshBuilder UVs are in metres). Wood and cloth are near-grey so vertex
    /// colours can tint them; brick, plaster and cobbles carry their own colour.
    ///
    /// Pure C# (no UnityEngine), deterministic, and soft-edged rather than noisy, to suit the
    /// hand-painted style.
    /// </summary>
    public static class TexturePainter
    {
        public const int DefaultSize = 256;

        public static byte[] Paint(string id, int size = DefaultSize)
        {
            switch (id)
            {
                case "planks": return Planks(size);
                case "wood": return WoodGrain(size);
                case "brick": return Brick(size);
                case "plaster": return Plaster(size);
                case "cobble": return Cobble(size);
                case "cloth": return Cloth(size);
                default: return Flat(size, 255, 255, 255);
            }
        }

        public static readonly string[] Ids = { "planks", "wood", "brick", "plaster", "cobble", "cloth" };

        // ------------------------------------------------------------------ Textures

        private static byte[] Planks(int size)
        {
            const int boards = 4;
            var rnd = new Random(7);
            var boardShade = new float[boards];
            var seamAt = new float[boards];
            for (int i = 0; i < boards; i++)
            {
                boardShade[i] = 0.86f + 0.14f * (float)rnd.NextDouble();
                seamAt[i] = (float)rnd.NextDouble();
            }

            return Fill(size, (u, v) =>
            {
                int board = Math.Min(boards - 1, (int)(v * boards));
                float inBoard = v * boards - board;
                float grain = Fbm(u * 2f, v * 24f, 2, 24 * boards / 4, 3, 11);
                float streak = Fbm(u * 1f, v * 60f, 1, 60, 2, 5);
                float shade = boardShade[board] * (0.82f + 0.18f * grain) * (0.94f + 0.06f * streak);

                float edge = Math.Min(inBoard, 1f - inBoard) * size / boards;
                if (edge < 2.2f) shade *= 0.55f + 0.45f * (edge / 2.2f);

                float du = Math.Abs(Frac(u - seamAt[board] + 0.5f) - 0.5f) * size;
                if (du < 1.6f) shade *= 0.6f + 0.4f * (du / 1.6f);

                return Gray(shade, 1.0f, 0.97f, 0.93f);
            });
        }

        private static byte[] WoodGrain(int size)
        {
            return Fill(size, (u, v) =>
            {
                float warp = Fbm(u * 3f, v * 3f, 3, 3, 3, 21) * 0.25f;
                float rings = (float)Math.Sin((u + warp) * Math.PI * 2 * 9) * 0.5f + 0.5f;
                float fine = Fbm(u * 6f, v * 40f, 6, 40, 2, 4);
                float shade = 0.84f + 0.1f * rings + 0.08f * (fine - 0.5f);
                return Gray(shade, 1.0f, 0.96f, 0.92f);
            });
        }

        private static byte[] Brick(int size)
        {
            const int columns = 4, rows = 12;
            return Fill(size, (u, v) =>
            {
                int row = (int)(v * rows);
                float offset = row % 2 == 0 ? 0f : 0.5f;
                float x = u * columns + offset;
                int col = (int)Math.Floor(x);
                float bx = x - col;
                float by = v * rows - row;
                float mortarX = Math.Min(bx, 1f - bx) * size / columns;
                float mortarY = Math.Min(by, 1f - by) * size / rows;
                float mortar = Math.Min(mortarX, mortarY);

                float h = Hash(((col % columns) + columns) % columns, row, 3);
                float n = Fbm(u * 8f, v * 8f, 8, 8, 3, 9);
                float r = 0.62f + 0.12f * h + 0.08f * (n - 0.5f);
                float g = 0.36f + 0.06f * h + 0.05f * (n - 0.5f);
                float b = 0.27f + 0.04f * h + 0.04f * (n - 0.5f);

                if (mortar < 2.5f)
                {
                    float t = mortar / 2.5f;
                    r = Lerp(0.58f, r, t);
                    g = Lerp(0.53f, g, t);
                    b = Lerp(0.47f, b, t);
                }

                return Rgb(r, g, b);
            });
        }

        private static byte[] Plaster(int size)
        {
            return Fill(size, (u, v) =>
            {
                float n = Fbm(u * 4f, v * 4f, 4, 4, 4, 13);
                float fine = Fbm(u * 24f, v * 24f, 24, 24, 2, 17);
                float s = 0.9f + 0.1f * n + 0.04f * (fine - 0.5f);
                return Rgb(0.80f * s, 0.69f * s, 0.55f * s);
            });
        }

        private static byte[] Cobble(int size)
        {
            const int cells = 6;
            return Fill(size, (u, v) =>
            {
                // Distance to nearest and second-nearest jittered cell centre (Voronoi stones).
                float x = u * cells, y = v * cells;
                int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y);
                float d1 = 9f, d2 = 9f, id = 0f;
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int gx = cx + ox, gy = cy + oy;
                        int wx = ((gx % cells) + cells) % cells, wy = ((gy % cells) + cells) % cells;
                        float px = gx + 0.2f + 0.6f * Hash(wx, wy, 1);
                        float py = gy + 0.2f + 0.6f * Hash(wx, wy, 2);
                        float d = (float)Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                        if (d < d1) { d2 = d1; d1 = d; id = Hash(wx, wy, 5); }
                        else if (d < d2) d2 = d;
                    }
                }

                float gap = d2 - d1;
                float s = 0.62f + 0.18f * id;
                float bevel = Math.Min(1f, gap * 6f);
                s *= 0.55f + 0.45f * bevel;
                return Rgb(s * 0.78f, s * 0.74f, s * 0.78f);
            });
        }

        private static byte[] Cloth(int size)
        {
            return Fill(size, (u, v) =>
            {
                float warp = (float)Math.Sin(u * size * Math.PI * 0.5) * 0.5f + 0.5f;
                float weft = (float)Math.Sin(v * size * Math.PI * 0.5) * 0.5f + 0.5f;
                float n = Fbm(u * 6f, v * 6f, 6, 6, 3, 31);
                float s = 0.86f + 0.06f * (warp * weft) + 0.08f * (n - 0.5f);
                return Gray(s, 1f, 0.98f, 0.96f);
            });
        }

        private static byte[] Flat(int size, byte r, byte g, byte b)
        {
            var data = new byte[size * size * 4];
            for (int i = 0; i < data.Length; i += 4)
            {
                data[i] = r;
                data[i + 1] = g;
                data[i + 2] = b;
                data[i + 3] = 255;
            }

            return data;
        }

        // ------------------------------------------------------------------ Helpers

        private delegate (float r, float g, float b) Shader(float u, float v);

        private static byte[] Fill(int size, Shader shader)
        {
            var data = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    (float r, float g, float b) = shader((x + 0.5f) / size, (y + 0.5f) / size);
                    int i = (y * size + x) * 4;
                    data[i] = ToByte(r);
                    data[i + 1] = ToByte(g);
                    data[i + 2] = ToByte(b);
                    data[i + 3] = 255;
                }
            }

            return data;
        }

        private static (float, float, float) Gray(float s, float tr, float tg, float tb) => (s * tr, s * tg, s * tb);
        private static (float, float, float) Rgb(float r, float g, float b) => (r, g, b);

        private static byte ToByte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v * 255f)));
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Frac(float v) => v - (float)Math.Floor(v);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144269504;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        // Tileable value noise: the lattice wraps every (periodX, periodY) cells.
        private static float Noise(float x, float y, int periodX, int periodY, int seed)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
            int Wrap(int a, int p) => ((a % p) + p) % p;
            float a00 = Hash(Wrap(x0, periodX), Wrap(y0, periodY), seed);
            float a10 = Hash(Wrap(x0 + 1, periodX), Wrap(y0, periodY), seed);
            float a01 = Hash(Wrap(x0, periodX), Wrap(y0 + 1, periodY), seed);
            float a11 = Hash(Wrap(x0 + 1, periodX), Wrap(y0 + 1, periodY), seed);
            return Lerp(Lerp(a00, a10, sx), Lerp(a01, a11, sx), sy);
        }

        private static float Fbm(float x, float y, int periodX, int periodY, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(x, y, Math.Max(1, periodX), Math.Max(1, periodY), seed + o * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                x *= 2f;
                y *= 2f;
                periodX *= 2;
                periodY *= 2;
            }

            return sum / norm;
        }
    }
}
