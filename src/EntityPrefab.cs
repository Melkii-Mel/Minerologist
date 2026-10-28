using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class EntityPrefabs
{
    public static readonly Dictionary<string, Dictionary<string, EntityPrefab>> Skeletons = [];
    public static readonly Dictionary<string, Dictionary<string, EntityPrefab>> Others = [];
    public static bool Initialized { get; private set; } = false;

    public static void Init()
    {
        Initialized = true;

        foreach (var meshGroup in MeshCollection.Skeletons)
        {
            var skeleton = Skeletons[meshGroup.Key] = [];
            foreach (var mesh in meshGroup.Value)
            {
                skeleton[mesh.Key] = new(meshGroup.Key, mesh.Key, mesh.Value);
            }
        }

        foreach (var meshGroup in MeshCollection.OtherDiggables)
        {
            var diggable = Others[meshGroup.Key] = [];
            foreach (var mesh in meshGroup.Value)
            {
                diggable[mesh.Key] = new(meshGroup.Key, mesh.Key, mesh.Value);
            }
        }
    }

    public static IEnumerable<EntityPrefab> All()
    {
        if (!Initialized)
        {
            Init();
        }
        return Skeletons.Values.Concat(Others.Values).SelectMany(dictionary => dictionary.Values);
    }
}

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
        var skeletonPieces = EntityPrefabs.Skeletons[MuseumCommunication.CurrentSkeleton];
        var remainingPieces = skeletonPieces
            .Where(p => !MuseumCommunication.CollectedSkeletonPieces.Contains(p.Key))
            .ToList();
        if (remainingPieces.Count == 0)
        {
            remainingPieces = [.. skeletonPieces];
        }
        var piece = RandomUtils.ChooseRandomItem(remainingPieces, rng).Value;
        return piece;
    }

    public static EntityPrefab RandomOfType(string type, Random rng)
    {
        var pieces = EntityPrefabs.Others[type];
        var piece = RandomUtils.ChooseRandomItem([.. pieces.Values], rng);
        return piece;
    }
}

public class Entity
{
    public Vector3I GlobalPosition { get; }
    public Vector3I LocalPosition { get; }
    public Vector3I ChunkPosition { get; }

    public EntityPrefab Prefab { get; }

    public MeshInstance MeshInstance { get; }
    public Vector3I[] SupportingTiles { get; }

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    private readonly StaticBody _staticBody;

    // Must be called on Godot's main thread
    public Entity(Vector3I globalPosition, BakedEntityData entityData, EntityPrefab prefab)
    {
        Prefab = prefab;

        GlobalPosition = globalPosition;
        ChunkPosition = _coordinatesConverter.WorldToChunk(GlobalPosition);
        LocalPosition = GlobalPosition - _coordinatesConverter.ChunkToWorld(ChunkPosition);

        MeshInstance = (MeshInstance)entityData.EntityInstance.Duplicate();
        SupportingTiles = new Vector3I[entityData.EntitySupporingTiles.Length];

        MeshInstance.Position += GlobalPosition.ToGdVector3();
        for (var i = 0; i < entityData.EntitySupporingTiles.Length; i++)
        {
            var cell = entityData.EntitySupporingTiles[i];
            SupportingTiles[i] = cell + globalPosition;
        }

        _staticBody = new StaticBody();
        _staticBody.AddChild(
            new CollisionShape() { Shape = MeshInstance.Mesh.CreateTrimeshShape() }
        );
        _staticBody.AddToGroup(NodeGroups.DIGGABLE);

        MeshInstance.AddChild(_staticBody);
    }

    public void DestroyCollider()
    {
        _staticBody.QueueFree();
    }

    public static Entity RandomFromPrefab(
        Vector3I globalPosition,
        EntityPrefab prefab,
        Random? rng = default
    )
    {
        var bakedEntityData = EntityVariantsContainer.RandomForPrefab(prefab, rng);
        return new(globalPosition, bakedEntityData, prefab);
    }
}
