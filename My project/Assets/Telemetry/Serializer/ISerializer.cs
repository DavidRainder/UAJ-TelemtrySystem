using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetrySystem
{
    public interface ISerializer
    {
        string StartingContent();
        string Serialize(TrackerEvent e);
        string FinalContent();
    }
}
