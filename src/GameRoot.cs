using System.Collections.Generic;
using Godot;

public class GameRoot(Root root) : Node
{
    public Root Root { get; } = root;

    private JoypadSessionDispatcher<SessionContainer> _sessionDispatcher = null!;

    public List<SessionContainer> Sessions => _sessionDispatcher.Sessions;

    public override void _Ready()
    {
        AddChild(SceneLoader.Instantiate<Node>("Environment"));

        EntityVariantsContainer.Init();

        _sessionDispatcher = new JoypadSessionDispatcher<SessionContainer>(
            this,
            id => new SessionContainer(id, this),
            includeKeyboardInput: true
        );

        _sessionDispatcher.SessionAdded += (sessions, _) =>
            SessionResizer.ResizeAllSessions(sessions, this);

        _sessionDispatcher.AssignSessions();

        _sessionDispatcher.ListenForChanges();

        Animations.Init(a => Root.Update += a);
    }
}
