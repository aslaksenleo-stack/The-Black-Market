using System.Collections.Generic;
using System.IO;
using BlackMarket.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlackMarket.EditorTools
{
    /// <summary>
    /// "The Black Market > Setup" menu for when you have the Unity Editor open.
    /// The real work is in ProjectSetup; this class only adds dialogs around it.
    /// Cloud builds run the same steps automatically (see CloudBuild).
    /// </summary>
    public static class ProjectSetupMenu
    {
        private const string MenuRoot = "The Black Market/Setup/";

        [MenuItem(MenuRoot + "1. Create Scenes", priority = 1)]
        public static void CreateMilestone0Scenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool anyExist = File.Exists(ProjectSetup.ScenePath(SceneNames.Bootstrap)) ||
                            File.Exists(ProjectSetup.ScenePath(SceneNames.Shop));

            if (anyExist && !EditorUtility.DisplayDialog(
                    "Scenes already exist",
                    "Bootstrap and/or Shop scenes already exist. Overwrite them with freshly generated versions?\n\n" +
                    "Any changes you made to those scenes will be lost.",
                    "Overwrite", "Cancel"))
            {
                return;
            }

            ProjectSetup.CreateGeneratedScenes(forceOverwrite: true);
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath(SceneNames.Bootstrap));
        }

        [MenuItem(MenuRoot + "2. Configure Android Settings", priority = 2)]
        public static void ConfigureAndroid()
        {
            ProjectSetup.ConfigureAndroidPlayerSettings();

            bool onAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                             EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            if (!onAndroid)
            {
                EditorUtility.DisplayDialog("Android Build Support missing",
                    "Unity could not switch to Android. Open Unity Hub > Installs > (your Unity version) > Add modules, " +
                    "tick 'Android Build Support' (with OpenJDK and Android SDK & NDK Tools), install, then run this again.",
                    "OK");
                return;
            }

            Debug.Log("[Setup] Android configured: IL2CPP, ARM64, landscape, product name set.");
        }

        [MenuItem(MenuRoot + "3. Validate Milestone 0 Setup", priority = 3)]
        public static void ValidateSetup()
        {
            List<string> problems = ProjectSetup.Validate(requireAndroidTarget: true);

            if (problems.Count == 0)
            {
                Debug.Log("[Setup] Milestone 0 setup is valid.");
                EditorUtility.DisplayDialog("Setup valid", "Milestone 0 setup looks correct.", "OK");
                return;
            }

            string report = "- " + string.Join("\n- ", problems);
            Debug.LogWarning("[Setup] Problems found:\n" + report);
            EditorUtility.DisplayDialog("Setup problems", report, "OK");
        }
    }
}
