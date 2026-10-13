using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface ISession
{
    void Freeze();
    void Resume(int deviceId);
}

public class JoypadSessionDispatcher<TSession>(Node root, Func<int, TSession> createSession)
    where TSession : ISession
{
    private readonly List<TSession> _disconnectedSessions = [];
    private readonly Dictionary<int, TSession> _deviceIdSessionMap = [];
    private readonly List<TSession> _sessions = [];

    public event Action<TSession[], TSession>? SessionAdded;

    public void AssignSessions()
    {
        foreach (var deviceId in Input.GetConnectedJoypads())
        {
            AssignToNextSession((int)deviceId);
        }
    }

    public void ListenForChanges()
    {
        Input.Singleton.Connect("joy_connection_changed", root, nameof(OnJoyConnectionChanged));
    }

    private void OnJoyConnectionChanged(int deviceId, bool connected)
    {
        if (connected)
        {
            AssignToNextSession(deviceId);
        }
        else
        {
            DisconnectDevice(deviceId);
        }
    }

    private void DisconnectDevice(int deviceId)
    {
        var session = _deviceIdSessionMap[deviceId];
        _disconnectedSessions.Add(session);
        _deviceIdSessionMap.Remove(deviceId);
        session.Freeze();
    }

    private void AssignToNextSession(int deviceId)
    {
        TSession session;
        if (_disconnectedSessions.Count > 0)
        {
            session = _disconnectedSessions.Last();
            _disconnectedSessions.RemoveAt(_disconnectedSessions.Count - 1);
            session.Resume(deviceId);
        }
        else
        {
            session = createSession(deviceId);
            _sessions.Add(session);
            SessionAdded?.Invoke([.. _sessions], session);
        }
        _deviceIdSessionMap.Add(deviceId, session);
    }
}
