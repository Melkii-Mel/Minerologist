using System;
using System.Linq;
using Godot;

public record EntityPrefab(string Type, string Id, MeshInstance MeshInstance)
{
    public static EntityPrefab Random(Random rng)
    {
        var remnantsConfig = Config.Instance.RemnantsConfig;
        var remnantTypeWeights = remnantsConfig.TypeWeights;
        var keys = remnantTypeWeights.Keys.ToList();
        var probabilities = keys.Select(k => remnantTypeWeights[k]).ToList();
        probabilities.Add(remnantsConfig.SkeletonPieceProbabilityWeight);
        var remnantTypeIndex = RandomUtils.ChooseWeighted(probabilities, rng);
        if (remnantTypeIndex == probabilities.Count - 1)
        {
            return RandomSkeletonPiece(rng);
        }
        return RandomOfType(keys[remnantTypeIndex], rng);
    }

    public static EntityPrefab RandomSkeletonPiece(Random rng)
    {
        var skeletonPieces = MeshCollection.Skeletons[Server.CurrentSkeleton];
        var remainingPieces = skeletonPieces
            .Where(p => !Server.CollectedSkeletonPieces.Contains(p.Key))
            .ToList();
        if (remainingPieces.Count == 0)
        {
            remainingPieces = [.. skeletonPieces];
        }
        var piece = RandomUtils.ChooseRandomItem(remainingPieces, rng);
        return new("Skeleton", piece.Key, piece.Value);
    }

    public static EntityPrefab RandomOfType(string type, Random rng)
    {
        var pieces = MeshCollection.OtherDiggables[type];
        var piece = RandomUtils.ChooseRandomItem([.. pieces], rng);
        return new(type, piece.Key, piece.Value);
    }
}

public class Entity
{
    public MeshInstance MeshInstance { get; }
    public Surface[] MeshData { get; }
    public Vector3I GlobalPosition { get; }
    public EntityPrefab Prefab { get; }
    public EntitiesChunk Chunk { get; set; } = null!;

    public Vector3I LocalPosition =>
        GlobalPosition
        - _coordinatesConverter.ChunkToWorld(_coordinatesConverter.WorldToChunk(GlobalPosition));

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    // Must be called on Godot's main thread
    public Entity(EntityPrefab prefab, Vector3I globalPosition, Vector3 rotation, float scale)
    {
        Prefab = prefab;
        GlobalPosition = globalPosition;
        MeshInstance = new MeshInstance
        {
            Position = globalPosition.ToGdVector3(),
            Rotation = rotation,
            Mesh = prefab.MeshInstance.Mesh,
            Scale = new(scale, scale, scale),
        };
        var staticBody = new StaticBody();
        staticBody.AddChild(
            new CollisionShape() { Shape = MeshInstance.Mesh.CreateTrimeshShape() }
        );
        staticBody.AddToGroup(NodeGroups.DIGGABLE);
        MeshInstance.AddChild(staticBody);
        MeshData = MeshUtils.GetMeshData(MeshInstance);
    }
}
