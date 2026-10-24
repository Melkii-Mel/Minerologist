using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

public class EntityNodesContainer : GodotNodeContainer
{
    private readonly ConcurrentDictionary<Vector3I, Spatial> _entitiesChunkCache = [];
    private readonly HashSet<Vector3I> _queueRemove = [];

    public void Load(Vector3I chunkPosition, EntitiesChunk entities)
    {
        DeferOrCall(() =>
        {
            if (_queueRemove.Remove(chunkPosition))
            {
                return;
            }
            if (_entitiesChunkCache.TryGetValue(chunkPosition, out var chunk))
            {
                if (chunk.GetParent() == null)
                {
                    AddChild(chunk);
                    return;
                }
            }
            var chunkParent = new Spatial();
            foreach (var entity in entities.Entities)
            {
                var meshInstance = entity.MeshInstance;
                if (entity.MeshInstance.GetParent() == null)
                {
                    chunkParent.AddChild(meshInstance);
                }
            }
            _entitiesChunkCache[chunkPosition] = chunkParent;
            AddChild(chunkParent);
        });
    }

    public void Unload(Vector3I chunkPosition)
    {
        if (_entitiesChunkCache.TryGetValue(chunkPosition, out var chunk))
        {
            chunk.GetParent()?.RemoveChild(chunk);
        }
        else
        {
            _queueRemove.Add(chunkPosition);
        }
    }
}
