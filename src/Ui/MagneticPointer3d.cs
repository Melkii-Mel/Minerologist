using System;
using Godot;

public class MagneticPointer3d : Godot.Object
{
    public MuseumRoot Root { get; }

    public MuseumPointerConfig MuseumPointerConfig =>
        Config.Instance.MuseumConfig.MuseumPointerConfig;

    public string CurrentTargetGroup { get; set; } = "";

    private readonly Control _pointer;

    private float _magnetCapacity;
    private MeshInstance? _currentTarget;
    private Rect2 _currentTargetProjectedBounds;
    private Vector2 _currentTargetCenter;
    private MeshInstance? _pressedItem;

    public event Action<MeshInstance>? MeshClicked;

    public MagneticPointer3d(MuseumRoot root)
    {
        Root = root;
        _pointer = new();
        var canvasLayer = new CanvasLayer() { Layer = 10 };
        root.RootUi.AddChild(canvasLayer);
        var texture = GD.Load<Texture>(
            "res://" + Manifest.Instance.Dir("Textures").Dir("Ui").File("Pointer.png").FullPath
        );
        var pointerVisuals = new TextureRect { Texture = texture, RectSize = texture.GetSize() };
        _pointer.AddChild(pointerVisuals);
        pointerVisuals.MouseFilter = Control.MouseFilterEnum.Ignore;
        _pointer.MouseFilter = Control.MouseFilterEnum.Ignore;
        canvasLayer.AddChild(_pointer);
        _pointer.Modulate = new(MuseumPointerConfig.ColorModulation);

        Root.Root.Update += Update;
        Root.Root.Input += Input;
        Root.Root.UnhandledInput += UnhandledInput;
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
    }

    private bool _guiHandled;

    private void Update(float delta)
    {
        _magnetCapacity += delta;
        _magnetCapacity = Mathf.Min(_magnetCapacity, MuseumPointerConfig.MagnetEnergyCapacity);

        var camera = Root.Camera.Camera;
        var pointerPosition = _pointer.RectGlobalPosition;
        var rayFrom = camera.ProjectRayOrigin(pointerPosition);
        var direction = camera.ProjectRayNormal(pointerPosition);
        var rayTo = rayFrom + direction * MuseumPointerConfig.ScanRange;
        var scanRadius = MuseumPointerConfig.ScanConeRadius;
        var hits = ConeCaster.Cast(
            Root.Building.Building.GetWorld(),
            rayFrom,
            rayTo,
            Mathf.Deg2Rad(scanRadius) / 2
        );
        if (_guiHandled)
        {
            _currentTarget = null;
            return;
        }
        var targetFound = false;
        foreach (var rayHit in hits)
        {
            if (rayHit.Collider.IsInGroup(CurrentTargetGroup))
            {
                targetFound = true;
                var newTarget = (MeshInstance)rayHit.Collider.GetParent();
                if (newTarget == _currentTarget)
                {
                    break;
                }
                _currentTargetProjectedBounds = GetProjectedBounds(newTarget);
                _currentTargetCenter = _currentTargetProjectedBounds.GetCenter();
                _currentTarget = newTarget;
                break;
            }
        }
        if (!targetFound)
        {
            _currentTarget = null;
        }
        if (_currentTarget != null)
        {
            Magnet(delta);
        }
    }

    public const int VirtualDeviceId = 666;

    private static bool IsVirtualEvent(InputEvent e) => e.Device == VirtualDeviceId;

    private void UnhandledInput(InputEvent @event)
    {
        if (!IsVirtualEvent(@event))
        {
            return;
        }

        _guiHandled = false;

        if (@event is InputEventMouseButton buttonEvent)
        {
            if (buttonEvent.ButtonIndex != (int)ButtonList.Left)
            {
                return;
            }

            if (!buttonEvent.Pressed)
            {
                if (_pressedItem == _currentTarget && _currentTarget != null)
                {
                    MeshClicked?.Invoke(_currentTarget);
                }

                _pressedItem = null;
            }
            else
            {
                _pressedItem = _currentTarget;
            }
        }
    }

