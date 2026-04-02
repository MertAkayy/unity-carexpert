using System;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    /// <summary>
    /// ScriptableObject-based event for decoupled communication.
    /// Create instances via Create > GameEvents menu.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "Core/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        private readonly UnityEvent _event = new UnityEvent();

        /// <summary>
        /// Subscribe to this event.
        /// </summary>
        public void Subscribe(UnityAction listener)
        {
            _event.AddListener(listener);
        }

        /// <summary>
        /// Unsubscribe from this event.
        /// </summary>
        public void Unsubscribe(UnityAction listener)
        {
            _event.RemoveListener(listener);
        }

        /// <summary>
        /// Invoke this event.
        /// </summary>
        public void Invoke()
        {
            _event.Invoke();
        }
    }

    /// <summary>
    /// Generic ScriptableObject-based event for typed data.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "Core/GameEvent (Generic)")]
    public class GameEvent<T> : ScriptableObject
    {
        private readonly UnityEvent<T> _event = new UnityEvent<T>();

        public void Subscribe(UnityAction<T> listener)
        {
            _event.AddListener(listener);
        }

        public void Unsubscribe(UnityAction<T> listener)
        {
            _event.RemoveListener(listener);
        }

        public void Invoke(T value)
        {
            _event.Invoke(value);
        }
    }

    // Note: Specific game events (InspectionStartedEvent, CustomerArrivedEvent, etc.)
    // should be defined in their respective namespaces (Inspection, Customer, etc.)
    // to avoid circular dependencies. Use the generic GameEvent<T> class directly:
    //
    // Example usage:
    //   [CreateAssetMenu(fileName = "InspectionStartedEvent", menuName = "Events/Inspection Started")]
    //   public class InspectionStartedEvent : GameEvent<Vehicle> { }

    #region Game Event Listener Component

    /// <summary>
    /// MonoBehaviour component to listen to GameEvents and respond with UnityEvents.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _gameEvent;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Subscribe(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Unsubscribe(OnEventRaised);
            }
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }

    /// <summary>
    /// Generic MonoBehaviour component to listen to typed GameEvents.
    /// </summary>
    public class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] private GameEvent<T> _gameEvent;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEnable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Subscribe(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Unsubscribe(OnEventRaised);
            }
        }

        private void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }

    #endregion
}
