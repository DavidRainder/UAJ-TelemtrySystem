
public class LevelEndEvent : TrackerLevelEvent
{
    public LevelEndEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelEnd";
}
