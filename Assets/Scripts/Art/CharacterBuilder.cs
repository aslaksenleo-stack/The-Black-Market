using System;
using UnityEngine;

namespace BlackMarket.Art
{
    /// <summary>The meshes for one character, split into the parts that animate separately.</summary>
    public sealed class CharacterMeshes
    {
        public Mesh Body;
        public Mesh Head;
        public Mesh ArmLeft;
        public Mesh ArmRight;
        public Mesh LegLeft;
        public Mesh LegRight;

        // Joint positions relative to the character's feet. Each part mesh is built around its joint.
        public Vector3 HeadPivot;
        public Vector3 ArmLeftPivot;
        public Vector3 ArmRightPivot;
        public Vector3 LegLeftPivot;
        public Vector3 LegRightPivot;
    }

    /// <summary>
    /// Builds stylised, chunky characters from simple shapes. Each look has its own silhouette
    /// (hat, hood, beard, coat length) so visitors are recognisable at a glance, and a seed varies
    /// skin and hair so repeat visitors aren't clones. Characters face +Z with feet at the origin.
    /// </summary>
    public static class CharacterBuilder
    {
        public static readonly string[] LookIds = { "explorer", "wanderer", "noble", "scholar", "peddler", "elder", "tinker", "sailor" };

        private const float HeadScale = 1.18f;
        private static readonly Vector3 HeadPivot = new Vector3(0f, 1.3f, 0f);
        private static readonly Vector3 ArmPivotRight = new Vector3(0.29f, 1.17f, 0f);
        private static readonly Vector3 LegPivotRight = new Vector3(0.11f, 0.62f, 0f);

        private sealed class Look
        {
            public Color Skin;
            public Color Hair;
            public Color Coat;
            public Color Sleeve;
            public Color Pants;
            public Color Boots;
            public Color Hands;
            public bool LongRobe;
            public bool HideEyes;
            public bool DarkFace;
            public float HandSize = 0.07f;
        }

        /// <param name="browTilt">Degrees. Positive = angry/stern, negative = worried.</param>
        public static CharacterMeshes Build(string lookId, int seed, float browTilt = 0f)
        {
            var random = new System.Random(seed);
            Look look = CreateLook(lookId, random);

            var meshes = new CharacterMeshes
            {
                HeadPivot = HeadPivot,
                ArmRightPivot = ArmPivotRight,
                ArmLeftPivot = Mirror(ArmPivotRight),
                LegRightPivot = LegPivotRight,
                LegLeftPivot = Mirror(LegPivotRight)
            };

            var b = new MeshBuilder();

            BuildBody(b, lookId, look);
            meshes.Body = Take(b, lookId + "_Body");

            BuildHead(b, lookId, look, browTilt, random);
            meshes.Head = Take(b, lookId + "_Head");

            BuildArm(b, look, 1f, lookId);
            meshes.ArmRight = Take(b, lookId + "_ArmR");
            BuildArm(b, look, -1f, lookId);
            meshes.ArmLeft = Take(b, lookId + "_ArmL");

            BuildLeg(b, look);
            meshes.LegRight = Take(b, lookId + "_LegR");
            BuildLeg(b, look);
            meshes.LegLeft = Take(b, lookId + "_LegL");

            return meshes;
        }

        private static Mesh Take(MeshBuilder b, string name)
        {
            Mesh m = b.ToMesh(name);
            b.Clear();
            return m;
        }

