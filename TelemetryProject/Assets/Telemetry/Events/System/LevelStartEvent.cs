public class LevelStartEvent : TrackerLevelEvent
{
    public LevelStartEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelStart";
}
