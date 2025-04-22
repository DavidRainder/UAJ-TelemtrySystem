using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetrySystem
{
    public interface IPersistence
    {
        void Flush(ref Queue<TrackerEvent> eQueue);
        void EndFlush();
    }
}

