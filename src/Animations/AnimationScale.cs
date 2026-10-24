using Godot;

public class AnimationScale(Spatial target, float relativeScale) : AnimationInstance
{
    private readonly Vector3 _initialScale = target.Scale;

    protected override void ApplyInternal(float completion)
    {
        var scaleDelta = _initialScale * relativeScale - _initialScale;
        scaleDelta *= completion;
        target.Scale = _initialScale + scaleDelta;
    }
}
