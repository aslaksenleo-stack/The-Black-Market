using BlackMarket.Economy;
using BlackMarket.Save;
using BlackMarket.Trading;
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

        /// <summary>The shop's trading state (coins, stock, upgrades, clock). Lives as long as the game.</summary>
        public ShopGame Shop { get; private set; }

        private SaveStore saves;

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
            Shop = LoadOrCreateShop();
            Shop.Checkpoint += SaveGame;

            ServiceRegistry.Register(State);
            ServiceRegistry.Register(Scenes);
            ServiceRegistry.Register(Shop);

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
            if (Shop != null) Shop.Checkpoint -= SaveGame;
            if (State != null)
            {
                State.StateChanged -= LogStateChange;
            }

            ServiceRegistry.Clear();
            current = null;
        }

        // ------------------------------------------------------------------ Saving

        private ShopGame LoadOrCreateShop()
        {
            saves = new SaveStore(Application.persistentDataPath, d => JsonUtility.ToJson(d), JsonUtility.FromJson<SaveData>);
            var random = new SeededRandom(System.Environment.TickCount);

            SaveData data = saves.Read(out string note);
            if (note != null) Debug.LogWarning("[GameManager] " + note);
            if (data == null)
            {
                Debug.Log("[GameManager] No save found: starting a new shop.");
                return new ShopGame(random);
            }

            Debug.Log($"[GameManager] Loaded save: day {data.day}, {data.coins} coins, {data.items.Count} items.");
            return ShopGame.FromSave(data, random);
        }

        private void SaveGame()
        {
            if (Shop == null || saves == null) return;
            if (!saves.Write(Shop.CreateSave(), out string error))
            {
                Debug.LogError("[GameManager] Save failed: " + error);
            }
        }

        // Phones often kill apps in the background without warning, so save whenever the app is paused.
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
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
