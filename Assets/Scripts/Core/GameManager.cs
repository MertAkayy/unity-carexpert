using System;
using UnityEngine;
using PlayerScripts;

// Updated: Fixed infinite recursion in Shutdown()

namespace Core
{
    /// <summary>
    /// Central game orchestrator.
    /// Manages game lifecycle, system initialization, and global game state.
    /// </summary>
    public class GameManager : MonoBehaviour, ISystem
    {
        #region Singleton
        public static GameManager Instance { get; private set; }
        #endregion

        #region ISystem Implementation
        public int Priority => 0; // Highest priority - initializes first

        public void OnRegistered()
        {
            UnityEngine.Debug.Log("[GameManager] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            UnityEngine.Debug.Log("[GameManager] Initializing...");
            InitializeGame();
        }

        public void Shutdown()
        {
            UnityEngine.Debug.Log("[GameManager] Shutting down...");
            PauseGame();
            // Note: Do NOT call ServiceLocator.ShutdownAll() here - it would cause infinite recursion
            // ServiceLocator.ShutdownAll() calls Shutdown() on all systems including this one
        }
        #endregion

        #region Game State
        public enum GameState
        {
            None,
            Loading,
            MainMenu,
            Playing,
            Paused,
            Inspecting,
            Dialogue,
            GameOver
        }

        public GameState CurrentState { get; private set; } = GameState.None;
        public GameState PreviousState { get; private set; } = GameState.None;

        public event Action<GameState, GameState> OnGameStateChanged;
        #endregion

        #region Configuration
        [Header("Game Configuration")]
        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private GameState _initialState = GameState.Playing;

        [Header("References")]
        [SerializeField] private Transform _vehicleSpawnPoint;

        private bool _gameInitialized = false;
        #endregion

        #region Properties
        public bool DebugMode => _debugMode;
        public Transform VehicleSpawnPoint => _vehicleSpawnPoint;
        public bool IsPaused => CurrentState == GameState.Paused;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register self with ServiceLocator
            ServiceLocator.Register<ISystem>(this);
        }

        private void Start()
        {
            if (_initializeOnStart)
            {
                InitializeGame();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<ISystem>();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (!pauseStatus && CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }
        #endregion

        #region Game Lifecycle
        /// <summary>
        /// Initializes the game and all registered systems.
        /// </summary>
        public void InitializeGame()
        {
            // Guard against double initialization (can be called from both Start() and Initialize())
            if (_gameInitialized)
            {
                return;
            }
            _gameInitialized = true;

            UnityEngine.Debug.Log("[GameManager] Initializing game...");

            // Register existing managers with ServiceLocator
            RegisterExistingManagers();

            // Initialize all registered systems
            ServiceLocator.InitializeAll();

            // Set initial state
            SetState(_initialState);

            UnityEngine.Debug.Log("[GameManager] Game initialized successfully.");
        }

        private void RegisterExistingManagers()
        {
            // Register existing singleton managers that don't implement ISystem
            // They will be wrapped or accessed via their singletons

            if (PlayerDataManager.Instance != null)
            {
                // PlayerDataManager is accessed via singleton pattern
                // We'll create a wrapper to make it available via ServiceLocator
                var playerSystem = new PlayerDataSystem(PlayerDataManager.Instance);
                ServiceLocator.Register<IPlayerDataSystem>(playerSystem);
            }

            if (TimeManager.Instance != null)
            {
                var timeSystem = new TimeSystem(TimeManager.Instance);
                ServiceLocator.Register<ITimeSystem>(timeSystem);
            }

            UnityEngine.Debug.Log("[GameManager] Registered existing managers with ServiceLocator.");
        }
        #endregion

        #region State Management
        /// <summary>
        /// Sets the game state.
        /// </summary>
        /// <param name="newState">The new state to set</param>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;

            HandleStateChange(newState);

            OnGameStateChanged?.Invoke(PreviousState, newState);

            UnityEngine.Debug.Log($"[GameManager] State changed: {PreviousState} -> {CurrentState}");
        }

        private void HandleStateChange(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;

                case GameState.Loading:
                    Time.timeScale = 0f;
                    break;

                default:
                    Time.timeScale = 1f;
                    break;
            }
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Paused)
            {
                SetState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resumes the game from pause.
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                SetState(PreviousState != GameState.Paused ? PreviousState : GameState.Playing);
            }
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Logs a debug message if debug mode is enabled.
        /// </summary>
        public void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }
        #endregion
    }

    #region System Wrappers
    /// <summary>
    /// Wrapper interface for PlayerDataManager to work with ServiceLocator.
    /// </summary>
    public interface IPlayerDataSystem : ISystem
    {
        PlayerData PlayerData { get; }
        void SaveData();
        void LoadData();
    }

    /// <summary>
    /// Wrapper for PlayerDataManager.
    /// </summary>
    public class PlayerDataSystem : IPlayerDataSystem
    {
        private readonly PlayerDataManager _manager;

        public int Priority => 10;

        public PlayerData PlayerData => _manager.playerData;

        public PlayerDataSystem(PlayerDataManager manager)
        {
            _manager = manager;
        }

        public void OnRegistered() { }
        public void Initialize() { }
        public void Shutdown() { _manager.SaveData(); }
        public void SaveData() => _manager.SaveData();
        public void LoadData()
        {
            // LoadData is private in PlayerDataManager, would need to make public or use reflection
            Debug.Log("[PlayerDataSystem] LoadData called - requires PlayerDataManager.LoadData to be public");
        }
    }

    /// <summary>
    /// Wrapper interface for TimeManager to work with ServiceLocator.
    /// </summary>
    public interface ITimeSystem : ISystem
    {
        int CurrentDay { get; }
        int CurrentHour { get; }
        int CurrentMinute { get; }
        event Action OnTimeChanged;
    }

    /// <summary>
    /// Wrapper for TimeManager.
    /// </summary>
    public class TimeSystem : ITimeSystem
    {
        private readonly TimeManager _manager;

        public int Priority => 5;

        public int CurrentDay => _manager.currentDay;
        public int CurrentHour => _manager.currentHour;
        public int CurrentMinute => _manager.currentMinute;

        public event Action OnTimeChanged;

        public TimeSystem(TimeManager manager)
        {
            _manager = manager;

            // Subscribe to time changes via EventManager
            EventManager.StartListening("OnTimeChanged", () => OnTimeChanged?.Invoke());
        }

        public void OnRegistered() { }
        public void Initialize() { }
        public void Shutdown()
        {
            EventManager.StopListening("OnTimeChanged", () => OnTimeChanged?.Invoke());
        }
    }
    #endregion
}