        private static Vector3 Mirror(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        private static T Pick<T>(System.Random r, T[] options) => options[r.Next(options.Length)];

        private static Color Vary(System.Random r, Color c, float amount = 0.08f)
        {
            float f = 1f + ((float)r.NextDouble() * 2f - 1f) * amount;
            return Palette.Shade(c, f);
        }

        // ------------------------------------------------------------------ Looks

        private static Look CreateLook(string id, System.Random r)
        {
            var look = new Look
            {
                Skin = Pick(r, Palette.Skin),
                Hair = Pick(r, Palette.Hair),
                Pants = Palette.Shade(Palette.ClothBrown, 0.7f),
                Boots = Palette.LeatherDark
            };

            switch (id)
            {
                case "explorer":
                    look.Coat = Vary(r, Palette.ClothBrown);
                    look.Hair = Pick(r, new[] { Palette.Hair[0], Palette.Hair[1], Palette.Hair[2] });
                    break;
                case "wanderer":
                    look.Coat = Vary(r, Palette.Mix(Palette.ClothBrown, Palette.ClothGrey, 0.5f));
                    look.LongRobe = true;
                    look.HideEyes = true;
                    look.DarkFace = true;
                    look.Hands = Palette.Shade(Palette.ClothGrey, 0.45f);
                    break;
                case "noble":
                    look.Coat = Vary(r, Palette.ClothPurple, 0.05f);
                    look.Pants = Palette.Ink;
                    look.Boots = Palette.Ink;
                    look.Hands = Palette.ClothCream;
                    break;
                case "scholar":
                    look.Coat = Vary(r, Palette.ClothBlue);
                    look.LongRobe = true;
                    look.Hair = Palette.Hair[4];
                    break;
                case "peddler":
                    look.Coat = Palette.ClothCream;
                    look.Sleeve = Palette.ClothCream;
                    look.Pants = Palette.ClothRed;
                    break;
                case "elder":
                    look.Coat = Vary(r, Palette.Mix(Palette.ClothGreen, Palette.ClothBrown, 0.4f));
                    look.Hair = Palette.Hair[4];
                    look.Pants = Palette.ClothGrey;
                    break;
                case "tinker":
                    look.Coat = Vary(r, Palette.Mix(Palette.ClothBlue, Palette.ClothGrey, 0.6f));
                    look.Hair = Pick(r, new[] { Palette.Hair[3], Palette.Hair[2] });
                    look.Hands = Palette.LeatherDark;
                    look.HandSize = 0.085f;
                    look.HideEyes = true;
                    break;
                case "sailor":
                    look.Coat = Palette.ClothCream;
                    look.Sleeve = Palette.ClothBlue;
                    look.Pants = Palette.ClothBlue;
                    look.Hair = Pick(r, new[] { Palette.Hair[3], Palette.Hair[2], Palette.Hair[0] });
                    break;
                default:
                    look.Coat = Palette.ClothGrey;
                    break;
            }

            if (look.Sleeve == default) look.Sleeve = look.Coat;
            if (look.Hands == default) look.Hands = look.Skin;
            return look;
        }

        // ------------------------------------------------------------------ Body

        private static void BuildBody(MeshBuilder b, string id, Look look)
        {
            Vector2[] profile = look.LongRobe
                ? new[]
                {
                    new Vector2(0f, 0.04f), new Vector2(0.34f, 0.04f), new Vector2(0.32f, 0.35f), new Vector2(0.29f, 0.7f),
                    new Vector2(0.31f, 0.94f), new Vector2(0.3f, 1.1f), new Vector2(0.24f, 1.25f), new Vector2(0.13f, 1.32f), new Vector2(0f, 1.33f)
                }
                : new[]
                {
                    new Vector2(0f, 0.52f), new Vector2(0.27f, 0.52f), new Vector2(0.29f, 0.7f), new Vector2(0.31f, 0.94f),
                    new Vector2(0.3f, 1.1f), new Vector2(0.24f, 1.25f), new Vector2(0.13f, 1.32f), new Vector2(0f, 1.33f)
                };

            b.Push(Vector3.zero, Vector3.zero, new Vector3(1f, 1f, 0.82f));
            b.AddLathe(profile, 16, look.Coat);
            b.Pop();

            // Hips under a short coat.
            if (!look.LongRobe)
            {
                b.AddCylinder(new Vector3(0f, 0.5f, 0f), 0.23f, 0.25f, 0.12f, look.Pants, 0f, 14);
            }

            // Neck
            b.AddCylinder(new Vector3(0f, 1.26f, 0f), 0.08f, 0.08f, 0.1f, look.DarkFace ? Palette.Ink : look.Skin, 0f, 10);

            const float front = 0.245f;
            switch (id)
            {
                case "explorer":
                    b.AddTorus(new Vector3(0f, 1.27f, 0f), 0.17f, 0.065f, Palette.Fur, 0f, 16, 8);
                    b.Push(new Vector3(0.0f, 0.93f, front), new Vector3(0f, 0f, 38f));
                    b.AddBox(Vector3.zero, new Vector3(0.07f, 0.72f, 0.03f), Palette.LeatherDark);
                    b.AddBox(new Vector3(0f, 0.08f, 0.02f), new Vector3(0.11f, 0.11f, 0.02f), Palette.Brass);
                    b.AddBox(new Vector3(0f, 0.08f, 0.031f), new Vector3(0.06f, 0.06f, 0.01f), Palette.LeatherDark);
                    b.Pop();
                    AddButtons(b, front, Palette.Brass, 3);
                    b.AddCylinder(new Vector3(0f, 0.6f, 0f), 0.285f, 0.285f, 0.07f, Palette.LeatherDark, 0f, 16);
                    break;
                case "wanderer":
                    b.AddTorus(new Vector3(0f, 1.25f, 0f), 0.19f, 0.07f, Palette.Shade(look.Coat, 0.8f), 0f, 16, 8);
                    b.AddCylinder(new Vector3(0f, 0.72f, 0f), 0.29f, 0.29f, 0.05f, Palette.LeatherDark, 0f, 16);
                    break;
                case "noble":
                    b.AddCylinder(new Vector3(0f, 0.72f, 0f), 0.296f, 0.298f, 0.05f, Palette.Gold, 0f, 16);
                    b.AddSphere(new Vector3(0f, 1.2f, 0.17f), new Vector3(0.08f, 0.07f, 0.06f), Palette.ClothCream);
                    b.AddSphere(new Vector3(0.27f, 1.2f, 0f), new Vector3(0.1f, 0.04f, 0.08f), Palette.Gold);
                    b.AddSphere(new Vector3(-0.27f, 1.2f, 0f), new Vector3(0.1f, 0.04f, 0.08f), Palette.Gold);
                    AddButtons(b, front, Palette.Gold, 3);
                    break;
                case "scholar":
                    b.AddCylinder(new Vector3(0f, 0.78f, 0f), 0.29f, 0.3f, 0.05f, Palette.Brass, 0f, 16);
                    b.AddGem(new Vector3(0.1f, 1.02f, front - 0.01f), 0.022f, 0.03f, 0.03f, 4, Palette.Gold, 0.4f);
                    b.AddGem(new Vector3(-0.12f, 0.9f, front - 0.01f), 0.018f, 0.025f, 0.025f, 4, Palette.Gold, 0.4f);
                    b.AddGem(new Vector3(0.05f, 0.55f, 0.28f), 0.02f, 0.028f, 0.028f, 4, Palette.Gold, 0.4f);
                    break;
                case "peddler":
                    b.Push(new Vector3(0.13f, 0.93f, 0.21f), new Vector3(0f, 25f, 0f));
                    b.AddBox(Vector3.zero, new Vector3(0.15f, 0.5f, 0.04f), Palette.ClothGreen);
                    b.Pop();
                    b.Push(new Vector3(-0.13f, 0.93f, 0.21f), new Vector3(0f, -25f, 0f));
                    b.AddBox(Vector3.zero, new Vector3(0.15f, 0.5f, 0.04f), Palette.ClothGreen);
                    b.Pop();
                    b.AddTorus(new Vector3(0f, 1.27f, 0f), 0.15f, 0.05f, Palette.ClothRed, 0f, 16, 8);
                    b.AddCylinder(new Vector3(0f, 0.66f, 0f), 0.295f, 0.297f, 0.06f, Palette.ClothRed, 0f, 16);
                    break;
                case "elder":
                    AddButtons(b, front, Palette.Shade(Palette.Bone, 0.8f), 4);
                    b.AddGem(new Vector3(0.05f, 1.23f, 0.17f), 0.035f, 0.02f, 0.02f, 4, Palette.ClothRed);
                    b.AddGem(new Vector3(-0.05f, 1.23f, 0.17f), 0.035f, 0.02f, 0.02f, 4, Palette.ClothRed);
                    break;
                case "tinker":
                    b.AddBox(new Vector3(0f, 0.85f, 0.235f), new Vector3(0.42f, 0.62f, 0.04f), Palette.Leather);
                    b.AddBox(new Vector3(0f, 0.75f, 0.26f), new Vector3(0.22f, 0.14f, 0.02f), Palette.LeatherDark);
                    b.AddCylinder(new Vector3(0.08f, 0.74f, 0.265f), 0.012f, 0.012f, 0.14f, Palette.Iron, 0f, 6);
                    b.AddCylinder(new Vector3(-0.06f, 0.74f, 0.265f), 0.015f, 0.008f, 0.12f, Palette.Brass, 0f, 6);
                    break;
                case "sailor":
                    for (int i = 0; i < 4; i++)
                    {
                        float y = 0.66f + i * 0.13f;
                        b.Push(Vector3.zero, Vector3.zero, new Vector3(1f, 1f, 0.82f));
                        b.AddTorus(new Vector3(0f, y, 0f), 0.297f, 0.022f, Palette.ClothBlue, 0f, 20, 6);
                        b.Pop();
                    }

                    b.AddTorus(new Vector3(0f, 1.27f, 0f), 0.14f, 0.045f, Palette.ClothRed, 0f, 16, 8);
                    break;
            }
        }

        private static void AddButtons(MeshBuilder b, float front, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                b.AddSphere(new Vector3(0f, 0.8f + i * 0.14f, front + 0.01f), new Vector3(0.025f, 0.025f, 0.015f), color, 0f, 8);
            }
        }

