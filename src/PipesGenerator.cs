using System;
using Godot;

public class Pipes(Node parent, MeshInstance pipeMeshInstance, Random rng)
    : PeriodicVerticalSpawner<MeshInstance>(Config.Instance.PipeConfig.GenerationRange)
{
    private readonly Node _parent = parent;

    private Vector3I ChunkSize => Config.Instance.GenerationConfig.ChunkSize;

    private Vector2 Offset => new(ChunkSize.X / 2, ChunkSize.Z / 2);

    public PipeConfig PipeConfig => Config.Instance.PipeConfig;

    public override float UnitHeight => PipeConfig.UnitHeight;
    public float Scale => PipeConfig.Scale;
    public float GenerationChance => PipeConfig.GenerationChance;
    public bool AlwaysGenerateTop => PipeConfig.AlwaysGenerateTop;

    public float OffsetY => 2;

    private readonly int _seed = rng.Next();

    public override MeshInstance AddItem(int verticalGlobalPosition, Vector3I worldPosition)
    {
        var instance = (MeshInstance)pipeMeshInstance.Duplicate();
        _parent.AddChild(instance);
        instance.Scale = new(Scale, Scale, Scale);
        instance.Position = new(
            worldPosition.X + Offset.x,
            UnitHeight * verticalGlobalPosition + OffsetY,
            worldPosition.Z + Offset.y
        );
        var angle = Angle(verticalGlobalPosition);
        instance.Position += Vector3.Left.Rotated(Vector3.Up, angle) * BorderDiameter / 2;
        instance.Rotation = new Vector3(Mathf.Pi / 2, angle + Mathf.Pi / 2, 0);
        var staticBody = new StaticBody();
        instance.AddChild(staticBody);
        staticBody.AddToGroup(NodeGroups.PIPE);
        var shape = new CollisionShape() { Shape = instance.Mesh.CreateTrimeshShape() };
        staticBody.AddChild(shape);
        return instance;
    }

    public override void RemoveItem(MeshInstance item)
    {
        item.QueueFree();
    }

    public float Angle(int id)
    {
        var rng = new Random(_seed + id);
        return (float)rng.NextDouble() * Mathf.Pi * 2;
    }
}
