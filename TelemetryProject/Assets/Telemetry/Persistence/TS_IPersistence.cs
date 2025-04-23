using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetrySystem
{
    /// <summary>
    /// Interfaz de sistemas de persistencia
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Se encarga del volcado de la cola
        /// a donde se necesite (archivo, servidor, etc.)
        /// </summary>
        /// <param name="eQueue"> Referencia a la cola de eventos</param>
        void Flush(ref Queue<TrackerEvent> eQueue);

        /// <summary>
        /// Encargado de cerrar el volcado a�adiendo
        /// Deber�a llamarse a este m�todo solo al finalizar
        /// el trackeo de los eventos.
        /// </summary>
        void EndFlush();
    }
}