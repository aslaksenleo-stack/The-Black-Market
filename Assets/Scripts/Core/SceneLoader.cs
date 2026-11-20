using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlackMarket.Core
{
    /// <summary>
    /// Loads scenes asynchronously and refuses to start a second load while one is running
    /// (prevents double-tapping the travel button from loading two scenes).
    ///
    /// It needs a MonoBehaviour only to run its coroutine; the GameManager provides one.
    /// </summary>
    public sealed class SceneLoader
    {
        private readonly MonoBehaviour coroutineHost;

        public SceneLoader(MonoBehaviour coroutineHost)
        {
            this.coroutineHost = coroutineHost != null
                ? coroutineHost
                : throw new ArgumentNullException(nameof(coroutineHost));
        }

        public bool IsLoading { get; private set; }

        public event Action<string> LoadStarted;
        public event Action<string> LoadCompleted;

        /// <summary>Starts loading a scene. Returns false if the request was refused.</summary>
        public bool LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignored request to load '{sceneName}': another scene is still loading.");
                return false;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is empty.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings. " +
                               "Run 'The Black Market > Setup > 1. Create Milestone 0 Scenes' or add it via File > Build Profiles.");
                return false;
            }

            coroutineHost.StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            LoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                Debug.LogError($"[SceneLoader] Unity could not start loading '{sceneName}'.");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            LoadCompleted?.Invoke(sceneName);
        }
    }
}
