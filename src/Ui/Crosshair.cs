using System;
using System.Collections.Generic;
using Godot;

public class Crosshair
{
    public CrosshairMode CrosshairMode { get; set; }
    public bool Active { get; set; }

    private CrosshairConfig CrosshairConfig => Config.Instance.CrosshairConfig;

    private readonly Dictionary<CrosshairPart, CrosshairPartState> _states = [];

    private readonly Control _controlsParent;

    private readonly Dictionary<CrosshairPart, Control> _controls = [];

    private float _activityState = 0;

    private Vector2 CrosshairPartSize =>
        new(CrosshairConfig.CrosshairPartSizeX, CrosshairConfig.CrosshairPartSizeY);

    public Crosshair(Session session)
    {
        session.Update += Update;

        _controlsParent = new();

        session.RootUi.AddChild(_controlsParent);
        _controlsParent.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Center);
        _controlsParent.RectPosition -= CrosshairPartSize / 2;
        _controlsParent.MouseFilter = Control.MouseFilterEnum.Ignore;

        foreach (var part in (CrosshairPart[])Enum.GetValues(typeof(CrosshairPart)))
        {
            _states[part] = new(0, 0, 0);
            _controls[part] = CreateCrosshairPart(part);
        }

        return;

        Control CreateCrosshairPart(CrosshairPart part)
        {
            var rect = new TextureRect();
            _controlsParent.AddChild(rect);
            rect.Texture = GD.Load<Texture>($"res://Assets/Textures/Ui/CrosshairParts/{part}.png");
            rect.RectSize = CrosshairPartSize / 2;
            rect.RectPivotOffset = rect.RectSize / 2;
            rect.MouseFilter = Control.MouseFilterEnum.Ignore;
            return rect;
        }
    }

    private void Update(float delta)
    {
        UpdateState(delta);
        UpdateCrosshair();
    }

    private void UpdateCrosshair()
    {
        foreach (var part in _controls.Keys)
        {
            var control = _controls[part];
            var state = _states[part];
            var scale = state.Scale + _activityState - 1;
            control.RectPosition =
                new Vector2(state.OffsetX, state.OffsetY) * control.RectSize.x * scale;
            control.RectRotation = state.Rotation;
            control.RectScale = new(scale, scale);
        }
    }

    private void UpdateState(float delta)
    {
        _activityState = MoveTowards.Calculate(
            _activityState,
            Active ? CrosshairConfig.ActiveScale : CrosshairConfig.InactiveScale,
            CrosshairConfig.RelativeTransitionSpeed,
            CrosshairConfig.MinScaleTransitionSpeed,
            delta
        );

        foreach (var crosshairPart in _states.Keys)
        {
            var currentState = _states[crosshairPart];
            var targetState = CrosshairConfig.CrosshairModeConfigs[CrosshairMode.ToString()][
                crosshairPart.ToString()
            ];

            var currentOffset = new Vector2(currentState.OffsetX, currentState.OffsetY);
            var targetOffset = new Vector2(targetState.OffsetX, targetState.OffsetY);

            var offset = MoveTowards.Calculate(
                currentOffset,
                targetOffset,
                CrosshairConfig.RelativeTransitionSpeed,
                CrosshairConfig.MinOffsetTransitionSpeed,
                delta
            );

            currentState.OffsetX = offset.x;
            currentState.OffsetY = offset.y;

            currentState.Rotation = MoveTowards.Calculate(
                currentState.Rotation,
                targetState.Rotation,
                CrosshairConfig.RelativeTransitionSpeed,
                CrosshairConfig.MinRotationSpeedDegrees,
                delta
            );

            currentState.Scale = MoveTowards.Calculate(
                currentState.Scale,
                targetState.Scale,
                CrosshairConfig.RelativeTransitionSpeed,
                CrosshairConfig.MinScaleTransitionSpeed,
                delta
            );
        }
    }
}

public class CrosshairPartState(float offsetX, float offsetY, float rotation, float scale = 1)
{
    public float OffsetX { get; set; } = offsetX;
    public float OffsetY { get; set; } = offsetY;
    public float Rotation { get; set; } = rotation;
    public float Scale { get; set; } = scale;
}

public enum CrosshairPart
{
    Left,
    Right,
    Top,
    Bottom,
}

public enum CrosshairMode
{
    Air,
    Ground,
    Diggable,
    Pipe,
}
