using System;
using Godot;

public class Root : Node
{
    public event Action<float>? Update;
    public event Action<float>? PhysicsUpdate;
    public event Action<InputEvent>? Input;

    public override void _Ready()
    {
        var joypadSessionDispatcher = new JoypadSessionDispatcher<SessionContainer>(
            this,
            id => new SessionContainer(id, this)
        );
        joypadSessionDispatcher.SessionAdded += (sessions, _) =>
            SessionResizer.ResizeAllSessions(sessions, this);
        joypadSessionDispatcher.AssignSessions();
        joypadSessionDispatcher.ListenForChanges();
    }

    public override void _Process(float delta)
    {
        Update?.Invoke(delta);
    }

    public override void _PhysicsProcess(float delta)
    {
        PhysicsUpdate?.Invoke(delta);
    }

    public override void _Input(InputEvent @event)
    {
        Input?.Invoke(@event);
    }
}
