using Godot;

public class Border(Node parent, MeshInstance borderMeshInstance)
    : PeriodicVerticalSpawner<MeshInstance>(4)
{
    private readonly Node _parent = parent;
    private readonly MeshInstance _borderMeshInstance = borderMeshInstance;

    private Vector3I ChunkSize => Config.Instance.GenerationConfig.ChunkSize;

    private Vector2 Offset => new(ChunkSize.X / 2, ChunkSize.Z / 2);

    private AABB Aabb => _borderMeshInstance.GetAabb();

    public override float UnitHeight => Aabb.Size.y * BorderDiameter / Aabb.Size.x;

    public override MeshInstance AddItem(int verticalGlobalPosition, Vector3I worldPosition)
    {
        var instance = (MeshInstance)_borderMeshInstance.Duplicate();
        _parent.AddChild(instance);
        var scale = BorderDiameter / Aabb.Size.x;
        instance.Scale = new(scale, scale, scale);
        instance.Position = new(
            worldPosition.X + Offset.x,
            verticalGlobalPosition,
            worldPosition.Z + Offset.y
        );
        var staticBody = new StaticBody();
        staticBody.AddToGroup(NodeGroups.BORDER);
        instance.AddChild(staticBody);
        var shape = new CollisionShape() { Shape = instance.Mesh.CreateTrimeshShape() };
        staticBody.AddChild(shape);
        return instance;
    }

    public override void RemoveItem(MeshInstance item)
    {
        item.QueueFree();
    }
}
