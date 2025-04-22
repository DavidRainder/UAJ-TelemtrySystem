using System; 
using System.Collections.Generic;
using UnityEngine;

namespace TelemetrySystem
{
public class EventRegistry : MonoBehaviour
{
    [Serializable]
    public struct EventActivation
    {
        public string name;
        public bool activation;
    }

    [SerializeField]
    List<EventActivation> eventNames = new List<EventActivation>();

    private HashSet<string> eventsRegistry = new HashSet<string>();
    public bool IsEventActive(string eventID)
    {
        return eventsRegistry.Contains(eventID);
    }

    private void Awake()
    {
        foreach (var eventName in eventNames) {
            if(eventName.activation)
                eventsRegistry.Add(eventName.name);    
        }

        Debug.Log($"Eventos activos registrados: " + eventsRegistry.Count);
    }

}

}
