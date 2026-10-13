using System.Collections.Generic;
using System.Linq;

public class EntitiesChunk
{
    public Vector3I ChunkPosition { get; }
    public Vector3I GlobalPosition => ChunkPosition * Config.Instance.GenerationConfig.ChunkSize;
    public readonly Dictionary<Vector3I, Entity> Entities;
    public readonly Dictionary<Entity, Vector3I> Positions;
    public readonly Dictionary<Entity, HashSet<Vector3I>> EntitySupportingTiles;
    public readonly Dictionary<Vector3I, HashSet<Entity>> SupportingTileEntities;

    public EntitiesChunk(Vector3I chunkPosition, Dictionary<Vector3I, Entity> entities)
    {
        ChunkPosition = chunkPosition;
        Entities = entities;
        Positions = entities.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        EntitySupportingTiles = Entities.Values.ToDictionary(
            v => v,
            v => CalculateSupportingTiles(v).ToHashSet()
        );
        SupportingTileEntities = MapSupportingTilesToEntities();
    }

    public IEnumerable<Vector3I> CalculateSupportingTiles(Entity entity)
    {
        var mesh = entity.Prefab.MeshInstance.Mesh;
        var localPosition = entity.LocalPosition;
        var globalPosition = localPosition + entity.Chunk.GlobalPosition;
        mesh = MeshUtils.Transformed(
            mesh,
            entity.LocalPosition.ToVector3(),
            entity.Rotation,
            entity.Scale
        );
        var aabb = MeshUtils.FindAabb(mesh, globalPosition);
        var candidates = MeshUtils.GetCandidatePoints(aabb);
        foreach (var triangle in MeshUtils.GetTriangles(mesh))
        {
            foreach (var candidate in candidates)
            {
                if (MeshUtils.TestTriangleAabb(triangle, aabb))
                {
                    yield return candidate;
                }
            }
        }
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
