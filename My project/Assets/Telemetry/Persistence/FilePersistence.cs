using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TelemetrySystem;
using UnityEngine;

namespace TelemetrySystem
{
    public class FilePersistence : IPersistence
    {
        private string filePath;
        private string finalFileName;
        private ISerializer serializer;

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
            }
        }

        private void CheckPreviousFiles(string fileExtension)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(filePath);

            int numSession = 0;
            string baseFileName = "Telemetry" + DateTime.Now.ToString("-d-M-yyyy") + fileExtension;
            Debug.Log("Files under the destiny directory: ");

            foreach (FileInfo info in directoryInfo.GetFiles())
            {
                string f = info.Name.Split("_")[1];
                if (f == baseFileName)
                {
                    numSession++;
                }
            }

            finalFileName = filePath + numSession + "_" + baseFileName;
        }
        public FilePersistence(string filePath, ISerializer serializer)
        {
            this.filePath = filePath;
            this.serializer = serializer;

            switch (serializer)
            {
                case JsonSerializer:
                    CheckPreviousFiles(".json");
                    break;
                case XMLSerializer:
                    CheckPreviousFiles(".xml");
                    break;
            }
            WriteToFile(serializer.StartingContent());
        }

        public void Flush(ref Queue<TrackerEvent> eQueue)
        {
            string content = "";
            while (eQueue.Count > 0)
            {
                content += serializer.Serialize(eQueue.Dequeue());
            }
            WriteToFile(content);
        }

        public void EndFlush()
        {
            WriteToFile(serializer.FinalContent());
        }
    }
}