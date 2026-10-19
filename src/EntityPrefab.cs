using System;
using System.Linq;
using Godot;

public record EntityPrefab(string Type, MeshInstance MeshInstance)
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
            .Select(p => p.Value)
            .ToList();
        if (remainingPieces.Count == 0)
        {
            remainingPieces = [.. skeletonPieces.Select(p => p.Value)];
        }
        return new("Skeleton", RandomUtils.ChooseRandomItem(remainingPieces, rng));
    }

    public static EntityPrefab RandomOfType(string type, Random rng)
    {
        var pieces = MeshCollection.OtherDiggables[type];
        return new(type, RandomUtils.ChooseRandomItem([.. pieces.Values], rng));
    }
}

public record Entity(EntityPrefab Prefab, Vector3I GlobalPosition, Vector3 Rotation, float Scale)
{
    private MeshInstance? _globallyPositionedTransformedInstanceWithCollisionShape;

    public Vector3I LocalPosition =>
        GlobalPosition
        - _coordinatesConverter.ChunkToWorld(_coordinatesConverter.WorldToChunk(GlobalPosition));

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    // Must be called on Godot's main thread
    public MeshInstance GetGloballyPositionedTransformedInstanceWithCollisionShape()
    {
        if (_globallyPositionedTransformedInstanceWithCollisionShape == null)
        {
            var meshInstance = new MeshInstance
            {
                Position = GlobalPosition.ToGdVector3(),
                Rotation = Rotation,
                Mesh = Prefab.MeshInstance.Mesh,
                Scale = new(Scale, Scale, Scale),
            };
            var staticBody = new StaticBody();
            staticBody.AddChild(
                new CollisionShape() { Shape = meshInstance.Mesh.CreateTrimeshShape() }
            );
            meshInstance.AddChild(staticBody);
            _globallyPositionedTransformedInstanceWithCollisionShape = meshInstance;
        }
        return _globallyPositionedTransformedInstanceWithCollisionShape;
    }
}
