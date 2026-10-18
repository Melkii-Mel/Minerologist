using System;
using System.Collections.Generic;
using System.Linq;

public class EntitiesChunk
{
    public Vector3I ChunkPosition { get; }
    public Vector3I GlobalPosition => ChunkPosition * Config.Instance.GenerationConfig.ChunkSize;
    public readonly Dictionary<Vector3I, Entity> Entities;
    public readonly Dictionary<Entity, HashSet<Vector3I>> EntitySupportingTiles;
    public readonly Dictionary<Vector3I, HashSet<Entity>> SupportingTileEntities;

    private readonly VoxelGrid _voxelGrid;

    public EntitiesChunk(Vector3I chunkPosition, IEnumerable<Entity> entities, VoxelGrid voxelGrid)
    {
        _voxelGrid = voxelGrid;
        ChunkPosition = chunkPosition;
        Entities = entities.ToDictionary(v => v.LocalPosition, v => v);
        EntitySupportingTiles = Entities.Values.ToDictionary(
            v => v,
            v => CalculateSupportingTiles(v).ToHashSet()
        );
        SupportingTileEntities = MapSupportingTilesToEntities();
    }

    public IEnumerable<Vector3I> CalculateSupportingTiles(Entity entity)
    {
        var meshInstance = entity.GloballyPositionedTransformedInstanceWithCollisionShape;
        var meshAabb = MeshUtils.FindAabb(meshInstance);
        var candidateVoxelPositions = MeshUtils.GetCandidatePoints(meshAabb);
        foreach (var candidateVoxelPosition in candidateVoxelPositions)
        {
            var voxelBounds = new Bounds(candidateVoxelPosition, Vector3I.One);
            foreach (var triangle in MeshUtils.GetTriangles(meshInstance))
            {
                if (MeshUtils.TestTriangleAabb(triangle, voxelBounds))
                {
                    yield return candidateVoxelPosition;
                }
            }
        }
    }

    public void CheckTileUpdate(Vector3I globalPosition)
    {
        foreach (var entity in SupportingTileEntities[globalPosition])
        {
            if (!IsEntityUnsupported(entity))
            {
                continue;
            }
            RemoveEntity(entity);
            entity.GloballyPositionedTransformedInstanceWithCollisionShape.QueueFree();
            AddToInventory(entity);
        }
    }

    private void AddToInventory(Entity entity)
    {
        throw new NotImplementedException();
    }

    public bool IsEntityUnsupported(Entity entity)
    {
        return EntitySupportingTiles[entity]
            .All(tile =>
                _voxelGrid.Sample(tile - GlobalPosition)
                < Config.Instance.MarchingCubesOptions.IsoLevel
            );
    }

    private void RemoveEntity(Entity entity)
    {
        var supportingTiles = EntitySupportingTiles[entity];
        foreach (var t in supportingTiles)
        {
            SupportingTileEntities[t].Remove(entity);
            if (SupportingTileEntities[t].Count == 0)
            {
                SupportingTileEntities.Remove(t);
            }
        }
        EntitySupportingTiles.Remove(entity);
        Entities.Remove(entity.LocalPosition);
    }

    private Dictionary<Vector3I, HashSet<Entity>> MapSupportingTilesToEntities()
    {
        var result = new Dictionary<Vector3I, HashSet<Entity>>();
        foreach (var kvp in EntitySupportingTiles)
        {
            var entity = kvp.Key;
            var tiles = kvp.Value;
            foreach (var tile in tiles)
            {
                result[tile] ??= [];
                result[tile].Add(entity);
            }
        }
        return result;
    }
}
