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

    public Crosshair(Session session)
    {
        session.Update += Update;

        _controlsParent = new();

        session.RootUi.AddChild(_controlsParent);
        _controlsParent.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Center);
        _controlsParent.RectPosition -= CrosshairConfig.CrosshairPartSize / 2;
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
            rect.RectSize = CrosshairConfig.CrosshairPartSize;
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
        var activityDifference =
            (Active ? CrosshairConfig.ActiveScale : CrosshairConfig.InactiveScale) - _activityState;
        var activitySign = Mathf.Sign(activityDifference);
        activityDifference = Mathf.Abs(activityDifference);
        _activityState +=
            Mathf.Min(
                Mathf.Max(
                    activityDifference * CrosshairConfig.RelativeTransitionSpeed,
                    CrosshairConfig.MinScaleTransitionSpeed
                ) * delta,
                activityDifference
            ) * activitySign;

        foreach (var crosshairPart in _states.Keys)
        {
            var currentState = _states[crosshairPart];
            var targetState = CrosshairConfig.CrosshairModeConfigs[CrosshairMode.ToString()][
                crosshairPart.ToString()
            ];

            var offsetDifference =
                new Vector2(targetState.OffsetX, targetState.OffsetY)
                - new Vector2(currentState.OffsetX, currentState.OffsetY);
            var offsetDifferenceDirection = offsetDifference.Normalized();
            var offsetDifferenceLength = offsetDifference.Length();

            var offsetDelta =
                Mathf.Min(
                    Mathf.Max(
                        offsetDifferenceLength * CrosshairConfig.RelativeTransitionSpeed,
                        CrosshairConfig.MinOffsetTransitionSpeed
                    ) * delta,
                    offsetDifferenceLength
                ) * offsetDifferenceDirection;
            currentState.OffsetX += offsetDelta.x;
            currentState.OffsetY += offsetDelta.y;

            var rotationDifference = targetState.Rotation - currentState.Rotation;
            var rotationSign = Mathf.Sign(rotationDifference);
            rotationDifference = Mathf.Abs(rotationDifference);

            currentState.Rotation +=
                Mathf.Min(
                    Mathf.Max(
                        rotationDifference * CrosshairConfig.RelativeTransitionSpeed,
                        CrosshairConfig.MinRotationSpeedDegrees
                    ) * delta,
                    rotationDifference
                ) * rotationSign;

            var scaleDifference = targetState.Scale - currentState.Scale;
            var scaleSign = Mathf.Sign(scaleDifference);
            scaleDifference = Mathf.Abs(scaleDifference);

            currentState.Scale +=
                Mathf.Min(
                    Mathf.Max(scaleDifference, CrosshairConfig.MinScaleTransitionSpeed) * delta,
                    scaleDifference
                ) * scaleSign;
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
