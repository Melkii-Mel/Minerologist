using System;
using Godot;

public class PlayerDiggerVisuals
{
    private readonly Session Session;
    public DiggerVisualsConfig DiggerVisualsConfig = Config.Instance.DiggerVisualsConfig;

    public DiggerAnimationConfig DiggerAnimationConfig => DiggerVisualsConfig.DiggerAnimationConfig;

    public Vector3I ToolRotationDegrees => DiggerVisualsConfig.ToolRotationDegrees;
    public float ToolScale => DiggerVisualsConfig.ToolScale;
    public Vector3 ToolOffset => DiggerVisualsConfig.ToolOffset.ToGdVector3() / 100;

    private readonly MeshInstance _model;
    private readonly Spatial _pivot;

    private Vector3 _pivotInitialPosition;
    private Vector3 _stateRotationDegrees;
    private Vector3 _stateOffset;

    private readonly Func<bool> _isIdle;

    public PlayerDiggerVisuals(Session session, Func<bool> isIdle)
    {
        Session = session;

        (_pivot, _model) = CreateModel();
        _pivotInitialPosition = _pivot.Position;
        GD.Print("Model createad");

        _isIdle = isIdle;

        Session.Update += Update;
    }

    private void Update(float delta)
    {
        _stateRotationDegrees = Move(
            _stateRotationDegrees,
            _isIdle() ? DiggerAnimationConfig.RotationIdleDegrees.ToGdVector3() : Vector3.Zero,
            delta / DiggerAnimationConfig.RotationDurationSeconds
        );
        _stateOffset = Move(
            _stateOffset,
            _isIdle() ? DiggerAnimationConfig.OffsetIdle.ToGdVector3() / 100 : Vector3.Zero,
            delta / DiggerAnimationConfig.RotationDurationSeconds
        );

        _pivot.RotationDegrees = _stateRotationDegrees;
        _pivot.Position = _pivotInitialPosition + _stateOffset;
    }

    private (Spatial Pivot, MeshInstance Model) CreateModel()
    {
        var rootUi = Session.RootUi;
        var viewportContainer = new ViewportContainer();
        var viewport = new Viewport();
        var root3d = new Spatial();
        var camera = new Camera();

        rootUi.AddChild(viewportContainer);

        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportContainer.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);

        viewportContainer.AddChild(viewport);

        viewport.Size = viewportContainer.RectSize;
        viewport.World = new();
        viewport.TransparentBg = true;

        viewport.AddChild(root3d);

        root3d.AddChild(camera);

        MeshInstance digger;
        if (MeshCollection.OtherMeshes.TryGetValue(DiggerVisualsConfig.ToolModel, out var d))
        {
            digger = (MeshInstance)d.Duplicate();
            digger.Scale *= ToolScale;
            digger.RotationDegrees = ToolRotationDegrees.ToGdVector3();
        }
        else
        {
            GD.PrintErr("Tool model not found");
            digger = new();
        }
        var pivot = new Spatial();
        var pivotOffset = DiggerAnimationConfig.AnimationPivotOffset.ToGdVector3() / 100;
        root3d.AddChild(pivot);
        pivot.Position = ToolOffset + pivotOffset;
        digger.Position = -pivotOffset;
        pivot.AddChild(digger);

        return (pivot, digger);
    }

    private Vector3 Move(Vector3 from, Vector3 to, float delta)
    {
        var deltaPosition = (to - from) * delta;
        return from + deltaPosition;
    }
}
