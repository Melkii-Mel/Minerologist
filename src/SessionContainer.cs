using System;
using Godot;

public class SessionContainer : ISession
{
    private readonly Viewport _viewport;
    private readonly ViewportContainer _viewportContainer;
    private readonly Control _localRootUi;
    private readonly Spatial _localRoot3d;
    private readonly Session _session;
    private readonly Root _root;
    private Control? _deviceDisconnectedPopup;
    public bool Frozen { get; private set; }
    public int ControllerId { get; private set; }

    public SessionContainer(int controllerId, Root root)
    {
        _viewportContainer = new ViewportContainer();
        _viewport = new Viewport();
        _viewportContainer.AddChild(_viewport);
        _localRoot3d = new Spatial();
        _localRootUi = new Control();
        _viewport.AddChild(_localRootUi);
        _viewport.AddChild(_localRoot3d);
        _root = root;
        ControllerId = controllerId;
        Frozen = false;

        _session = new Session(
            this,
            _localRoot3d,
            _localRootUi,
            action => _root.Update += action,
            action => _root.PhysicsUpdate += action
        );
    }

    public void Freeze()
    {
        Frozen = true;
        _deviceDisconnectedPopup = SceneLoader.Instantiate<Control>("PopupDeviceDisconnected");
        _localRootUi.AddChild(_deviceDisconnectedPopup);
        _viewportContainer.AddChild(_deviceDisconnectedPopup);
        _session.TimeScale = 0;
        throw new NotImplementedException("Session freezing is not yet implemented");
    }

    public void Resume(int controllerId)
    {
        ControllerId = controllerId;
        Frozen = false;
        _deviceDisconnectedPopup?.QueueFree();
        _session.TimeScale = 1;
        throw new NotImplementedException("Session unfreezing is not yet implemented");
    }

    public void SetPositionAndSize(Vector2 position, Vector2 size)
    {
        _viewport.Size = size;
        _viewportContainer.SetSize(size);
        _viewportContainer.RectPosition = position;
    }
}