        // ------------------------------------------------------------------ Head (built around the neck joint)

        private static void BuildHead(MeshBuilder b, string id, Look look, float browTilt, System.Random r)
        {
            Color skin = look.DarkFace ? Palette.Hex(0x1E1A1C) : look.Skin;

            // Slightly oversized heads give the friendly, cartoon proportions.
            b.Push(Vector3.zero, Vector3.zero, Vector3.one * HeadScale);
            b.AddSphere(new Vector3(0f, 0.19f, 0f), new Vector3(0.2f, 0.21f, 0.19f), skin, 0f, 18);

            if (!look.DarkFace)
            {
                // Ears and nose
                b.AddSphere(new Vector3(0.195f, 0.18f, 0f), new Vector3(0.035f, 0.05f, 0.03f), skin, 0f, 8);
                b.AddSphere(new Vector3(-0.195f, 0.18f, 0f), new Vector3(0.035f, 0.05f, 0.03f), skin, 0f, 8);
                b.AddSphere(new Vector3(0f, 0.165f, 0.195f), new Vector3(0.05f, 0.045f, 0.05f), Palette.Shade(skin, 0.93f), 0f, 10);
            }

            if (!look.HideEyes)
            {
                AddEyes(b, browTilt, look.Hair);
            }

            if (!look.DarkFace)
            {
                b.AddBox(new Vector3(0f, 0.085f, 0.176f), new Vector3(0.07f, 0.014f, 0.02f), Palette.Shade(skin, 0.45f));
            }

            switch (id)
            {
                case "explorer": ExplorerHead(b, look); break;
                case "wanderer": WandererHead(b, look); break;
                case "noble": NobleHead(b, look); break;
                case "scholar": ScholarHead(b, look); break;
                case "peddler": PeddlerHead(b, look); break;
                case "elder": ElderHead(b, look); break;
                case "tinker": TinkerHead(b, look); break;
                case "sailor": SailorHead(b, look); break;
            }

            b.Pop();
        }

