using System;

public class GameEndEvent : TelemetrySystem.TrackerEvent
{
    public GameEndEvent() : base(DateTimeOffset.UtcNow) { }
    public override string GetID() => "GameEnd";
}

