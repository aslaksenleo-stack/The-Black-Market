using System.IO;
using BlackMarket.Interaction;
using BlackMarket.Player;
using BlackMarket.Shop;
using BlackMarket.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BlackMarket.EditorTools
{
    /// <summary>
    /// Builds the Shop scene from code: a greybox (blocked-out) shop interior, the player rig, lighting
    /// and the scene's gameplay components.
    ///
    /// "Greybox" means simple blocks standing in for real models, so layout, scale and camera can be
    /// tested before art exists. Every block name starts with "PH_" (placeholder) and is listed in
    /// ASSET_CHECKLIST.md. Real models replace them later without changing gameplay code.
    ///
    /// Layout (metres, player faces +Z):
    ///   z = -2.5  back wall with shelves (behind the player)
    ///   z = -0.3  player, standing behind the counter
    ///   z =  1.0  counter
    ///   z =  1.8  where customers stand
    ///   z =  5.0  front wall with the entrance door
    ///   x = +3.3  portal device (to the player's right)
    /// </summary>
    public static class ShopSceneBuilder
    {
        private const string MaterialsFolder = "Assets/Art/Materials/Placeholder";

        private const float RoomWidth = 8f;
        private const float RoomHeight = 3.2f;
        private const float BackZ = -2.5f;
        private const float FrontZ = 5f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 1.6f;
        private const float DoorHeight = 2.4f;

        private static Material wood;
        private static Material darkWood;
        private static Material plaster;
        private static Material floorBoards;
        private static Material brass;
        private static Material leather;
        private static Material glass;
        private static Material portalCore;

        public static void Build(string scenePath)
        {
            CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProjectSetup.AddGeneratedMarker();

            ConfigureLighting();
            BuildRoom();
            BuildCounter();
            BuildBackShelves();
            BuildDisplayTable();
            BuildPortalDevice();
            BuildCustomerPoints();
            BuildPlayer();
            BuildGameplayObjects();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // ------------------------------------------------------------------ Materials

        private static void CreateMaterials()
        {
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh();

            wood = GetOrCreateMaterial("M_PH_Wood", new Color(0.42f, 0.27f, 0.16f), 0.35f);
            darkWood = GetOrCreateMaterial("M_PH_DarkWood", new Color(0.20f, 0.13f, 0.08f), 0.45f);
            plaster = GetOrCreateMaterial("M_PH_Plaster", new Color(0.52f, 0.47f, 0.40f), 0.05f);
            floorBoards = GetOrCreateMaterial("M_PH_Floor", new Color(0.30f, 0.22f, 0.15f), 0.25f);
            brass = GetOrCreateMaterial("M_PH_Brass", new Color(0.72f, 0.56f, 0.32f), 0.65f, 0.85f);
            leather = GetOrCreateMaterial("M_PH_Leather", new Color(0.36f, 0.10f, 0.08f), 0.3f);
            glass = GetOrCreateMaterial("M_PH_GlassJar", new Color(0.45f, 0.62f, 0.55f), 0.9f);
            portalCore = GetOrCreateMaterial("M_PH_PortalCore", new Color(0.05f, 0.08f, 0.10f), 0.9f, 0f,
                new Color(0.10f, 0.55f, 0.60f) * 1.2f);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness, float metallic = 0f, Color? emission = null)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ Lighting

        private static void ConfigureLighting()
        {
            // Indoor scene: no sky. A warm, low ambient light so shadows aren't pitch black.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.17f, 0.14f);

            var lights = new GameObject("Lighting").transform;

            // Faint fill light, as if from windows.
            Light fill = CreateLight("FillLight", lights, LightType.Directional, new Color(1f, 0.92f, 0.80f), 0.35f, 0f);
            fill.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            fill.shadows = LightShadows.None;

            // Warm ceiling lamps: over the counter, and over the shelves behind the player.
            CreateLight("CounterLamp", lights, LightType.Point, new Color(1f, 0.80f, 0.58f), 1.6f, 7f)
                .transform.position = new Vector3(0f, 2.9f, 0.8f);
            CreateLight("ShelfLamp", lights, LightType.Point, new Color(1f, 0.80f, 0.58f), 1.1f, 5f)
                .transform.position = new Vector3(0f, 2.9f, -1.8f);

            // Cool glow from the portal device.
            CreateLight("PortalGlow", lights, LightType.Point, new Color(0.35f, 0.85f, 0.90f), 0.8f, 3f)
                .transform.position = new Vector3(2.8f, 1.3f, 0.2f);

            // Lamp fittings so the light has a visible source.
            CreateBlock("PH_LampShade_Counter", lights, new Vector3(0f, 3.05f, 0.8f), new Vector3(0.5f, 0.12f, 0.5f), brass);
            CreateBlock("PH_LampShade_Shelf", lights, new Vector3(0f, 3.05f, -1.8f), new Vector3(0.5f, 0.12f, 0.5f), brass);
        }

        private static Light CreateLight(string name, Transform parent, LightType type, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            var light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            if (range > 0f)
            {
                light.range = range;
            }

            return light;
        }

        // ------------------------------------------------------------------ Room

        private static void BuildRoom()
        {
            var room = new GameObject("Room").transform;
            float depth = FrontZ - BackZ;
            float centreZ = (FrontZ + BackZ) * 0.5f;
            float halfWidth = RoomWidth * 0.5f;

            CreateBlock("PH_Floor", room, new Vector3(0f, -0.05f, centreZ), new Vector3(RoomWidth, 0.1f, depth), floorBoards);
            CreateBlock("PH_Ceiling", room, new Vector3(0f, RoomHeight + 0.05f, centreZ), new Vector3(RoomWidth, 0.1f, depth), darkWood);
            CreateBlock("PH_Wall_Back", room, new Vector3(0f, RoomHeight * 0.5f, BackZ), new Vector3(RoomWidth, RoomHeight, WallThickness), plaster);
            CreateBlock("PH_Wall_Left", room, new Vector3(-halfWidth, RoomHeight * 0.5f, centreZ), new Vector3(WallThickness, RoomHeight, depth), plaster);
            CreateBlock("PH_Wall_Right", room, new Vector3(halfWidth, RoomHeight * 0.5f, centreZ), new Vector3(WallThickness, RoomHeight, depth), plaster);

            // Front wall: two sides and a lintel around the entrance.
            float sideWidth = (RoomWidth - DoorWidth) * 0.5f;
            float sideX = (DoorWidth + sideWidth) * 0.5f;
            CreateBlock("PH_Wall_Front_Left", room, new Vector3(-sideX, RoomHeight * 0.5f, FrontZ), new Vector3(sideWidth, RoomHeight, WallThickness), plaster);
            CreateBlock("PH_Wall_Front_Right", room, new Vector3(sideX, RoomHeight * 0.5f, FrontZ), new Vector3(sideWidth, RoomHeight, WallThickness), plaster);
            float lintelHeight = RoomHeight - DoorHeight;
            CreateBlock("PH_Wall_Front_Lintel", room, new Vector3(0f, DoorHeight + lintelHeight * 0.5f, FrontZ), new Vector3(DoorWidth, lintelHeight, WallThickness), plaster);

            // Door frame.
            float postX = DoorWidth * 0.5f + 0.05f;
            CreateBlock("PH_DoorFrame_Left", room, new Vector3(-postX, DoorHeight * 0.5f, FrontZ), new Vector3(0.1f, DoorHeight, 0.3f), darkWood);
            CreateBlock("PH_DoorFrame_Right", room, new Vector3(postX, DoorHeight * 0.5f, FrontZ), new Vector3(0.1f, DoorHeight, 0.3f), darkWood);
            CreateBlock("PH_DoorFrame_Top", room, new Vector3(0f, DoorHeight + 0.04f, FrontZ), new Vector3(DoorWidth + 0.2f, 0.08f, 0.3f), darkWood);

            // Skirting along the walls makes the room read as a real interior.
            CreateBlock("PH_Skirting_Back", room, new Vector3(0f, 0.08f, BackZ + 0.12f), new Vector3(RoomWidth, 0.16f, 0.04f), darkWood);
            CreateBlock("PH_Skirting_Left", room, new Vector3(-halfWidth + 0.12f, 0.08f, centreZ), new Vector3(0.04f, 0.16f, depth), darkWood);
            CreateBlock("PH_Skirting_Right", room, new Vector3(halfWidth - 0.12f, 0.08f, centreZ), new Vector3(0.04f, 0.16f, depth), darkWood);
        }

        // ------------------------------------------------------------------ Counter

        private static void BuildCounter()
        {
            var counter = new GameObject("Counter");
            counter.transform.position = new Vector3(0f, 0f, 1f);
            counter.AddComponent<CounterInteractable>();
            Transform t = counter.transform;

            CreateBlock("PH_Counter_Base", t, new Vector3(0f, 0.475f, 0f), new Vector3(3.2f, 0.95f, 0.7f), wood);
            CreateBlock("PH_Counter_Top", t, new Vector3(0f, 0.975f, 0f), new Vector3(3.4f, 0.05f, 0.85f), darkWood);
            CreateBlock("PH_Counter_Trim", t, new Vector3(0f, 0.1f, 0.36f), new Vector3(3.2f, 0.2f, 0.04f), darkWood);

            // Props on the counter: ledger, bell and a set of scales.
            CreateBlock("PH_Ledger", t, new Vector3(0.7f, 1.02f, -0.1f), new Vector3(0.38f, 0.05f, 0.27f), leather);
            CreateShape(PrimitiveType.Cylinder, "PH_Bell_Base", t, new Vector3(-0.9f, 1.01f, 0.05f), new Vector3(0.14f, 0.02f, 0.14f), brass);
            CreateShape(PrimitiveType.Sphere, "PH_Bell_Dome", t, new Vector3(-0.9f, 1.05f, 0.05f), new Vector3(0.1f, 0.08f, 0.1f), brass);
            CreateShape(PrimitiveType.Cylinder, "PH_Scales_Post", t, new Vector3(-0.2f, 1.15f, -0.15f), new Vector3(0.03f, 0.15f, 0.03f), brass);
            CreateBlock("PH_Scales_Beam", t, new Vector3(-0.2f, 1.3f, -0.15f), new Vector3(0.45f, 0.02f, 0.02f), brass);
            CreateShape(PrimitiveType.Cylinder, "PH_Scales_PanL", t, new Vector3(-0.42f, 1.2f, -0.15f), new Vector3(0.14f, 0.01f, 0.14f), brass);
            CreateShape(PrimitiveType.Cylinder, "PH_Scales_PanR", t, new Vector3(0.02f, 1.2f, -0.15f), new Vector3(0.14f, 0.01f, 0.14f), brass);
        }

        // ------------------------------------------------------------------ Shelves

        private static void BuildBackShelves()
        {
            var shelves = new GameObject("Shelves").transform;
            BuildShelfUnit("ShelfUnit_Left", shelves, new Vector3(-2f, 0f, BackZ + 0.35f));
            BuildShelfUnit("ShelfUnit_Right", shelves, new Vector3(2f, 0f, BackZ + 0.35f));
        }

        private static void BuildShelfUnit(string name, Transform parent, Vector3 position)
        {
            var unit = new GameObject(name).transform;
            unit.SetParent(parent, false);
            unit.localPosition = position;

            const float width = 2.4f;
            const float height = 2.3f;
            const float depth = 0.45f;

            CreateBlock("PH_Shelf_Back", unit, new Vector3(0f, height * 0.5f, -depth * 0.5f), new Vector3(width, height, 0.03f), darkWood);
            CreateBlock("PH_Shelf_SideL", unit, new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.05f, height, depth), wood);
            CreateBlock("PH_Shelf_SideR", unit, new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.05f, height, depth), wood);

            float[] boardHeights = { 0.15f, 0.75f, 1.35f, 1.95f };
            for (int i = 0; i < boardHeights.Length; i++)
            {
                CreateBlock($"PH_Shelf_Board{i}", unit, new Vector3(0f, boardHeights[i], 0f), new Vector3(width, 0.04f, depth), wood);
            }

            // Assorted placeholder merchandise, different shapes so the shelves don't look empty.
            CreateShape(PrimitiveType.Cylinder, "PH_Item_Jar", unit, new Vector3(-0.8f, 0.92f, 0f), new Vector3(0.18f, 0.15f, 0.18f), glass);
            CreateShape(PrimitiveType.Sphere, "PH_Item_Orb", unit, new Vector3(-0.3f, 0.86f, 0f), new Vector3(0.2f, 0.2f, 0.2f), glass);
            CreateBlock("PH_Item_Chest", unit, new Vector3(0.45f, 0.88f, 0f), new Vector3(0.4f, 0.22f, 0.28f), darkWood);
            CreateShape(PrimitiveType.Capsule, "PH_Item_Idol", unit, new Vector3(-0.5f, 1.52f, 0f), new Vector3(0.12f, 0.15f, 0.12f), brass);
            CreateBlock("PH_Item_Books", unit, new Vector3(0.3f, 1.5f, 0f), new Vector3(0.5f, 0.26f, 0.22f), leather);
            CreateShape(PrimitiveType.Cylinder, "PH_Item_Candle", unit, new Vector3(0.85f, 2.07f, 0f), new Vector3(0.08f, 0.1f, 0.08f), plaster);
            CreateShape(PrimitiveType.Sphere, "PH_Item_Gem", unit, new Vector3(-0.85f, 2.03f, 0f), new Vector3(0.1f, 0.1f, 0.1f), portalCore);
        }

        // ------------------------------------------------------------------ Display table

        private static void BuildDisplayTable()
        {
            var table = new GameObject("DisplayTable").transform;
            table.position = new Vector3(-2.6f, 0f, 2.8f);

            CreateBlock("PH_Table_Base", table, new Vector3(0f, 0.38f, 0f), new Vector3(1.2f, 0.76f, 0.6f), darkWood);
            CreateBlock("PH_Table_Top", table, new Vector3(0f, 0.78f, 0f), new Vector3(1.4f, 0.05f, 0.8f), wood);
            CreateBlock("PH_Table_GlassCase", table, new Vector3(0f, 0.98f, 0f), new Vector3(1.2f, 0.35f, 0.6f), glass);
        }

        // ------------------------------------------------------------------ Portal device

        private static void BuildPortalDevice()
        {
            // Gameplay arrives in Milestone 5; for now it is scenery.
            var portal = new GameObject("PortalDevice").transform;
            portal.position = new Vector3(3.3f, 0f, 0.2f);

            CreateBlock("PH_Portal_Plinth", portal, new Vector3(0f, 0.06f, 0f), new Vector3(0.7f, 0.12f, 1.8f), darkWood);
            CreateBlock("PH_Portal_PostA", portal, new Vector3(0f, 1.3f, -0.72f), new Vector3(0.14f, 2.4f, 0.14f), brass);
            CreateBlock("PH_Portal_PostB", portal, new Vector3(0f, 1.3f, 0.72f), new Vector3(0.14f, 2.4f, 0.14f), brass);
            CreateBlock("PH_Portal_Top", portal, new Vector3(0f, 2.5f, 0f), new Vector3(0.16f, 0.16f, 1.6f), brass);
            CreateBlock("PH_Portal_Core", portal, new Vector3(0.02f, 1.3f, 0f), new Vector3(0.04f, 2.2f, 1.3f), portalCore);
        }

        // ------------------------------------------------------------------ Customer markers

        private static void BuildCustomerPoints()
        {
            // Empty markers the customer system (Milestone 2) walks between.
            var points = new GameObject("CustomerPoints").transform;
            CreateMarker("CustomerEntrance", points, new Vector3(0f, 0f, FrontZ + 0.5f));
            CreateMarker("CustomerCounterSpot", points, new Vector3(0f, 0f, 1.8f));
            CreateMarker("CustomerExit", points, new Vector3(0f, 0f, FrontZ + 0.5f));
        }

        // ------------------------------------------------------------------ Player

        private static void BuildPlayer()
        {
            var playerRig = new GameObject("PlayerRig");
            playerRig.transform.position = new Vector3(0f, 0f, -0.3f);
            playerRig.AddComponent<MobileInputController>();
            playerRig.AddComponent<CameraController>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerRig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f); // eye height

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            camera.fieldOfView = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.035f); // the dark "void" outside the door

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<InteractionController>();
        }

        private static void BuildGameplayObjects()
        {
            new GameObject("ShopUI").AddComponent<ShopHUD>();
            new GameObject("ShopSceneController").AddComponent<ShopSceneController>();
        }

        // ------------------------------------------------------------------ Helpers

        private static GameObject CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            return CreateShape(PrimitiveType.Cube, name, parent, localPosition, scale, material);
        }

        private static GameObject CreateShape(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            return shape;
        }

        private static void CreateMarker(string name, Transform parent, Vector3 localPosition)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
        }
    }
}
