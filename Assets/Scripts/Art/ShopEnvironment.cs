using System.Collections.Generic;
using UnityEngine;

namespace BlackMarket.Art
{
    /// <summary>Which shared material a piece of the shop uses.</summary>
    public enum EnvMaterial
    {
        /// <summary>Plain vertex colours (most props).</summary>
        Props,
        Wood,
        Planks,
        Brick,
        Plaster,
        Cobble,
        Cloth,
        Glass
    }

    /// <summary>One mesh placed in the shop.</summary>
    public sealed class EnvPiece
    {
        public string Name;
        public EnvMaterial Material;
        public Mesh Mesh;
        public Vector3 Position;
        public Vector3 Euler;
        public bool IsStatic = true;
        public bool CastShadows = true;

        /// <summary>Gameplay role (e.g. "Bell", "Ledger", "Counter", "Door"), or null for scenery.</summary>
        public string Tag;
    }

    public sealed class EnvLight
    {
        public string Name;
        public bool Directional;
        public Vector3 Position;
        public Vector3 Euler;
        public Color Color;
        public float Intensity;
        public float Range;
        public bool Shadows;

        /// <summary>Important lights are always per-pixel; others may be per-vertex on low-end phones.</summary>
        public bool Important;
    }

    /// <summary>Painted lettering (rendered with a TextMesh by the scene builder).</summary>
    public sealed class EnvLabel
    {
        public string Text;
        public Vector3 Position;
        public Vector3 Euler;

        /// <summary>Height (metres) of the box the text is scaled to fit; the width is the banner's.</summary>
        public float BoxHeight;
        public Color Color;
    }

    public sealed class ShopLayout
    {
        public readonly List<EnvPiece> Pieces = new List<EnvPiece>();
        public readonly List<EnvLight> Lights = new List<EnvLight>();
        public readonly List<EnvLabel> Labels = new List<EnvLabel>();
        public readonly Dictionary<string, Vector3> Anchors = new Dictionary<string, Vector3>();
    }

    /// <summary>
    /// The shop interior and the street outside, generated as meshes, lights and named anchor points.
    /// The editor's ShopSceneBuilder turns this into GameObjects; nothing here touches the scene, which
    /// keeps the layout in one readable place.
    ///
    /// Layout (metres, the player stands at the origin facing +Z):
    ///   z = -2.2  back wall: shelves and the curtain to the back room
    ///   z =  0.85 the counter (top at 1.0 m)
    ///   z =  1.72 where visitors stand
    ///   z =  5.5  front wall: entrance door and two windows onto the street
    ///   x = ±4.5  side walls: shelves, banner, display case, storage corner, portal device
    /// </summary>
    public static class ShopEnvironment
    {
        public const float HalfWidth = 4.5f;
        public const float BackZ = -2.2f;
        public const float FrontZ = 5.5f;
        public const float Height = 3.4f;
        public const float DoorHalfWidth = 0.75f;
        public const float DoorHeight = 2.5f;
        public const float CounterZ = 0.85f;
        public const float CounterTop = 1.0f;
        public const float DoorOpenAngle = -100f;

        private const float Wall = 0.25f;

        /// <summary>Accumulates shapes per (group, material) and turns each into one piece.</summary>
        private sealed class Kit
        {
            private readonly Dictionary<string, MeshBuilder> builders = new Dictionary<string, MeshBuilder>();
            private readonly List<string> order = new List<string>();

            public MeshBuilder this[string group, EnvMaterial material]
            {
                get
                {
                    string key = group + "|" + material;
                    if (!builders.TryGetValue(key, out MeshBuilder b))
                    {
                        b = new MeshBuilder();
                        builders.Add(key, b);
                        order.Add(key);
                    }

                    return b;
                }
            }

            public void FlushInto(ShopLayout layout)
            {
                foreach (string key in order)
                {
                    MeshBuilder b = builders[key];
                    if (b.IsEmpty) continue;
                    string[] parts = key.Split('|');
                    var material = (EnvMaterial)System.Enum.Parse(typeof(EnvMaterial), parts[1]);
                    layout.Pieces.Add(new EnvPiece
                    {
                        Name = parts[0] + "_" + parts[1],
                        Material = material,
                        Mesh = b.ToMesh(parts[0] + "_" + parts[1]),
                        CastShadows = material != EnvMaterial.Glass
                    });
                }
            }
        }

        public static ShopLayout Build()
        {
            var layout = new ShopLayout();
            var kit = new Kit();

            BuildRoom(kit);
            BuildFrontWall(kit);
            BuildCounter(kit, layout);
            BuildShelves(kit);
            BuildDisplayCase(kit);
            BuildStorageCorner(kit);
            BuildPortal(kit, layout);
            BuildBanner(kit, layout);
            BuildWallDecor(kit);
            BuildLanterns(kit, layout);
            BuildStreet(kit, layout);

            kit.FlushInto(layout);
            AddDoor(layout);
            AddStreetFolk(layout);
            AddAnchors(layout);
            return layout;
        }

        // ------------------------------------------------------------------ Room shell

        private static void BuildRoom(Kit kit)
        {
            float depth = FrontZ - BackZ;
            float midZ = (FrontZ + BackZ) * 0.5f;
            float width = HalfWidth * 2f;

            kit["Floor", EnvMaterial.Planks].AddBox(new Vector3(0f, -0.05f, midZ), new Vector3(width, 0.1f, depth), Palette.Shade(Palette.WoodMid, 1.15f));
            kit["Ceiling", EnvMaterial.Planks].AddBox(new Vector3(0f, Height + 0.05f, midZ), new Vector3(width, 0.1f, depth), Palette.Shade(Palette.WoodDark, 0.9f));

            // Ceiling beams with the heavy look from the reference interiors
            MeshBuilder beams = kit["Beams", EnvMaterial.Wood];
            for (float z = -1.4f; z < FrontZ; z += 1.6f)
            {
                beams.AddBox(new Vector3(0f, Height - 0.13f, z), new Vector3(width, 0.26f, 0.24f), Palette.WoodDeep);
            }

            beams.AddBox(new Vector3(-2.2f, Height - 0.3f, midZ), new Vector3(0.2f, 0.12f, depth), Palette.WoodDeep);
            beams.AddBox(new Vector3(2.2f, Height - 0.3f, midZ), new Vector3(0.2f, 0.12f, depth), Palette.WoodDeep);

            // Side walls: wood wainscot, plaster above, timber posts
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (HalfWidth + Wall * 0.5f);
                kit["Walls", EnvMaterial.Wood].AddBox(new Vector3(x - side * 0.02f, 0.5f, midZ), new Vector3(Wall, 1f, depth), Palette.WoodDark);
                kit["Walls", EnvMaterial.Plaster].AddBox(new Vector3(x, 1f + (Height - 1f) * 0.5f, midZ), new Vector3(Wall, Height - 1f, depth), Color.white);
                kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(side * (HalfWidth - 0.03f), 1.02f, midZ), new Vector3(0.08f, 0.07f, depth), Palette.WoodDeep);
                kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(side * (HalfWidth - 0.02f), 0.06f, midZ), new Vector3(0.05f, 0.12f, depth), Palette.WoodDeep);

