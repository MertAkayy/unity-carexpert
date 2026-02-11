using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    private static Dictionary<string, UnityEvent> eventDictionary = new();

    public static void StartListening(string eventName, UnityAction listener)
    {
        if (!eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent = new UnityEvent();
            eventDictionary[eventName] = thisEvent;
        }
        thisEvent.AddListener(listener);
    }

    public static void StopListening(string eventName, UnityAction listener)
    {
        if (eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }

    public static void TriggerEvent(string eventName)
    {
        if (eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent.Invoke();
        }
    }
}