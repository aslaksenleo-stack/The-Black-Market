using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackMarket.Core;
using BlackMarket.Player;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlackMarket.EditorTools
{
    /// <summary>
    /// Project setup steps that never show a dialog, so they can run both from the menu
    /// (ProjectSetupMenu) and on a cloud build machine where nobody can click anything (CloudBuild).
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenesFolder = "Assets/Scenes";
        public const string ProductName = "The Black Market";
        public const string FallbackPackageName = "com.theblackmarket.prototype";

        public static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        // ------------------------------------------------------------------ Scenes

        /// <summary>
        /// Creates the Milestone 0 scenes. With overwrite = false, existing scenes are kept
        /// untouched (so later hand-made changes are never thrown away by a build).
        /// </summary>
        public static void CreateMilestone0Scenes(bool overwrite)
        {
            Directory.CreateDirectory(ScenesFolder);

            string bootstrapPath = ScenePath(SceneNames.Bootstrap);
            string shopPath = ScenePath(SceneNames.Shop);

            if (overwrite || !File.Exists(bootstrapPath))
            {
                CreateBootstrapScene(bootstrapPath);
                Debug.Log($"[Setup] Created {bootstrapPath}");
            }

            if (overwrite || !File.Exists(shopPath))
            {
                CreateShopScene(shopPath);
                Debug.Log($"[Setup] Created {shopPath}");
            }

            AddScenesToBuildSettings(bootstrapPath, shopPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateBootstrapScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapper = new GameObject("Bootstrapper");
            bootstrapper.AddComponent<Bootstrapper>();

            // Renders black for the moment before the Shop loads, instead of "No cameras rendering".
            var cameraObject = new GameObject("BootCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateShopScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting (placeholder; real shop lighting comes later).
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Layout: the player stands at z = 0 facing +z. The counter is in front of them,
            // customers come from the door at z = +4. Shelves will go on the back wall behind the player.
            // "PH_" means placeholder: these grey blocks get replaced by real models later.
            var environment = new GameObject("Environment").transform;
            CreatePlaceholder("PH_Floor", environment, new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f));
            CreatePlaceholder("PH_Counter", environment, new Vector3(0f, 0.5f, 1f), new Vector3(3f, 1f, 0.6f));
            CreatePlaceholder("PH_BackWall", environment, new Vector3(0f, 1.5f, -1.5f), new Vector3(8f, 3f, 0.2f));
            CreatePlaceholder("PH_FrontWall_Left", environment, new Vector3(-2.5f, 1.5f, 4f), new Vector3(3f, 3f, 0.2f));
            CreatePlaceholder("PH_FrontWall_Right", environment, new Vector3(2.5f, 1.5f, 4f), new Vector3(3f, 3f, 0.2f));

            // Empty marker objects the customer system (Milestone 2) will walk between.
            var customerPoints = new GameObject("CustomerPoints").transform;
            CreateMarker("CustomerEntrance", customerPoints, new Vector3(0f, 0f, 4.5f));
            CreateMarker("CustomerCounterSpot", customerPoints, new Vector3(0f, 0f, 1.8f));
            CreateMarker("CustomerExit", customerPoints, new Vector3(0f, 0f, 4.5f));

            // Player: stationary behind the counter.
            var playerRig = new GameObject("PlayerRig");
            playerRig.AddComponent<MobileInputController>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerRig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f); // eye height
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreatePlaceholder(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
        }

        private static void CreateMarker(string name, Transform parent, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
        }

        /// <summary>Puts the given scenes first in Build Settings, in order, and disables any other scene.</summary>
        private static void AddScenesToBuildSettings(params string[] orderedPaths)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in orderedPaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!orderedPaths.Contains(existing.path))
                {
                    scenes.Add(new EditorBuildSettingsScene(existing.path, false));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ Android

        /// <summary>Android player settings. Does NOT switch the build target.</summary>
        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.productName = ProductName;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP); // required for 64-bit
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;     // Google Play requirement

            // First-person game: landscape only, either way up.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            string packageName = PlayerSettings.GetApplicationIdentifier(android);
            if (string.IsNullOrEmpty(packageName) || packageName.Contains("DefaultCompany"))
            {
                PlayerSettings.SetApplicationIdentifier(android, FallbackPackageName);
                Debug.Log($"[Setup] Android package name set to {FallbackPackageName}. Change it before publishing.");
            }

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Validation

        /// <summary>Returns a list of problems. Empty list = valid.</summary>
        public static List<string> Validate(bool requireAndroidTarget)
        {
            var problems = new List<string>();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            foreach (string sceneName in SceneNames.Milestone0Scenes)
            {
                string path = ScenePath(sceneName);
                if (!File.Exists(path))
                {
                    problems.Add($"Missing scene file: {path}");
                }
                else if (!buildScenes.Any(s => s.path == path && s.enabled))
                {
                    problems.Add($"Scene not enabled in Build Settings: {path}");
                }
            }

            EditorBuildSettingsScene firstEnabled = buildScenes.FirstOrDefault(s => s.enabled);
            if (firstEnabled == null || firstEnabled.path != ScenePath(SceneNames.Bootstrap))
            {
                problems.Add("Bootstrap must be the FIRST enabled scene in Build Settings.");
            }

            if (requireAndroidTarget && EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                problems.Add("Active build target is not Android.");
            }

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                problems.Add("Android scripting backend is not IL2CPP.");
            }

            return problems;
        }
    }
}
