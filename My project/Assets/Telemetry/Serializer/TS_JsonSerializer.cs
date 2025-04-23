namespace TelemetrySystem
{
    /// <summary>
    /// Serializador JSON de eventos
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        // Debemos comprobar si el evento actual es el primero o no,
        // ya que el formato JSON debe incluir una coma entre 
        // objetos dentro de un array, pero el primer objeto no debe tenerla
        bool firstEvent = true;

        /// <summary>
        /// Contenido incial del archivo JSON
        /// </summary>
        /// <returns> '{\n\"events\": [\n' </events></returns>
        public string StartingContent()
        {
            return "{\n\"events\": [\n";
        }

        /// <summary>
        /// Contenido final del archivo JSON
        /// </summary>
        /// <returns> ']\n}' </events></returns>
        public string FinalContent()
        {
            return "]\n}";
        }

        /// <summary>
        /// Serializador en JSON del evento recibido
        /// </summary>
        /// <param name="e"> Evento a serializar </param>
        /// <returns> String que representa un objeto JSON con los atributos del evento</returns>
        public string Serialize(TrackerEvent e)
        {
            string content = "";

            if (firstEvent)
            {
                content += "{";
                firstEvent = false;
            }
            // añadimos una ',' en caso de no ser el primer evento
            else content += ",{";

            // serializamos según el evento nos diga
            content += e.ToJSON();
            content += "}\n";

            return content;
        }

        public string FileExtension() => ".json";
    }
}