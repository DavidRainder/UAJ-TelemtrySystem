using System;
using System.Xml;

public class ChangeSceneEvent : TelemetrySystem.TrackerEvent
{
    public string _oldScene;
    public string _newScene;

    public ChangeSceneEvent(string oldScene, string newScene) : base(DateTimeOffset.UtcNow)
    {
        _oldScene = oldScene;
        _newScene = newScene;
    }

    public override string GetID() => "ChangeScene";

    public override string ToJSON()
    {
        return base.ToJSON() +
            $", \"old_scene\": \"{_oldScene}\", \"new_scene\": \"{_newScene}\"";
    }

    public override string ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute oldScene = doc.CreateAttribute("old_scene");
        oldScene.Value = _oldScene;
        myEvent.Attributes.Append(oldScene);
        XmlAttribute newScene = doc.CreateAttribute("new_scene");
        newScene.Value = _newScene;
        myEvent.Attributes.Append(newScene);
        return myEvent.OuterXml;
    }
}
