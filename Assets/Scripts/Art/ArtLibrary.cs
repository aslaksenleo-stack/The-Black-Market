using System.Collections.Generic;
using UnityEngine;

namespace BlackMarket.Art
{
    /// <summary>
    /// Holds the shared materials (assigned by the scene builder) and turns generated meshes into
    /// GameObjects at runtime. Referencing the materials from the scene guarantees their shaders are
    /// included in the build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtLibrary : MonoBehaviour
    {
        [SerializeField] private Material props;
        [SerializeField] private Material character;
        [SerializeField] private Material glass;

        private static ArtLibrary current;
        private static readonly Dictionary<string, ItemModel> ItemModels = new Dictionary<string, ItemModel>();

        public static ArtLibrary Current
        {
            get
            {
                if (current == null)
                {
                    current = FindFirstObjectByType<ArtLibrary>();
                    if (current == null)
                    {
                        current = new GameObject("ArtLibrary").AddComponent<ArtLibrary>();
                    }
                }

                return current;
            }
        }

        public Material Props => props != null ? props : props = CreateFallback("BlackMarket/Toon");
        public Material Character => character != null ? character : character = CreateFallback("BlackMarket/Toon");
        public Material Glass => glass != null ? glass : glass = CreateFallback("BlackMarket/Glass");

        public void Assign(Material propsMaterial, Material characterMaterial, Material glassMaterial)
        {
            props = propsMaterial;
            character = characterMaterial;
            glass = glassMaterial;
        }

        private void Awake()
        {
            if (current == null) current = this;
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }

        private static Material CreateFallback(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { name = shaderName + " (runtime)" };
        }

        // ------------------------------------------------------------------ Builders

        /// <summary>Creates a child object showing a mesh with the given material.</summary>
        public static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Builds an item's 3D model (cached per model and flaw). Returns the root object.</summary>
        public GameObject CreateItemObject(string modelId, bool flawed, Transform parent, string name = null)
        {
            string key = modelId + (flawed ? "#flawed" : string.Empty);
            if (!ItemModels.TryGetValue(key, out ItemModel model))
            {
                model = ItemModelBuilder.Build(modelId, flawed);
                ItemModels[key] = model;
            }

            var root = new GameObject(name ?? "Item_" + modelId);
            root.transform.SetParent(parent, false);
            CreateMeshObject("Solid", root.transform, model.Solid, Props);
            if (model.Glass != null)
            {
                CreateMeshObject("Glass", root.transform, model.Glass, Glass, false);
            }

            return root;
        }

        /// <summary>World-space bounds of everything rendered under root.</summary>
        public static Bounds RenderBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.1f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
