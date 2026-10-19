using System.Collections.Generic;
using Godot;

public class TerrainMeshContainer(Material material) : GodotNodeContainer
{
    private readonly Dictionary<Vector3I, MeshInstance> _instances = [];
    private readonly WorldChunkCoordinatesConverter _worldChunkCoordinatesConverter = new();

    public void Commit(Vector3I chunkPosition, GodotMeshBuilder builder)
    {
        DeferOrCall(() =>
        {
            var mesh = builder.Commit();
            if (!_instances.TryGetValue(chunkPosition, out var chunk))
            {
                chunk = CreateChunkNode(chunkPosition);
            }
            UpdateMesh(chunk, mesh);
        });
    }

    public MeshInstance CreateChunkNode(Vector3I chunkPosition)
    {
        var chunk = new MeshInstance
        {
            MaterialOverride = material,
            Translation = _worldChunkCoordinatesConverter.ChunkToWorld(chunkPosition).ToGdVector3(),
        };
        var body = new StaticBody();
        body.AddToGroup(NodeGroups.DIGGABLE);
        var collisionShape = new CollisionShape();
        body.AddChild(collisionShape);
        chunk.AddChild(body);
        AddChild(chunk);
        _instances[chunkPosition] = chunk;
        return chunk;
    }

    public void UpdateMesh(MeshInstance node, ArrayMesh? mesh)
    {
        ((CollisionShape)node.GetChild(0).GetChild(0)).Shape = mesh?.CreateTrimeshShape() ?? null;
        if (Config.Instance.GenerationConfig.XRay)
        {
            mesh = null;
        }
        node.Mesh = mesh;
    }

    public void Unload(Vector3I chunkPosition)
    {
        DeferOrCall(() =>
        {
            if (!_instances.ContainsKey(chunkPosition))
            {
                return;
            }
            _instances[chunkPosition]?.QueueFree();
            _instances.Remove(chunkPosition);
        });
    }

    public bool ContainsMesh(Vector3I chunkPosition)
    {
        if (!_instances.TryGetValue(chunkPosition, out var chunk))
        {
            return false;
        }
        return chunk != null;
    }
}
