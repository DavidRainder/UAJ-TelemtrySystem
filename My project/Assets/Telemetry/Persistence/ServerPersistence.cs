using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TelemetrySystem;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Networking;

namespace TelemetrySystem
{
    public class ServerPersistence : IPersistence
    {
        string serverPath = "http://localhost:5000"; //Este path es de ejemplo, habria que cambiarlo al link del servidor
        ISerializer serializer;
        

        public ServerPersistence(string _serverPath, ISerializer serializer)
        {
            serverPath = _serverPath;
            this.serializer = serializer;
            
        }

        public void EndFlush()
        {
            //No es necesario implementar al no tener que acabar ning�n archivo
        }

        public void Flush(ref Queue<TrackerEvent> eQueue)
        {
            string content = "";
            while (eQueue.Count > 0)
            {
                content += serializer.Serialize(eQueue.Dequeue());
            }

            Upload(content);

        }

       
        void Upload(string content)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(serverPath);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            //Env�o de trazas al servidor
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = content;

                streamWriter.Write(json);
                streamWriter.Close();
            }

            ////Respuesta del servidor --> Deber�a haber un error handler, ejemplo b�sico de nuestro sistema de persistencia el env�o de 
            ///trazas al servidor. Al no haber servidor no se puede probar apropiadamente
            //var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //{
            //    var result = streamReader.ReadToEnd();
            //}
        }
    }
}