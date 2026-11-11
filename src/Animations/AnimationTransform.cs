using Godot;

public class AnimationTransform(
    Spatial target,
    Vector3 targetPosition,
    Vector3 targetRotation,
    Vector3 targetScale
) : AnimationInstance
{
    private readonly Spatial _target = target;

    private readonly Vector3 _initialPosition = target.Position;
    private readonly Vector3 _initialRotation = target.Rotation;
    private readonly Vector3 _initialScale = target.Scale;

    protected override void ApplyInternal(float completion)
    {
        if (!Object.IsInstanceValid(_target))
        {
            return;
        }

        _target.Position = _initialPosition.LinearInterpolate(targetPosition, completion);
        _target.Rotation = _initialRotation.LinearInterpolate(targetRotation, completion);
        _target.Scale = _initialScale.LinearInterpolate(targetScale, completion);
    }
}
