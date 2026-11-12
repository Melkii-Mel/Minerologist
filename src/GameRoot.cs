using System.Collections.Generic;
using Godot;

public class GameRoot(Root root, string? host) : Node
{
    public Root Root { get; } = root;

    private JoypadSessionDispatcher<SessionContainer> _sessionDispatcher = null!;

    public List<SessionContainer> Sessions => _sessionDispatcher.Sessions;

    public override void _Ready()
    {
        EntityVariantsContainer.Init();

        _sessionDispatcher = new JoypadSessionDispatcher<SessionContainer>(
            id => new SessionContainer(id, this),
            includeKeyboardInput: !Root.NoKeyboard
        );
        _sessionDispatcher.SessionAdded += (sessions, _) =>
            SessionResizer.ResizeAllSessions(sessions, this);

        _sessionDispatcher.AssignSessions();

        _sessionDispatcher.ListenForChanges();

        _ = new MuseumCommunicator(this, host);
    }
}
