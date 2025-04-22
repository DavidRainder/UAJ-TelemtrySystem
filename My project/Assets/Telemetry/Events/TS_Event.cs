using System;
using System.Xml;

namespace TelemetrySystem
{
    /// <summary>
    /// Evento básico del sistema de telemetría
    /// </summary>
    public abstract class TrackerEvent
    {
        /// <summary>
        /// Tiempo del evento en el que ha sucedido.
        /// En formato POSIX
        /// </summary>
        protected long timeStamp;

        /// <summary>
        /// Tiempo del evento en el que ha sucedido.
        /// En formato POSIX
        /// </summary>
        public long TimeStamp { get { return timeStamp; } }

        /// <summary>
        /// Constructora que recibe el tiempo
        /// </summary>
        /// <param name="time"> Tiempo de cuándo ha sucedido el evento </param>
        public TrackerEvent(DateTimeOffset time)
        {
            // se convierte a tiempo POSIX
            timeStamp = time.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Método abstracto que devolverá la ID específica de cada evento
        /// </summary>
        /// <returns> La ID del evento </returns>
        public abstract string GetID();

        /// <summary>
        /// Método serializador a JSON
        /// </summary>
        /// <returns> El contenido del evento en JSON </returns>
        public virtual string ToJSON()
        {
            return $"\"event_type\": \"{GetID()}\", \"time_stamp\": \"{TimeStamp.ToString()}\"";
        }

        /// <summary>
        /// Método serializador a XML
        /// </summary>
        /// <param name="doc"> Documento XML </param>
        /// <param name="eventsNode"> Nodo XML del que colgará este evento </param>
        /// <param name="myEvent"> parámetro de salido myEvent al que se le podrán añadir más atributos </param>
        /// <returns> El contenido del evento en XML </returns>
        public virtual string ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
        {
            myEvent = doc.CreateElement(GetID());
            eventsNode.AppendChild(myEvent);

            XmlAttribute timeStamp = doc.CreateAttribute("timestamp");
            timeStamp.Value = TimeStamp.ToString();
            myEvent.Attributes.Append(timeStamp);
            return myEvent.OuterXml;
        }
    }

    /// <summary>
    /// Evente "persistente" del sistema de telemetría
    /// 
    /// Este evento se registrará en el Tracker y se añadirá a
    /// la cola de eventos cada X milisegundos (definidos por el usuario).
    /// </summary>
    public abstract class TrackerPersistentEvent : TrackerEvent
    {
        /// <summary>
        /// Tiempo en milisegundos que pasa hasta que el evento ocurre.
        /// </summary>
        public readonly int PersistentTime;

        /// <summary>
        /// El tiempo que lleva acumulado
        /// Se ha sucedido 1 vez, será PersistentTime. 
        /// Si se sucedido 2 veces, será 2*PersistenTime, etc.
        /// </summary>
        public long _currentPersistentTime;

        /// <summary>
        /// Constructora
        /// </summary>
        /// <param name="persistencyTime"> Periodo de tiempo tras el cual se registrará el evento </param>
        public TrackerPersistentEvent(int persistencyTime) : base(DateTimeOffset.UtcNow)
        {
            PersistentTime = persistencyTime;
            // el tiempo en el que se debe registrar
            _currentPersistentTime = DateTimeOffset.UtcNow.AddMilliseconds(persistencyTime).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Método abstracto con el que se podrá recoger los 
        /// datos necesarios para el evento
        /// en el momento de su registro
        /// </summary>
        public abstract void GetDataCallback();

        /// <summary>
        /// Actualización del tiempo de registro
        /// </summary>
        public void UpdatePersistentTime()
        {
            _currentPersistentTime = DateTimeOffset.UtcNow.AddMilliseconds(PersistentTime).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Actualización de tiempo en el que ha registrado
        /// </summary>
        public void UpdateTimeStamp()
        {
            timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Avanzamos el tiempo en el que se debe registrar
        /// </summary>
        /// <returns></returns>
        public long AdvanceTimer()
        {
            _currentPersistentTime += PersistentTime;
            return _currentPersistentTime;
        }
    }
}
