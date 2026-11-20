using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlackMarket.Core
{
    /// <summary>
    /// The composition root: the single persistent object that creates the game's core systems
    /// and registers them in the ServiceRegistry. It deliberately contains no gameplay logic.
    ///
    /// It creates itself automatically before the first scene loads, so you can press Play in
    /// ANY scene (for example Shop) and the core systems will still exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        // Used only to stop duplicates. Other code should use ServiceRegistry, not this field.
        private static GameManager current;

        public GameStateManager State { get; private set; }
        public SceneLoader Scenes { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (current != null)
            {
                return;
            }

            var gameObject = new GameObject("[GameManager]");
            gameObject.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            DontDestroyOnLoad(gameObject);

            ServiceRegistry.Clear();

            State = new GameStateManager(GameState.Booting);
            Scenes = new SceneLoader(this);

            ServiceRegistry.Register(State);
            ServiceRegistry.Register(Scenes);

            State.StateChanged += LogStateChange;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (current != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (State != null)
            {
                State.StateChanged -= LogStateChange;
            }

            ServiceRegistry.Clear();
            current = null;
        }

        // Keeps GameState in sync with whichever scene is open.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!SceneNames.TryGetStateForScene(scene.name, out GameState target) || State.Current == target)
            {
                return;
            }

            if (!State.TryChangeState(target))
            {
                // Not fatal, but means some code changed scene without going through the
                // correct flow (for example skipping the Traveling state).
                Debug.LogWarning($"[GameManager] Scene '{scene.name}' loaded, but changing state " +
                                 $"{State.Current} -> {target} is not allowed.");
            }
        }

        private static void LogStateChange(GameState previous, GameState next)
        {
            Debug.Log($"[GameManager] State: {previous} -> {next}");
        }
    }
}
