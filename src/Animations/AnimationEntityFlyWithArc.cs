using Godot;

/// <summary>
/// Animates <paramref name="from"/> towards <paramref name="to"/>'s
/// current global position along a cubic Bezier curve.
/// </summary>
/// <param name="arrivalDirection">
/// The global direction in which the object should approach the target.
/// </param>
/// <param name="maxApproachDistance">
/// Distance from the target to the final control point.
/// </param>
public class AnimationEntityFlyWithArc(
    Spatial from,
    Spatial to,
    Vector3 arrivalDirection,
    float maxApproachDistance = 1.5f
) : AnimationInstance
{
    private readonly Vector3 _initialPosition = from.GlobalPosition;

    private readonly Vector3 _initialControlPoint =
        from.GlobalPosition + (to.GlobalPosition - from.GlobalPosition) * 0.3f;

    private readonly float _approachDistance =
        maxApproachDistance
        * (
            1f
            - Mathf.Max(
                0f,
                (to.GlobalPosition - from.GlobalPosition)
                    .Normalized()
                    .Dot(arrivalDirection.Normalized())
            )
        );

    protected override void ApplyInternal(float completion)
    {
        Vector3 arrivalControlPoint =
            to.GlobalPosition - arrivalDirection.Normalized() * _approachDistance;

        float t = Mathf.Clamp(completion, 0f, 1f);
        float u = 1f - t;

        Vector3 end = to.GlobalPosition;

        from.GlobalPosition =
            u * u * u * _initialPosition
            + 3f * u * u * t * _initialControlPoint
            + 3f * u * t * t * arrivalControlPoint
            + t * t * t * end;
    }
}
