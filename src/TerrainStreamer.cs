using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IMeshContainer
{
    void Defer(Action action);
}

public interface ITerrainGenerator
{
    public VoxelGrid Generate(IVoxelGridContainer container, Vector3I chunkPosition);
}

public class TerrainStreamer<TMeshContainer> : IVoxelGridContainer
    where TMeshContainer : Node, IMeshContainer
{
    public const int CHUNK_UPDATE_PRIORITY = 999;

    public int IsoLevel { get; private set; } = 0;

    public Vector3I ChunkSize { get; private set; }
    public int ChunkSizeFlat => IVoxelGridContainer.ChunkSizeFlatDefault(ChunkSize);

    private readonly TMeshContainer _meshContainer;
    private readonly Material _material;

    private readonly int _loadRadius;

    private readonly Dictionary<Vector3I, VoxelGrid> _grids = [];
    private readonly Dictionary<Vector3I, MeshInstance?> _instances = [];
    private readonly Scheduler _dispatcher = new(0.5f);
    private readonly Dictionary<Vector3I, int> _queuedChunks = [];

    private readonly HashSet<Vector3I> _dirty = [];
    private volatile int _lastUpdateChunkCount;
    private readonly ConcurrentBag<(Vector3I position, GodotMeshBuilder builder)> _finishedMeshes =
    [];
    private readonly ConcurrentDictionary<Vector3I, VoxelGrid> _snapshots = [];
    private readonly ITerrainGenerator _terrainGenerator;

    public TerrainStreamer(
        TMeshContainer meshContainer,
        Material material,
        Vector3I chunkSize,
        int loadRadius,
        ITerrainGenerator terrainGenerator
    )
    {
        _meshContainer = meshContainer;
        _material = material;
        ChunkSize = chunkSize;
        _loadRadius = loadRadius;
        _terrainGenerator = terrainGenerator;

        return;
    }

    public void Update(Vector3 worldPosition)
    {
        var centerChunk = WorldToChunk(worldPosition);

        var required = new HashSet<Vector3I>();

        if (_lastUpdateChunkCount == _finishedMeshes.Count)
        {
            ApplyChunkUpdate();
            _lastUpdateChunkCount = 0;
            _snapshots.Clear();
            foreach (var chunk in _dirty)
            {
                _lastUpdateChunkCount++;
                _snapshots[chunk] = _grids[chunk].CreateSnapshot();
                _dispatcher.QueueJob(() => BuildChunkToBag(chunk), CHUNK_UPDATE_PRIORITY);
            }
        }

        _dirty.Clear();

        var loadRadiusSquared = _loadRadius * _loadRadius;

        lock (_lock)
        {
            for (var x = -_loadRadius; x <= _loadRadius; x++)
            {
                for (var y = -_loadRadius; y <= _loadRadius; y++)
                {
                    for (var z = -_loadRadius; z <= _loadRadius; z++)
                    {
                        var vec = new Vector3I(x, y, z);
                        var chunk = centerChunk + vec;
                        var distanceSquared = (int)vec.ToGdVector3().LengthSquared();
                        if (distanceSquared > loadRadiusSquared)
                        {
                            continue;
                        }

                        required.Add(chunk);

                        if (_instances.ContainsKey(chunk))
                        {
                            continue;
                        }
                        QueueChunkLoadOrUpdatePriority(
                            chunk,
                            urgent: (chunk.X == centerChunk.X && chunk.Z == centerChunk.Z)
                                || chunk.IsNeighborOf(centerChunk),
                            priority: _loadRadius - distanceSquared
                        );
                    }
                }
            }

            foreach (var chunk in _instances.Keys.Concat(_queuedChunks.Keys).ToList())
            {
                if (!required.Contains(chunk))
                {
                    UnloadOrDequeue(chunk);
                }
            }
        }
    }

    private void BuildChunkToBag(Vector3I chunk)
    {
        var builder = new GodotMeshBuilder();
        Generate(chunk, () => _finishedMeshes.Add((chunk, builder)), builder);
    }

    private void ApplyChunkUpdate()
    {
        while (_finishedMeshes.TryTake(out var mesh))
        {
            Commit(mesh.position, mesh.builder);
        }
    }

    private readonly object _lock = new();

    private void QueueChunkLoadOrUpdatePriority(Vector3I chunkPosition, bool urgent, int priority)
    {
        var builder = new GodotMeshBuilder();
        if (urgent)
        {
            TryDequeue(chunkPosition);
            Generate(chunkPosition, CommitOrDefer, builder);
            return;
        }
        if (!_queuedChunks.ContainsKey(chunkPosition))
        {
            var id = _dispatcher.QueueJob(
                () => Generate(chunkPosition, CommitOrDefer, builder),
                priority
            );
            _queuedChunks[chunkPosition] = id;
        }
        else
        {
            if (_queuedChunks.ContainsKey(chunkPosition))
            {
                _dispatcher.TryUpdatePriority(_queuedChunks[chunkPosition], priority);
            }
        }
        void CommitOrDefer()
        {
            if (!urgent)
            {
                _meshContainer.Defer(() => Commit(chunkPosition, builder));
            }
            else
            {
                Commit(chunkPosition, builder);
            }
        }
    }

    private void Commit(Vector3I chunkPosition, GodotMeshBuilder builder)
    {
        lock (_lock)
        {
            if (_instances.ContainsKey(chunkPosition))
            {
                var chunk = _instances[chunkPosition];
                _instances.Remove(chunkPosition);
                chunk?.QueueFree();
            }
        }
        var mesh = builder.Commit();
        if (mesh is null)
        {
            SetLoaded(chunkPosition, null);
            return;
        }
        var instance = new MeshInstance
        {
            Mesh = mesh,
            MaterialOverride = _material,
            Translation = ChunkToWorld(chunkPosition).ToGdVector3(),
        };
        var body = new StaticBody();
        body.AddChild(new CollisionShape { Shape = mesh.CreateTrimeshShape() });
        instance.AddChild(body);
        _meshContainer.AddChild(instance);

        SetLoaded(chunkPosition, instance);
    }

    private void Generate(Vector3I chunkPosition, Action commit, GodotMeshBuilder builder)
    {
        var grid = GetOrGenerateGrid(chunkPosition);
        if (grid.IsEmpty(IsoLevel))
        {
            commit();
            return;
        }
        MarchingCubes.Generate(
            grid,
            builder,
            new()
            {
                IsoLevel = IsoLevel,
                GenerateNormals = true,
                WeldVertices = true,
            }
        );
        commit();
    }

    private Action? TryDequeue(Vector3I chunk)
    {
        lock (_lock)
        {
            if (_queuedChunks.ContainsKey(chunk))
            {
                var id = _queuedChunks[chunk];
                _queuedChunks.Remove(chunk);
                if (_dispatcher.TryCancelJob(id, out var action))
                {
                    return action;
                }
            }
            return null;
        }
    }

    private void UnloadOrDequeue(Vector3I chunk)
    {
        TryDequeue(chunk);
        UnloadChunk(chunk);
    }

    private void SetLoaded(Vector3I chunk, MeshInstance? instance)
    {
        lock (_lock)
        {
            _queuedChunks.Remove(chunk);
            _instances.Add(chunk, instance);
        }
    }

    private MeshInstance? SetUnloaded(Vector3I chunk)
    {
        lock (_lock)
        {
            if (_instances.ContainsKey(chunk))
            {
                var instance = _instances[chunk];
                _instances.Remove(chunk);
                return instance;
            }
            return null;
        }
    }

    private VoxelGrid? TryGetGrid(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            if (_grids.TryGetValue(chunkPosition, out var grid))
                return grid;
            return null;
        }
    }

    private VoxelGrid GetOrGenerateGrid(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            if (_snapshots.TryGetValue(chunkPosition, out var grid))
            {
                return grid;
            }
            if (_grids.TryGetValue(chunkPosition, out grid))
            {
                return grid;
            }
        }

        var generated = _terrainGenerator.Generate(this, chunkPosition);

        lock (_lock)
        {
            if (_grids.TryGetValue(chunkPosition, out var grid))
            {
                return grid;
            }
            _grids[chunkPosition] = generated;
            return generated;
        }
    }

    private void UnloadChunk(Vector3I chunkPosition)
    {
        var instance = SetUnloaded(chunkPosition);
        instance?.QueueFree();
    }

    public Vector3I WorldToChunk(Vector3 worldPosition)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPosition.x / ChunkSize.X),
            Mathf.FloorToInt(worldPosition.y / ChunkSize.Y),
            Mathf.FloorToInt(worldPosition.z / ChunkSize.Z)
        );
    }

    public Vector3I WorldToChunk(Vector3I worldPosition)
    {
        return new Vector3I(
            Mathf.FloorToInt((float)worldPosition.X / ChunkSize.X),
            Mathf.FloorToInt((float)worldPosition.Y / ChunkSize.Y),
            Mathf.FloorToInt((float)worldPosition.Z / ChunkSize.Z)
        );
    }

    public Vector3I ChunkToWorld(Vector3I chunkPosition)
    {
        return new Vector3I(
            chunkPosition.X * ChunkSize.X,
            chunkPosition.Y * ChunkSize.Y,
            chunkPosition.Z * ChunkSize.Z
        );
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var instance in _instances.Values)
                instance?.QueueFree();

            _instances.Clear();
            _grids.Clear();
        }
    }

    VoxelGrid IVoxelGridContainer.GetOrCreateVoxelGrid(Vector3I chunkPosition)
    {
        return GetOrGenerateGrid(chunkPosition);
    }

    VoxelGrid? IVoxelGridContainer.TryGetVoxelGrid(Vector3I chunkPosition)
    {
        return TryGetGrid(chunkPosition);
    }

    public VoxelGrid GetChunk(Vector3I chunkPosition)
    {
        return _grids[chunkPosition];
    }

    public void MarkDirty(Vector3I chunkPosition)
    {
        _dirty.Add(chunkPosition);
    }
}
