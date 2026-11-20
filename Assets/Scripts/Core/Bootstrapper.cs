using System.Collections;
using UnityEngine;

namespace BlackMarket.Core
{
    /// <summary>
    /// Lives in the Bootstrap scene (the first scene in the build). The GameManager has
    /// already created the core systems by now; this simply loads the first real scene.
    /// Later, saved-game loading and the main menu will start from here.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        [Tooltip("Scene to open after start-up. Becomes MainMenu once the menu exists.")]
        [SerializeField] private string firstSceneName = SceneNames.Shop;

        private IEnumerator Start()
        {
            // Wait one frame so every system has finished its Awake.
            yield return null;

            if (!ServiceRegistry.TryGet(out SceneLoader loader))
            {
                Debug.LogError("[Bootstrapper] SceneLoader is missing. The GameManager did not start.");
                yield break;
            }

            loader.LoadScene(firstSceneName);
        }
    }
}
