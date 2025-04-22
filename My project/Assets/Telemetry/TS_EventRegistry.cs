using System;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetrySystem
{
    public class EventRegistry : MonoBehaviour
    {
        [Serializable]
        // Estructura que permite al usuario desde el inspector rellenar la ID de un evento
        // y si quiere que esté activada o no
        public struct EventActivation
        {
            /// <summary>
            /// ID del evento. Debe estar bien escrita, tal y como aparece en el GetID() del evento en cuestión
            /// </summary>
            public string name;

            /// <summary>
            /// Si se quiere activar el evento o no.
            /// </summary>
            public bool activation;
        }

        [SerializeField,
            Tooltip("Eventos que queremos el sistema trackee. " +
            "Para que un evento se tenga en cuenta debe aparecer en la lista con " +
            "su nombre igual al GetID() de dicho evento. " +
            "Si no aparece en la lista o está mal escrito, se asume que está desactivado.")]
        List<EventActivation> eventNames = new List<EventActivation>();

        /// <summary>
        /// Estructura utilizada para ver si se contiene o no un nombre.
        /// Se utiliza un Set por su eficiencia. 
        /// Solo se guardarán los nombres de los eventos activos, por lo que 
        /// no hace falta guardar el valor del booleano "Activation".
        /// </summary>
        private HashSet<string> eventsRegistry = new HashSet<string>();

        /// <summary>
        /// Dice si un evento está activo o no
        /// según la lista de nombres rellenada por el usuario
        /// </summary>
        /// <param name="eventID"> Evento en cuestión </param>
        /// <returns> True si está activo, Falso en caso contrario </returns>
        public bool IsEventActive(string eventID)
        {
            return eventsRegistry.Contains(eventID);
        }

        private void Awake()
        {
            // comprobamos qué eventos existen y están activos
            // y los metemos en el Set eventsRegistry
            foreach (var eventName in eventNames) {
                if(eventName.activation)
                    eventsRegistry.Add(eventName.name);    
            }
        }
    }
}
