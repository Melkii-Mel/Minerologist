using Godot;

/// <summary>
/// Animates `from` towards `to`'s current global position.
/// </summary>
public class AnimationEntityFlyTowards : AnimationInstance
{
    private readonly Vector3 _initialPosition;
    private readonly Spatial from;
    private readonly Spatial to;

    public AnimationEntityFlyTowards(Spatial from, Spatial to)
    {
        this.from = from;
        this.to = to;
        _initialPosition = from.GlobalPosition;
    }

    protected override void ApplyInternal(float completion)
    {
        from.GlobalPosition =
            _initialPosition + (to.GlobalPosition - _initialPosition) * completion;
    }
}
