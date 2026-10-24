using System;
using Godot;

public class SessionContainer : ISession
{
    public Root Root { get; }
    public bool Frozen { get; private set; }
    public int ControllerId { get; private set; }
    public Session Session { get; }

    private readonly Viewport _viewport;
    private readonly ViewportContainer _viewportContainer;
    private readonly Control _localRootUi;
    private readonly Spatial _localRoot3d;
    private Control? _deviceDisconnectedPopup;

    public SessionContainer(int controllerId, Root root)
    {
        _viewportContainer = new ViewportContainer();
        root.AddChild(_viewportContainer);
        _viewport = new Viewport();
        _viewportContainer.AddChild(_viewport);
        _localRoot3d = new Spatial();
        _localRootUi = new Control();
        _viewport.AddChild(_localRootUi);
        _viewport.AddChild(_localRoot3d);
        Root = root;
        ControllerId = controllerId;
        Frozen = false;
        GD.Print("New session container created");

        Session = new Session(
            this,
            _localRoot3d,
            _localRootUi,
            new Random(),
            action => Root.Update += action,
            action => Root.PhysicsUpdate += action
        );
    }

    public void Freeze()
    {
        Frozen = true;
        _deviceDisconnectedPopup = SceneLoader.Instantiate<Control>("PopupDeviceDisconnected");
        _localRootUi.AddChild(_deviceDisconnectedPopup);
        _viewportContainer.AddChild(_deviceDisconnectedPopup);
        Session.TimeScale = 0;
        throw new NotImplementedException("Session freezing is not yet implemented");
    }

    public void Resume(int controllerId)
    {
        ControllerId = controllerId;
        Frozen = false;
        _deviceDisconnectedPopup?.QueueFree();
        Session.TimeScale = 1;
        throw new NotImplementedException("Session unfreezing is not yet implemented");
    }

    public void SetPositionAndSize(Vector2 position, Vector2 size)
    {
        _viewport.Size = size;
        _viewportContainer.SetSize(size);
        _viewportContainer.RectPosition = position;
    }
}