                for (float z = BackZ + 0.2f; z <= FrontZ - 0.1f; z += 1.9f)
                {
                    kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(side * (HalfWidth - 0.06f), Height * 0.5f, z), new Vector3(0.16f, Height, 0.2f), Palette.WoodDeep);
                }

                kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(side * (HalfWidth - 0.06f), 2.55f, midZ), new Vector3(0.14f, 0.16f, depth), Palette.WoodDeep);
            }

            // Back wall: brick behind the shelves
            kit["Walls", EnvMaterial.Brick].AddBox(new Vector3(0f, Height * 0.5f, BackZ - Wall * 0.5f), new Vector3(width, Height, Wall), Color.white);
            kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(0f, 0.06f, BackZ + 0.03f), new Vector3(width, 0.12f, 0.05f), Palette.WoodDeep);

            // Doorway to the back room (where bought stock goes), hung with a heavy curtain
            kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(-0.62f, 1.15f, BackZ + 0.06f), new Vector3(0.14f, 2.3f, 0.14f), Palette.WoodDeep);
            kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(0.62f, 1.15f, BackZ + 0.06f), new Vector3(0.14f, 2.3f, 0.14f), Palette.WoodDeep);
            kit["Trim", EnvMaterial.Wood].AddBox(new Vector3(0f, 2.36f, BackZ + 0.06f), new Vector3(1.4f, 0.16f, 0.16f), Palette.WoodDeep);
            MeshBuilder curtain = kit["Curtain", EnvMaterial.Cloth];
            for (int i = 0; i < 7; i++)
            {
                float x = -0.48f + i * 0.16f;
                curtain.AddBox(new Vector3(x, 1.14f, BackZ + 0.1f + (i % 2) * 0.03f), new Vector3(0.17f, 2.26f, 0.04f), Palette.Shade(Palette.ClothRed, i % 2 == 0 ? 0.8f : 0.7f));
            }

            // Rug where visitors stand
            MeshBuilder rug = kit["Rug", EnvMaterial.Cloth];
            rug.AddBox(new Vector3(0f, 0.004f, 3.0f), new Vector3(2.4f, 0.008f, 3.4f), Palette.Shade(Palette.ClothRed, 0.85f));
            rug.AddBox(new Vector3(0f, 0.009f, 3.0f), new Vector3(2.1f, 0.008f, 3.1f), Palette.Shade(Palette.Gold, 0.75f));
            rug.AddBox(new Vector3(0f, 0.014f, 3.0f), new Vector3(1.95f, 0.008f, 2.95f), Palette.ClothPurple);
            rug.Push(new Vector3(0f, 0.019f, 3.0f), new Vector3(0f, 45f, 0f));
            rug.AddBox(Vector3.zero, new Vector3(0.9f, 0.008f, 0.9f), Palette.Shade(Palette.Gold, 0.7f));
            rug.AddBox(new Vector3(0f, 0.004f, 0f), new Vector3(0.7f, 0.008f, 0.7f), Palette.ClothRed);
            rug.Pop();
        }

        private static void BuildFrontWall(Kit kit)
        {
            float z = FrontZ + Wall * 0.5f;
            float windowX = 2.6f, windowHalf = 0.65f, sill = 1.1f, top = 2.3f;

            // Wall segments around the door and two windows (plaster above wood wainscot)
            void Segment(float x0, float x1, float y0, float y1)
            {
                if (x1 - x0 < 0.01f || y1 - y0 < 0.01f) return;
                Vector3 c = new Vector3((x0 + x1) * 0.5f, 0f, z);
                if (y0 < 1f)
                {
                    float yTop = Mathf.Min(1f, y1);
                    kit["Walls", EnvMaterial.Wood].AddBox(new Vector3(c.x, (y0 + yTop) * 0.5f, z - 0.02f), new Vector3(x1 - x0, yTop - y0, Wall), Palette.WoodDark);
                }

                if (y1 > 1f)
                {
                    float yBottom = Mathf.Max(1f, y0);
                    kit["Walls", EnvMaterial.Plaster].AddBox(new Vector3(c.x, (yBottom + y1) * 0.5f, z), new Vector3(x1 - x0, y1 - yBottom, Wall), Color.white);
                }
            }

            float d = DoorHalfWidth;
            for (int side = -1; side <= 1; side += 2)
            {
                float inner = d, outer = HalfWidth + Wall;
                float w0 = windowX - windowHalf, w1 = windowX + windowHalf;
                float sx(float v) => side * v;
                // Between door and window, window column, window to corner
                Segment(Mathf.Min(sx(inner), sx(w0)), Mathf.Max(sx(inner), sx(w0)), 0f, Height);
                Segment(Mathf.Min(sx(w0), sx(w1)), Mathf.Max(sx(w0), sx(w1)), 0f, sill);
                Segment(Mathf.Min(sx(w0), sx(w1)), Mathf.Max(sx(w0), sx(w1)), top, Height);
                Segment(Mathf.Min(sx(w1), sx(outer)), Mathf.Max(sx(w1), sx(outer)), 0f, Height);

                // Window frame, cross bars, sill and glass
                float cx = side * windowX;
                MeshBuilder frame = kit["Windows", EnvMaterial.Wood];
                frame.AddBox(new Vector3(cx, top, FrontZ), new Vector3(windowHalf * 2f + 0.16f, 0.1f, 0.3f), Palette.WoodDeep);
                frame.AddBox(new Vector3(cx, sill, FrontZ - 0.04f), new Vector3(windowHalf * 2f + 0.24f, 0.08f, 0.38f), Palette.WoodDeep);
                frame.AddBox(new Vector3(cx - windowHalf, (sill + top) * 0.5f, FrontZ), new Vector3(0.1f, top - sill, 0.3f), Palette.WoodDeep);
                frame.AddBox(new Vector3(cx + windowHalf, (sill + top) * 0.5f, FrontZ), new Vector3(0.1f, top - sill, 0.3f), Palette.WoodDeep);
                frame.AddBox(new Vector3(cx, (sill + top) * 0.5f, FrontZ + 0.02f), new Vector3(0.05f, top - sill, 0.06f), Palette.WoodDeep);
                frame.AddBox(new Vector3(cx, (sill + top) * 0.5f + 0.1f, FrontZ + 0.02f), new Vector3(windowHalf * 2f, 0.05f, 0.06f), Palette.WoodDeep);
                kit["Windows", EnvMaterial.Glass].AddBox(new Vector3(cx, (sill + top) * 0.5f, FrontZ + 0.03f), new Vector3(windowHalf * 2f, top - sill, 0.02f), Palette.Hex(0xBFA8E0));

                // A few things in the window: a potion and a jar silhouetted against the street
                MeshBuilder props = kit["Windows", EnvMaterial.Props];
                MeshBuilder glass = kit["Windows", EnvMaterial.Glass];
                props.Push(new Vector3(cx - 0.35f, sill + 0.04f, FrontZ - 0.1f), new Vector3(0f, 20f, 0f), Vector3.one * 1.3f);
                ItemModelBuilder.BuildInto("potion", props, glass);
                props.Pop();
                props.Push(new Vector3(cx + 0.35f, sill + 0.04f, FrontZ - 0.1f), new Vector3(0f, -30f, 0f), Vector3.one * 1.2f);
                ItemModelBuilder.BuildInto(side < 0 ? "skull" : "lantern", props, glass);
                props.Pop();
            }

            Segment(-d, d, DoorHeight, Height);

            // Door frame
            MeshBuilder doorFrame = kit["DoorFrame", EnvMaterial.Wood];
            doorFrame.AddBox(new Vector3(-d - 0.07f, DoorHeight * 0.5f, FrontZ), new Vector3(0.14f, DoorHeight, 0.34f), Palette.WoodDeep);
            doorFrame.AddBox(new Vector3(d + 0.07f, DoorHeight * 0.5f, FrontZ), new Vector3(0.14f, DoorHeight, 0.34f), Palette.WoodDeep);
            doorFrame.AddBox(new Vector3(0f, DoorHeight + 0.08f, FrontZ), new Vector3(d * 2f + 0.34f, 0.16f, 0.36f), Palette.WoodDeep);
            doorFrame.AddBox(new Vector3(0f, 0.015f, FrontZ), new Vector3(d * 2f, 0.03f, 0.34f), Palette.Stone);

            // Shop bell over the door, inside
            MeshBuilder bell = kit["DoorFrame", EnvMaterial.Props];
            bell.AddBox(new Vector3(0f, DoorHeight + 0.1f, FrontZ - 0.25f), new Vector3(0.04f, 0.04f, 0.18f), Palette.Iron);
            bell.AddCylinder(new Vector3(0f, DoorHeight - 0.02f, FrontZ - 0.32f), 0.006f, 0.006f, 0.1f, Palette.Iron, 0f, 6);
            bell.AddLathe(BellProfile(0.06f), 12, Palette.Brass, 0f, 50f, new Vector3(0f, DoorHeight - 0.1f, FrontZ - 0.32f));
        }

        private static Vector2[] BellProfile(float r)
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(r * 1.05f, 0f), new Vector2(r, r * 0.25f), new Vector2(r * 0.7f, r * 0.8f),
                new Vector2(r * 0.6f, r * 1.25f), new Vector2(r * 0.3f, r * 1.45f), new Vector2(0f, r * 1.5f)
            };
        }

        // ------------------------------------------------------------------ Counter

        private static void BuildCounter(Kit kit, ShopLayout layout)
        {
            const float halfW = 1.8f, depth = 0.72f;
            float frontZ = CounterZ + depth * 0.5f;
            float backZ = CounterZ - depth * 0.5f;
            MeshBuilder wood = kit["Counter", EnvMaterial.Wood];
            MeshBuilder props = kit["Counter", EnvMaterial.Props];

            wood.AddBox(new Vector3(0f, 0.04f, CounterZ + 0.02f), new Vector3(halfW * 2f + 0.06f, 0.08f, depth + 0.04f), Palette.WoodDeep);
            wood.AddBox(new Vector3(0f, 0.5f, frontZ - 0.03f), new Vector3(halfW * 2f, 0.86f, 0.06f), Palette.WoodMid);
            wood.AddBox(new Vector3(0f, 0.5f, backZ + 0.03f), new Vector3(halfW * 2f, 0.86f, 0.06f), Palette.WoodDark);
            wood.AddBox(new Vector3(-halfW + 0.03f, 0.5f, CounterZ), new Vector3(0.06f, 0.86f, depth - 0.1f), Palette.WoodMid);
            wood.AddBox(new Vector3(halfW - 0.03f, 0.5f, CounterZ), new Vector3(0.06f, 0.86f, depth - 0.1f), Palette.WoodMid);

            // Raised panels on the customer side
            for (int i = 0; i < 4; i++)
            {
                float x = -1.32f + i * 0.88f;
                wood.AddBox(new Vector3(x, 0.5f, frontZ + 0.005f), new Vector3(0.74f, 0.6f, 0.03f), Palette.WoodDark);
                wood.AddBox(new Vector3(x, 0.5f, frontZ + 0.02f), new Vector3(0.6f, 0.46f, 0.02f), Palette.Shade(Palette.WoodMid, 1.05f));
            }

            wood.AddBox(new Vector3(0f, 0.9f, frontZ + 0.01f), new Vector3(halfW * 2f + 0.04f, 0.06f, 0.08f), Palette.WoodDeep);

            // Thick worn top
            wood.AddBox(new Vector3(0f, CounterTop - 0.035f, CounterZ), new Vector3(halfW * 2f + 0.2f, 0.07f, depth + 0.16f), Palette.WoodDark);

            // Player-side shelf under the top (visible when looking down)
            wood.AddBox(new Vector3(0f, 0.55f, backZ + 0.02f), new Vector3(halfW * 2f - 0.2f, 0.03f, 0.04f), Palette.WoodDeep);
            props.AddBox(new Vector3(-1.1f, CounterTop - 0.13f, backZ - 0.01f), new Vector3(0.5f, 0.12f, 0.03f), Palette.WoodDeep);
            props.AddBox(new Vector3(-1.1f, CounterTop - 0.13f, backZ - 0.03f), new Vector3(0.08f, 0.03f, 0.02f), Palette.Brass);
            props.AddBox(new Vector3(0.6f, CounterTop - 0.13f, backZ - 0.01f), new Vector3(0.5f, 0.12f, 0.03f), Palette.WoodDeep);
            props.AddBox(new Vector3(0.6f, CounterTop - 0.13f, backZ - 0.03f), new Vector3(0.08f, 0.03f, 0.02f), Palette.Brass);

            // Brass corner caps
            for (int side = -1; side <= 1; side += 2)
            {
                props.AddBox(new Vector3(side * (halfW + 0.07f), CounterTop - 0.035f, frontZ + 0.07f), new Vector3(0.08f, 0.08f, 0.08f), Palette.Brass);
            }

            // Leather trading mat: where items are placed
            float matZ = CounterZ - 0.02f;
            props.AddBox(new Vector3(0f, CounterTop + 0.004f, matZ), new Vector3(0.82f, 0.008f, 0.56f), Palette.LeatherDark);
            props.AddBox(new Vector3(0f, CounterTop + 0.007f, matZ), new Vector3(0.74f, 0.008f, 0.48f), Palette.Leather);
            props.AddBox(new Vector3(0f, CounterTop + 0.01f, matZ), new Vector3(0.68f, 0.008f, 0.42f), Palette.Shade(Palette.Leather, 1.12f));
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    props.AddSphere(new Vector3(x * 0.37f, CounterTop + 0.01f, matZ + z * 0.24f), new Vector3(0.018f, 0.008f, 0.018f), Palette.Brass, 0f, 8);
                }
            }

            // Candles at the far left, magnifying glass and coin stacks at the right
            props.AddCylinder(new Vector3(-1.62f, CounterTop, 1.08f), 0.1f, 0.09f, 0.02f, Palette.BrassDark, 0f, 12);
            float[] heights = { 0.2f, 0.14f, 0.1f };
            Vector3[] spots = { new Vector3(-1.62f, 0f, 1.08f), new Vector3(-1.55f, 0f, 1.01f), new Vector3(-1.69f, 0f, 1.02f) };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = spots[i] + Vector3.up * (CounterTop + 0.02f);
                props.AddCylinder(p, 0.025f, 0.023f, heights[i], Palette.ClothCream, 0f, 10);
                props.AddGem(p + Vector3.up * (heights[i] + 0.03f), 0.012f, 0.035f, 0.01f, 6, Palette.Flame, 1f);
            }

            props.Push(new Vector3(0.62f, CounterTop + 0.012f, 1.0f), new Vector3(0f, -30f, 0f));
            props.AddTorus(Vector3.zero, 0.05f, 0.009f, Palette.Brass, 0f, 18, 6);
            props.AddBox(new Vector3(0.11f, 0f, 0f), new Vector3(0.12f, 0.018f, 0.022f), Palette.WoodDark);
            props.Pop();
            kit["Counter", EnvMaterial.Glass].AddCylinder(new Vector3(0.62f, CounterTop + 0.008f, 1.0f), 0.045f, 0.045f, 0.006f, Palette.Hex(0xDDEEFF), 0f, 16);

            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(0.66f + i * 0.07f, CounterTop, 0.66f - i * 0.03f);
                props.AddCylinder(p, 0.026f, 0.026f, 0.02f + i * 0.015f, Palette.Gold, 0f, 12);
            }

            // Bell (its own object so it can be rung)
            var bell = new MeshBuilder();
            bell.AddCylinder(Vector3.zero, 0.095f, 0.085f, 0.03f, Palette.WoodDeep, 0f, 16);
            bell.AddLathe(BellProfile(0.075f), 16, Palette.Brass, 0f, 50f, new Vector3(0f, 0.03f, 0f));
            bell.AddCylinder(new Vector3(0f, 0.14f, 0f), 0.008f, 0.008f, 0.03f, Palette.BrassDark, 0f, 6);
            bell.AddSphere(new Vector3(0f, 0.175f, 0f), new Vector3(0.022f, 0.012f, 0.022f), Palette.Brass, 0f, 8);
            layout.Pieces.Add(new EnvPiece
            {
                Name = "Bell", Material = EnvMaterial.Props, Mesh = bell.ToMesh("Bell"),
                Position = new Vector3(1.25f, CounterTop, 0.72f), Tag = "Bell", IsStatic = false
            });

            // Ledger: open book with a quill (opens the trade ledger)
            var ledger = new MeshBuilder();
            ledger.AddBox(new Vector3(0f, 0.008f, 0f), new Vector3(0.44f, 0.016f, 0.3f), Palette.LeatherDark);
            ledger.Push(new Vector3(-0.105f, 0.022f, 0f), new Vector3(0f, 0f, 4f));
            ledger.AddBox(Vector3.zero, new Vector3(0.2f, 0.018f, 0.27f), Palette.Parchment);
            ledger.Pop();
            ledger.Push(new Vector3(0.105f, 0.022f, 0f), new Vector3(0f, 0f, -4f));
            ledger.AddBox(Vector3.zero, new Vector3(0.2f, 0.018f, 0.27f), Palette.Parchment);
            ledger.Pop();
            for (int i = 0; i < 6; i++)
            {
                ledger.AddBox(new Vector3(-0.105f, 0.033f, -0.09f + i * 0.035f), new Vector3(0.15f, 0.002f, 0.006f), Palette.Shade(Palette.Ink, 2.5f));
                if (i < 4) ledger.AddBox(new Vector3(0.105f, 0.033f, -0.09f + i * 0.035f), new Vector3(0.15f, 0.002f, 0.006f), Palette.Shade(Palette.Ink, 2.5f));
            }

            ledger.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(0.03f, 0f), new Vector2(0.032f, 0.03f), new Vector2(0.015f, 0.045f), new Vector2(0.015f, 0.055f), new Vector2(0f, 0.055f) },
                10, Palette.Ink, 0f, 45f, new Vector3(0.27f, 0f, 0.08f));
            ledger.Push(new Vector3(0.25f, 0.075f, 0.08f), new Vector3(15f, 0f, 25f), new Vector3(0.25f, 1f, 1f));
            ledger.AddGem(Vector3.zero, 0.022f, 0.07f, 0.03f, 4, Palette.Fur);
            ledger.Pop();
            layout.Pieces.Add(new EnvPiece
            {
                Name = "Ledger", Material = EnvMaterial.Props, Mesh = ledger.ToMesh("Ledger"),
                Position = new Vector3(-0.95f, CounterTop, 0.72f), Euler = new Vector3(0f, 12f, 0f), Tag = "Ledger", IsStatic = false
            });
        }

        // ------------------------------------------------------------------ Shelves

        private static void BuildShelves(Kit kit)
        {
            // Left wall: tall shelf facing into the room
            ShelfUnit(kit, "ShelvesLeft", new Vector3(-HalfWidth + 0.25f, 0f, -0.3f), 90f, 3.4f, 2.7f, 5, 1);
            // Right wall
            ShelfUnit(kit, "ShelvesRight", new Vector3(HalfWidth - 0.25f, 0f, -0.9f), -90f, 2.4f, 2.5f, 4, 2);
            // Behind the player, either side of the back-room curtain
            ShelfUnit(kit, "ShelvesBack", new Vector3(-2.3f, 0f, BackZ + 0.25f), 0f, 2.6f, 2.6f, 4, 3);
            ShelfUnit(kit, "ShelvesBack", new Vector3(2.3f, 0f, BackZ + 0.25f), 0f, 2.6f, 2.6f, 4, 4);
        }

        private static readonly string[] ShelfItems =
        {
            "potion", "jar", "crystal", "skull", "orb", "tankard", "scroll", "lantern", "salts", "retort", "helm", "astrolabe", "songbird"
        };

        private static void ShelfUnit(Kit kit, string group, Vector3 origin, float yaw, float width, float height, int levels, int seed)
        {
            const float depth = 0.42f;
            var rnd = new System.Random(seed * 97);
            MeshBuilder wood = kit[group, EnvMaterial.Wood];
            MeshBuilder props = kit[group, EnvMaterial.Props];
            MeshBuilder glass = kit[group, EnvMaterial.Glass];

            wood.Push(origin, new Vector3(0f, yaw, 0f));
            props.Push(origin, new Vector3(0f, yaw, 0f));
            glass.Push(origin, new Vector3(0f, yaw, 0f));

            wood.AddBox(new Vector3(0f, height * 0.5f, -depth * 0.5f + 0.015f), new Vector3(width, height, 0.03f), Palette.WoodDeep);
            wood.AddBox(new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.07f, height, depth), Palette.WoodMid);
            wood.AddBox(new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.07f, height, depth), Palette.WoodMid);
            wood.AddBox(new Vector3(0f, height + 0.04f, 0.02f), new Vector3(width + 0.16f, 0.08f, depth + 0.06f), Palette.WoodDark);
            wood.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(width, 0.12f, depth), Palette.WoodDark);

            float spacing = (height - 0.12f) / levels;
            for (int level = 0; level < levels; level++)
            {
                float y = 0.12f + level * spacing;
                wood.AddBox(new Vector3(0f, y, 0f), new Vector3(width - 0.07f, 0.035f, depth), Palette.WoodMid);
                wood.AddBox(new Vector3(0f, y - 0.02f, depth * 0.5f - 0.01f), new Vector3(width - 0.07f, 0.05f, 0.02f), Palette.WoodDark);
                FillShelf(props, glass, rnd, y + 0.018f, width - 0.2f, spacing - 0.08f);
            }

            wood.Pop();
            props.Pop();
            glass.Pop();
        }

        private static void FillShelf(MeshBuilder props, MeshBuilder glass, System.Random rnd, float y, float width, float clearance)
        {
            float x = -width * 0.5f;
            while (x < width * 0.5f - 0.1f)
            {
                double roll = rnd.NextDouble();
                if (roll < 0.35)
                {
                    // A run of books
                    int count = 3 + rnd.Next(6);
                    for (int i = 0; i < count && x < width * 0.5f - 0.05f; i++)
                    {
                        float h = Mathf.Min(clearance - 0.04f, 0.18f + 0.12f * (float)rnd.NextDouble());
                        float t = 0.035f + 0.025f * (float)rnd.NextDouble();
                        Color c = BookColors[rnd.Next(BookColors.Length)];
                        bool lean = i == count - 1 && rnd.NextDouble() < 0.5;
                        props.Push(new Vector3(x + t * 0.5f, y, 0.02f), new Vector3(0f, 0f, lean ? -14f : 0f));
                        props.AddBox(new Vector3(0f, h * 0.5f, 0f), new Vector3(t, h, 0.22f + 0.05f * (float)rnd.NextDouble()), c);
                        props.AddBox(new Vector3(0f, h * 0.75f, 0.12f), new Vector3(t + 0.002f, 0.015f, 0.01f), Palette.Gold);
                        props.Pop();
                        x += t + 0.004f;
                    }

                    x += 0.06f;
                }
                else if (roll < 0.45)
                {
                    // Stacked books lying flat
                    int count = 2 + rnd.Next(3);
                    for (int i = 0; i < count; i++)
                    {
                        props.Push(new Vector3(x + 0.13f, y + i * 0.045f, 0.02f), new Vector3(0f, (float)rnd.NextDouble() * 20f - 10f, 0f));
                        props.AddBox(new Vector3(0f, 0.022f, 0f), new Vector3(0.24f, 0.044f, 0.2f), BookColors[rnd.Next(BookColors.Length)]);
                        props.Pop();
                    }

                    x += 0.32f;
                }
                else
                {
                    string id = ShelfItems[rnd.Next(ShelfItems.Length)];
                    float scale = 1.1f + 0.3f * (float)rnd.NextDouble();
                    var pos = new Vector3(x + 0.13f, y, 0.02f);
                    var rot = new Vector3(0f, (float)rnd.NextDouble() * 60f - 30f, 0f);
                    props.Push(pos, rot, Vector3.one * scale);
                    glass.Push(pos, rot, Vector3.one * scale);
                    ItemModelBuilder.BuildInto(id, props, glass);
                    glass.Pop();
                    props.Pop();
                    x += 0.3f + 0.1f * (float)rnd.NextDouble();
                }
            }
        }

        private static readonly Color[] BookColors =
        {
            Palette.ClothRed, Palette.ClothBlue, Palette.ClothGreen, Palette.ClothPurple, Palette.Leather,
            Palette.Shade(Palette.ClothBlue, 0.7f), Palette.Shade(Palette.ClothRed, 0.7f), Palette.Parchment, Palette.LeatherDark
        };

        // ------------------------------------------------------------------ Display case, storage, portal

        private static void BuildDisplayCase(Kit kit)
        {
            var c = new Vector3(-3.55f, 0f, 2.9f);
            MeshBuilder wood = kit["DisplayCase", EnvMaterial.Wood];
            MeshBuilder props = kit["DisplayCase", EnvMaterial.Props];
            MeshBuilder glass = kit["DisplayCase", EnvMaterial.Glass];

            wood.Push(c, new Vector3(0f, 90f, 0f));
            props.Push(c, new Vector3(0f, 90f, 0f));
            glass.Push(c, new Vector3(0f, 90f, 0f));

            wood.AddBox(new Vector3(0f, 0.42f, 0f), new Vector3(1.5f, 0.84f, 0.62f), Palette.WoodDark);
            wood.AddBox(new Vector3(0f, 0.86f, 0f), new Vector3(1.56f, 0.04f, 0.68f), Palette.WoodDeep);
            wood.AddBox(new Vector3(0f, 0.42f, 0.315f), new Vector3(1.3f, 0.6f, 0.02f), Palette.WoodMid);
            props.AddBox(new Vector3(0f, 0.885f, 0f), new Vector3(1.4f, 0.01f, 0.56f), Palette.Velvet);
            glass.AddBox(new Vector3(0f, 1.08f, 0f), new Vector3(1.44f, 0.38f, 0.6f), Palette.Hex(0xE8F0FF));
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    props.AddBox(new Vector3(x * 0.72f, 1.08f, z * 0.3f), new Vector3(0.03f, 0.38f, 0.03f), Palette.Brass);
                }
            }

            props.AddBox(new Vector3(0f, 1.27f, 0f), new Vector3(1.47f, 0.03f, 0.63f), Palette.BrassDark);

            string[] items = { "orb", "crown", "astrolabe" };
            for (int i = 0; i < items.Length; i++)
            {
                props.Push(new Vector3(-0.45f + i * 0.45f, 0.89f, 0f), new Vector3(0f, 180f + (i - 1) * 20f, 0f), Vector3.one * 1.05f);
                glass.Push(new Vector3(-0.45f + i * 0.45f, 0.89f, 0f), new Vector3(0f, 180f + (i - 1) * 20f, 0f), Vector3.one * 1.05f);
                ItemModelBuilder.BuildInto(items[i], props, glass);
                glass.Pop();
                props.Pop();
            }

            wood.Pop();
            props.Pop();
            glass.Pop();
        }

        private static void BuildStorageCorner(Kit kit)
        {
            MeshBuilder wood = kit["Storage", EnvMaterial.Wood];
            MeshBuilder props = kit["Storage", EnvMaterial.Props];
            MeshBuilder cloth = kit["Storage", EnvMaterial.Cloth];

            void Crate(Vector3 p, float size, float yaw)
            {
                wood.Push(p, new Vector3(0f, yaw, 0f));
                wood.AddBox(new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, Palette.WoodMid);
                for (int i = -1; i <= 1; i += 2)
                {
                    wood.AddBox(new Vector3(0f, size * 0.5f, i * (size * 0.5f + 0.005f)), new Vector3(size * 0.98f, 0.06f, 0.012f), Palette.WoodDark);
                    wood.AddBox(new Vector3(i * (size * 0.5f + 0.005f), size * 0.5f, 0f), new Vector3(0.012f, 0.06f, size * 0.98f), Palette.WoodDark);
                }

                wood.AddBox(new Vector3(0f, size * 0.5f, size * 0.5f + 0.008f), new Vector3(0.06f, size * 0.98f, 0.012f), Palette.WoodDark);
                wood.Pop();
            }

            void Barrel(Vector3 p)
            {
                var profile = new[]
                {
                    new Vector2(0f, 0f), new Vector2(0.24f, 0f), new Vector2(0.28f, 0.2f), new Vector2(0.3f, 0.4f),
                    new Vector2(0.28f, 0.6f), new Vector2(0.24f, 0.8f), new Vector2(0f, 0.8f)
                };
                wood.AddLathe(profile, 16, Palette.WoodMid, 0f, 50f, p);
                props.AddTorus(p + new Vector3(0f, 0.15f, 0f), 0.272f, 0.012f, Palette.Iron, 0f, 18, 4);
                props.AddTorus(p + new Vector3(0f, 0.65f, 0f), 0.272f, 0.012f, Palette.Iron, 0f, 18, 4);
            }

            void Sack(Vector3 p, float yaw)
            {
                cloth.Push(p, new Vector3(0f, yaw, 6f));
                cloth.AddSphere(new Vector3(0f, 0.22f, 0f), new Vector3(0.24f, 0.24f, 0.2f), Palette.Mix(Palette.ClothCream, Palette.ClothBrown, 0.4f), 0f, 12);
                cloth.AddCylinder(new Vector3(0f, 0.4f, 0f), 0.08f, 0.05f, 0.1f, Palette.Mix(Palette.ClothCream, Palette.ClothBrown, 0.5f), 0f, 8);
                cloth.Pop();
            }

            Crate(new Vector3(3.85f, 0f, 4.75f), 0.62f, 8f);
            Crate(new Vector3(3.25f, 0f, 4.95f), 0.5f, -12f);
            Crate(new Vector3(3.8f, 0.62f, 4.72f), 0.45f, 30f);
            Barrel(new Vector3(3.95f, 0f, 3.85f));
            Sack(new Vector3(3.25f, 0f, 4.2f), 30f);
            Sack(new Vector3(3.5f, 0.5f, 4.98f), -20f);

            // Left front corner: a chest and a barrel
            Barrel(new Vector3(-3.95f, 0f, 4.85f));
            wood.Push(new Vector3(-3.2f, 0f, 5.0f), new Vector3(0f, 10f, 0f));
            wood.AddBox(new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.4f, 0.42f), Palette.WoodDark);
            wood.Push(new Vector3(0f, 0.4f, 0f), new Vector3(0f, 0f, 90f));
            wood.AddCylinder(new Vector3(0f, -0.35f, 0f), 0.21f, 0.21f, 0.7f, Palette.WoodDark, 0f, 12);
            wood.Pop();
            wood.Pop();
            props.Push(new Vector3(-3.2f, 0f, 5.0f), new Vector3(0f, 10f, 0f));
            for (int i = -1; i <= 1; i += 2)
            {
                props.AddBox(new Vector3(i * 0.25f, 0.3f, 0.215f), new Vector3(0.05f, 0.5f, 0.01f), Palette.Iron);
            }

            props.AddBox(new Vector3(0f, 0.36f, 0.22f), new Vector3(0.08f, 0.1f, 0.02f), Palette.Brass);
            props.Pop();
        }

        private static void BuildPortal(Kit kit, ShopLayout layout)
        {
            // Scenery for now: the dimensional portal device (travel is a later milestone).
            var p = new Vector3(HalfWidth - 0.45f, 0f, 1.9f);
            MeshBuilder props = kit["Portal", EnvMaterial.Props];
            props.Push(p, new Vector3(0f, -90f, 0f));
            props.AddCylinder(Vector3.zero, 0.85f, 0.8f, 0.12f, Palette.StoneDark, 0f, 20);
            props.Push(new Vector3(0f, 1.15f, 0f), new Vector3(90f, 0f, 0f));
            props.AddTorus(Vector3.zero, 0.78f, 0.07f, Palette.Brass, 0f, 32, 8);
            props.AddTorus(Vector3.zero, 0.66f, 0.025f, Palette.BrassDark, 0f, 32, 6);
            props.AddCylinder(new Vector3(0f, -0.01f, 0f), 0.65f, 0.65f, 0.02f, Palette.Hex(0x2A1F4A), 0.55f, 32);
            props.AddCylinder(new Vector3(0f, -0.005f, 0f), 0.35f, 0.35f, 0.02f, Palette.Hex(0x5A3FA0), 0.6f, 24);
            props.Pop();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                props.AddGem(new Vector3(Mathf.Cos(a) * 0.78f, 1.15f + Mathf.Sin(a) * 0.78f, 0.06f), 0.045f, 0.05f, 0.05f, 4, Palette.Arcane, 0.5f);
            }

            props.AddBox(new Vector3(-0.75f, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.2f), Palette.BrassDark);
            props.AddBox(new Vector3(0.75f, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.2f), Palette.BrassDark);
            props.Pop();

            layout.Lights.Add(new EnvLight
            {
                Name = "PortalGlow", Position = p + new Vector3(-0.5f, 1.2f, 0f), Color = Palette.Hex(0x8A6BFF),
                Intensity = 0.6f, Range = 2.6f
            });
        }

        private static void BuildBanner(Kit kit, ShopLayout layout)
        {
            // Hanging shop banner on the left wall, facing into the room.
            var p = new Vector3(-HalfWidth + 0.06f, 3.0f, 2.9f);
            MeshBuilder cloth = kit["Banner", EnvMaterial.Cloth];
            MeshBuilder props = kit["Banner", EnvMaterial.Props];
            cloth.Push(p, new Vector3(0f, 90f, 0f));
            props.Push(p, new Vector3(0f, 90f, 0f));

            const float w = 0.95f, h = 1.2f;
            Color purple = Palette.Shade(Palette.ClothPurple, 0.9f);
            cloth.AddBox(new Vector3(0f, -h * 0.5f, 0.02f), new Vector3(w, h, 0.02f), purple);
            cloth.AddFlatTriangle(new Vector3(-w * 0.5f, -h, 0.031f), new Vector3(w * 0.5f, -h, 0.031f), new Vector3(0f, -h - 0.35f, 0.031f), new Vector3(0f, -h, -1f), purple);
            cloth.AddFlatTriangle(new Vector3(-w * 0.5f, -h, 0.009f), new Vector3(w * 0.5f, -h, 0.009f), new Vector3(0f, -h - 0.35f, 0.009f), new Vector3(0f, -h, 1f), purple);
            cloth.AddBox(new Vector3(0f, -0.12f, 0.032f), new Vector3(w - 0.08f, 0.03f, 0.004f), Palette.Gold);
            cloth.AddBox(new Vector3(0f, -h + 0.08f, 0.032f), new Vector3(w - 0.08f, 0.03f, 0.004f), Palette.Gold);

            props.Push(Vector3.zero, new Vector3(0f, 0f, 90f));
            props.AddCylinder(new Vector3(0f, -w * 0.6f, 0.02f), 0.025f, 0.025f, w * 1.2f, Palette.Brass, 0f, 8);
            props.Pop();
            props.AddSphere(new Vector3(-w * 0.62f, 0f, 0.02f), 0.04f, Palette.Brass, 0f, 8);
            props.AddSphere(new Vector3(w * 0.62f, 0f, 0.02f), 0.04f, Palette.Brass, 0f, 8);

            // Scales emblem
            Vector3 e = new Vector3(0f, -0.88f, 0.04f);
            props.Push(e, Vector3.zero, Vector3.one * 0.75f);
            e = Vector3.zero;
            props.AddBox(e + new Vector3(0f, 0.0f, 0f), new Vector3(0.03f, 0.32f, 0.01f), Palette.Gold);
            props.AddBox(e + new Vector3(0f, 0.14f, 0f), new Vector3(0.4f, 0.025f, 0.01f), Palette.Gold);
            props.AddBox(e + new Vector3(0f, -0.16f, 0f), new Vector3(0.18f, 0.03f, 0.01f), Palette.Gold);
            for (int side = -1; side <= 1; side += 2)
            {
                props.AddBox(e + new Vector3(side * 0.18f, 0.06f, 0f), new Vector3(0.008f, 0.15f, 0.01f), Palette.Gold);
                props.Push(e + new Vector3(side * 0.18f, -0.02f, 0f), new Vector3(90f, 0f, 0f));
                props.AddCylinder(Vector3.zero, 0.08f, 0.05f, 0.012f, Palette.Gold, 0f, 12);
                props.Pop();
            }

            props.Pop();
            cloth.Pop();
            props.Pop();

            layout.Labels.Add(new EnvLabel
            {
                Text = "THE\nBLACK\nMARKET", Position = p + new Vector3(0.05f, -0.4f, 0f), Euler = new Vector3(0f, -90f, 0f),
                BoxHeight = 0.45f, Color = Palette.Gold
            });
        }

        private static void BuildWallDecor(Kit kit)
        {
            MeshBuilder props = kit["Decor", EnvMaterial.Props];

            // Framed map on the right wall, above the storage corner
            props.Push(new Vector3(HalfWidth - 0.04f, 2.0f, 4.0f), new Vector3(0f, -90f, 0f));
            props.AddBox(Vector3.zero, new Vector3(1.0f, 0.7f, 0.04f), Palette.WoodDeep);
            props.AddBox(new Vector3(0f, 0f, 0.02f), new Vector3(0.88f, 0.58f, 0.01f), Palette.Parchment);
            var rnd = new System.Random(9);
            for (int i = 0; i < 9; i++)
            {
                props.Push(new Vector3((float)rnd.NextDouble() * 0.7f - 0.35f, (float)rnd.NextDouble() * 0.4f - 0.2f, 0.027f), new Vector3(0f, 0f, (float)rnd.NextDouble() * 180f));
                props.AddBox(Vector3.zero, new Vector3(0.12f + 0.1f * (float)rnd.NextDouble(), 0.006f, 0.004f), Palette.Shade(Palette.Leather, 0.8f));
                props.Pop();
            }

            props.AddSphere(new Vector3(0.22f, -0.1f, 0.027f), new Vector3(0.025f, 0.025f, 0.004f), Palette.Wax, 0f, 8);
            props.Pop();

            // Mounted ram skull on the left wall, above the shelves
            props.Push(new Vector3(-HalfWidth + 0.06f, 3.0f, 0.8f), new Vector3(0f, 90f, 0f), Vector3.one * 2.2f);
            props.AddBox(new Vector3(0f, 0.06f, -0.01f), new Vector3(0.2f, 0.24f, 0.02f), Palette.WoodDark);
            props.Push(new Vector3(0f, -0.02f, 0f), new Vector3(-10f, 0f, 0f));
            ItemModelBuilder.BuildInto("skull", props, props);
            props.Pop();
            props.Pop();

            // Clock on the back wall, above the curtain
            props.Push(new Vector3(0f, 2.85f, BackZ + 0.03f), new Vector3(90f, 0f, 0f));
            props.AddCylinder(Vector3.zero, 0.24f, 0.24f, 0.05f, Palette.WoodDeep, 0f, 20);
            props.AddCylinder(new Vector3(0f, 0.05f, 0f), 0.2f, 0.2f, 0.01f, Palette.Parchment, 0f, 20);
            props.AddTorus(new Vector3(0f, 0.055f, 0f), 0.2f, 0.012f, Palette.Brass, 0f, 20, 5);
            props.AddBox(new Vector3(0f, 0.065f, 0.06f), new Vector3(0.012f, 0.01f, 0.12f), Palette.Ink);
            props.AddBox(new Vector3(0.05f, 0.065f, 0f), new Vector3(0.1f, 0.01f, 0.012f), Palette.Ink);
            props.Pop();
        }

        // ------------------------------------------------------------------ Lighting

        private static void BuildLanterns(Kit kit, ShopLayout layout)
        {
            HangingLantern(kit, new Vector3(0f, 2.35f, CounterZ + 0.05f), 0.8f, 1.15f);
            HangingLantern(kit, new Vector3(0f, 2.55f, 3.55f), 0.6f, 1f);
            HangingLantern(kit, new Vector3(-2.6f, 2.6f, -1.0f), 0.55f, 0.9f);
            HangingLantern(kit, new Vector3(2.6f, 2.6f, -1.0f), 0.55f, 0.9f);
            WallSconce(kit, new Vector3(-HalfWidth + 0.12f, 2.05f, 3.6f), 90f);
            WallSconce(kit, new Vector3(HalfWidth - 0.12f, 2.05f, 0.3f), -90f);
            WallSconce(kit, new Vector3(HalfWidth - 0.12f, 2.05f, 3.4f), -90f);

            Color warm = Palette.Hex(0xFFC98A);
            layout.Lights.Add(new EnvLight
            {
                Name = "KeyLight", Directional = true, Euler = new Vector3(52f, -28f, 0f), Color = Palette.Hex(0xFFE0B8),
                Intensity = 0.32f, Shadows = true, Important = true
            });
            layout.Lights.Add(new EnvLight { Name = "CounterLantern", Position = new Vector3(0f, 2.2f, CounterZ + 0.05f), Color = warm, Intensity = 1.5f, Range = 6.5f, Important = true });
            layout.Lights.Add(new EnvLight { Name = "EntranceLantern", Position = new Vector3(0f, 2.4f, 3.55f), Color = warm, Intensity = 1.0f, Range = 5.5f, Important = true });
            layout.Lights.Add(new EnvLight { Name = "BackLanterns", Position = new Vector3(0f, 2.4f, -1.2f), Color = warm, Intensity = 0.9f, Range = 5f, Important = true });
            layout.Lights.Add(new EnvLight { Name = "LeftSconce", Position = new Vector3(-HalfWidth + 0.5f, 2.0f, 2.6f), Color = warm, Intensity = 0.7f, Range = 4f });
            layout.Lights.Add(new EnvLight { Name = "RightSconce", Position = new Vector3(HalfWidth - 0.5f, 2.0f, 1.8f), Color = warm, Intensity = 0.6f, Range = 4f });
        }

        private static void HangingLantern(Kit kit, Vector3 p, float chain, float scale)
        {
            MeshBuilder props = kit["Lanterns", EnvMaterial.Props];
            MeshBuilder glass = kit["Lanterns", EnvMaterial.Glass];

            float top = Height - 0.26f;
            props.AddCylinder(new Vector3(p.x, p.y + 0.2f * scale, p.z), 0.006f, 0.006f, top - p.y - 0.2f * scale, Palette.Iron, 0f, 6);

            props.Push(p, Vector3.zero, Vector3.one * scale);
            glass.Push(p, Vector3.zero, Vector3.one * scale);
            props.AddCylinder(new Vector3(0f, -0.2f, 0f), 0.11f, 0.13f, 0.04f, Palette.Iron, 0f, 8);
            props.AddCylinder(new Vector3(0f, 0.1f, 0f), 0.15f, 0.04f, 0.1f, Palette.Iron, 0f, 8);
            props.Push(new Vector3(0f, 0.2f, 0f), new Vector3(90f, 0f, 0f));
            props.AddTorus(Vector3.zero, 0.035f, 0.008f, Palette.Iron, 0f, 10, 5);
            props.Pop();
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
                props.AddBox(new Vector3(Mathf.Cos(a) * 0.105f, -0.05f, Mathf.Sin(a) * 0.105f), new Vector3(0.018f, 0.32f, 0.018f), Palette.Iron);
            }

            props.AddCylinder(new Vector3(0f, -0.16f, 0f), 0.025f, 0.025f, 0.06f, Palette.ClothCream, 0f, 8);
            props.AddGem(new Vector3(0f, -0.07f, 0f), 0.022f, 0.06f, 0.02f, 6, Palette.Flame, 1f);
            props.AddSphere(new Vector3(0f, -0.06f, 0f), 0.06f, Palette.Shade(Palette.LampGlass, 0.9f), 0.85f, 10);
            glass.AddCylinder(new Vector3(0f, -0.2f, 0f), 0.1f, 0.1f, 0.3f, Palette.LampGlass, 0f, 4);
            props.Pop();
            glass.Pop();
        }

        private static void WallSconce(Kit kit, Vector3 p, float yaw)
        {
            MeshBuilder props = kit["Lanterns", EnvMaterial.Props];
            props.Push(p, new Vector3(0f, yaw, 0f));
            props.AddBox(new Vector3(0f, 0f, -0.02f), new Vector3(0.14f, 0.22f, 0.03f), Palette.Iron);
            props.AddBox(new Vector3(0f, -0.06f, 0.1f), new Vector3(0.03f, 0.03f, 0.2f), Palette.Iron);
            props.AddCylinder(new Vector3(0f, -0.08f, 0.2f), 0.06f, 0.06f, 0.02f, Palette.Brass, 0f, 10);
            props.AddCylinder(new Vector3(0f, -0.06f, 0.2f), 0.03f, 0.028f, 0.1f, Palette.ClothCream, 0f, 8);
            props.AddGem(new Vector3(0f, 0.07f, 0.2f), 0.016f, 0.045f, 0.012f, 6, Palette.Flame, 1f);
            props.AddSphere(new Vector3(0f, 0.08f, 0.2f), 0.04f, Palette.LampGlass, 0.8f, 8);
            props.Pop();
        }

        // ------------------------------------------------------------------ Outside

        private static void BuildStreet(Kit kit, ShopLayout layout)
        {
            kit["Street", EnvMaterial.Cobble].AddBox(new Vector3(0f, -0.06f, 12f), new Vector3(30f, 0.1f, 13f), Color.white);

            MeshBuilder props = kit["Street", EnvMaterial.Props];
            MeshBuilder cloth = kit["Street", EnvMaterial.Cloth];


            // Night sky: layered so the horizon glows
            props.AddBox(new Vector3(0f, 3f, 24f), new Vector3(60f, 6f, 0.2f), Palette.Hex(0x6A4A7A), 1f);
            props.AddBox(new Vector3(0f, 9f, 24f), new Vector3(60f, 6f, 0.2f), Palette.Hex(0x3E2E62), 1f);
            props.AddBox(new Vector3(0f, 16f, 24f), new Vector3(60f, 8f, 0.2f), Palette.Hex(0x221C3E), 1f);
            props.AddSphere(new Vector3(-6f, 11f, 23.7f), 1.3f, Palette.Hex(0xF4E9D0), 1f, 16);

            // Houses across the street
            var rnd = new System.Random(5);
            float x = -13f;
            while (x < 13f)
            {
                float w = 3.2f + (float)rnd.NextDouble() * 1.6f;
                float h = 5.2f + (float)rnd.NextDouble() * 2.2f;
                House(props, new Vector3(x + w * 0.5f, 0f, 15.5f + (float)rnd.NextDouble() * 0.8f), w, h, rnd);
                x += w + 0.05f;
            }

            // Distant rooftops (silhouettes against the sky)
            for (int i = 0; i < 9; i++)
            {
                float rx = -16f + i * 4.1f;
                float rh = 7f + (float)rnd.NextDouble() * 3f;
                props.AddBox(new Vector3(rx, rh * 0.5f, 20f), new Vector3(3.6f, rh, 1f), Palette.Hex(0x2C2240));
                props.Push(new Vector3(rx, rh, 20f), new Vector3(0f, 0f, 45f));
                props.AddBox(Vector3.zero, new Vector3(2.4f, 2.4f, 1f), Palette.Hex(0x2C2240));
                props.Pop();
                if (rnd.NextDouble() < 0.6) props.AddBox(new Vector3(rx + 0.6f, rh - 1.5f, 19.4f), new Vector3(0.35f, 0.5f, 0.05f), Palette.Flame, 0.9f);
            }

            // Market stalls with striped awnings
            Stall(props, cloth, new Vector3(-1.9f, 0f, 9.3f), Palette.ClothRed, rnd);
            Stall(props, cloth, new Vector3(2.0f, 0f, 9.6f), Palette.ClothPurple, rnd);
            Stall(props, cloth, new Vector3(6.2f, 0f, 9.4f), Palette.ClothGreen, rnd);
            Stall(props, cloth, new Vector3(-6.0f, 0f, 9.2f), Palette.ClothBlue, rnd);

            // String lights across the street
            for (int strand = 0; strand < 2; strand++)
            {
                float z = 8.2f + strand * 3.2f;
                const int bulbs = 28;
                for (int i = 0; i <= bulbs; i++)
                {
                    float t = i / (float)bulbs;
                    float bx = Mathf.Lerp(-9f, 9f, t);
                    float sag = 4f * t * (1f - t);
                    float y = 4.1f - sag * 0.9f;
                    Color c = i % 3 == 0 ? Palette.Hex(0xFFB0E0) : (i % 3 == 1 ? Palette.Flame : Palette.Hex(0xB89BFF));
                    props.AddSphere(new Vector3(bx, y, z), 0.06f, c, 1f, 6);
                }
            }

            // Street lamp
            var lamp = new Vector3(2.9f, 0f, 7.4f);
            props.AddCylinder(lamp, 0.12f, 0.08f, 0.3f, Palette.Iron, 0f, 8);
            props.AddCylinder(lamp, 0.05f, 0.04f, 3f, Palette.Iron, 0f, 8);
            props.AddCylinder(lamp + new Vector3(0f, 3.0f, 0f), 0.16f, 0.04f, 0.15f, Palette.Iron, 0f, 6);
            props.AddCylinder(lamp + new Vector3(0f, 2.62f, 0f), 0.1f, 0.13f, 0.38f, Palette.LampGlass, 0.9f, 6);

            layout.Lights.Add(new EnvLight { Name = "StreetGlow", Position = new Vector3(0f, 3.2f, 9.5f), Color = Palette.Hex(0xC59BFF), Intensity = 1.3f, Range = 10f });
            layout.Lights.Add(new EnvLight { Name = "StreetLamp", Position = lamp + new Vector3(0f, 2.8f, 0f), Color = Palette.Hex(0xFFC27A), Intensity = 1.0f, Range = 5f });
        }

        private static void House(MeshBuilder b, Vector3 p, float w, float h, System.Random rnd)
        {
            Color wall = Palette.Shade(rnd.NextDouble() < 0.5 ? Palette.Plaster : Palette.Mix(Palette.Plaster, Palette.ClothPurple, 0.3f), 0.55f + 0.15f * (float)rnd.NextDouble());
            Color timber = Palette.Shade(Palette.WoodDeep, 0.8f);
            b.AddBox(p + new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 1f), wall);
            b.AddBox(p + new Vector3(0f, 0.4f, -0.52f), new Vector3(w, 0.8f, 0.06f), Palette.Shade(Palette.Stone, 0.6f));
            b.AddBox(p + new Vector3(-w * 0.5f + 0.08f, h * 0.5f, -0.52f), new Vector3(0.16f, h, 0.06f), timber);
            b.AddBox(p + new Vector3(w * 0.5f - 0.08f, h * 0.5f, -0.52f), new Vector3(0.16f, h, 0.06f), timber);
            b.AddBox(p + new Vector3(0f, h * 0.5f, -0.52f), new Vector3(w, 0.14f, 0.06f), timber);

            // Roof
            b.Push(p + new Vector3(0f, h, 0f), new Vector3(0f, 0f, 0f));
            b.AddFlatTriangle(new Vector3(-w * 0.55f, 0f, -0.6f), new Vector3(w * 0.55f, 0f, -0.6f), new Vector3(0f, w * 0.38f, -0.6f), new Vector3(0f, 0f, 1f), Palette.Hex(0x3A2A3A));
            b.Pop();

            // Glowing windows
            int cols = w > 4f ? 3 : 2;
            for (int row = 0; row < 2; row++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float wx = -w * 0.5f + (c + 0.5f) * w / cols;
                    float wy = 1.6f + row * 1.9f;
                    bool lit = rnd.NextDouble() < 0.65;
                    b.AddBox(p + new Vector3(wx, wy, -0.54f), new Vector3(0.62f, 0.85f, 0.04f), timber);
                    b.AddBox(p + new Vector3(wx, wy, -0.56f), new Vector3(0.5f, 0.72f, 0.04f), lit ? Palette.Hex(0xFFB866) : Palette.Hex(0x2A2238), lit ? 0.9f : 0f);
                    b.AddBox(p + new Vector3(wx, wy, -0.585f), new Vector3(0.04f, 0.72f, 0.02f), timber);
                }
            }

            // Door
            if (rnd.NextDouble() < 0.6)
            {
                b.AddBox(p + new Vector3(w * 0.25f, 1.0f, -0.55f), new Vector3(0.9f, 2.0f, 0.05f), Palette.WoodDark);
            }
        }

        private static void Stall(MeshBuilder props, MeshBuilder cloth, Vector3 p, Color stripe, System.Random rnd)
        {
            const float w = 2.2f, d = 1.1f;
            props.AddBox(p + new Vector3(0f, 0.45f, 0f), new Vector3(w, 0.9f, d * 0.7f), Palette.WoodMid);
            props.AddBox(p + new Vector3(0f, 0.92f, 0f), new Vector3(w + 0.1f, 0.05f, d * 0.75f), Palette.WoodDark);
            for (int i = -1; i <= 1; i += 2)
            {
                props.AddBox(p + new Vector3(i * w * 0.48f, 1.2f, -d * 0.4f), new Vector3(0.07f, 2.4f, 0.07f), Palette.WoodDark);
                props.AddBox(p + new Vector3(i * w * 0.48f, 1.1f, d * 0.4f), new Vector3(0.07f, 2.2f, 0.07f), Palette.WoodDark);
            }

            // Striped awning, sloping toward the street
            int stripes = 8;
            for (int s = 0; s < stripes; s++)
            {
                float sx = -w * 0.5f - 0.1f + (s + 0.5f) * (w + 0.2f) / stripes;
                cloth.Push(p + new Vector3(sx, 2.3f, 0f), new Vector3(-18f, 0f, 0f));
                cloth.AddBox(Vector3.zero, new Vector3((w + 0.2f) / stripes, 0.03f, d + 0.4f), s % 2 == 0 ? stripe : Palette.ClothCream);
                cloth.AddBox(new Vector3(0f, -0.1f, -(d + 0.4f) * 0.5f), new Vector3((w + 0.2f) / stripes, 0.2f, 0.02f), s % 2 == 0 ? stripe : Palette.ClothCream);
                cloth.Pop();
            }

            // Goods on the stall
            for (int i = 0; i < 4; i++)
            {
                string id = ShelfItems[rnd.Next(ShelfItems.Length)];
                props.Push(p + new Vector3(-0.75f + i * 0.5f, 0.95f, 0f), new Vector3(0f, 180f, 0f), Vector3.one * 1.6f);
                ItemModelBuilder.BuildInto(id, props, props);
                props.Pop();
            }

            props.AddSphere(p + new Vector3(0f, 2.05f, -0.5f), 0.05f, Palette.Flame, 1f, 6);
        }

        // ------------------------------------------------------------------ Door, street folk, anchors

        private static void AddDoor(ShopLayout layout)
        {
            var door = new MeshBuilder();
            float w = DoorHalfWidth * 2f - 0.02f;
            int planks = 5;
            for (int i = 0; i < planks; i++)
            {
                float px = (i + 0.5f) * w / planks;
                door.AddBox(new Vector3(px, DoorHeight * 0.5f - 0.01f, 0f), new Vector3(w / planks - 0.008f, DoorHeight - 0.02f, 0.07f),
                    Palette.Shade(Palette.WoodMid, i % 2 == 0 ? 0.95f : 1.05f));
            }

            foreach (float y in new[] { 0.45f, 1.3f, 2.15f })
            {
                door.AddBox(new Vector3(w * 0.5f, y, -0.05f), new Vector3(w - 0.08f, 0.14f, 0.03f), Palette.WoodDark);
                door.AddBox(new Vector3(0.12f, y, -0.07f), new Vector3(0.28f, 0.05f, 0.02f), Palette.Iron);
            }

            door.AddBox(new Vector3(w * 0.5f, 1.75f, 0f), new Vector3(0.5f, 0.42f, 0.08f), Palette.WoodDark);
            door.AddBox(new Vector3(w * 0.5f, 1.75f, -0.01f), new Vector3(0.4f, 0.32f, 0.08f), Palette.Hex(0x8A6AB0), 0.6f);
            door.AddBox(new Vector3(w * 0.5f, 1.75f, -0.05f), new Vector3(0.02f, 0.32f, 0.02f), Palette.Iron);
            door.Push(new Vector3(w - 0.14f, 1.1f, -0.07f), new Vector3(90f, 0f, 0f));
            door.AddTorus(Vector3.zero, 0.05f, 0.01f, Palette.Brass, 0f, 14, 6);
            door.Pop();

            layout.Pieces.Add(new EnvPiece
            {
                Name = "Door", Material = EnvMaterial.Wood, Mesh = door.ToMesh("Door"),
                Position = new Vector3(-DoorHalfWidth + 0.01f, 0f, FrontZ + 0.05f), Tag = "Door", IsStatic = false
            });
        }

        private static void AddStreetFolk(ShopLayout layout)
        {
            // A few passers-by across the street, so the market outside feels alive.
            var folk = new[]
            {
                (look: "peddler", pos: new Vector3(-2.2f, 0f, 10.6f), yaw: 160f),
                (look: "noble", pos: new Vector3(1.5f, 0f, 11.4f), yaw: 200f),
                (look: "sailor", pos: new Vector3(5.2f, 0f, 10.4f), yaw: 120f)
            };

            int seed = 40;
            foreach (var f in folk)
            {
                CharacterMeshes c = CharacterBuilder.Build(f.look, seed++);
                var b = new MeshBuilder();
                b.Push(f.pos, new Vector3(0f, f.yaw, 0f));
                AppendPart(b, c.Body, Vector3.zero);
                AppendPart(b, c.Head, c.HeadPivot);
                AppendPart(b, c.ArmLeft, c.ArmLeftPivot);
                AppendPart(b, c.ArmRight, c.ArmRightPivot);
                AppendPart(b, c.LegLeft, c.LegLeftPivot);
                AppendPart(b, c.LegRight, c.LegRightPivot);
                b.Pop();
                layout.Pieces.Add(new EnvPiece { Name = "StreetFolk_" + f.look, Material = EnvMaterial.Props, Mesh = b.ToMesh("StreetFolk_" + f.look), Tag = "StreetFolk" });
            }
        }

        private static void AppendPart(MeshBuilder b, Mesh part, Vector3 offset)
        {
            b.Push(offset);
            b.AddMesh(part);
            b.Pop();
        }

        private static void AddAnchors(ShopLayout layout)
        {
            layout.Anchors["PlayerRig"] = Vector3.zero;
            layout.Anchors["CustomerStart"] = new Vector3(0.6f, 0f, 11f);
            layout.Anchors["DoorOutside"] = new Vector3(0f, 0f, FrontZ + 0.9f);
            layout.Anchors["DoorInside"] = new Vector3(0f, 0f, FrontZ - 0.8f);
            layout.Anchors["CustomerCounter"] = new Vector3(0f, 0f, 2.0f);
            layout.Anchors["ItemSpot"] = new Vector3(0f, CounterTop + 0.014f, CounterZ - 0.02f);
            layout.Anchors["StockDrop"] = new Vector3(0.85f, 0.62f, 0.3f);
            layout.Anchors["DoorHinge"] = new Vector3(-DoorHalfWidth + 0.01f, 0f, FrontZ + 0.05f);
        }
    }
}
