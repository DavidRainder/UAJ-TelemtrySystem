using System.Xml;

namespace TelemetrySystem
{
    /// <summary>
    /// Serializador XML de eventos
    /// </summary>
    public class XMLSerializer : ISerializer
    {
        // Utilizamos la implementación de Xml que tiene .NET
        // para facilitar la recogida de los datos
        XmlDocument xmlDocument = null; // documento xml
        XmlNode eventsNode = null; // nodo bajo el que todos los eventos se registrarán

        /// <summary>
        /// Constructora
        /// </summary>
        public XMLSerializer()
        {
            // Construye el documento XML
            // y el nodo base del que colgarán los eventos
            xmlDocument = new XmlDocument();
            eventsNode = xmlDocument.CreateElement("events");
            xmlDocument.AppendChild(eventsNode);
        }

        /// <summary>
        /// Serializador del evento 'e'
        /// </summary>
        /// <param name="e"> Evento a serializar </param>
        /// <returns> El contenido del evento en formato XML, con sus atributos
        /// como atributos XML </returns>
        public string Serialize(TrackerEvent e)
        {
            e.ToXML(xmlDocument, eventsNode, out XmlNode myEvent);
            return myEvent.OuterXml + "\n";
        }

        /// <summary>
        /// Contenido incial del archivo XML
        /// </summary>
        /// <returns> '<events>\n' </events></returns>
        public string StartingContent()
        {
            return "<events>\n";
        }

        /// <summary>
        /// Contenido final del archivo XML
        /// </summary>
        /// <returns> '</events>' </returns>
        public string FinalContent()
        {
            return "</events>";
        }

        public string FileExtension() => ".xml";
    }
}