        private static void AddEyes(MeshBuilder b, float browTilt, Color browColor)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.07f;
                b.AddSphere(new Vector3(x, 0.215f, 0.168f), new Vector3(0.03f, 0.04f, 0.02f), Palette.Ink, 0f, 10);
                b.AddSphere(new Vector3(x + 0.01f, 0.23f, 0.184f), 0.009f, Color.white, 0.7f, 6);

                b.Push(new Vector3(side * 0.075f, 0.285f, 0.172f), new Vector3(0f, 0f, side * browTilt));
                b.AddBox(Vector3.zero, new Vector3(0.085f, 0.024f, 0.03f), Palette.Shade(browColor, 0.9f));
                b.Pop();
            }
        }

        private static void Mustache(MeshBuilder b, Color hair, float size = 1f)
        {
            b.AddSphere(new Vector3(0.04f, 0.115f, 0.19f), new Vector3(0.055f * size, 0.025f * size, 0.03f), hair, 0f, 10);
            b.AddSphere(new Vector3(-0.04f, 0.115f, 0.19f), new Vector3(0.055f * size, 0.025f * size, 0.03f), hair, 0f, 10);
        }

        private static void ExplorerHead(MeshBuilder b, Look look)
        {
            // Big beard and mustache
            b.AddSphere(new Vector3(0f, 0.06f, 0.11f), new Vector3(0.18f, 0.15f, 0.11f), look.Hair, 0f, 14);
            Mustache(b, Palette.Shade(look.Hair, 1.1f));

            // Leather aviator cap with ear flaps
            var cap = new[]
            {
                new Vector2(0.205f, 0.3f), new Vector2(0.212f, 0.24f), new Vector2(0.216f, 0.3f), new Vector2(0.19f, 0.36f),
                new Vector2(0.12f, 0.41f), new Vector2(0f, 0.425f)
            };
            b.Push(new Vector3(0f, 0f, -0.012f));
            b.AddLathe(cap, 18, Palette.Leather, 0f, 60f);
            b.Pop();
            b.AddBox(new Vector3(0.2f, 0.17f, -0.02f), new Vector3(0.05f, 0.17f, 0.11f), Palette.Leather);
            b.AddBox(new Vector3(-0.2f, 0.17f, -0.02f), new Vector3(0.05f, 0.17f, 0.11f), Palette.Leather);

            // Goggles pushed up on the cap
            b.AddBox(new Vector3(0f, 0.335f, 0.16f), new Vector3(0.3f, 0.035f, 0.04f), Palette.LeatherDark);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.07f, 0.34f, 0.18f), new Vector3(75f, 0f, 0f));
                b.AddTorus(Vector3.zero, 0.05f, 0.016f, Palette.Brass, 0f, 14, 6);
                b.AddCylinder(new Vector3(0f, -0.008f, 0f), 0.045f, 0.045f, 0.016f, Palette.Hex(0x9FD3D8), 0.25f, 14);
                b.Pop();
            }
        }

        private static void WandererHead(MeshBuilder b, Look look)
        {
            // Glowing eyes in the shadow of the hood (subtle, not neon)
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.065f, 0.215f, 0.17f), new Vector3(0f, 0f, side * -12f));
                b.AddSphere(Vector3.zero, new Vector3(0.032f, 0.02f, 0.015f), Palette.Hex(0xFFF2C8), 0.95f, 8);
                b.Pop();
            }

            // Cloth mask over the lower face
            b.AddSphere(new Vector3(0f, 0.09f, 0.1f), new Vector3(0.18f, 0.11f, 0.12f), Palette.Shade(look.Coat, 0.55f), 0f, 12);

            // Hood: a thick shell open at the front, so the dark face sits inside it.
            b.Push(new Vector3(0f, 0.21f, -0.02f), new Vector3(90f, 0f, 0f));
            var hood = new[]
            {
                new Vector2(0f, -0.27f), new Vector2(0.17f, -0.23f), new Vector2(0.26f, -0.1f), new Vector2(0.28f, 0.05f),
                new Vector2(0.255f, 0.2f), new Vector2(0.215f, 0.19f), new Vector2(0.225f, 0.04f), new Vector2(0.2f, -0.1f),
                new Vector2(0.12f, -0.2f), new Vector2(0f, -0.22f)
            };
            b.AddLathe(hood, 18, look.Coat, 0f, 50f);
            b.Pop();
            // Peak at the back of the hood
            b.Push(new Vector3(0f, 0.38f, -0.16f), new Vector3(-50f, 0f, 0f));
            b.AddCylinder(Vector3.zero, 0.09f, 0f, 0.2f, look.Coat, 0f, 10);
            b.Pop();
        }

        private static void NobleHead(MeshBuilder b, Look look)
        {
            Color hair = look.Hair;
            b.AddSphere(new Vector3(0.17f, 0.24f, -0.04f), new Vector3(0.06f, 0.09f, 0.1f), hair, 0f, 10);
            b.AddSphere(new Vector3(-0.17f, 0.24f, -0.04f), new Vector3(0.06f, 0.09f, 0.1f), hair, 0f, 10);

            // Curled mustache
            Mustache(b, hair, 1.1f);
            b.AddSphere(new Vector3(0.1f, 0.135f, 0.18f), 0.02f, hair, 0f, 8);
            b.AddSphere(new Vector3(-0.1f, 0.135f, 0.18f), 0.02f, hair, 0f, 8);

            // Monocle on the right eye
            b.Push(new Vector3(0.07f, 0.215f, 0.19f), new Vector3(90f, 0f, 0f));
            b.AddTorus(Vector3.zero, 0.045f, 0.008f, Palette.Gold, 0f, 16, 6);
            b.Pop();

            // Tall top hat, tilted a touch
            b.Push(new Vector3(0f, 0.34f, -0.01f), new Vector3(-6f, 0f, 4f));
            b.AddCylinder(Vector3.zero, 0.28f, 0.28f, 0.025f, Palette.Ink, 0f, 20);
            b.AddCylinder(new Vector3(0f, 0.02f, 0f), 0.165f, 0.18f, 0.3f, Palette.Ink, 0f, 18);
            b.AddCylinder(new Vector3(0f, 0.03f, 0f), 0.172f, 0.174f, 0.05f, Palette.ClothPurple, 0f, 18);
            b.Pop();
        }

        private static void ScholarHead(MeshBuilder b, Look look)
        {
            // Long white beard
            b.Push(new Vector3(0f, 0.06f, 0.15f), new Vector3(-20f, 0f, 0f));
            b.AddCylinder(new Vector3(0f, -0.16f, 0f), 0.02f, 0.14f, 0.2f, look.Hair, 0f, 12);
            b.Pop();
            b.AddSphere(new Vector3(0f, 0.07f, 0.11f), new Vector3(0.15f, 0.1f, 0.1f), look.Hair, 0f, 12);
            Mustache(b, look.Hair);

            // Round spectacles
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.07f, 0.215f, 0.195f), new Vector3(90f, 0f, 0f));
                b.AddTorus(Vector3.zero, 0.045f, 0.007f, Palette.Gold, 0f, 16, 6);
                b.Pop();
            }

            b.AddBox(new Vector3(0f, 0.22f, 0.2f), new Vector3(0.05f, 0.01f, 0.01f), Palette.Gold);

            // Wide-brimmed pointed hat
            b.Push(new Vector3(0f, 0.33f, -0.01f), new Vector3(-8f, 0f, 0f));
            b.AddCylinder(Vector3.zero, 0.34f, 0.32f, 0.03f, Palette.Shade(look.Coat, 0.85f), 0f, 22);
            b.AddCylinder(new Vector3(0f, 0.025f, 0f), 0.19f, 0f, 0.42f, Palette.Shade(look.Coat, 0.85f), 0f, 18);
            b.AddCylinder(new Vector3(0f, 0.025f, 0f), 0.192f, 0.17f, 0.06f, Palette.Brass, 0f, 18);
            b.Pop();
        }

        private static void PeddlerHead(MeshBuilder b, Look look)
        {
            // Smile instead of a flat mouth
            b.AddBox(new Vector3(0.025f, 0.093f, 0.178f), new Vector3(0.03f, 0.012f, 0.02f), Palette.Shade(look.Skin, 0.45f));

            // Headscarf with a knot at the back
            var scarf = new[]
            {
                new Vector2(0.212f, 0.2f), new Vector2(0.21f, 0.28f), new Vector2(0.17f, 0.37f),
                new Vector2(0.09f, 0.42f), new Vector2(0f, 0.43f)
            };
            b.Push(new Vector3(0f, 0f, -0.01f), new Vector3(-10f, 0f, 0f));
            b.AddLathe(scarf, 18, Palette.ClothRed, 0f, 60f);
            b.Pop();
            b.AddSphere(new Vector3(0f, 0.25f, -0.21f), new Vector3(0.07f, 0.06f, 0.06f), Palette.ClothRed, 0f, 10);
            b.Push(new Vector3(0.03f, 0.15f, -0.24f), new Vector3(15f, 0f, 10f));
            b.AddBox(Vector3.zero, new Vector3(0.06f, 0.16f, 0.02f), Palette.Shade(Palette.ClothRed, 0.85f));
            b.Pop();

            // Gold hoop earrings
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.205f, 0.1f, 0.01f), new Vector3(0f, 0f, 90f));
                b.AddTorus(Vector3.zero, 0.035f, 0.007f, Palette.Gold, 0f, 14, 6);
                b.Pop();
            }
        }

        private static void ElderHead(MeshBuilder b, Look look)
        {
            // Bald on top, tufts at the sides
            b.AddSphere(new Vector3(0.18f, 0.2f, -0.03f), new Vector3(0.06f, 0.07f, 0.09f), look.Hair, 0f, 10);
            b.AddSphere(new Vector3(-0.18f, 0.2f, -0.03f), new Vector3(0.06f, 0.07f, 0.09f), look.Hair, 0f, 10);

            // Walrus mustache
            b.AddSphere(new Vector3(0.05f, 0.11f, 0.19f), new Vector3(0.07f, 0.04f, 0.035f), look.Hair, 0f, 10);
            b.AddSphere(new Vector3(-0.05f, 0.11f, 0.19f), new Vector3(0.07f, 0.04f, 0.035f), look.Hair, 0f, 10);

            // Small round spectacles low on the nose
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.065f, 0.2f, 0.2f), new Vector3(90f, 0f, 0f));
                b.AddTorus(Vector3.zero, 0.038f, 0.006f, Palette.Silver, 0f, 14, 6);
                b.Pop();
            }
        }

        private static void TinkerHead(MeshBuilder b, Look look)
        {
            // Big welding goggles worn over the eyes
            b.AddBox(new Vector3(0f, 0.22f, 0.16f), new Vector3(0.36f, 0.04f, 0.05f), Palette.LeatherDark);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.075f, 0.22f, 0.185f), new Vector3(90f, 0f, 0f));
                b.AddTorus(Vector3.zero, 0.055f, 0.02f, Palette.Brass, 0f, 16, 6);
                b.AddCylinder(new Vector3(0f, -0.01f, 0f), 0.05f, 0.05f, 0.02f, Palette.Ember, 0.35f, 14);
                b.Pop();
            }

            // Soot smudge
            b.AddSphere(new Vector3(-0.11f, 0.12f, 0.15f), new Vector3(0.035f, 0.02f, 0.02f), Palette.Shade(look.Skin, 0.6f), 0f, 8);

            // Spiky hair
            var spikes = new[]
            {
                new Vector3(0f, 0.38f, 0f), new Vector3(0.09f, 0.36f, -0.03f), new Vector3(-0.09f, 0.36f, -0.03f),
                new Vector3(0.05f, 0.37f, 0.07f), new Vector3(-0.05f, 0.37f, 0.07f), new Vector3(0f, 0.34f, -0.11f),
                new Vector3(0.13f, 0.31f, 0.04f), new Vector3(-0.13f, 0.31f, 0.04f)
            };
            foreach (Vector3 p in spikes)
            {
                Vector3 dir = new Vector3(p.x, p.y - 0.15f, p.z).normalized;
                b.Push(p - dir * 0.05f, Quaternion.FromToRotation(Vector3.up, dir).eulerAngles);
                b.AddGem(Vector3.zero, 0.05f, 0.11f, 0.02f, 5, look.Hair);
                b.Pop();
            }

            b.AddSphere(new Vector3(0f, 0.25f, -0.02f), new Vector3(0.205f, 0.14f, 0.19f), look.Hair, 0f, 12);
        }

        private static void SailorHead(MeshBuilder b, Look look)
        {
            // Full beard
            b.AddSphere(new Vector3(0f, 0.07f, 0.1f), new Vector3(0.19f, 0.13f, 0.12f), look.Hair, 0f, 14);
            Mustache(b, look.Hair, 1.05f);

            // Knit beanie with a rolled cuff
            var beanie = new[]
            {
                new Vector2(0.215f, 0.22f), new Vector2(0.21f, 0.3f), new Vector2(0.17f, 0.38f),
                new Vector2(0.08f, 0.42f), new Vector2(0f, 0.43f)
            };
            b.AddLathe(beanie, 18, Palette.ClothBlue, 0f, 60f);
            b.AddTorus(new Vector3(0f, 0.24f, 0f), 0.212f, 0.035f, Palette.Shade(Palette.ClothBlue, 0.8f), 0f, 18, 8);

            // Gold earring
            b.Push(new Vector3(0.205f, 0.1f, 0.01f), new Vector3(0f, 0f, 90f));
            b.AddTorus(Vector3.zero, 0.025f, 0.006f, Palette.Gold, 0f, 12, 6);
            b.Pop();
        }

        // ------------------------------------------------------------------ Limbs (built around their joints)

        private static void BuildArm(MeshBuilder b, Look look, float side, string id)
        {
            Vector3 elbow = new Vector3(side * 0.04f, -0.24f, 0.02f);
            Vector3 wrist = new Vector3(side * 0.05f, -0.46f, 0.04f);
            b.AddLimb(Vector3.zero, elbow, 0.085f, 0.075f, look.Sleeve);
            b.AddLimb(elbow, wrist, 0.075f, 0.068f, look.Sleeve);

            if (id == "explorer" || id == "sailor")
            {
                b.AddTorus(wrist + new Vector3(0f, 0.02f, 0f), 0.068f, 0.025f, id == "explorer" ? Palette.Fur : Palette.Shade(look.Sleeve, 0.8f), 0f, 12, 6);
            }

            b.AddSphere(wrist + new Vector3(0f, -0.05f, 0.01f), new Vector3(look.HandSize, look.HandSize * 1.05f, look.HandSize * 0.9f), look.Hands, 0f, 10);
        }

        private static void BuildLeg(MeshBuilder b, Look look)
        {
            b.AddLimb(Vector3.zero, new Vector3(0f, -0.5f, 0f), 0.095f, 0.08f, look.Pants);
            b.AddBox(new Vector3(0f, -0.565f, 0.04f), new Vector3(0.15f, 0.11f, 0.25f), look.Boots);
        }
    }
}
