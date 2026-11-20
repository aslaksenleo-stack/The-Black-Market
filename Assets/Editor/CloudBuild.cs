using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlackMarket.EditorTools
{
    /// <summary>
    /// Entry point for Unity Build Automation (Unity's cloud build service).
    ///
    /// In the Unity Cloud dashboard, set the build configuration's
    /// "Pre-export method name" to:  BlackMarket.EditorTools.CloudBuild.PreExport
    ///
    /// Before every cloud build this creates any missing scenes, puts them in Build Settings
    /// and applies the Android settings, so the build works without anyone opening the Editor.
    /// Existing scenes are never overwritten.
    /// </summary>
    public static class CloudBuild
    {
        public static void PreExport()
        {
            Debug.Log("[CloudBuild] Pre-export setup starting.");

            ProjectSetup.CreateMilestone0Scenes(overwrite: false);
            ProjectSetup.ConfigureAndroidPlayerSettings();

            // The cloud machine already targets Android, so the target is not checked here.
            List<string> problems = ProjectSetup.Validate(requireAndroidTarget: false);
            if (problems.Count > 0)
            {
                // Throwing stops the build with a clear message in the build log.
                throw new InvalidOperationException("[CloudBuild] Setup problems:\n- " + string.Join("\n- ", problems));
            }

            Debug.Log("[CloudBuild] Pre-export setup complete.");
        }
    }
}
