using Godot;

/// <summary>
/// Animates `from` towards `to`'s current global position.
/// </summary>
public class AnimationEntityFlyTowards(Spatial from, Spatial to) : AnimationInstance
{
    private readonly Vector3 _initialPosition = from.GlobalPosition;
    private readonly Spatial from = from;
    private readonly Spatial to = to;

    protected override void ApplyInternal(float completion)
    {
        if (!Object.IsInstanceValid(from) || !Object.IsInstanceValid(to))
        {
            return;
        }
        from.GlobalPosition =
            _initialPosition + (to.GlobalPosition - _initialPosition) * completion;
    }
}
