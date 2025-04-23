public class LevelUnpauseEvent : TrackerLevelEvent
{
    public LevelUnpauseEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelUnpause";
}
