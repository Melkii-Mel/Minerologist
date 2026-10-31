using Godot;

public readonly struct ModelFitResult(Transform transform, AABB bounds, Vector3 anchor)
{
    public Transform Transform { get; } = transform;
    public AABB Bounds { get; } = bounds;

    public Vector3 Anchor { get; } = anchor;
}
