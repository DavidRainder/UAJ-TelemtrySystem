using System.Collections.Generic;
using UnityEngine;
using TS_CollectionUtils;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace TelemetrySystem {

    public class Tracker : MonoBehaviour
    {
        #region Singleton
        private static Tracker _instance = null;
        public static Tracker Instance { get { return _instance; } }
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        /// <summary>
        /// Enumerado que nos indica qué formato de serialiazación utilizará el sistema.
        /// En caso de escoger XML para persistencia en Servidor, no funcionará. Se utilizará JSON siempre.
        /// </summary>
        enum SerializationFormat { JSON, XML }

        /// <summary>
        /// El tipo de persistencia que usará el TelemetrySystem.
        /// </summary>
        enum PersistenceType { File, Server}

        #region Parameters

        [SerializeField, 
            Tooltip("El tipo de persistencia que utilizará el sistema." +
            "El sistema guarda los archivos en 'Directory' en caso de escoger persistencia por 'File'." +
            "Si se ha escogido un Persistency Type de tipo 'Server', " +
            "el formato de los datos siempre será JSON.")] 
        PersistenceType persistencyType = PersistenceType.File;

        [SerializeField, 
            Tooltip("El formato de los archivos que generará el sistema con los trazas. " +
            "Si se ha escogido un Persistency Type de tipo 'Server', " +
            "el formato del archivo siempre será en JSON.")] 
        SerializationFormat _outputFormat = SerializationFormat.JSON;

        [SerializeField, 
            Tooltip("Tiempo que tardará el sistema en hacer el volcado de eventos a archivo/servidor")] 
        float timeToDumpQueue;

        [SerializeField, 
            Tooltip("Directorio donde se guardarán los archivos generados al escoger el tipo de persistencia " +
            "por archivos. Para escoger el valor por defecto (Application.persistenDataPath), ponga 'DEFAULT' como valor")]
        string directory = "DEFAULT";

        [SerializeField, 
            Tooltip("URL del servidor web al que se deberían enviar las trazas." +
            "Esta opción no se utiliza si se utiliza un Persistency Type de tipo 'File'")] 
        string serverPath = "http://localhost:5000"; //Este path es de ejemplo, habria que cambiarlo al link del servidor
        #endregion

        #region Private Variables
        // Mutex que ayudan a que las colas de TrackerEvents y de TrackerPersistentEvents
        // no sufran carreras
        private Mutex mutEvents; // usada con la variable events
        private Mutex mutPersistentEvents; // usada con la variable persistentEvents

        // Variables de control que dicen cuándo se deberían detener los procesos 
        // que se gestionan en otros hilos
        bool destroyed = false; // Variable global para controlar destruir todos los hilos
        bool killDumpEvents = false; // Destruye el hilo que realiza el volcado
        bool killPersistentEvents = false; // Destruye el hilo que realiza el tracking de eventos persistentes

        // Referencia al EventRegistry
        EventRegistry _eventRegistry = null; 

        // Objeto con el que realizaremos la persistencia
        private IPersistence persistenceObject; 

        // Cola de eventos
        Queue<TrackerEvent> events;

        // Cola de eventos persistentes
        // El Long es el tiempo en POSIX en el que se debe registrar cada evento
        PriorityQueue<TrackerPersistentEvent, long> persistentEvents;

        // Variables de qué evento persistente se está tratando actualmente
        // Son necesarias para comprobar si el evento persistente al que se está esperando
        // en el método PersistentEventTracking es borrado mediante el método
        // StopTrackingPersistentEvent. Más información en dichos métodos
        TrackerPersistentEvent _currentPersistentEvent; // evento tratado
        long _currentPriority; // prioridad del evento tratado

        #endregion

        private void Start()
        {
            // Utiliza la ruta por defecto de escritura de archivos.
            if(directory == "DEFAULT") 
                directory = Application.persistentDataPath + "/";

            // Creamos las colas de eventos
            events = new Queue<TrackerEvent>();
            persistentEvents = new PriorityQueue<TrackerPersistentEvent, long>();

            // Conseguimos la referencia al eventRegistry
            _eventRegistry = GetComponent<EventRegistry>();

            // En caso de no conseguirla, trataremos todos los eventos como activos
            if(_eventRegistry == null)
            {
                Debug.LogError("EventRegistry component was not found. All events received will be accepted as if they were Active.");
            }

            // Dependiendo de qué tipo de persistencia haya escogido el usuario, crearemos una
            // instancia diferente
            switch (persistencyType)
            {
                // Persistencia por archivos
                case PersistenceType.File:
                    // Escogemos el formato
                    switch (_outputFormat)
                    {
                        case SerializationFormat.JSON:
                            persistenceObject = new FilePersistence(directory, new JsonSerializer());
                            break;
                        case SerializationFormat.XML:
                            persistenceObject = new FilePersistence(directory, new XMLSerializer());
                            break;

                    }
                    break;
                // Persistencia por servidor
                case PersistenceType.Server:
                    persistenceObject = new ServerPersistence(serverPath, new JsonSerializer());
                    break;
            }

            // Creamos los mutex
            mutEvents = new Mutex();
            mutPersistentEvents = new Mutex();

            // Añadimos el evento GameStartEvent a la cola para que sea el primero
            PushEvent(new GameStartEvent());

            // Llamamos a dos hilos diferentes para que ejecuten el volcado y 
            // el tracking de los eventos persistentes
            Parallel.Invoke(DumpEvents, PersistentEventTracking);
        }

        /// <summary>
        /// Al destruirse el Tracker, debemos decirle a los hilos que dejen de ejecutarse.
        /// Para ello utilizamos diferentes flags que pondremos a "true"
        /// También pusheamos un último evento de final de juego y
        /// forzamos el volcado de la cola de eventos para acabar con el trackeo
        /// </summary>
        private void OnDestroy()
        {
            PushEvent(new GameEndEvent());

            // flags
            destroyed = true;
            killDumpEvents = true;
            killPersistentEvents = true;

            // forzamos volcado
            ForceFlush();
        }

        /// <summary>
        /// Forzamos el volcado de la cola de eventos
        /// y llamamos al método 'EndFlush',
        /// que añade al final del archivo
        /// lo necesario para que el formato esté correcto.
        /// Por ejemplo: 
        ///     - JSON: Añade ']}' para cerrar el archivo
        ///     - XML: Añade '</events>' para cerrar el archivo
        /// </summary>
        private void ForceFlush()
        {
            mutEvents.WaitOne();
            
            try
            {
                persistenceObject.Flush(ref events);
            }
            catch(Exception e)
            {
                Debug.LogError($"Error encountered while registering events: {e.Message} " +
                    "Telemetry System is shutting down.");
                
                mutEvents.ReleaseMutex();
                Destroy(gameObject);
            }

            mutEvents.ReleaseMutex();

            persistenceObject.EndFlush();
        }

        /// <summary>
        /// Método asíncrono que se debe ejecutar en un 
        /// hilo distinto al 'main'.
        /// 
        /// Está encargado de llamar al Persistency para
        /// volcar los eventos tras el tiempo establecido
        /// 
        /// Este método nunca "cerrará" el archivo, para ello
        /// es necesario llamar a ForceFlush. Más información en los
        /// comentarios del método.
        /// </summary>
        private async void DumpEvents()
        {
            while (!destroyed && !killDumpEvents)
            {
                mutEvents.WaitOne();
                try
                {
                    persistenceObject.Flush(ref events);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error encountered while dumping events: {e.Message} " +
                        "Telemetry System is shutting down.");

                    mutEvents.ReleaseMutex();
                    Destroy(gameObject);
                }
                mutEvents.ReleaseMutex();

                await Task.Delay((int)(timeToDumpQueue * 1000));
            }
        }

        /// <summary>
        /// Tracking de eventos persistentes
        /// Es un método asíncrono que debe ser llamado desde un
        /// hilo de ejecución diferente a 'main'.
        /// Comprueba que la cola de eventos persistentes no esté vacía y,
        /// en ese caso, espera el tiempo necesario para que llegue el momento 
        /// justo para meterlo a la cola de eventos como un evento más
        /// 
        /// Un ejemplo del comportamiento: Pongamos que tenemos 2 eventos persistentes 
        /// que se ejecutan cada 100 y 150 milisegundos. Sucedería de la siguiente forma:
        ///     1 - Se esperan 100 milisegundos para el primer evento
        ///     2 - Se mete a la cola de eventos mediante un PushEvent
        ///     3 - Se mete a la cola de eventos persistente actualizando su tiempo de ejecución (100 + 100 = 200)
        ///     4 - Se esperan 50 milisegundos para el segundo evento
        ///     5 - Se repiten los pasos 2 y 3 para este evento, consiguiendo un nuevo tiempo de 300 ms
        ///     6 - Se esperan 50 milisegundos para el primer evento
        ///     
        /// Y el bucle continúa hasta que la cola se vacíe (porque se han sacado los eventos
        ///     mediante el método StopTrackingPersistentEvents) o porque se ha destruído el Tracker.
        ///     
        /// </summary>
        async void PersistentEventTracking()
        {
            // Comprobamos si la cola está vacía
            bool empty = !persistentEvents.TryPeek(out TrackerPersistentEvent _, out long firstPrio);
            
            // Si la cola está vacía, salimos del método
            if (empty)
                return;
            
            // Establecemos el tiempo actual como el tiempo en el que se debe ejecutar el primer evento
            long currentTimeStamp = firstPrio;

            // Mientras no se haya destruído el tracker o no se quiera detener la función...
            while(!destroyed && !killPersistentEvents)
            {
                // mutex
                mutPersistentEvents.WaitOne();

                // Cogemos un evento de la cola
                empty = !persistentEvents.TryPeek(out _currentPersistentEvent, out _currentPriority);
                
                // unlock
                mutPersistentEvents.ReleaseMutex();

                // Si resulta estar vacía, por cualquiera razón, salimos del método
                if (empty) 
                    return;
                
                // Esperamos el tiempo necesario entre la prioridad del evento actual y el tiempo del último 
                // evento procesado
                await Task.Run(async () => { await Task.Delay((int)(_currentPriority - currentTimeStamp)); });

                // Comprueba si el evento, por alguna razon, ha sido destruido durante la espera.
                // Solo va a ser destruido en caso de que llegue un "StopTrackingPersistentEvent".
                // O también al haber sido destruído el objeto. 
                if (_currentPersistentEvent != null)
                {
                    // Establecemos el nuevo tiempo
                    currentTimeStamp = _currentPriority;

                    // Actualizamos el tiempo del evento persistente
                    _currentPersistentEvent.UpdateTimeStamp();

                    // Llamamos a su callback para que actualice sus datos
                    _currentPersistentEvent.GetDataCallback();

                    // mutex
                    mutEvents.WaitOne();

                    // Metemos el evento a la cola de eventos
                    PushEvent(_currentPersistentEvent);

                    // unlock
                    mutEvents.ReleaseMutex();

                    // mutex
                    mutPersistentEvents.WaitOne();

                    // Quitamos este evento de la cola de eventos persistentes
                    persistentEvents.Dequeue();
                    // Pero lo volvemos a añadir tras actualizar su prioridad
                    persistentEvents.Enqueue(_currentPersistentEvent, _currentPersistentEvent.AdvanceTimer());

                    // Decimos que ya no tenemos ningún evento siendo tratado
                    _currentPersistentEvent = null;

                    // unlock
                    mutPersistentEvents.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Método llamado para parar el tracking de un tipo de eventos 
        /// persistentes en concreto
        /// </summary>
        /// <param name="eventID"> La ID del evento a parar </param>
        public void StopTrackingPersistentEvent(string eventID)
        {
            int i = 0;
            int n = persistentEvents.Count;
             
            // Colas temporales para guardar los valores de la cola de prioridad de eventos que no deban borrarse
            Queue<TrackerPersistentEvent> temporalQ = new Queue<TrackerPersistentEvent>();
            Queue<long> temporalPrioritiesQ = new Queue<long>();

            // mutex lock
            mutPersistentEvents.WaitOne();

            // recorremos la cola de eventos persistentes
            while(i < n && persistentEvents.Count > 0)
            {
                persistentEvents.TryDequeue(out TrackerPersistentEvent myEvent, out long priority);

                // si encontramos un evento que NO tiene la ID que buscamos...
                if (myEvent.GetID() != eventID)
                {
                    // Lo guardamos en estas dos colas para volver a ponerlo al final
                    temporalQ.Enqueue(myEvent);
                    temporalPrioritiesQ.Enqueue(priority);
                }
                // en caso contrario...
                    // No necesitamos hacer nada, ya que el evento se ha sacado de la 
                    // cola tal como queríamos

                ++i;
            }

            // Volvemos a meter los eventos que no queremos borrar a la cola
            while (temporalPrioritiesQ.Count > 0)
            {
                persistentEvents.Enqueue(temporalQ.Dequeue(), temporalPrioritiesQ.Dequeue());
            }

            // Si estábamos tratando un evento de este tipo en el PersistentEventTracking,
            // lo ponemos a null, para que ese método sepa que ya no puede tratar el evento
            if (_currentPersistentEvent != null && _currentPersistentEvent.GetID() == eventID)
            {
                _currentPersistentEvent = null;
            }

            // mutex unlock
            mutPersistentEvents.ReleaseMutex();
        }

        /// <summary>
        /// Método auxiliar que comprueba si un evento está activo o no.
        /// 
        /// En caso de que EventRegistry sea nulo, se entiende que el evento está activo
        /// y que quiere trackearse.
        /// </summary>
        /// <param name="e"> Evento </param>
        /// <returns> Si está activo </returns>
        private bool IsEventActive(TrackerEvent e)
        {
            return _eventRegistry == null || _eventRegistry.IsEventActive(e.GetID());
        }

        /// <summary>
        /// Método para meter un evento a la cola.
        /// Será eventualmente volcada a un archivo o a un servidor,
        /// según el usuario haya escogido.
        /// 
        /// El evento deberá ser rellenado previamente.
        /// </summary>
        /// <param name="e"> Evento a meter en la cola </param>
        public void PushEvent(TrackerEvent e)
        {
            // Si el evento no esta activo, no lo pusheamos
            if (!IsEventActive(e)) return;

            // mutex lock
            mutEvents.WaitOne();
            
            // Pusheado a la cola
            events.Enqueue(e);

            // mutex unlock
            mutEvents.ReleaseMutex();
        }

        public void TrackPersistentEvent(TrackerPersistentEvent e)
        {
            // Si el evento no esta activo, no lo pusheamos
            if (!IsEventActive(e)) return;

            // Actualizamos el tiempo en el que debe ejecturarse
            e.UpdatePersistentTime();

            // LOCK MUTEX
            mutPersistentEvents.WaitOne();

            // Comprobamos si la cola está vacía antes de pushear el evento
            bool wasEmpty = persistentEvents.Count == 0;

            // lo metemos en la cola
            persistentEvents.Enqueue(e, e._currentPersistentTime);

            // UNLOCK MUTEX
            mutPersistentEvents.ReleaseMutex();

            // Si la cola estaba vacía, llamamos de nuevo al método de tracking persistente
            if(wasEmpty)
            {
                Parallel.Invoke(PersistentEventTracking);
            }
        }
    }
}
