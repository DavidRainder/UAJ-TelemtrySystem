using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace TelemetrySystem
{
    public class JsonSerializer : ISerializer
    {
        bool firstEvent = true;

        public string StartingContent()
        {
            return "{\n\"events\": [\n";
        }

        public string FinalContent()
        {
            return "\n]\n}";
        }

        public string Serialize(TrackerEvent e)
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
    }
}