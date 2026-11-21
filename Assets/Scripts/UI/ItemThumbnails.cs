using System.Collections.Generic;
using BlackMarket.Art;
using UnityEngine;

namespace BlackMarket.UI
{
    /// <summary>
    /// Renders item icons from the real 3D models (so the inventory shows the same objects you see on
    /// the counter, not generic clip art). Each model is rendered once, off-screen, and cached.
    /// </summary>
    public static class ItemThumbnails
    {
        private const int Size = 256;

        // An unnamed layer, used only by the thumbnail camera so nothing else is drawn.
        private const int Layer = 30;
        private static readonly Vector3 StudioPosition = new Vector3(0f, -200f, 0f);
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private static Camera studioCamera;
        private static Light keyLight;
        private static RenderTexture target;

        public static Texture2D Get(string modelId, bool flawed = false)
        {
            string key = modelId + (flawed ? "#f" : string.Empty);
            if (Cache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            Texture2D texture = Render(modelId, flawed);
            Cache[key] = texture;
            return texture;
        }

        private static void EnsureStudio()
        {
            if (studioCamera != null) return;

            var root = new GameObject("[ThumbnailStudio]");
            Object.DontDestroyOnLoad(root);
            root.transform.position = StudioPosition;

            studioCamera = new GameObject("Camera").AddComponent<Camera>();
            studioCamera.transform.SetParent(root.transform, false);
            studioCamera.enabled = false;
            studioCamera.clearFlags = CameraClearFlags.SolidColor;
            studioCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            studioCamera.cullingMask = 1 << Layer;
            studioCamera.fieldOfView = 26f;
            studioCamera.nearClipPlane = 0.05f;
            studioCamera.farClipPlane = 10f;

            keyLight = new GameObject("Key").AddComponent<Light>();
            keyLight.transform.SetParent(root.transform, false);
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.9f, 0.78f);
            keyLight.intensity = 1.25f;
            keyLight.cullingMask = 1 << Layer;
            keyLight.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            target = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
            studioCamera.targetTexture = target;
        }

        private static Texture2D Render(string modelId, bool flawed)
        {
            EnsureStudio();

            GameObject model = ArtLibrary.Current.CreateItemObject(modelId, flawed, null, "ThumbModel");
            model.transform.position = StudioPosition;
            model.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            foreach (Transform t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer = Layer;

            Bounds b = ArtLibrary.RenderBounds(model);
            float radius = Mathf.Max(b.extents.magnitude, 0.05f);
            float distance = radius / Mathf.Sin(studioCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            Vector3 direction = Quaternion.Euler(22f, 0f, 0f) * Vector3.back;
            studioCamera.transform.position = b.center + direction * distance;
            studioCamera.transform.LookAt(b.center);

            // Ambient light comes from the scene; the key light gives the toon shading its shape.
            RenderTexture previous = RenderTexture.active;
            studioCamera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Thumb_" + modelId };
            texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            texture.Apply(false, true);
            RenderTexture.active = previous;

            Object.Destroy(model);
            return texture;
        }
    }
}
