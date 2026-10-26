using System;
using Godot;

public class PlayerDiggerVisuals
{
    private readonly Session Session;
    public DiggerVisualsConfig DiggerVisualsConfig => Config.Instance.DiggerVisualsConfig;

    public DiggerAnimationConfig DiggerAnimationConfig => DiggerVisualsConfig.DiggerAnimationConfig;

    public Vector3I ToolRotationDegrees => DiggerVisualsConfig.ToolRotationDegrees;
    public float ToolScale => DiggerVisualsConfig.ToolScale;
    public Vector3 ToolOffset => DiggerVisualsConfig.ToolOffset.ToGdVector3() / 100;

    private Spatial _root3d = null!;
    private MeshInstance _digger = null!;
    private Spatial _pivot = null!;

    private Vector3 _pivotInitialPosition;
    private Vector3 _stateRotationDegrees;
    private Vector3 _stateOffset;
    private Vector3 _shakeOffset;

    private readonly Func<bool> _isIdle;

    public PlayerDiggerVisuals(Session session, Func<bool> isIdle)
    {
        Session = session;

        CreateModel();
        _pivotInitialPosition = _pivot.Position;
        GD.Print("Model createad");

        _isIdle = isIdle;

        Session.Update += Update;
    }

    private void Update(float delta)
    {
        _root3d.Rotation = Session.Player.PlayerNode.Rotation;
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
        _pivot.Position = _pivotInitialPosition + _stateOffset + _shakeOffset;
    }

    private void CreateModel()
    {
        var rootUi = Session.RootUi;
        var viewportContainer = new ViewportContainer();
        var viewport = new Viewport();
        _root3d = new Spatial();
        var camera = new Camera();

        rootUi.AddChild(viewportContainer);

        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportContainer.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);

        viewportContainer.AddChild(viewport);

        viewport.Size = viewportContainer.RectSize;
        viewport.World = new();
        viewport.TransparentBg = true;

        viewport.AddChild(SceneLoader.Instantiate<Node>("Environment"));

        viewport.AddChild(_root3d);

        _root3d.AddChild(camera);

        if (MeshCollection.OtherMeshes.TryGetValue(DiggerVisualsConfig.ToolModel, out var d))
        {
            _digger = (MeshInstance)d.Duplicate();
            _digger.Scale *= ToolScale;
            _digger.RotationDegrees = ToolRotationDegrees.ToGdVector3();
        }
        else
        {
            GD.PrintErr("Tool model not found");
            _digger = new();
        }
        _pivot = new Spatial();
        var pivotOffset = DiggerAnimationConfig.AnimationPivotOffset.ToGdVector3() / 100;
        _root3d.AddChild(_pivot);
        _pivot.Position = ToolOffset + pivotOffset;
        _digger.Position = -pivotOffset;
        _pivot.AddChild(_digger);
    }

    private Vector3 Move(Vector3 from, Vector3 to, float delta)
    {
        var deltaPosition = (to - from) * delta;
        return from + deltaPosition;
    }
}