    private void Input(InputEvent @event)
    {
        if (IsVirtualEvent(@event))
        {
            return;
        }

        if (@event is InputEventMouseButton buttonEvent)
        {
            var e = new InputEventMouseButton
            {
                ButtonIndex = buttonEvent.ButtonIndex,
                Pressed = buttonEvent.Pressed,
                Factor = buttonEvent.Factor,
                Doubleclick = buttonEvent.Doubleclick,
            };

            ConfigureInputEventMouse(e, buttonEvent);

            _guiHandled = true;
            Godot.Input.ParseInputEvent(e);

            Root.GetTree().SetInputAsHandled();
        }

        if (@event is InputEventMouseMotion motionEvent)
        {
            Offset(motionEvent.Relative * MuseumPointerConfig.Sensitivity);

            var e = new InputEventMouseMotion
            {
                Relative = motionEvent.Relative,
                Speed = motionEvent.Speed,
            };

            ConfigureInputEventMouse(e, motionEvent);

            _guiHandled = true;
            Godot.Input.ParseInputEvent(e);

            Root.GetTree().SetInputAsHandled();
        }

        void ConfigureInputEventMouse(InputEventMouse e, InputEventMouse originalEvent)
        {
            e.GlobalPosition = _pointer.RectGlobalPosition;
            e.Position = _pointer.RectGlobalPosition;
            e.Device = VirtualDeviceId;
            e.ButtonMask = originalEvent.ButtonMask;
        }
    }

    private void Magnet(float delta)
    {
        var positionDifference = _currentTargetCenter - _pointer.RectGlobalPosition;
        var positionDifferenceLength = positionDifference.Length();

        if (positionDifferenceLength <= 0)
            return;

        var direction = positionDifference / positionDifferenceLength;

        var maxMovement = delta * MuseumPointerConfig.MagnetStrength;
        var affordableMovement =
            _magnetCapacity
            * MuseumPointerConfig.MagnetStrength
            / MuseumPointerConfig.MagnetEnergyConsumptionRate;

        var movement = Mathf.Min(
            Mathf.Min(positionDifferenceLength, maxMovement),
            affordableMovement
        );

        _magnetCapacity -=
            movement
            / MuseumPointerConfig.MagnetStrength
            * MuseumPointerConfig.MagnetEnergyConsumptionRate;

        Offset(movement * direction);
    }

    private void Offset(Vector2 delta)
    {
        var borders = Root.GetViewport().GetVisibleRect();
        var rectPosition = _pointer.RectGlobalPosition + delta;

        rectPosition.x = Mathf.Clamp(rectPosition.x, borders.Position.x, borders.End.x);
        rectPosition.y = Mathf.Clamp(rectPosition.y, borders.Position.y, borders.End.y);

        _pointer.RectGlobalPosition = rectPosition;
    }

    private Rect2 GetProjectedBounds(MeshInstance meshInstance)
    {
        var camera = Root.Camera.Camera;
        var aabb = meshInstance.GetTransformedAabb();

        var corners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            corners[i] =
                aabb.Position
                + new Vector3(
                    (i & 1) != 0 ? aabb.Size.x : 0,
                    (i & 2) != 0 ? aabb.Size.y : 0,
                    (i & 4) != 0 ? aabb.Size.z : 0
                );
        }

        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector3 corner in corners)
        {
            var screenPos = camera.UnprojectPosition(corner);

            min.x = Mathf.Min(min.x, screenPos.x);
            min.y = Mathf.Min(min.y, screenPos.y);

            max.x = Mathf.Max(max.x, screenPos.x);
            max.y = Mathf.Max(max.y, screenPos.y);
        }

        return new Rect2(min, max - min);
    }
}
