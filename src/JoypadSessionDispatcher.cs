using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface ISession
{
    void Freeze();
    void Resume(int deviceId);
}

public class JoypadSessionDispatcher<TSession>(
    Node root,
    Func<int, TSession> createSession,
    bool includeKeyboardInput
)
    where TSession : ISession
{
    private readonly List<TSession> _disconnectedSessions = [];
    private readonly Dictionary<int, TSession> _deviceIdSessionMap = [];
    public readonly List<TSession> Sessions = [];
    private readonly Node _root = root;
    private readonly Func<int, TSession> _createSession = createSession;
    private readonly bool _includeKeyboardInput = includeKeyboardInput;

    public event Action<TSession[], TSession>? SessionAdded;

    public void AssignSessions()
    {
        foreach (var deviceId in Input.GetConnectedJoypads())
        {
            AssignToNextSession((int)deviceId);
        }
        if (_includeKeyboardInput)
        {
            AssignToNextSession(-1);
        }
    }

    public void ListenForChanges()
    {
        Input.Singleton.Connect("joy_connection_changed", _root, nameof(OnJoyConnectionChanged));
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
            session = _createSession(deviceId);
            Sessions.Add(session);
            SessionAdded?.Invoke([.. Sessions], session);
        }
        _deviceIdSessionMap.Add(deviceId, session);
    }
}
