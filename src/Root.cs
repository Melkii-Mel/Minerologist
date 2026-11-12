using System;
using System.Text;
using Godot;

public class Root : Node
{
    public event Action<float>? Update;
    public event Action<float>? PhysicsUpdate;
    public event Action<InputEvent>? Input;
    public event Action<InputEvent>? UnhandledInput;

    public bool NoKeyboard { get; private set; }

    public override void _Ready()
    {
        Console.OutputEncoding = Encoding.UTF8;
        AsyncIO.ForceDotNet.Force();

        Manifest.Root = "Assets";
        MeshCollection.Initialize();

        AudioCollection.Init();
        SoundPlayer.Init(this);

        var args = System.Environment.GetCommandLineArgs();
        NoKeyboard = Array.IndexOf(args, "--no-keyboard") != -1;
        bool isMuseum = Array.IndexOf(args, "--museum") != -1;
        var hostIndex = Array.IndexOf(args, "--host");
        string? host = null;
        if (hostIndex != -1)
        {
            host = args[hostIndex + 1];
        }
        _ = new DebugUI(this, false);

        Animations.Init(a => Update += a);

        if (isMuseum)
        {
            AddChild(new MuseumRoot(this));
        }
        else
        {
            AddChild(new GameRoot(this, host));
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

    public override void _UnhandledInput(InputEvent @event)
    {
        UnhandledInput?.Invoke(@event);
    }
}
