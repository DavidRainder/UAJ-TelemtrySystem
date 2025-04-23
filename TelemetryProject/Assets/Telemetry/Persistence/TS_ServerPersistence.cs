using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TelemetrySystem
{
    /// <summary>
    /// Persistencia en Servidor
    /// 
    /// EJEMPLO NO TERMINADO DE CÓMO SE PODRÍA CONECTAR CON UN SERVIDOR
    /// USANDO NUESTRO SISTEMA DE TELEMETRÍA. NO DEBERÍA UTILIZARSE ESTA CLASE PORQUE LANZA EXCEPCIÓN
    /// 
    /// Al no tener un servidor a dónde enviar las trazas, hemos decidido dar un "ejemplo" de cómo
    /// podría hacerse, ya que la práctica especifica que el sistema debe ser ampliable.
    /// </summary>
    public class ServerPersistence : IPersistence
    {
        string serverPath; // url del servidor
        JsonSerializer serializer; // serializador a utilizar para el formato JSON

        /// <summary>
        /// Constructora
        /// </summary>
        /// <param name="_serverPath"> URL del servidor al que enviaremos información </param>
        /// <param name="serializer"> Serializador en formato JSON para serializar las trazas </param>
        public ServerPersistence(string _serverPath, JsonSerializer serializer)
        {
            serverPath = _serverPath;
            this.serializer = serializer;
        }

        public void EndFlush()
        {
            // No es necesario implementar al no tener que acabar ningún archivo
            // Posiblemente este método se encargaría de desconcetarse del servidor,
            // si existiera una conexión previa que se deba cerrar.
        }

        /// <summary>
        /// Volcado de eventos en la cola
        /// 
        /// Este método modifica la cola. 
        /// Es posible que se deba encerrar en un mutex
        /// para evitar carreras.
        /// </summary>
        /// <param name="eQueue"></param>
        public void Flush(ref Queue<TrackerEvent> eQueue)
        {
            string content = "";
            while (eQueue.Count > 0)
            {
                // conseguimos todo el contenido usando el serializador
                content += serializer.Serialize(eQueue.Dequeue());
            }
            try
            {
                // lo enviamos al servidor
                Upload(content);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Sending data to Server failed.");
                throw e;
            }
        }

        /// <summary>
        /// ---- NO TERMINADO ----
        /// ---- LANZA "NotImplementedException" ----
        /// 
        /// Método de ejemplo para conexión con una Web, donde mandamos
        /// las trazas al servidor y esperamos una respuesta
        /// En caso de respuesta negativa, se lanzaría excepción que se debería
        /// manejar desde el tracker
        /// 
        /// </summary>
        /// <param name="content"></param>
        void Upload(string content)
        {
            // Creamos una web request para conectarse a un servidor
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(serverPath);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            //Envío de trazas al servidor
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = content;

                streamWriter.Write(json);
                streamWriter.Close();
            }

            // Respuesta del servidor --> Debería comprobarse la respuesta del servidor
            // y manejarla debidamente. En caso de que no haya conexión a internet, por ejemplo,
            // se debe lanzar una excepción para detener el tracking de eventos.

            throw new NotImplementedException();

            // var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        }
    }
}