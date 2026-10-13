using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class TerrainHolder
{
    private readonly Dictionary<Vector3I, Chunk> _loadedChunks = [];
    private readonly Terrain _terrain;
    private Dictionary<Vector3I, Chunk> _unloadedChunks = [];
    private HashSet<Vector3I> _requiredChunks = [];
    private Vector3I? _lastChunkCoordinates;

    public TerrainHolder(Action<Action<Vector3>> subscribeToLoadCenterUpdate, Vector3 loadCenter)
    {
        subscribeToLoadCenterUpdate(LoadCenterUpdate);
        _terrain = new();
    }

    public void LoadCenterUpdate(Vector3 loadCenter)
    {
        var chunkCoordinates = MapPositionToChunkCoordinates(loadCenter);
        if (!_lastChunkCoordinates.HasValue || _lastChunkCoordinates.Value != chunkCoordinates)
            _lastChunkCoordinates = chunkCoordinates;
        else
            return;
        UpdateRequiredChunks(loadCenter, chunkCoordinates);
        foreach (var pos in _loadedChunks.Keys.Where(c => !_requiredChunks.Contains(c)))
        {
            UnloadChunk(pos);
            UpdateNeighbors(pos);
        }
        foreach (var pos in _requiredChunks.Where(c => !_loadedChunks.ContainsKey(c)))
        {
            LoadChunk(pos);
            UpdateNeighbors(pos);
        }
    }

    private void LoadChunk(Vector3I pos)
    {
        _unloadedChunks.TryGetValue(pos, out var chunk);
        chunk ??= GenerateNewChunk(pos);
        _unloadedChunks.Remove(pos);
        _loadedChunks.Add(pos, chunk);
    }

    private void UnloadChunk(Vector3I pos)
    {
        var chunk = _loadedChunks[pos];
        _loadedChunks.Remove(pos);
        _unloadedChunks.Add(pos, chunk);

        // TODO: Remove Godot Objects
        // TODO: Figure out what to do with fossils and stuff
    }

    private Chunk GenerateNewChunk(Vector3I pos)
    {
        return _terrain.GenerateChunk(pos);
    }

    private void UpdateRequiredChunks(Vector3 loadCenter, Vector3I chunkCoordinates)
    {
        _requiredChunks = [];
        var radiusChunks = Mathf.CeilToInt(
            Config.Instance.GenerationConfig.HoleRadiusChunks / Chunk.SIZE_X
        );
        var boundingBoxSize = radiusChunks * 2;
        for (var i = 0; i < boundingBoxSize; i++)
        {
            for (var j = 0; j < boundingBoxSize; j++)
            {
                for (var k = 0; k < boundingBoxSize; k++)
                {
                    var newChunkCoordinates = new Vector3I(i, j, k) + chunkCoordinates;
                    if (ShouldGenerateChunk(newChunkCoordinates, loadCenter))
                    {
                        _requiredChunks.Add(chunkCoordinates);
                    }
                }
            }
        }
    }

    private bool ShouldGenerateChunk(
        Vector3I chunkCoordinates,
        Vector3 generationCenterWorldCoordinates
    )
    {
        var left = chunkCoordinates.X * Chunk.SIZE_X;
        var right = (chunkCoordinates.X + 1) * Chunk.SIZE_X;
        var top = chunkCoordinates.Y * Chunk.SIZE_Y;
        var bottom = (chunkCoordinates.Y + 1) * Chunk.SIZE_Y;
        var behind = chunkCoordinates.Z * Chunk.SIZE_Z;
        var front = (chunkCoordinates.Z + 1) * Chunk.SIZE_Z;

        var closestX = Mathf.Max(left, Mathf.Min(right, generationCenterWorldCoordinates.x));
        var closestY = Mathf.Max(top, Mathf.Min(bottom, generationCenterWorldCoordinates.y));
        var closestZ = Mathf.Max(behind, Mathf.Min(front, generationCenterWorldCoordinates.z));

        var distanceSquared = closestX * closestX + closestY * closestY + closestZ * closestZ;

        return distanceSquared <= Config.Instance.GenerationConfig.HoleRadiusChunks;
    }

    public Vector3I MapPositionToChunkCoordinates(Vector3 worldCoordinates)
    {
        return new Vector3(
            worldCoordinates.x / Chunk.SIZE_X,
            worldCoordinates.y / Chunk.SIZE_Y,
            worldCoordinates.z / Chunk.SIZE_Z
        ).ToVector3I();
    }
}
