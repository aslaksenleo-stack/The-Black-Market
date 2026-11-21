using System.Collections.Generic;
using UnityEngine;

namespace BlackMarket.Art
{
    /// <summary>An item's meshes: the solid part (Toon material) and an optional glass part (Glass material).</summary>
    public sealed class ItemModel
    {
        public Mesh Solid;
        public Mesh Glass;
    }

    /// <summary>
    /// Builds each item's 3D model from shapes. Every model id has its own recipe and silhouette
    /// (see ItemCatalog.ModelId). Models rest on y = 0, centred on x/z, sized for a shop counter.
    /// Flawed items get a dark crack, which is small enough that you only notice it when inspecting.
    /// </summary>
    public static class ItemModelBuilder
    {
        private delegate Vector3 Recipe(MeshBuilder solid, MeshBuilder glass);

        // Each recipe returns where a crack would go if the item is flawed.
        private static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>
        {
            { "crystal", Crystal },
            { "astrolabe", Astrolabe },
            { "ring", Ring },
            { "orb", Orb },
            { "retort", Retort },
            { "dagger", Dagger },
            { "book", Book },
            { "tankard", Tankard },
            { "locket", Locket },
            { "songbird", Songbird },
            { "lantern", Lantern },
            { "scroll", Scroll },
            { "salts", Salts },
            { "helm", Helm },
            { "jar", Jar },
            { "crown", Crown },
            { "skull", Skull },
            { "potion", Potion }
        };

        public static bool HasRecipe(string modelId) => modelId != null && Recipes.ContainsKey(modelId);

        public static ItemModel Build(string modelId, bool flawed = false)
        {
            var solid = new MeshBuilder();
            var glass = new MeshBuilder();
            BuildInto(modelId, solid, glass, flawed);

            return new ItemModel
            {
                Solid = solid.ToMesh("Item_" + modelId),
                Glass = glass.IsEmpty ? null : glass.ToMesh("Item_" + modelId + "_Glass")
            };
        }

        /// <summary>
        /// Adds the item's shapes to existing builders at their current transform, so many items can be
        /// merged into one mesh (e.g. a whole shelf of decorations).
        /// </summary>
        public static void BuildInto(string modelId, MeshBuilder solid, MeshBuilder glass, bool flawed = false)
        {
            Recipe recipe = HasRecipe(modelId) ? Recipes[modelId] : Crate;
            Vector3 crackAt = recipe(solid, glass);
            if (flawed) AddCrack(solid, crackAt);
        }

        /// <summary>A zig-zag crack on the item's front surface.</summary>
        private static void AddCrack(MeshBuilder b, Vector3 at)
        {
            Color dark = Palette.Shade(Palette.Ink, 1.2f);
            b.Push(at, new Vector3(0f, 0f, 20f));
            b.AddBox(new Vector3(0f, 0.012f, 0f), new Vector3(0.004f, 0.03f, 0.006f), dark);
            b.Pop();
            b.Push(at + new Vector3(0.006f, -0.012f, 0f), new Vector3(0f, 0f, -35f));
            b.AddBox(Vector3.zero, new Vector3(0.004f, 0.026f, 0.006f), dark);
            b.Pop();
            b.Push(at + new Vector3(-0.002f, -0.032f, 0f), new Vector3(0f, 0f, 15f));
            b.AddBox(Vector3.zero, new Vector3(0.0035f, 0.022f, 0.006f), dark);
            b.Pop();
        }

        // ------------------------------------------------------------------ Recipes

        private static Vector3 Crate(MeshBuilder b, MeshBuilder g)
        {
            b.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(0.18f, 0.16f, 0.18f), Palette.WoodMid);
            return new Vector3(0f, 0.1f, 0.092f);
        }

