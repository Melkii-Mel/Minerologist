using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class EntityVariantsContainer
{
    public static List<EntityConfiguration> EntityConfigurations = [];
    public static Dictionary<
        (EntityPrefab, EntityConfiguration),
        BakedEntityData
    > BakedEntityData = [];

    private static readonly Random _rng = new();

    public static int ConfigurationsCount =>
        Config.Instance.GenerationConfig.EntitiesGenerationConfig.EntityVariationsDetalization;

    public static void Init()
    {
        if (!MeshCollection.Initialized)
        {
            throw new InvalidOperationException(
                "Mesh collection must be initialized before initializing entity variants collection"
            );
        }
        GenerateConfigurations();
        MapConfigurationsToEntities();
    }

    public static BakedEntityData RandomForPrefab(EntityPrefab entityPrefab, Random? rng = default)
    {
        rng ??= _rng;
        var entityConfiguration = RandomUtils.ChooseRandomItem(EntityConfigurations, rng);
        return BakedEntityData[(entityPrefab, entityConfiguration)];
    }

    private static void MapConfigurationsToEntities()
    {
        foreach (var prefab in EntityPrefabs.All())
        {
            foreach (var configuration in EntityConfigurations)
            {
                BakedEntityData[(prefab, configuration)] = BakeEntityData(prefab, configuration);
            }
        }
    }

    private static void GenerateConfigurations()
    {
        for (var i = 0; i < ConfigurationsCount; i++)
        {
            EntityConfigurations.Add(
                new EntityConfiguration(
                    RandomUtils.Rotation(_rng),
                    RandomUtils.FromRange(0, 1, _rng)
                )
            );
        }
    }

    private static BakedEntityData BakeEntityData(
        EntityPrefab prefab,
        EntityConfiguration configuration
    )
    {
        var sizes = Config.Instance.RemnantsSizeConfig.Sizes;
        var sizeFound =
            sizes.TryGetValue(prefab.Type + "." + prefab.Id, out var size)
            || sizes.TryGetValue(prefab.Type, out size)
            || sizes.TryGetValue(prefab.Id, out size);
        if (!sizeFound)
        {
            size = new(1, 1);
        }

        var rotation = configuration.Rotation;
        var scale = size.Min + configuration.Scale * (size.Max - size.Min);

        var meshInstance = new MeshInstance
        {
            Rotation = rotation,
            Mesh = prefab.MeshInstance.Mesh,
            Scale = new(scale, scale, scale),
        };

        var meshData = MeshUtils.GetMeshData(meshInstance);
        var supportingTiles = CalculateSupportingTiles(meshData).ToArray();

        var bakedData = new BakedEntityData(supportingTiles, meshInstance);
        return bakedData;
    }

    private static IEnumerable<Vector3I> CalculateSupportingTiles(Surface[] meshData)
    {
        var meshAabb = MeshUtils.FindAabb(meshData);
        var candidateVoxelPositions = MeshUtils.GetCandidatePoints(meshAabb);

        foreach (var candidateVoxelPosition in candidateVoxelPositions)
        {
            var voxelBounds = new Bounds(candidateVoxelPosition, Vector3I.One);

            if (
                meshData
                    .SelectMany(d => d.Triangles)
                    .Any(t => MeshUtils.TestTriangleAabb(t, voxelBounds))
            )
            {
                yield return candidateVoxelPosition;
            }
        }
    }
}

public readonly record struct EntityConfiguration(Vector3 Rotation, float Scale);

public record BakedEntityData(Vector3I[] EntitySupporingTiles, MeshInstance EntityInstance);
