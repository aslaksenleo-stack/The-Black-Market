using UnityEngine;
using UnityEngine.Rendering;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Applies the shop's ambient light and fog when the scene starts. Done at runtime (not only stored in
    /// the scene) because the cloud build never bakes lighting, and without baked data Unity's ambient
    /// light can come out black. Warm from above, dark below, violet haze toward the street.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopAtmosphere : MonoBehaviour
    {
        public Color Sky = new Color(0.36f, 0.28f, 0.30f);
        public Color Equator = new Color(0.24f, 0.18f, 0.17f);
        public Color Ground = new Color(0.10f, 0.075f, 0.07f);
        public Color FogColor = new Color(0.13f, 0.09f, 0.13f);
        public float FogStart = 7f;
        public float FogEnd = 26f;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Sky;
            RenderSettings.ambientEquatorColor = Equator;
            RenderSettings.ambientGroundColor = Ground;

            // Build the ambient probe directly so it never depends on baked lighting.
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(Equator.linear);
            sh.AddDirectionalLight(Vector3.up, (Sky.linear - Equator.linear) * 1.2f, 1f);
            sh.AddDirectionalLight(Vector3.down, (Ground.linear - Equator.linear) * 1.2f, 1f);
            RenderSettings.ambientProbe = sh;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
        }
    }
}
