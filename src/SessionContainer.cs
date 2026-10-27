using System;
using Godot;

public class SessionContainer : ISession
{
    public GameRoot Root { get; }
    public bool Frozen { get; private set; }
    public int ControllerId { get; private set; }
    public Session? Session { get; private set; }

    private Viewport? _viewport;
    private ViewportContainer? _viewportContainer;
    private Control? _localRootUi;
    private Spatial? _localRoot3d;
    private Control? _deviceDisconnectedPopup;

    private Action<float>? _sessionUpdate;
    private Action<float>? _sessionPhysicsUpdate;
    private Action<InputEvent>? _sessionInput;

    public SessionContainer(int controllerId, GameRoot root)
    {
        Root = root;
        ControllerId = controllerId;
        Start();
    }

    public void Start()
    {
        _viewportContainer = new ViewportContainer();
        Root.AddChild(_viewportContainer);
        _viewport = new Viewport();
        _viewportContainer.AddChild(_viewport);
        _localRoot3d = new Spatial();
        _localRootUi = new Control();
        _viewport.AddChild(_localRootUi);
        _viewport.AddChild(_localRoot3d);
        _localRootUi.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
        _localRootUi.MouseFilter = Control.MouseFilterEnum.Ignore;
        Frozen = false;
        GD.Print("New session container created");

        Session = new Session(
            this,
            _localRoot3d,
            _localRootUi,
            new Random(),
            action =>
            {
                _sessionUpdate = action;
                Root.Root.Update += action;
            },
            action =>
            {
                _sessionPhysicsUpdate = action;
                Root.Root.PhysicsUpdate += action;
            },
            action =>
            {
                _sessionInput += action;
                Root.Root.Input += action;
            }
        );
    }

    public void Freeze()
    {
        Frozen = true;
        _deviceDisconnectedPopup = SceneLoader.Instantiate<Control>("PopupDeviceDisconnected");
        _localRootUi?.AddChild(_deviceDisconnectedPopup);
        _viewportContainer?.AddChild(_deviceDisconnectedPopup);
        Session?.TimeScale = 0;
        throw new NotImplementedException("Session freezing is not yet implemented");
    }

    public void Resume(int controllerId)
    {
        ControllerId = controllerId;
        Frozen = false;
        _deviceDisconnectedPopup?.QueueFree();
        Session?.TimeScale = 1;
        throw new NotImplementedException("Session unfreezing is not yet implemented");
    }

    public void SetPositionAndSize(Vector2 position, Vector2 size)
    {
        _viewport?.Size = size;
        _viewportContainer?.SetSize(size);
        _viewportContainer?.RectPosition = position;
    }

    public void Restart()
    {
        Root.Root.Update -= _sessionUpdate;
        Root.Root.PhysicsUpdate -= _sessionPhysicsUpdate;
        Root.Root.Input -= _sessionInput;
        _viewportContainer?.QueueFree();

        Start();
        SessionResizer.ResizeAllSessions([.. Root.Sessions], Root);
    }
}
