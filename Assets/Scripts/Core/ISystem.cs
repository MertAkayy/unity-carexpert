namespace Core
{
    /// <summary>
    /// Base interface for all game systems.
    /// Systems implementing this interface can be registered with ServiceLocator
    /// for dependency injection and lifecycle management.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Priority for initialization order. Lower values initialize first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Called when the system is registered with ServiceLocator.
        /// Use for initial setup that doesn't depend on other systems.
        /// </summary>
        void OnRegistered();

        /// <summary>
        /// Called after all systems are registered.
        /// Use for setup that depends on other systems.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when the system is being removed or game is shutting down.
        /// Use for cleanup and saving data.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// Base interface for systems that need MonoBehaviour capabilities.
    /// </summary>
    public interface IMonoSystem : ISystem
    {
        /// <summary>
        /// The MonoBehaviour component hosting this system.
        /// </summary>
        UnityEngine.MonoBehaviour Component { get; }
    }
}
