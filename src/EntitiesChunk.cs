using System.Collections.Generic;
using System.Linq;
using Godot;

public class EntitiesChunk
{
    public Vector3I ChunkPosition { get; }
    public Vector3I GlobalPosition => ChunkPosition * Config.Instance.GenerationConfig.ChunkSize;
    public readonly HashSet<Vector3I> ChunksToObserve = [];
    public readonly Dictionary<MeshInstance, Entity> MeshInstanceEntities = [];
    public readonly Dictionary<Vector3I, Entity> Entities;
    public readonly Dictionary<Entity, List<Vector3I>> EntitySupportingTiles;
    public readonly Dictionary<Vector3I, List<Entity>> SupportingTileEntities;

    private readonly Terrain _terrain;
    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    public EntitiesChunk(Vector3I chunkPosition, IEnumerable<Entity> entities, Terrain terrain)
    {
        _terrain = terrain;
        ChunkPosition = chunkPosition;
        Entities = entities.ToDictionary(v => v.LocalPosition, v => v);
        MeshInstanceEntities = entities.ToDictionary(v => v.MeshInstance, v => v);
        EntitySupportingTiles = Entities.Values.ToDictionary(
            v => v,
            v => CalculateSupportingTiles(v).ToList()
        );
        SupportingTileEntities = MapSupportingTilesToEntities();
    }

    public IEnumerable<Vector3I> CalculateSupportingTiles(Entity entity)
    {
        var meshAabb = MeshUtils.FindAabb(entity.MeshData);
        var candidateVoxelPositions = MeshUtils.GetCandidatePoints(meshAabb);
        foreach (var candidateVoxelPosition in candidateVoxelPositions)
        {
            var voxelBounds = new Bounds(candidateVoxelPosition, Vector3I.One);
            if (
                entity
                    .MeshData.SelectMany(d => d.Triangles)
                    .Any(t => MeshUtils.TestTriangleAabb(t, voxelBounds))
            )
            {
                yield return candidateVoxelPosition;
                ChunksToObserve.Add(_coordinatesConverter.WorldToChunk(candidateVoxelPosition));
            }
        }
    }

    public void OnTileUpdate(Vector3I globalPosition)
    {
        if (!SupportingTileEntities.ContainsKey(globalPosition))
        {
            return;
        }
        foreach (var entity in SupportingTileEntities[globalPosition].ToArray())
        {
            UpdateEntity(entity);
        }
    }

    public bool UpdateEntity(Entity entity)
    {
        if (IsEntitySupported(entity))
        {
            return false;
        }
        RemoveEntity(entity);
        entity.MeshInstance.QueueFree();
        return true;
    }

    public bool IsEntitySupported(Entity entity)
    {
        return EntitySupportingTiles[entity]
            .Any(tile =>
                _terrain.TryGetCellValue(tile) > Config.Instance.MarchingCubesOptions.IsoLevel
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

    private Dictionary<Vector3I, List<Entity>> MapSupportingTilesToEntities()
    {
        var result = new Dictionary<Vector3I, List<Entity>>();
        foreach (var kvp in EntitySupportingTiles)
        {
            var entity = kvp.Key;
            var tiles = kvp.Value;
            foreach (var tile in tiles)
            {
                if (!result.ContainsKey(tile))
                {
                    result[tile] = [];
                }
                if (!result[tile].Contains(entity))
                {
                    result[tile].Add(entity);
                }
            }
        }
        return result;
    }
}