        private static Vector3 Crystal(MeshBuilder b, MeshBuilder g)
        {
            b.AddSphere(new Vector3(0f, 0.025f, 0f), new Vector3(0.1f, 0.045f, 0.085f), Palette.StoneDark, 0f, 10);
            Color c = Palette.Crystal;
            AddCrystal(b, new Vector3(0f, 0.03f, 0f), new Vector3(0f, 0f, 0f), 0.045f, 0.2f, c);
            AddCrystal(b, new Vector3(0.05f, 0.03f, 0.01f), new Vector3(0f, 0f, -28f), 0.03f, 0.13f, Palette.Shade(c, 0.9f));
            AddCrystal(b, new Vector3(-0.05f, 0.03f, 0.02f), new Vector3(10f, 0f, 30f), 0.032f, 0.14f, Palette.Shade(c, 1.1f));
            AddCrystal(b, new Vector3(0.01f, 0.03f, 0.05f), new Vector3(30f, 0f, 5f), 0.024f, 0.1f, c);
            AddCrystal(b, new Vector3(-0.02f, 0.03f, -0.05f), new Vector3(-30f, 0f, -10f), 0.026f, 0.11f, Palette.Shade(c, 0.85f));
            return new Vector3(0.0f, 0.12f, 0.042f);
        }

        private static void AddCrystal(MeshBuilder b, Vector3 basePos, Vector3 tilt, float radius, float height, Color color)
        {
            b.Push(basePos, tilt);
            b.AddGem(new Vector3(0f, height * 0.75f, 0f), radius, height * 0.25f, height * 0.75f, 6, color, 0.35f);
            b.Pop();
        }

        private static Vector3 Astrolabe(MeshBuilder b, MeshBuilder g)
        {
            b.AddCylinder(Vector3.zero, 0.07f, 0.06f, 0.025f, Palette.WoodDark, 0f, 14);
            b.AddCylinder(new Vector3(0f, 0.02f, 0f), 0.012f, 0.012f, 0.06f, Palette.BrassDark, 0f, 8);
            Vector3 c = new Vector3(0f, 0.15f, 0f);
            b.Push(c, new Vector3(90f, 0f, 0f));
            b.AddTorus(Vector3.zero, 0.075f, 0.008f, Palette.Brass, 0f, 24, 6);
            b.Pop();
            b.Push(c, new Vector3(90f, 60f, 0f));
            b.AddTorus(Vector3.zero, 0.062f, 0.007f, Palette.Gold, 0f, 22, 6);
            b.Pop();
            b.Push(c, new Vector3(25f, 0f, 20f));
            b.AddTorus(Vector3.zero, 0.05f, 0.006f, Palette.BrassDark, 0f, 20, 6);
            b.Pop();
            b.AddSphere(c, 0.018f, Palette.Crystal, 0.3f, 10);
            b.AddCylinder(c + new Vector3(0f, -0.085f, 0f), 0.004f, 0.004f, 0.17f, Palette.BrassDark, 0f, 6);
            return new Vector3(0.02f, 0.2f, 0.06f);
        }

        private static Vector3 Ring(MeshBuilder b, MeshBuilder g)
        {
            // Small velvet cushion so the ring reads on the counter
            b.AddBox(new Vector3(0f, 0.02f, 0f), new Vector3(0.13f, 0.04f, 0.13f), Palette.Velvet);
            b.AddSphere(new Vector3(0f, 0.04f, 0f), new Vector3(0.06f, 0.012f, 0.06f), Palette.Shade(Palette.Velvet, 1.1f), 0f, 10);
            b.Push(new Vector3(0f, 0.085f, 0f), new Vector3(90f, 0f, 0f));
            b.AddTorus(Vector3.zero, 0.035f, 0.009f, Palette.Gold, 0f, 22, 8);
            b.Pop();
            b.AddCylinder(new Vector3(0f, 0.115f, 0f), 0.02f, 0.022f, 0.016f, Palette.Gold, 0f, 12);
            b.AddCylinder(new Vector3(0f, 0.129f, 0f), 0.014f, 0.014f, 0.004f, Palette.Wax, 0f, 12);
            return new Vector3(0f, 0.1f, 0.012f);
        }

