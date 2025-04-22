using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace TelemetrySystem
{
    public class XMLSerializer : ISerializer
    {
        XmlDocument xmlDocument = null;
        XmlNode eventsNode = null;

        public string Serialize(TrackerEvent e)
        {
            return e.ToXML(xmlDocument, eventsNode, out XmlNode myEvent);
        }

        public string StartingContent()
        {
            throw new System.NotImplementedException();
        }

        public string FinalContent()
        {
            throw new System.NotImplementedException();
        }
    }
}

