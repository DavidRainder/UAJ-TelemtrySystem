using System.Collections.Generic;
using UnityEngine;
using Utils;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;
using System;
using System.Xml;
using System.Collections;

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

        enum SerializationFormat { JSON, XML }
        [SerializeField] SerializationFormat _outputFormat = SerializationFormat.JSON;

        enum PersistenceType { File, Server}
        [SerializeField] PersistenceType _saveFormat = PersistenceType.File;

        #region Parameters
        [SerializeField] float timeToDumpQueue;
        string filePath = "";
        [SerializeField] string serverPath = "http://localhost:5000"; //Este path es de ejemplo, habria que cambiarlo al link del servidor
        #endregion

        #region Private Variables
        private Mutex mutEvents;
        private Mutex mutPersistentEvents;

        bool destroyed = false;
        bool killDumpEvents = false;
        bool killPersistentEvents = false;

        EventRegistry _eventRegistry = null;
        private IPersistence persistenceObject;

        Queue<TrackerEvent> events;
        // el Long es el tiempo en POSIX en el que se debe ejecutar
        PriorityQueue<TrackerPersistentEvent, long> persistentEvents;
        #endregion

        private void Start()
        {
            filePath = Application.persistentDataPath + "/";
            events = new Queue<TrackerEvent>();
            persistentEvents = new PriorityQueue<TrackerPersistentEvent, long>();
            _eventRegistry = GetComponent<EventRegistry>();


            //if(_eventRegistry == null)
            // error handling de ^^

            switch (_saveFormat)
            {
                case PersistenceType.File:
                    switch (_outputFormat)
                    {
                        case SerializationFormat.JSON:
                            persistenceObject = new FilePersistence(filePath, new JsonSerializer());
                            break;
                        case SerializationFormat.XML:
                            persistenceObject = new FilePersistence(filePath, new XMLSerializer());
                            break;

                    }
                    break;

                case PersistenceType.Server:
                    persistenceObject = new ServerPersistence(serverPath, new JsonSerializer());
                    break;

            }
            mutEvents = new Mutex();
            mutPersistentEvents = new Mutex();

            Parallel.Invoke(DumpEvents, PersistentEventTracking);

            PushEvent(new GameStartEvent());
            PushEvent(new PlayerDeathEvent(new Vector2(0,0)));
            TrackPersistentEvent(new PlayerPositionEvent(this.transform, 1000));
        }

        private async void DumpEvents()
        {
            while (!destroyed && !killDumpEvents)
            {
                await Task.Delay((int)(timeToDumpQueue * 1000));

                mutEvents.WaitOne();
                persistenceObject.Flush(ref events);
                mutEvents.ReleaseMutex();
            }

            persistenceObject.EndFlush();
        }
        private void OnDestroy()
        {
            destroyed = true;
            killDumpEvents = true;
            killPersistentEvents = true;
        }

        TrackerPersistentEvent _currentPersistentEvent;
        long _currentPriority;

        async void PersistentEventTracking()
        {
            bool empty = !persistentEvents.TryPeek(out TrackerPersistentEvent _, out long firstPrio);
            
            if (empty)
                return;
            
            long currentTimeStamp = firstPrio;
            while(!destroyed && !killPersistentEvents)
            {
                // mutex
                mutPersistentEvents.WaitOne();

                // pillas un evento
                empty = !persistentEvents.TryPeek(out _currentPersistentEvent, out _currentPriority);
                
                // unlock
                mutPersistentEvents.ReleaseMutex();

                if (empty) 
                    return;

                // Debug.Log("Waiting for: " + (priority - currentTimeStamp).ToString() + "ms.");
                
                await Task.Run(async () => { await Task.Delay((int)(_currentPriority - currentTimeStamp)); });

                // Este trozo de c�digo comprueba si el evento, por alguna raz�n, ha sido destru�do durante la espera.
                // Solo va a ser destru�do en caso de que llegue un "StopTrackingPersistentEvent".
                if (_currentPersistentEvent != null)
                {
                    currentTimeStamp = _currentPriority;

                    _currentPersistentEvent.UpdateTimeStamp();

                    _currentPersistentEvent.GetDataCallback();

                    // mutex
                    mutEvents.WaitOne();

                    PushEvent(_currentPersistentEvent);

                    // unlock
                    mutEvents.ReleaseMutex();

                    // mutex
                    mutPersistentEvents.WaitOne();

                    persistentEvents.Dequeue();

                    persistentEvents.Enqueue(_currentPersistentEvent, _currentPersistentEvent.AdvanceTimer());

                    _currentPersistentEvent = null;

                    // unlock
                    mutPersistentEvents.ReleaseMutex();
                }
            }
        }

        public void StopTrackingPersistentEvent(string eventID)
        {
            mutPersistentEvents.WaitOne();

            int i = 0;
            int n = persistentEvents.Count;
             
            Queue<TrackerPersistentEvent> temporalQ = new Queue<TrackerPersistentEvent>();
            Queue<long> temporalPrioritiesQ = new Queue<long>();

            while(i < n && persistentEvents.Count > 0)
            {
                persistentEvents.TryDequeue(out TrackerPersistentEvent myEvent, out long priority);
                if (myEvent.GetID() != eventID)
                {
                    temporalQ.Enqueue(myEvent);
                    temporalPrioritiesQ.Enqueue(priority);
                }

                ++i;
            }

            while (temporalPrioritiesQ.Count > 0)
            {
                persistentEvents.Enqueue(temporalQ.Dequeue(), temporalPrioritiesQ.Dequeue());
            }

            if (_currentPersistentEvent != null && _currentPersistentEvent.GetID() == eventID)
            {
                _currentPersistentEvent = null;
            }

            mutPersistentEvents.ReleaseMutex();
        }

        public void PushEvent(TrackerEvent e)
        {
            // Si el evento esta activo...
            if (!_eventRegistry.IsEventActive(e.GetID())) return;

            // LOCK MUTEX
            mutEvents.WaitOne();
            
            events.Enqueue(e);

            // UNLOCK MUTEX
            mutEvents.ReleaseMutex();

            Debug.Log("Event Pushed");
        }

        public void TrackPersistentEvent(TrackerPersistentEvent e)
        {
            // Si esta activo... 
            if (!_eventRegistry.IsEventActive(e.GetID())) return;

            e.UpdatePersistentTime();

            // lo metemos en la cola
            // LOCK MUTEX
            mutPersistentEvents.WaitOne();

            bool wasEmpty = persistentEvents.Count == 0;

            persistentEvents.Enqueue(e, e._currentPersistentTime);

            // UNLOCK MUTEX
            mutPersistentEvents.ReleaseMutex();

            if(wasEmpty)
            {
                Parallel.Invoke(PersistentEventTracking);
            }
        }
    }
}
