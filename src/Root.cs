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
        MeshCollection.Initialize();

        AudioCollection.Init();
        SoundPlayer.Init(this);

        var args = OS.GetCmdlineArgs();
        bool isMuseum = Array.IndexOf(args, "--museum") != -1;

        if (isMuseum)
        {
            AddChild(new MuseumRoot(this));
        }
        else
        {
            AddChild(new GameRoot(this));
        }
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
