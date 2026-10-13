using System;
using System.Linq;
using Godot;

public record EntityPrefab(string Id, MeshInstance MeshInstance, bool IsSkeleton)
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
        var skeletonPieces = EntitiesCollection.Skeletons[Server.CurrentSkeleton];
        var remainingPieces = skeletonPieces
            .Where(p => !Server.CollectedSkeletonPieces.Contains(p.Key))
            .Select(p => p.Value)
            .ToList();
        if (remainingPieces.Count == 0)
        {
            remainingPieces = [.. skeletonPieces.Select(p => p.Value)];
        }
        return RandomUtils.ChooseRandomItem(remainingPieces, rng);
    }

    public static EntityPrefab RandomOfType(string type, Random rng)
    {
        var pieces = EntitiesCollection.Others[type];
        return RandomUtils.ChooseRandomItem([.. pieces.Values], rng);
    }
}

public record Entity(
    EntityPrefab Prefab,
    EntitiesChunk Chunk,
    Vector3I LocalPosition,
    Vector3 Rotation,
    float Scale
);
