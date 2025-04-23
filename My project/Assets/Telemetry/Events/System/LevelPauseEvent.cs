public class LevelPauseEvent : TrackerLevelEvent
{
    public LevelPauseEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelPause";
}
