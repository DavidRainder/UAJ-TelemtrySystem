using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TelemetrySystem
{
    /// <summary>
    /// Sistema de persistencia por archivos
    /// </summary>
    public class FilePersistence : IPersistence
    {
        private string filePath; // Directorio
        private string finalFileName; // Path completo del archivo
        private ISerializer serializer; // Serializador de eventos

        /// <summary>
        /// Constructora
        /// </summary>
        /// <param name="directoryPath"> Directorio donde guardaremos los archivos </param>
        /// <param name="serializer"> Serializador que utilizaremos </param>
        public FilePersistence(string directoryPath, ISerializer serializer)
        {
            this.filePath = directoryPath;
            this.serializer = serializer;

            // Queremos llamar al método estático del ISerializer "FileExtension" para saber qué
            // extensión tendrá nuestro archivo
            string fileExtension = (string)(serializer.GetType()).GetMethod("FileExtension").Invoke(null, null);
            CheckPreviousFiles(fileExtension);

            Debug.Log(fileExtension);

            WriteToFile(serializer.StartingContent());
        }

        /// <summary>
        /// Método auxiliar para escritura de archivos
        /// </summary>
        /// <param name="content"> Contenido a escribir en finalFileName </param>
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
                Debug.LogError("Couldn't write to file.");
                throw e;
            }
        }

        /// <summary>
        /// Comprueba qué archivos existen ya y 'genera'
        /// el nombre del nuevo archivo a escribir
        /// para evitar que se sobreescriban archivos.
        /// </summary>
        /// <param name="fileExtension"> Extensión del archivo a comprobar </param>
        private void CheckPreviousFiles(string fileExtension)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(filePath);

            int numSession = 0;
            // generamos el nombre 'base' del archivo con la fecha y la extensión correcta
            string baseFileName = "Telemetry" + DateTime.Now.ToString("-d-M-yyyy") + fileExtension;

            // recorremos los archivos del directorio para saber qué número
            // debemos poner al principio del archivo, para evitar que se sobreescriban datos
            foreach (FileInfo info in directoryInfo.GetFiles())
            {
                string[] split = info.Name.Split('_');
                if (split[1] == baseFileName)
                {
                    numSession = int.Parse(split[0]) + 1;
                }
            }

            // genramos el nombre final del archivo destino
            finalFileName = filePath + numSession + "_" + baseFileName;
        }

        /// <summary>
        /// Volcado de eventos en archivo
        /// Este método modifica la cola de eventos, así
        /// que es posible que deba ser encerrado
        /// en un mutex para evitar carreras con la cola de eventos
        /// </summary>
        /// <param name="eQueue"> referencia a la cola de eventos que se va a volcar </param>
        public void Flush(ref Queue<TrackerEvent> eQueue)
        {
            string content = "";
            while (eQueue.Count > 0)
            {
                content += serializer.Serialize(eQueue.Dequeue());
            }
            WriteToFile(content);
        }

        /// <summary>
        /// Escribimos el final del archivo
        /// </summary>
        public void EndFlush()
        {
            WriteToFile(serializer.FinalContent());
        }
    }
}