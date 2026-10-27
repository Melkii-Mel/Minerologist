using Godot;

public class CircularProgressBar(
    float ringWidth,
    float radius,
    Color bgColor,
    Color progressColor,
    float progress = 0
) : Control
{
    public float Progress { get; private set; } = progress;

    public float RingWidth { get; } = ringWidth;

    public float Radius { get; } = radius;

    public Color BackgroundColor { get; } = bgColor;

    public Color ProgressColor { get; } = progressColor;

    public override void _Draw()
    {
        var center = RectSize / 2.0f;
        var startAngle = -90.0f;

        DrawArc(
            center,
            Radius,
            Mathf.Deg2Rad(startAngle),
            Mathf.Deg2Rad(startAngle + 360.0f),
            128,
            BackgroundColor,
            RingWidth,
            true
        );

        DrawArc(
            center,
            Radius,
            Mathf.Deg2Rad(startAngle),
            Mathf.Deg2Rad(startAngle + 360.0f * Mathf.Clamp(Progress, 0, 1)),
            128,
            ProgressColor,
            RingWidth,
            true
        );
    }

    public void SetProgress(float value)
    {
        Progress = Mathf.Clamp(value, 0, 1);
        Update();
    }
}
