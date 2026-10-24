using System;
using Godot;

public class Root : Node
{
    public event Action<float>? Update;
    public event Action<float>? PhysicsUpdate;
    public event Action<InputEvent>? Input;

    public override void _Ready()
    {
        Manifest.Root = "Assets";

        Console.WriteLine("Initializing entities collection...");
        MeshCollection.Initialize();

        Console.WriteLine("Baking entity variants");
        EntityVariantsContainer.Init();

        AudioCollection.Init();
        SoundPlayer.Init(this);

        GD.Print("Setting up session dispatcher...");
        var joypadSessionDispatcher = new JoypadSessionDispatcher<SessionContainer>(
            this,
            id => new SessionContainer(id, this),
            includeKeyboardInput: true
        );
        joypadSessionDispatcher.SessionAdded += (sessions, _) =>
            SessionResizer.ResizeAllSessions(sessions, this);
        joypadSessionDispatcher.AssignSessions();
        GD.Print("Initial sessions assigned");

        joypadSessionDispatcher.ListenForChanges();

        Animations.Init(a => Update += a);
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
