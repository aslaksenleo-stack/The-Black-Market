using System.Collections.Generic;
using System.IO;
using BlackMarket.Art;
using BlackMarket.Audio;
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
    /// Builds the Shop scene from code: the stylised shop interior and street (from ShopEnvironment),
    /// its textures and materials, lights, the player rig behind the counter, and the gameplay objects.
    ///
    /// Everything visual is generated (meshes, textures, sounds), so the cloud build produces the
    /// whole game without hand-made assets. Generated assets go to Assets/Art/Generated and are
    /// recreated on every build.
    /// </summary>
    public static class ShopSceneBuilder
    {
        public const string GeneratedFolder = "Assets/Art/Generated";
        public const string ToonShaderName = "BlackMarket/Toon";
        public const string GlassShaderName = "BlackMarket/Glass";
        private const string TitleFontPath = "Assets/Resources/Fonts/AlegreyaSC-Black.ttf";

        private const float EyeHeight = 1.62f;

        private static readonly Dictionary<EnvMaterial, Material> Materials = new Dictionary<EnvMaterial, Material>();
        private static Material characterMaterial;

        public static void Build(string scenePath)
        {
            PrepareFolders();
            CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProjectSetup.AddGeneratedMarker();

            ShopLayout layout = ShopEnvironment.Build();
            var atmosphere = new GameObject("Atmosphere").AddComponent<ShopAtmosphere>();
            atmosphere.Apply();

            BuildEnvironment(layout);
            BuildLights(layout);
            BuildLabels(layout);
            Camera camera = BuildPlayer();
            BuildGameplayObjects(layout);

            camera.backgroundColor = Palette.Hex(0x221C3E);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Assets

        private static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.DeleteAsset(GeneratedFolder);
            }

            Directory.CreateDirectory(GeneratedFolder + "/Meshes");
            Directory.CreateDirectory(GeneratedFolder + "/Textures");
            Directory.CreateDirectory(GeneratedFolder + "/Materials");
            AssetDatabase.Refresh();
        }

        /// <summary>True when the game's shaders exist and compiled without errors.</summary>
        public static bool ShadersValid(out string problem)
        {
            foreach (string name in new[] { ToonShaderName, GlassShaderName })
            {
                Shader shader = Shader.Find(name);
                if (shader == null)
                {
                    problem = $"Shader '{name}' not found (Assets/Shaders).";
                    return false;
                }

                if (ShaderUtil.ShaderHasError(shader))
                {
                    problem = $"Shader '{name}' has compile errors. See the Console / build log.";
                    return false;
                }
            }

            problem = null;
            return true;
        }

        private static void CreateMaterials()
        {
            Materials.Clear();
            if (!ShadersValid(out string problem))
            {
                Debug.LogError("[ShopSceneBuilder] " + problem);
            }

            Shader toon = Shader.Find(ToonShaderName) ?? Shader.Find("Standard");
            Shader glass = Shader.Find(GlassShaderName) ?? Shader.Find("Standard");

            Materials[EnvMaterial.Props] = SaveMaterial("M_Props", toon, null, Vector2.one);
            Materials[EnvMaterial.Wood] = SaveMaterial("M_Wood", toon, SaveTexture("wood"), Vector2.one);
            Materials[EnvMaterial.Planks] = SaveMaterial("M_Planks", toon, SaveTexture("planks"), new Vector2(0.8f, 0.8f));
            Materials[EnvMaterial.Brick] = SaveMaterial("M_Brick", toon, SaveTexture("brick"), Vector2.one);
            Materials[EnvMaterial.Plaster] = SaveMaterial("M_Plaster", toon, SaveTexture("plaster"), new Vector2(0.5f, 0.5f));
            Materials[EnvMaterial.Cobble] = SaveMaterial("M_Cobble", toon, SaveTexture("cobble"), new Vector2(0.5f, 0.5f));
            Materials[EnvMaterial.Cloth] = SaveMaterial("M_Cloth", toon, SaveTexture("cloth"), new Vector2(2f, 2f));
            Materials[EnvMaterial.Glass] = SaveMaterial("M_Glass", glass, null, Vector2.one);

            characterMaterial = SaveMaterial("M_Character", toon, null, Vector2.one);
            characterMaterial.SetColor("_RimColor", new Color(1f, 0.82f, 0.6f, 0.22f));
            characterMaterial.SetFloat("_RampSmooth", 0.06f);
            EditorUtility.SetDirty(characterMaterial);
        }

        private static Texture2D SaveTexture(string id)
        {
            const int size = TexturePainter.DefaultSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "T_" + id,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2
            };
            texture.LoadRawTextureData(TexturePainter.Paint(id, size));
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, $"{GeneratedFolder}/Textures/T_{id}.asset");
            return texture;
        }

        private static Material SaveMaterial(string name, Shader shader, Texture2D texture, Vector2 tiling)
        {
            var material = new Material(shader) { name = name };
            if (texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = tiling;
            }

            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, $"{GeneratedFolder}/Materials/{name}.mat");
            return material;
        }

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            mesh.name = name;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedFolder}/Meshes/{name}.asset");
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ------------------------------------------------------------------ Scene content

        private static void BuildEnvironment(ShopLayout layout)
        {
            var root = new GameObject("Environment").transform;
            foreach (EnvPiece piece in layout.Pieces)
            {
                var go = new GameObject(piece.Name);
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(piece.Position, Quaternion.Euler(piece.Euler));
                go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(piece.Mesh, piece.Name);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = Materials[piece.Material];
                renderer.shadowCastingMode = piece.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (piece.IsStatic)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                }

                switch (piece.Tag)
                {
                    case "Bell":
                        AddCounterInteractable(go, piece.Mesh, "Ring the bell");
                        break;
                    case "Ledger":
                        AddCounterInteractable(go, piece.Mesh, "Open the ledger");
                        break;
                    case "Door":
                        go.AddComponent<ShopDoor>();
                        break;
                }
            }
        }

        private static void AddCounterInteractable(GameObject go, Mesh mesh, string prompt)
        {
            go.name = go.name.Split('_')[0];
            var collider = go.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = Vector3.Max(mesh.bounds.size * 1.4f, new Vector3(0.18f, 0.18f, 0.18f));
            go.AddComponent<CounterInteractable>().Configure(prompt);
        }

        private static void BuildLights(ShopLayout layout)
        {
            var root = new GameObject("Lighting").transform;
            foreach (EnvLight l in layout.Lights)
            {
                var go = new GameObject(l.Name);
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(l.Position, Quaternion.Euler(l.Euler));
                var light = go.AddComponent<Light>();
                light.type = l.Directional ? LightType.Directional : LightType.Point;
                light.color = l.Color;
                light.intensity = l.Intensity;
                if (!l.Directional) light.range = l.Range;
                SetRealtime(light);
                light.renderMode = l.Important ? LightRenderMode.ForcePixel : LightRenderMode.Auto;
                light.shadows = l.Shadows ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = 0.55f;
            }
        }

        // Lights must be realtime: the cloud build never bakes lighting, and a "Baked" light without
        // baked data lights nothing. (Serialized value 4 = Realtime.)
        private static void SetRealtime(Light light)
        {
            var so = new SerializedObject(light);
            SerializedProperty mode = so.FindProperty("m_Lightmapping");
            if (mode != null)
            {
                mode.intValue = 4;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildLabels(ShopLayout layout)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            foreach (EnvLabel label in layout.Labels)
            {
                var go = new GameObject("Label");
                go.transform.SetPositionAndRotation(label.Position, Quaternion.Euler(label.Euler));
                var text = go.AddComponent<TextMesh>();
                text.text = label.Text;
                text.fontSize = 96;
                text.characterSize = 0.01f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.lineSpacing = 0.9f;
                text.color = label.Color;
                if (font != null)
                {
                    text.font = font;
                    go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }

                go.AddComponent<TextFitter>().BoxSize = new Vector2(0.78f, label.BoxHeight);
            }
        }

        private static Camera BuildPlayer()
        {
            var playerRig = new GameObject("PlayerRig");
            playerRig.transform.position = Vector3.zero;
            playerRig.AddComponent<MobileInputController>();
            var cameraController = playerRig.AddComponent<CameraController>();
            var so = new SerializedObject(cameraController);
            so.FindProperty("startPitch").floatValue = 10f;
            so.FindProperty("maxPitch").floatValue = 62f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerRig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<InteractionController>();
            cameraObject.AddComponent<ItemInspector>();
            cameraObject.AddComponent<SoundKit>();
            return camera;
        }

        private static void BuildGameplayObjects(ShopLayout layout)
        {
            var art = new GameObject("ArtLibrary").AddComponent<ArtLibrary>();
            art.Assign(Materials[EnvMaterial.Props], characterMaterial, Materials[EnvMaterial.Glass]);

            var anchors = new GameObject("ShopAnchors").AddComponent<ShopAnchors>();
            anchors.CustomerStart = layout.Anchors["CustomerStart"];
            anchors.DoorOutside = layout.Anchors["DoorOutside"];
            anchors.DoorInside = layout.Anchors["DoorInside"];
            anchors.CustomerCounter = layout.Anchors["CustomerCounter"];
            anchors.ItemSpot = layout.Anchors["ItemSpot"];
            anchors.StockDrop = layout.Anchors["StockDrop"];

            new GameObject("ShopUI").AddComponent<ShopHUD>();
            new GameObject("VisitDirector").AddComponent<VisitDirector>();
            new GameObject("ShopSceneController").AddComponent<ShopSceneController>();
        }
    }
}
