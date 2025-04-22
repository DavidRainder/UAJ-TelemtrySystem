using System;

public class GameStartEvent : TelemetrySystem.TrackerEvent
{
    public GameStartEvent() : base(DateTimeOffset.UtcNow) { }
    public override string GetID() => "GameStart";
}
