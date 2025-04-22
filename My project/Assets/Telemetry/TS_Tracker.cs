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

        #region Parameters
        [SerializeField] float _timeToDumpQueue;
        [SerializeField] string _fileDestinationName = "Telemetry";
        [SerializeField] SerializationFormat _outputFormat = SerializationFormat.JSON;
        #endregion

        private void Start()
        {
            _events = new Queue<Event>();
            _persistentEvents = new PriorityQueue<PersistentEvent, long>();
            _eventRegistry = GetComponent<EventRegistry>();

            // error handling de ^^

            mutEvents = new Mutex();
            mutPersistentEvents = new Mutex();

            Parallel.Invoke(DumpEvents, PersistentEventTracking);

            PushEvent(new GameStartEvent());
        }

        private void OnDestroy()
        {
            destroyed = true;
            killDumpEvents = true;
            killPersistentEvents = true;
        }

        #region Private Variables
        private Mutex mutEvents;
        private Mutex mutPersistentEvents;

        bool destroyed = false;
        bool killDumpEvents = false;
        bool killPersistentEvents = false;

        EventRegistry _eventRegistry = null;

        Queue<Event> _events;
        // el Long es el tiempo en POSIX en el que se debe ejecutar
        PriorityQueue<PersistentEvent, long> _persistentEvents;

        int numSession = 0;
        string baseFileName = "";
        string finalFileName = "";

        // For XML Serialization
        XmlDocument xmlDocument = null;
        XmlNode eventsNode = null;
        #endregion

        private void CheckPreviousFiles(string fileExtension)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.persistentDataPath + "/");

            numSession = 0;
            baseFileName = _fileDestinationName + DateTime.Now.ToString("-d-M-yyyy") + fileExtension;
            Debug.Log("Files under the destiny directory: ");

            foreach (FileInfo info in directoryInfo.GetFiles())
            {
                string f = info.Name.Split("_")[1];
                if (f == baseFileName)
                {
                    numSession++;
                }
            }

            finalFileName = Application.persistentDataPath + "/" + numSession + "_" + baseFileName;
        }

        private void OpenAndStartXMLFile()
        {
            CheckPreviousFiles(".xml");

            xmlDocument = new XmlDocument();
            eventsNode = xmlDocument.CreateElement("events");
            xmlDocument.AppendChild(eventsNode);
        }

        private void GetXMLContentFromEvent(Event e)
        {
            e.ToXML(xmlDocument, eventsNode, out XmlNode myEvent);
        }

        private void CloseAndEndXMLFile()
        {
            if (xmlDocument != null) {
                try
                {
                    xmlDocument.Save(finalFileName);
                }
                catch (XmlException e)
                {
                    Debug.LogError($"XmlDocument couldn't be saved: {e.Message} \n" +
                    "Some events have been lost.\n" +
                    "Changing to JSON format");

                    StartCoroutine(ChangeFromXMLToJSON());
                }
            }
            else
            {
                Debug.LogError("XmlDocument couldn't be saved.\n" +
                   "Some events have been lost.\n" +
                   "Changing to JSON format");
                StartCoroutine(ChangeFromXMLToJSON());
            }
        }

        private void OpenAndStartJSONFile()
        {
            CheckPreviousFiles(".json");

            WriteToFile("{\n\"events\": [\n");
        }

        bool firstEvent = true;
        private string GetJSONContentFromEvent(Event e)
        {
            string content = "";

            if (firstEvent)
            {
                content += "{";
                firstEvent = false;
            }
            else content += ",{";

            content += e.ToJSON();
            content += "}\n";

            return content;
        }

        private void CloseAndEndJSONFile()
        {
            WriteToFile("\n]\n}");
        }

        private void WriteToFile(string content)
        {
            var encodedContent = new UTF8Encoding(true).GetBytes(content);

            try
            {
                FileStream file = File.Open(
                    finalFileName,
                    FileMode.Append);

                file.Write(encodedContent);
                file.Close();
            } 
            catch (Exception e)
            {
                Debug.LogError("Couldn't write to file. Maybe disk is full. Pleas check. Shutting Tracking System down");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Matamos el thread que est� llevando el XML y 
        /// creamos uno nuevo en formato JSON
        /// tras un tiempo igual al tiempo entre
        /// 'dumps' de la cola de eventos
        /// </summary>
        /// <returns></returns>
        private IEnumerator ChangeFromXMLToJSON()
        {
            killDumpEvents = true;
            yield return new WaitForSeconds(_timeToDumpQueue);
            killDumpEvents = false;
            _outputFormat = SerializationFormat.JSON;
            Parallel.Invoke(DumpEvents);
        }

        private void OpenDumpingFile()
        {
            switch (_outputFormat)
            {
                case SerializationFormat.JSON:
                    OpenAndStartJSONFile();
                    break;
                case SerializationFormat.XML:
                    OpenAndStartXMLFile();
                    break;
            }
        }

        private void GetDumpingContent()
        {
            switch (_outputFormat)
            {
                case SerializationFormat.JSON:

                    string content = "";
                    while (_events.Count > 0)
                    {
                        Event e = _events.Dequeue();
                        content += GetJSONContentFromEvent(e);
                    }

                    WriteToFile(content);

                    break;
                case SerializationFormat.XML:

                    while (_events.Count > 0)
                    {
                        GetXMLContentFromEvent(_events.Dequeue());
                    }

                    break;
            }
        }

        private void CloseDumpingFile() {
            switch (_outputFormat)
            {
                case SerializationFormat.JSON:
                    CloseAndEndJSONFile();
                    break;
                case SerializationFormat.XML:
                    CloseAndEndXMLFile();
                    break;
            }
        }

        async void DumpEvents()
        {
            OpenDumpingFile();

            while (!destroyed && !killDumpEvents)
            {
                await Task.Delay((int)(_timeToDumpQueue * 1000));
                
                mutEvents.WaitOne();

                GetDumpingContent();

                mutEvents.ReleaseMutex();

                Debug.Log("Events dumped");
            }

            CloseDumpingFile();
        }

        PersistentEvent _currentPersistentEvent;
        long _currentPriority;

        async void PersistentEventTracking()
        {
            bool empty = !_persistentEvents.TryPeek(out PersistentEvent _, out long firstPrio);
            
            if (empty)
                return;
            
            long currentTimeStamp = firstPrio;
            while(!destroyed && !killPersistentEvents)
            {
                // mutex
                mutPersistentEvents.WaitOne();

                // pillas un evento
                empty = !_persistentEvents.TryPeek(out _currentPersistentEvent, out _currentPriority);
                
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

                    _persistentEvents.Dequeue();

                    _persistentEvents.Enqueue(_currentPersistentEvent, _currentPersistentEvent.AdvanceTimer());

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
            int n = _persistentEvents.Count;
             
            Queue<PersistentEvent> temporalQ = new Queue<PersistentEvent>();
            Queue<long> temporalPrioritiesQ = new Queue<long>();

            while(i < n && _persistentEvents.Count > 0)
            {
                _persistentEvents.TryDequeue(out PersistentEvent myEvent, out long priority);
                if (myEvent.GetID() != eventID)
                {
                    temporalQ.Enqueue(myEvent);
                    temporalPrioritiesQ.Enqueue(priority);
                }

                ++i;
            }

            while (temporalPrioritiesQ.Count > 0)
            {
                _persistentEvents.Enqueue(temporalQ.Dequeue(), temporalPrioritiesQ.Dequeue());
            }

            if (_currentPersistentEvent != null && _currentPersistentEvent.GetID() == eventID)
            {
                _currentPersistentEvent = null;
            }

            mutPersistentEvents.ReleaseMutex();
        }

        public void PushEvent(Event e)
        {
            // Si el evento est� activo...
            if (!_eventRegistry.IsEventActive(e.GetID())) return;

            // LOCK MUTEX
            mutEvents.WaitOne();
            
            _events.Enqueue(e);

            // UNLOCK MUTEX
            mutEvents.ReleaseMutex();

            Debug.Log("Event Pushed");
        }

        public void TrackPersistentEvent(PersistentEvent e)
        {
            // Si est� activo... 
            if (!_eventRegistry.IsEventActive(e.GetID())) return;

            e.UpdatePersistentTime();

            // lo metemos en la cola
            // LOCK MUTEX
            mutPersistentEvents.WaitOne();

            bool wasEmpty = _persistentEvents.Count == 0;

            _persistentEvents.Enqueue(e, e._currentPersistentTime);

            // UNLOCK MUTEX
            mutPersistentEvents.ReleaseMutex();

            if(wasEmpty)
            {
                Parallel.Invoke(PersistentEventTracking);
            }
        }
    }
}
