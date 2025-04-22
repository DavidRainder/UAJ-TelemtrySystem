
using System;
using System.Numerics;
using System.Security.Authentication.ExtendedProtection;
using System.Xml;
using TelemetrySystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace TelemetrySystem
{
    public abstract class Event
    {
        protected long _timeStamp;
        public long TimeStamp { get { return _timeStamp; } }

        public Event(DateTimeOffset time)
        {
            _timeStamp = time.ToUnixTimeMilliseconds();
        }
        public abstract string GetID();

        public virtual string ToJSON()
        {
            return $"\"event_type\": \"{GetID()}\", \"time_stamp\": \"{TimeStamp.ToString()}\"";
        }

        public virtual void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
        {
            myEvent = doc.CreateElement(GetID());
            eventsNode.AppendChild(myEvent);

            XmlAttribute timeStamp = doc.CreateAttribute("timestamp");
            timeStamp.Value = TimeStamp.ToString();
            myEvent.Attributes.Append(timeStamp);
        }
    }

    public abstract class PersistentEvent : Event
    {
        // en ms
        /// <summary>
        /// Tiempo que pasa hasta que el evento ocurre.
        /// </summary>
        public readonly int PersistentTime;
        /// <summary>
        /// El tiempo que lleva acumulado
        /// Se ha sucedido 1 vez, será PersistentTime. 
        /// Si se sucedido 2 veces, será 2*PersistenTime, etc.
        /// </summary>
        public long _currentPersistentTime;

        public PersistentEvent(int persistencyTime) : base(DateTimeOffset.UtcNow)
        {
            PersistentTime = persistencyTime;
            _currentPersistentTime = DateTimeOffset.UtcNow.AddMilliseconds(persistencyTime).ToUnixTimeMilliseconds();
        }
        public abstract void GetDataCallback();

        public void UpdatePersistentTime()
        {
            _currentPersistentTime = DateTimeOffset.UtcNow.AddMilliseconds(PersistentTime).ToUnixTimeMilliseconds();
        }

        public void UpdateTimeStamp()
        {
            _timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public long AdvanceTimer()
        {
            _currentPersistentTime += PersistentTime;
            return _currentPersistentTime;
        }
    }
}

#region SYSTEM_EVENTS
public class GameStartEvent : TelemetrySystem.Event
{
    public GameStartEvent() : base(DateTimeOffset.UtcNow) { }
    public override string GetID() => "GameStart";
}

public class GameEndEvent : TelemetrySystem.Event
{
    public GameEndEvent() : base(DateTimeOffset.UtcNow) { }
    public override string GetID() => "GameEnd";
}

public class ChangeSceneEvent : TelemetrySystem.Event
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

    public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute oldScene = doc.CreateAttribute("old_scene");
        oldScene.Value = _oldScene;
        myEvent.Attributes.Append(oldScene);
        XmlAttribute newScene = doc.CreateAttribute("new_scene");
        newScene.Value = _newScene;
        myEvent.Attributes.Append(newScene);
    }
}

public abstract class LevelEvent : TelemetrySystem.Event
{
    public string levelName;
    public LevelEvent(string _levelName) : base(DateTimeOffset.UtcNow)
    {
        levelName = _levelName;
    }
    public override string ToJSON()
    {
        return base.ToJSON() +
            $", \"level_name\": \"{levelName}\"";
    }

    public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute levelName = doc.CreateAttribute("level_name");
        levelName.Value = this.levelName;
        myEvent.Attributes.Append(levelName);
    }
}

public class LevelStartEvent : LevelEvent
{
    public LevelStartEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelStart";
}

public class LevelEndEvent : LevelEvent
{
    public LevelEndEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelEnd";
}

public class LevelPauseEvent : LevelEvent
{
    public LevelPauseEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelPause";
}

public class LevelUnpauseEvent : LevelEvent
{
    public LevelUnpauseEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelUnpause";
}

public class LevelRestartEvent : LevelEvent
{
    public LevelRestartEvent(string _levelName) : base(_levelName) { }

    public override string GetID() => "LevelRestart";
}
#endregion

#region PROPIOS
public class PlayerDeathEvent : TelemetrySystem.Event
{
    public UnityEngine.Vector2 position;
    public PlayerDeathEvent(UnityEngine.Vector2 pos) : base(DateTimeOffset.UtcNow)
    {
        position = pos;
    }
    public override string GetID() => "PlayerDeath";
    public override string ToJSON()
    {
        return base.ToJSON() +
            $", \"death_position\": {{\"x\":\"{position.x}\",\"y\":\"{position.y}\"}}";
    }

    public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute playerPosition = doc.CreateAttribute("player_position");
        playerPosition.Value = $"X: {position.x}, Y: {position.y}";
        myEvent.Attributes.Append(playerPosition);
    }
}

public class InteractionEvent : TelemetrySystem.Event
{
    public string objectName;
    public bool success;
    public InteractionEvent(string obj, bool correct) : base(DateTimeOffset.UtcNow)
    {
        objectName = obj;
        success = correct;
    }
    public override string GetID() => "Interaction";
    public override string ToJSON()
    {
        return base.ToJSON() +
            $", \"object\": \"{objectName}\", \"success\":\"{success}\"";
    }

    public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute myObject = doc.CreateAttribute("object_name");
        myObject.Value = objectName;
        myEvent.Attributes.Append(myObject);

        XmlAttribute mySuccess = doc.CreateAttribute("success");
        mySuccess.Value = success.ToString();
        myEvent.Attributes.Append(mySuccess);
    }
}

public class PlayerPositionEvent : TelemetrySystem.PersistentEvent
{
    public UnityEngine.Vector2 position;
    private Transform playerTransform;
    public PlayerPositionEvent(Transform player, int persistencyTime) : base(persistencyTime)
    {
        playerTransform = player;
    }
    public override string GetID() => "PlayerPosition";

    public override void GetDataCallback()
    {
        position = playerTransform.position;
    }
    public override string ToJSON()
    {
        return base.ToJSON() +
            $", \"position\": {{\"x\":\"{position.x}\",\"y\":\"{position.y}\"}}";
    }

    public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
    {
        base.ToXML(doc, eventsNode, out myEvent);

        XmlAttribute playerPosition = doc.CreateAttribute("player_position");
        playerPosition.Value = $"X: {position.x}, Y: {position.y}";
        myEvent.Attributes.Append(playerPosition);
    }
}
#endregion
