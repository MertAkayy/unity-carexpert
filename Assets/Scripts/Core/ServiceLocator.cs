using System;
using System.Collections.Generic;

// Updated: Added recursion guards for InitializeAll() and ShutdownAll()

namespace Core
{
    /// <summary>
    /// Static service locator for dependency injection.
    /// Provides global access to game systems while maintaining loose coupling.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, ISystem> _systems = new Dictionary<Type, ISystem>();
        private static readonly List<ISystem> _orderedSystems = new List<ISystem>();
        private static bool _isInitialized = false;

        /// <summary>
        /// Whether all systems have been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Event fired when a system is registered.
        /// </summary>
        public static event Action<ISystem> OnSystemRegistered;

        /// <summary>
        /// Event fired when all systems are initialized.
        /// </summary>
        public static event Action OnAllSystemsInitialized;

        /// <summary>
        /// Registers a system with the service locator.
        /// </summary>
        /// <typeparam name="T">The system interface type</typeparam>
        /// <param name="system">The system instance</param>
        public static void Register<T>(T system) where T : ISystem
        {
            Type type = typeof(T);

            if (_systems.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"[ServiceLocator] System of type {type.Name} is already registered. Replacing...");
                _systems[type] = system;
            }
            else
            {
                _systems.Add(type, system);
                _orderedSystems.Add(system);
                _orderedSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            system.OnRegistered();
            OnSystemRegistered?.Invoke(system);

            UnityEngine.Debug.Log($"[ServiceLocator] Registered system: {type.Name}");
        }

        /// <summary>
        /// Unregisters a system from the service locator.
        /// </summary>
        /// <typeparam name="T">The system interface type</typeparam>
        public static void Unregister<T>() where T : ISystem
        {
            Type type = typeof(T);

            if (_systems.TryGetValue(type, out ISystem system))
            {
                system.Shutdown();
                _systems.Remove(type);
                _orderedSystems.Remove(system);
                UnityEngine.Debug.Log($"[ServiceLocator] Unregistered system: {type.Name}");
            }
        }

        /// <summary>
        /// Gets a registered system by type.
        /// </summary>
        /// <typeparam name="T">The system interface type</typeparam>
        /// <returns>The system instance or null if not found</returns>
        public static T Get<T>() where T : ISystem
        {
            Type type = typeof(T);

            if (_systems.TryGetValue(type, out ISystem system))
            {
                return (T)system;
            }

            UnityEngine.Debug.LogWarning($"[ServiceLocator] System of type {type.Name} not found.");
            return default;
        }

        /// <summary>
        /// Tries to get a registered system by type.
        /// </summary>
        /// <typeparam name="T">The system interface type</typeparam>
        /// <param name="system">The system instance if found</param>
        /// <returns>True if the system was found</returns>
        public static bool TryGet<T>(out T system) where T : ISystem
        {
            Type type = typeof(T);

            if (_systems.TryGetValue(type, out ISystem baseSystem))
            {
                system = (T)baseSystem;
                return true;
            }

            system = default;
            return false;
        }

        /// <summary>
        /// Checks if a system is registered.
        /// </summary>
        /// <typeparam name="T">The system interface type</typeparam>
        /// <returns>True if the system is registered</returns>
        public static bool IsRegistered<T>() where T : ISystem
        {
            return _systems.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Initializes all registered systems in priority order.
        /// Call this after all systems are registered.
        /// </summary>
        public static void InitializeAll()
        {
            if (_isInitialized)
            {
                // Silently return - this is expected if systems call InitializeAll() during their own Initialize()
                return;
            }

            // Set flag BEFORE the loop to prevent recursive calls from systems' Initialize() methods
            _isInitialized = true;

            UnityEngine.Debug.Log($"[ServiceLocator] Initializing {_orderedSystems.Count} systems...");

            foreach (var system in _orderedSystems)
            {
                try
                {
                    system.Initialize();
                    UnityEngine.Debug.Log($"[ServiceLocator] Initialized: {system.GetType().Name}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ServiceLocator] Failed to initialize {system.GetType().Name}: {ex.Message}");
                }
            }

            OnAllSystemsInitialized?.Invoke();
            UnityEngine.Debug.Log("[ServiceLocator] All systems initialized.");
        }

        private static bool _isShuttingDown = false;

        /// <summary>
        /// Shuts down all systems in reverse priority order.
        /// Call this when the game is closing.
        /// </summary>
        public static void ShutdownAll()
        {
            if (_isShuttingDown)
            {
                // Silently return - this is expected if systems call ShutdownAll() during their own Shutdown()
                return;
            }

            _isShuttingDown = true;

            UnityEngine.Debug.Log("[ServiceLocator] Shutting down all systems...");

            // Shutdown in reverse order
            for (int i = _orderedSystems.Count - 1; i >= 0; i--)
            {
                try
                {
                    _orderedSystems[i].Shutdown();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ServiceLocator] Failed to shutdown {_orderedSystems[i].GetType().Name}: {ex.Message}");
                }
            }

            _systems.Clear();
            _orderedSystems.Clear();
            _isInitialized = false;
            _isShuttingDown = false;

            UnityEngine.Debug.Log("[ServiceLocator] All systems shut down.");
        }

        /// <summary>
        /// Clears all registered systems without calling shutdown.
        /// Use only for testing or emergency cleanup.
        /// </summary>
        public static void Clear()
        {
            _systems.Clear();
            _orderedSystems.Clear();
            _isInitialized = false;
            _isShuttingDown = false;
        }

        /// <summary>
        /// Gets all registered systems.
        /// </summary>
        public static IReadOnlyList<ISystem> GetAllSystems()
        {
            return _orderedSystems.AsReadOnly();
        }
    }
}
