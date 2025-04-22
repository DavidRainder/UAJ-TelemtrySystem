using System.Collections;
using System.Collections.Generic;
using TelemetrySystem;
using UnityEngine;

namespace TelemetrySystem
{
    public class ServerPersistence : IPersistence
    {
        public void EndFlush()
        {
            throw new System.NotImplementedException();
        }

        public void Flush(ref Queue<TrackerEvent> eQueue)
        {
            throw new System.NotImplementedException();
        }

    }
}