        private static Vector3 Orb(MeshBuilder b, MeshBuilder g)
        {
            var stand = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.07f, 0f), new Vector2(0.07f, 0.015f), new Vector2(0.03f, 0.03f),
                new Vector2(0.025f, 0.06f), new Vector2(0.06f, 0.08f), new Vector2(0.05f, 0.09f), new Vector2(0f, 0.09f)
            };
            b.AddLathe(stand, 14, Palette.BrassDark, 0f, 35f);
            b.AddSphere(new Vector3(0f, 0.17f, 0f), 0.045f, Palette.Arcane, 0.55f, 12);
            b.AddSphere(new Vector3(0.02f, 0.19f, 0.01f), 0.022f, Palette.Shade(Palette.Arcane, 1.3f), 0.7f, 8);
            g.AddSphere(new Vector3(0f, 0.17f, 0f), 0.085f, Palette.Hex(0xC7B6FF), 0f, 18);
            return new Vector3(0.02f, 0.2f, 0.083f);
        }

        private static Vector3 Retort(MeshBuilder b, MeshBuilder g)
        {
            Color glassTint = Palette.Hex(0xBFE6C8);
            g.AddSphere(new Vector3(0f, 0.075f, 0f), 0.075f, glassTint, 0f, 16);
            g.Push(new Vector3(0.03f, 0.12f, 0f), new Vector3(0f, 0f, -55f));
            g.AddCylinder(Vector3.zero, 0.022f, 0.012f, 0.16f, glassTint, 0f, 10);
            g.Pop();
            b.AddSphere(new Vector3(0f, 0.055f, 0f), new Vector3(0.062f, 0.045f, 0.062f), Palette.Hex(0x58C26A), 0.3f, 12);
            b.AddTorus(new Vector3(0f, 0.004f, 0f), 0.05f, 0.006f, Palette.Iron, 0f, 14, 6);
            return new Vector3(-0.01f, 0.09f, 0.073f);
        }

        private static Vector3 Dagger(MeshBuilder b, MeshBuilder g)
        {
            // Lying on its side on the counter
            b.Push(new Vector3(0f, 0.022f, 0f), new Vector3(0f, 0f, 90f));
            b.Push(new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(1f, 1f, 0.28f));
            b.AddGem(new Vector3(0f, 0.0f, 0f), 0.03f, 0.2f, 0.005f, 4, Palette.Bone);
            b.Pop();
            b.AddBox(new Vector3(0f, 0.035f, 0f), new Vector3(0.1f, 0.018f, 0.03f), Palette.BrassDark);
            b.AddCylinder(new Vector3(0f, -0.06f, 0f), 0.014f, 0.016f, 0.095f, Palette.LeatherDark, 0f, 10);
            b.AddSphere(new Vector3(0f, -0.07f, 0f), 0.022f, Palette.Brass, 0f, 10);
            b.AddGem(new Vector3(0f, 0.035f, 0.016f), 0.01f, 0.008f, 0.008f, 4, Palette.Ember, 0.4f);
            b.Pop();
            return new Vector3(-0.08f, 0.03f, 0.01f);
        }

        private static Vector3 Book(MeshBuilder b, MeshBuilder g)
        {
            b.Push(Vector3.zero, new Vector3(0f, 12f, 0f));
            b.AddBox(new Vector3(0f, 0.035f, 0f), new Vector3(0.2f, 0.07f, 0.26f), Palette.Shade(Palette.ClothRed, 0.8f));
            b.AddBox(new Vector3(0.006f, 0.035f, 0f), new Vector3(0.192f, 0.052f, 0.245f), Palette.Parchment);
            b.AddBox(new Vector3(-0.1f, 0.035f, 0f), new Vector3(0.02f, 0.074f, 0.262f), Palette.Shade(Palette.ClothRed, 0.6f));
            foreach (float z in new[] { -0.11f, 0.11f })
            {
                b.AddBox(new Vector3(0.085f, 0.072f, z), new Vector3(0.035f, 0.004f, 0.035f), Palette.Gold);
            }

            b.AddSphere(new Vector3(0.0f, 0.072f, 0f), new Vector3(0.04f, 0.006f, 0.04f), Palette.Gold, 0f, 12);
            b.AddGem(new Vector3(0.0f, 0.078f, 0f), 0.014f, 0.01f, 0.004f, 6, Palette.SeaGlass, 0.4f);
            b.AddBox(new Vector3(0.1f, 0.035f, 0f), new Vector3(0.012f, 0.03f, 0.05f), Palette.Brass);
            b.Pop();
            return new Vector3(0.03f, 0.073f, 0.06f);
        }

        private static Vector3 Tankard(MeshBuilder b, MeshBuilder g)
        {
            var body = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.062f, 0.012f), new Vector2(0.055f, 0.03f),
                new Vector2(0.058f, 0.14f), new Vector2(0.063f, 0.15f), new Vector2(0f, 0.15f)
            };
            b.AddLathe(body, 16, Palette.Pewter, 0f, 35f);
            b.AddTorus(new Vector3(0f, 0.04f, 0f), 0.057f, 0.006f, Palette.Shade(Palette.Pewter, 0.8f), 0f, 16, 5);
            b.AddTorus(new Vector3(0f, 0.12f, 0f), 0.059f, 0.006f, Palette.Shade(Palette.Pewter, 0.8f), 0f, 16, 5);
            b.Push(new Vector3(-0.07f, 0.085f, 0f), new Vector3(90f, 0f, 0f));
            b.AddTorus(Vector3.zero, 0.035f, 0.01f, Palette.Pewter, 0f, 14, 6);
            b.Pop();
            b.Push(new Vector3(-0.012f, 0.158f, 0f), new Vector3(0f, 0f, 12f));
            b.AddCylinder(Vector3.zero, 0.064f, 0.05f, 0.012f, Palette.Shade(Palette.Pewter, 1.1f), 0f, 16);
            b.Pop();
            return new Vector3(0.0f, 0.1f, 0.06f);
        }

        private static Vector3 Locket(MeshBuilder b, MeshBuilder g)
        {
            b.AddBox(new Vector3(0f, 0.012f, 0f), new Vector3(0.12f, 0.024f, 0.12f), Palette.Shade(Palette.ClothBlue, 0.7f));
            b.Push(new Vector3(0f, 0.032f, 0f), new Vector3(0f, 0f, 0f));
            b.AddCylinder(Vector3.zero, 0.04f, 0.038f, 0.014f, Palette.Silver, 0f, 18);
            b.AddTorus(new Vector3(0f, 0.014f, 0f), 0.03f, 0.004f, Palette.Shade(Palette.Silver, 0.8f), 0f, 18, 5);
            b.AddGem(new Vector3(0f, 0.016f, 0f), 0.013f, 0.01f, 0.004f, 6, Palette.Arcane, 0.35f);
            b.Pop();
            // Chain coiled beside it
            for (int i = 0; i < 9; i++)
            {
                float a = i * 0.7f;
                var p = new Vector3(0.045f + Mathf.Cos(a) * 0.012f, 0.028f, -0.03f + i * 0.012f);
                b.Push(p, new Vector3(90f, i * 40f, 0f));
                b.AddTorus(Vector3.zero, 0.007f, 0.0022f, Palette.Silver, 0f, 8, 4);
                b.Pop();
            }

            return new Vector3(-0.015f, 0.047f, 0.0f);
        }

        private static Vector3 Songbird(MeshBuilder b, MeshBuilder g)
        {
            b.AddCylinder(Vector3.zero, 0.06f, 0.055f, 0.02f, Palette.WoodDark, 0f, 14);
            b.AddCylinder(new Vector3(0f, 0.02f, 0f), 0.008f, 0.008f, 0.06f, Palette.BrassDark, 0f, 6);
            b.Push(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0f, 90f));
            b.AddCylinder(new Vector3(0f, -0.045f, 0f), 0.007f, 0.007f, 0.09f, Palette.BrassDark, 0f, 6);
            b.Pop();

            Vector3 body = new Vector3(0f, 0.125f, 0f);
            b.AddSphere(body, new Vector3(0.045f, 0.042f, 0.06f), Palette.Brass, 0f, 14);
            b.AddSphere(body + new Vector3(0f, 0.045f, 0.045f), 0.032f, Palette.Brass, 0f, 12);
            b.Push(body + new Vector3(0f, 0.045f, 0.075f), new Vector3(90f, 0f, 0f));
            b.AddCylinder(Vector3.zero, 0.011f, 0f, 0.03f, Palette.BrassDark, 0f, 8);
            b.Pop();
            b.AddSphere(body + new Vector3(0.022f, 0.055f, 0.06f), 0.006f, Palette.Ink, 0f, 6);
            b.AddSphere(body + new Vector3(-0.022f, 0.055f, 0.06f), 0.006f, Palette.Ink, 0f, 6);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(body + new Vector3(side * 0.042f, 0.005f, -0.005f), new Vector3(10f, 0f, side * 15f), new Vector3(0.25f, 1f, 1f));
                b.AddGem(Vector3.zero, 0.04f, 0.015f, 0.015f, 6, Palette.Hex(0xC07A3A));
                b.Pop();
            }

            b.Push(body + new Vector3(0f, 0.01f, -0.07f), new Vector3(-70f, 0f, 0f), new Vector3(1f, 1f, 0.3f));
            b.AddGem(Vector3.zero, 0.03f, 0.05f, 0.005f, 5, Palette.Hex(0xC07A3A));
            b.Pop();

            // Wind-up key on its back
            b.AddCylinder(body + new Vector3(0f, 0.035f, -0.01f), 0.004f, 0.004f, 0.025f, Palette.Silver, 0f, 6);
            b.Push(body + new Vector3(0f, 0.07f, -0.01f), new Vector3(0f, 0f, 90f));
            b.AddTorus(Vector3.zero, 0.012f, 0.004f, Palette.Silver, 0f, 10, 5);
            b.Pop();
            return new Vector3(0.0f, 0.13f, 0.058f);
        }

        private static Vector3 Lantern(MeshBuilder b, MeshBuilder g)
        {
            b.AddBox(new Vector3(0f, 0.012f, 0f), new Vector3(0.1f, 0.024f, 0.1f), Palette.Iron);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    b.AddBox(new Vector3(x * 0.042f, 0.08f, z * 0.042f), new Vector3(0.012f, 0.13f, 0.012f), Palette.Iron);
                }
            }

            b.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(0.1f, 0.016f, 0.1f), Palette.Iron);
            b.AddCylinder(new Vector3(0f, 0.158f, 0f), 0.065f, 0.012f, 0.05f, Palette.Iron, 0f, 4);
            b.Push(new Vector3(0f, 0.225f, 0f), new Vector3(90f, 0f, 0f));
            b.AddTorus(Vector3.zero, 0.022f, 0.005f, Palette.Iron, 0f, 12, 5);
            b.Pop();
            b.AddCylinder(new Vector3(0f, 0.024f, 0f), 0.014f, 0.014f, 0.04f, Palette.ClothCream, 0f, 8);
            b.AddGem(new Vector3(0f, 0.075f, 0f), 0.01f, 0.025f, 0.008f, 5, Palette.Flame, 1f);
            g.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(0.078f, 0.12f, 0.078f), Palette.Hex(0xFFE2B0));
            return new Vector3(0.0f, 0.1f, 0.05f);
        }

        private static Vector3 Scroll(MeshBuilder b, MeshBuilder g)
        {
            b.Push(new Vector3(0f, 0.032f, 0f), new Vector3(0f, 20f, 90f));
            b.AddCylinder(new Vector3(0f, -0.11f, 0f), 0.03f, 0.03f, 0.22f, Palette.Parchment, 0f, 14);
            b.AddCylinder(new Vector3(0f, -0.13f, 0f), 0.012f, 0.012f, 0.26f, Palette.WoodDark, 0f, 8);
            b.AddSphere(new Vector3(0f, 0.135f, 0f), 0.018f, Palette.WoodDark, 0f, 8);
            b.AddSphere(new Vector3(0f, -0.135f, 0f), 0.018f, Palette.WoodDark, 0f, 8);
            b.AddTorus(Vector3.zero, 0.031f, 0.005f, Palette.ClothRed, 0f, 14, 5);
            b.Pop();
            b.AddCylinder(new Vector3(0f, 0.05f, 0.026f), 0.022f, 0.02f, 0.012f, Palette.Wax, 0f, 12);
            b.Push(new Vector3(0.015f, 0.025f, 0.05f), new Vector3(0f, 0f, 20f));
            b.AddBox(Vector3.zero, new Vector3(0.012f, 0.05f, 0.003f), Palette.ClothRed);
            b.Pop();
            return new Vector3(-0.05f, 0.04f, 0.03f);
        }

        private static Vector3 Salts(MeshBuilder b, MeshBuilder g)
        {
            var dish = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.09f, 0.03f), new Vector2(0.095f, 0.045f),
                new Vector2(0.085f, 0.045f), new Vector2(0.07f, 0.025f), new Vector2(0f, 0.022f)
            };
            b.AddLathe(dish, 18, Palette.Clay, 0f, 35f);
            var rnd = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.4f;
                float r = 0.012f + 0.045f * (float)rnd.NextDouble();
                var p = new Vector3(Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r);
                b.Push(p, new Vector3((float)rnd.NextDouble() * 40f - 20f, i * 37f, (float)rnd.NextDouble() * 40f - 20f));
                b.AddGem(Vector3.zero, 0.014f + 0.006f * (float)rnd.NextDouble(), 0.03f, 0.005f, 5,
                    Palette.Shade(Palette.Ember, 0.9f + 0.3f * (float)rnd.NextDouble()), 0.5f);
                b.Pop();
            }

            return new Vector3(0.0f, 0.03f, 0.09f);
        }

        private static Vector3 Helm(MeshBuilder b, MeshBuilder g)
        {
            var dome = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.095f, 0f), new Vector2(0.1f, 0.06f), new Vector2(0.098f, 0.12f),
                new Vector2(0.08f, 0.18f), new Vector2(0.045f, 0.215f), new Vector2(0f, 0.225f)
            };
            b.AddLathe(dome, 18, Palette.Steel, 0f, 45f);
            b.AddTorus(new Vector3(0f, 0.005f, 0f), 0.097f, 0.008f, Palette.Shade(Palette.Steel, 0.75f), 0f, 18, 5);
            // Visor plate with eye slits
            b.Push(new Vector3(0f, 0.1f, 0.085f), new Vector3(-8f, 0f, 0f));
            b.AddBox(Vector3.zero, new Vector3(0.15f, 0.07f, 0.03f), Palette.Shade(Palette.Steel, 0.9f));
            b.AddBox(new Vector3(0.035f, 0.01f, 0.012f), new Vector3(0.045f, 0.01f, 0.012f), Palette.Ink);
            b.AddBox(new Vector3(-0.035f, 0.01f, 0.012f), new Vector3(0.045f, 0.01f, 0.012f), Palette.Ink);
            b.Pop();
            b.AddBox(new Vector3(0f, 0.2f, 0f), new Vector3(0.012f, 0.05f, 0.16f), Palette.Shade(Palette.Steel, 0.8f));
            // Plume
            b.Push(new Vector3(0f, 0.22f, -0.03f), new Vector3(-30f, 0f, 0f), new Vector3(0.4f, 1f, 1f));
            b.AddGem(Vector3.zero, 0.03f, 0.12f, 0.01f, 6, Palette.ClothRed);
            b.Pop();
            return new Vector3(0.05f, 0.16f, 0.075f);
        }

        private static Vector3 Jar(MeshBuilder b, MeshBuilder g)
        {
            var jar = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.068f, 0.02f), new Vector2(0.068f, 0.14f),
                new Vector2(0.055f, 0.155f), new Vector2(0.05f, 0.17f), new Vector2(0f, 0.17f)
            };
            g.AddLathe(jar, 16, Palette.Hex(0xA8E07A), 0f, 35f);
            b.AddCylinder(new Vector3(0f, 0.165f, 0f), 0.048f, 0.05f, 0.03f, Palette.Cork, 0f, 12);
            b.AddCylinder(new Vector3(0f, 0.003f, 0f), 0.062f, 0.062f, 0.012f, Palette.Hex(0x5E7A3A), 0.1f, 14);
            // The root: a gnarled little body with leaves
            b.AddLimb(new Vector3(0f, 0.03f, 0f), new Vector3(0.005f, 0.11f, 0f), 0.012f, 0.028f, Palette.Hex(0x9A7350), 8);
            b.AddLimb(new Vector3(0.005f, 0.07f, 0f), new Vector3(0.03f, 0.05f, 0.01f), 0.008f, 0.005f, Palette.Hex(0x9A7350), 6);
            b.AddLimb(new Vector3(0.0f, 0.07f, 0f), new Vector3(-0.03f, 0.055f, 0f), 0.008f, 0.005f, Palette.Hex(0x9A7350), 6);
            b.AddSphere(new Vector3(-0.009f, 0.115f, 0.024f), 0.004f, Palette.Ink, 0f, 6);
            b.AddSphere(new Vector3(0.012f, 0.115f, 0.024f), 0.004f, Palette.Ink, 0f, 6);
            for (int i = 0; i < 3; i++)
            {
                b.Push(new Vector3(0.005f, 0.135f, 0f), new Vector3(25f, i * 120f, 0f), new Vector3(0.5f, 1f, 1f));
                b.AddGem(Vector3.zero, 0.015f, 0.035f, 0.004f, 4, Palette.ClothGreen);
                b.Pop();
            }

            return new Vector3(0.0f, 0.09f, 0.069f);
        }

        private static Vector3 Crown(MeshBuilder b, MeshBuilder g)
        {
            Color gold = Palette.Mix(Palette.Gold, Palette.SeaGlass, 0.15f);
            var band = new[]
            {
                new Vector2(0.07f, 0f), new Vector2(0.078f, 0f), new Vector2(0.084f, 0.06f), new Vector2(0.076f, 0.06f), new Vector2(0.07f, 0f)
            };
            b.AddLathe(band, 22, gold, 0f, 30f);
            int points = 7;
            for (int i = 0; i < points; i++)
            {
                float a = i / (float)points * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.Push(dir * 0.08f + Vector3.up * 0.058f, new Vector3(0f, 0f, 0f));
                b.AddCylinder(Vector3.zero, 0.016f, 0f, 0.05f, gold, 0f, 6);
                b.AddSphere(new Vector3(0f, 0.052f, 0f), 0.008f, Palette.Gold, 0f, 6);
                b.Pop();
                b.AddGem(dir * 0.085f + Vector3.up * 0.03f, 0.011f, 0.012f, 0.012f, 6, i % 2 == 0 ? Palette.SeaGlass : Palette.Crystal, 0.45f);
            }

            b.AddTorus(new Vector3(0f, 0.004f, 0f), 0.079f, 0.006f, Palette.Gold, 0f, 22, 5);
            return new Vector3(0.0f, 0.03f, 0.084f);
        }

        private static Vector3 Skull(MeshBuilder b, MeshBuilder g)
        {
            Color bone = Palette.Bone;
            b.AddSphere(new Vector3(0f, 0.07f, -0.01f), new Vector3(0.06f, 0.06f, 0.07f), bone, 0f, 14);
            b.Push(new Vector3(0f, 0.045f, 0.06f), new Vector3(20f, 0f, 0f));
            b.AddSphere(Vector3.zero, new Vector3(0.035f, 0.03f, 0.06f), bone, 0f, 12);
            b.Pop();
            b.AddSphere(new Vector3(0.033f, 0.075f, 0.045f), 0.016f, Palette.Ink, 0f, 8);
            b.AddSphere(new Vector3(-0.033f, 0.075f, 0.045f), 0.016f, Palette.Ink, 0f, 8);
            b.AddSphere(new Vector3(0.01f, 0.035f, 0.11f), 0.007f, Palette.Ink, 0f, 6);
            b.AddSphere(new Vector3(-0.01f, 0.035f, 0.11f), 0.007f, Palette.Ink, 0f, 6);
            // Curled horns: shrinking beads along a spiral
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 13f;
                    float a = t * Mathf.PI * 1.7f;
                    float r = 0.05f * (1f - t * 0.45f);
                    var p = new Vector3(side * (0.05f + t * 0.03f), 0.1f + Mathf.Sin(a) * r * 0.8f, -0.02f - Mathf.Cos(a) * r + r);
                    b.AddSphere(p, 0.024f * (1f - t * 0.6f), Palette.Shade(bone, 0.85f - t * 0.15f), 0f, 8);
                }
            }

            return new Vector3(0.02f, 0.1f, 0.05f);
        }

        private static Vector3 Potion(MeshBuilder b, MeshBuilder g)
        {
            var bottle = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.03f, 0f), new Vector2(0.06f, 0.02f), new Vector2(0.065f, 0.06f),
                new Vector2(0.05f, 0.1f), new Vector2(0.02f, 0.115f), new Vector2(0.018f, 0.16f), new Vector2(0f, 0.16f)
            };
            g.AddLathe(bottle, 16, Palette.Hex(0xF2C8C8), 0f, 50f);
            var liquid = new[]
            {
                new Vector2(0f, 0.005f), new Vector2(0.03f, 0.005f), new Vector2(0.054f, 0.022f), new Vector2(0.058f, 0.06f),
                new Vector2(0.047f, 0.085f), new Vector2(0f, 0.085f)
            };
            b.AddLathe(liquid, 14, Palette.Potion, 0.3f, 50f);
            b.AddCylinder(new Vector3(0f, 0.15f, 0f), 0.017f, 0.02f, 0.03f, Palette.Cork, 0f, 10);
            b.AddTorus(new Vector3(0f, 0.12f, 0f), 0.022f, 0.004f, Palette.Brass, 0f, 12, 5);
            return new Vector3(0.0f, 0.06f, 0.064f);
        }
    }
}
