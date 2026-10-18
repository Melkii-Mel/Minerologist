using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IMeshContainer
{
    void Defer(Action action);
}

public interface ITerrain<TUpdateSnapshot, TUpdateResult>
{
    public event Action<Vector3I>? ChunkChanged;

    public void Generate(Vector3I chunkPosition);
    public void Load(Vector3I chunkPosition);
    public void Unload(Vector3I chunkPosition);
    public bool IsGenerated(Vector3I chunkPosition);
    TUpdateSnapshot PrepareUpdate(Vector3I chunkPosition);
    TUpdateResult ExecuteUpdate(Vector3I chunkPosition, TUpdateSnapshot snapshot);
    void FinalizeUpdate(Vector3I chunkPosition, TUpdateResult result);
}

public class TerrainStreamer<TTerrainUpdateSnapshot, TTerrainUpdateResult>
{
    public const int CHUNK_UPDATE_PRIORITY = 999;

    public int IsoLevel { get; private set; } = 0;

    public Vector3I ChunkSize { get; private set; }

    private readonly int _loadRadius;
    private readonly int _loadHeight;

    private readonly Scheduler _scheduler = new(0.75f);

    private readonly ConcurrentDictionary<Vector3I, bool> _instantiatedChunks = [];
    private readonly Dictionary<Vector3I, int> _queuedChunks = [];

    private readonly List<Vector3I> _dirty = [];
    private int _lastUpdateChunkCount;
    private readonly ConcurrentBag<(
        Vector3I position,
        TTerrainUpdateResult updateResult
    )> _updateResults = [];

    private readonly ITerrain<TTerrainUpdateSnapshot, TTerrainUpdateResult> _terrain;

    private readonly WorldChunkCoordinatesConverter _worldChunkCoordinatesConverter;

    public TerrainStreamer(
        Vector3I chunkSize,
        int loadRadius,
        int loadHeight,
        ITerrain<TTerrainUpdateSnapshot, TTerrainUpdateResult> terrain
    )
    {
        ChunkSize = chunkSize;
        _worldChunkCoordinatesConverter = new(chunkSize);

        _loadRadius = loadRadius;
        _loadHeight = loadHeight;

        _terrain = terrain;
        terrain.ChunkChanged += MarkDirty;
    }

    public void Update(Vector3 worldPosition)
    {
        Update(_worldChunkCoordinatesConverter.WorldToChunk(new Vector3I(worldPosition)));
    }

    private int _framesBetweenUpdates = 0;

    public void Update(Vector3I centerChunk)
    {
        var requiredChunks = new HashSet<Vector3I>();

        if (_lastUpdateChunkCount == _updateResults.Count)
        {
            // GD.Print("Frames between last updates: ", _framesBetweenUpdates);
            _framesBetweenUpdates = 0;
            UpdateDirtyChunks();
        }
        else
        {
            _framesBetweenUpdates++;
        }

        var loadRadiusSquared = _loadRadius * _loadRadius;

        lock (_lock)
        {
            for (var x = -_loadRadius; x <= _loadRadius; x++)
            {
                for (var y = -_loadHeight; y <= _loadHeight; y++)
                {
                    for (var z = -_loadRadius; z <= _loadRadius; z++)
                    {
                        var vec = new Vector3I(x, y, z);

                        var chunk = centerChunk + vec;

                        var distanceSquared = (int)new Vector2(x, z).LengthSquared();

                        var chunkIsClose =
                            (
                                chunk.X == centerChunk.X
                                && chunk.Z == centerChunk.Z
                                && chunk.Y == centerChunk.Y
                            ) || chunk.IsNeighborOf(centerChunk);

                        if (distanceSquared > loadRadiusSquared)
                        {
                            continue;
                        }

                        requiredChunks.Add(chunk);

                        if (_instantiatedChunks.ContainsKey(chunk))
                        {
                            continue;
                        }

                        if (!_terrain.IsGenerated(chunk))
                        {
                            _terrain.Generate(chunk);
                        }

                        QueueChunkLoadOrUpdatePriority(
                            chunk,
                            urgent: chunkIsClose,
                            priority: _loadRadius - distanceSquared
                        );
                    }
                }
            }

            foreach (var chunk in _instantiatedChunks.Keys.Concat(_queuedChunks.Keys).ToList())
            {
                if (!requiredChunks.Contains(chunk))
                {
                    UnloadOrDequeue(chunk);
                }
            }
        }
    }

    private readonly object _lock = new();

    private void UpdateDirtyChunks()
    {
        while (_updateResults.TryTake(out var chunk))
        {
            _terrain.FinalizeUpdate(chunk.position, chunk.updateResult);
        }
        _lastUpdateChunkCount = 0;
        foreach (var chunk in _dirty)
        {
            if (!_instantiatedChunks.ContainsKey(chunk))
            {
                continue;
            }
            _lastUpdateChunkCount++;
            var preparedValue = _terrain.PrepareUpdate(chunk);
            _scheduler.QueueJob(
                () => _updateResults.Add((chunk, _terrain.ExecuteUpdate(chunk, preparedValue))),
                CHUNK_UPDATE_PRIORITY
            );
        }
        _dirty.Clear();
    }

    private void QueueChunkLoadOrUpdatePriority(Vector3I chunkPosition, bool urgent, int priority)
    {
        if (urgent)
        {
            TryDequeue(chunkPosition);
            _terrain.Load(chunkPosition);
            _instantiatedChunks.TryAdd(chunkPosition, true);
            return;
        }
        if (_queuedChunks.ContainsKey(chunkPosition))
        {
            _scheduler.TryUpdatePriority(_queuedChunks[chunkPosition], priority);
            return;
        }
        var id = _scheduler.QueueJob(
            () =>
            {
                _terrain.Load(chunkPosition);
                _instantiatedChunks.TryAdd(chunkPosition, true);
            },
            priority
        );
        _queuedChunks[chunkPosition] = id;
    }

    private Action? TryDequeue(Vector3I chunk)
    {
        lock (_lock)
        {
            if (_queuedChunks.ContainsKey(chunk))
            {
                var id = _queuedChunks[chunk];
                _queuedChunks.Remove(chunk);
                if (_scheduler.TryCancelJob(id, out var action))
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
        if (_instantiatedChunks.TryRemove(chunk, out _))
        {
            _terrain.Unload(chunk);
        }
    }

    private void MarkDirty(Vector3I chunkPosition)
    {
        if (!_dirty.Contains(chunkPosition))
        {
            _dirty.Add(chunkPosition);
        }
    }
}
