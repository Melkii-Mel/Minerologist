using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Terrain : ITerrain<VoxelGrid, GodotMeshBuilder>, IVoxelGridContainer
{
    public Session Session { get; }

    public static readonly int OffsetX = -Chunk.Size.X / 2;
    public static readonly int OffsetY = -Chunk.Size.Y / 2;
    public static readonly int OffsetZ = -Chunk.Size.Z / 2;

    public static readonly int SurfaceChunkYLevel = 0;

    private readonly Random _rng;

    private readonly ConcurrentDictionary<Vector3I, Chunk> _chunks = [];

    private readonly object _lock = new();

    private readonly WorldChunkCoordinatesConverter _worldChunkCoordinatesConverter;

    private readonly TerrainMeshContainer _terrainMeshContainer;

    public Vector3I ChunkSize => Config.Instance.GenerationConfig.TerrainGenerationConfig.ChunkSize;

    public int ChunkSizeFlat => ChunkSize.Product();

    public event Action<Vector3I>? ChunkChanged;
    public event Action<Entity>? EntityRemoved;

    private readonly List<float> _cellHealths = [];

    public Node Parent { get; }

    public DiggablesTerrain DiggablesTerrain { get; }

    public Terrain(
        Node parent,
        Session session,
        DiggablesTerrain diggablesTerrain,
        Random? rng = null
    )
    {
        Parent = parent;
        Session = session;
        DiggablesTerrain = diggablesTerrain;

        _worldChunkCoordinatesConverter = new(ChunkSize);

        _rng = rng ?? new();

        _terrainMeshContainer = new(MaterialProvider.CreateTerrainMaterial());
        parent.AddChild(_terrainMeshContainer);
    }

    public float? TryGetCellValue(Vector3I worldPosition)
    {
        if (!TryGetChunkAndLocalPos(worldPosition, out var chunk, out var localPos))
        {
            return null;
        }

        return chunk.VoxelGrid.Sample(localPos);
    }

    private readonly List<Vector3I> _changedChunks = [];

    public void FlushChunkChangeEvents()
    {
        foreach (var changedChunkPos in _changedChunks)
        {
            ChunkChanged?.Invoke(changedChunkPos);
        }
        _changedChunks.Clear();
    }

    public bool TryChangeCellValue(
        Vector3I worldPosition,
        float delta,
        bool notify = true,
        bool onlyIfContactsAir = false
    )
    {
        if (!TryGetChunkAndLocalPos(worldPosition, out var chunk, out var localPos))
        {
            return false;
        }

        var isoLevel = Config.Instance.MarchingCubesOptions.IsoLevel;

        var currentHealth = chunk.VoxelGrid.Sample(localPos);

        var depth = -worldPosition.Y;

        var maxCellHealth = CellHealthForChunk(chunk.VoxelGrid.ChunkPosition.Y);

        var minHealth =
            Config.Instance.GenerationConfig.TerrainGenerationConfig.MinCellHealthMultiplier
            * maxCellHealth;

        if (onlyIfContactsAir && !ContactsAir(worldPosition, isoLevel))
        {
            return false;
        }

        var newHealth = currentHealth + delta;

        float healthThreshold = Mathf.Max(
            Config.Instance.GenerationConfig.TerrainGenerationConfig.CellThresholdMultiplier
                * maxCellHealth,
            0
        );

        var healthDepleted = newHealth <= healthThreshold;

        var healthJustDepleted = healthDepleted && currentHealth >= healthThreshold;

        if (healthDepleted)
        {
            newHealth = Mathf.Clamp(newHealth, Mathf.Min(minHealth, isoLevel), isoLevel);
        }

        if (!healthDepleted && _rng.NextDouble() > 0.9)
        {
            var particlesSettings = new DigParticles.Settings()
            {
                Direction =
                    Session.Player.PlayerNode.Center.GlobalPosition - worldPosition.ToGdVector3(),
                Spread = 35,
                MaxCount = 8,
            };
            Session.DigParticles.Emit(worldPosition.ToGdVector3(), particlesSettings);
        }

        chunk.VoxelGrid.Set(localPos, newHealth);
        InvokeOrCollectChunkChange(chunk.VoxelGrid.ChunkPosition, notify);

        foreach (var neighbor in chunk.VoxelGrid.DrainInvalidatedNeighbors())
        {
            if (_terrainMeshContainer.ContainsMesh(neighbor.ChunkPosition))
            {
                InvokeOrCollectChunkChange(neighbor.ChunkPosition, notify);
            }
        }

        if (
            healthJustDepleted
            && DiggablesTerrain.GlobalTileSupportedEntitiesIndex.TryGetValue(
                worldPosition,
                out var entities
            )
        )
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                var e = entities[i];
                DiggablesTerrain.GlobalEntitySupportingTilesIndex[e].Remove(worldPosition);
                var entitySupported = DiggablesTerrain
                    .GlobalEntitySupportingTilesIndex[e]
                    .Any(t =>
                        (TryGetCellValue(t) ?? float.MinValue)
                        > Config.Instance.MarchingCubesOptions.IsoLevel
                    );
                if (!entitySupported)
                {
                    RemoveEntity(e);
                }
            }
        }

        GD.Print(currentHealth);

        return true;
    }

    private bool ContactsAir(Vector3I pos, float isoLevel)
    {
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        for (var z = -1; z <= 1; z++)
        {
            if (x == 0 && y == 0 && z == 0)
                continue;
            var v = TryGetCellValue(pos + new Vector3I(x, y, z));
            if (!v.HasValue || v.Value <= isoLevel)
                return true;
        }
        return false;
    }

    public void RemoveEntity(Entity e)
    {
        e.MeshInstance.GetParent().RemoveChild(e.MeshInstance);
        EntityRemoved?.Invoke(e);
        foreach (var tile in DiggablesTerrain.GlobalEntitySupportingTilesIndex[e])
        {
            var list = DiggablesTerrain.GlobalTileSupportedEntitiesIndex[tile];
            list.Remove(e);
            if (list.Count == 0)
            {
                DiggablesTerrain.GlobalTileSupportedEntitiesIndex.Remove(tile);
            }
        }
        DiggablesTerrain.GlobalEntitySupportingTilesIndex.Remove(e);
    }

    private void InvokeOrCollectChunkChange(Vector3I chunkPosition, bool notify)
    {
        if (notify)
        {
            ChunkChanged?.Invoke(chunkPosition);
        }
        else
        {
            if (!_changedChunks.Contains(chunkPosition))
            {
                _changedChunks.Add(chunkPosition);
            }
        }
    }

    public bool TryGetChunkAndLocalPos(
        Vector3I worldPosition,
        out Chunk chunk,
        out Vector3I localPos
    )
    {
        var chunkPosition = WorldToChunk(worldPosition);
        localPos = worldPosition - ChunkToWorld(chunkPosition);
        chunk = TryGetChunk(chunkPosition)!;

        return chunk is not null;
    }

    private Chunk? _cache;

    public Chunk? TryGetChunk(Vector3I chunkPosition)
    {
        if (_cache != null && _cache.VoxelGrid.ChunkPosition == chunkPosition)
        {
            return _cache;
        }
        if (_chunks.TryGetValue(chunkPosition, out var chunk))
        {
            _cache = chunk;
            return chunk;
        }
        return null;
    }

    public void Generate(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            if (!_chunks.TryGetValue(chunkPosition, out _))
            {
                GetOrGenerateVoxelGrid(chunkPosition);
            }
        }
    }

    public Vector3I ChunkToWorld(Vector3I chunkPosition)
    {
        return _worldChunkCoordinatesConverter.ChunkToWorld(chunkPosition);
    }

    public void PrepareLoad(Vector3I chunkPosition) { }

    public void ExecuteLoad(Vector3I chunkPosition)
    {
        var chunk = _chunks[chunkPosition];
        _terrainMeshContainer.Commit(
            chunkPosition,
            CreatePreparedMeshBuilderForVoxelGrid(chunk.VoxelGrid)
        );
    }

    public void Unload(Vector3I chunkPosition)
    {
        _terrainMeshContainer.Unload(chunkPosition);
    }

    public bool IsGenerated(Vector3I chunkPosition)
    {
        return _chunks.ContainsKey(chunkPosition);
    }

    public Vector3I WorldToChunk(Vector3I worldPosition)
    {
        return _worldChunkCoordinatesConverter.WorldToChunk(worldPosition);
    }

    public VoxelGrid PrepareUpdate(Vector3I chunkPosition)
    {
        var chunk = _chunks[chunkPosition];
        return chunk.VoxelGrid.CreateSnapshot();
    }

    public GodotMeshBuilder ExecuteUpdate(Vector3I chunkPosition, VoxelGrid snapshot)
    {
        if (snapshot.IsEmpty(Config.Instance.MarchingCubesOptions.IsoLevel))
        {
            return new GodotMeshBuilder();
        }
        var meshBuilder = CreatePreparedMeshBuilderForVoxelGrid(snapshot);
        return meshBuilder;
    }

    public void FinalizeUpdate(Vector3I chunkPosition, GodotMeshBuilder meshBuilder)
    {
        _terrainMeshContainer.Commit(chunkPosition, meshBuilder);
    }

    private VoxelGrid GetOrGenerateVoxelGrid(Vector3I chunkPosition)
    {
        if (_chunks.TryGetValue(chunkPosition, out var chunk))
        {
            return chunk.VoxelGrid;
        }
        var grid = new VoxelGrid(
            chunkPosition,
            this,
            ChunkOfValues(CellHealthForChunk(chunkPosition.Y), ChunkSizeFlat)
        );
        _chunks[chunkPosition] = new Chunk(grid);
        return grid;
    }

    private float[] ChunkOfValues(float value, int size)
    {
        var values = new float[size];
        for (var i = 0; i < size; i++)
        {
            values[i] = value;
        }
        return values;
    }

    public int ChunkSizeY => Config.Instance.GenerationConfig.TerrainGenerationConfig.ChunkSize.Y;

    public float CellHealthForChunk(int chunkY)
    {
        var level = -chunkY;
        if (level <= 0)
            return 0;

        while (_cellHealths.Count <= level)
        {
            var l = _cellHealths.Count;
            _cellHealths.Add(
                Config.Instance.GenerationConfig.TerrainGenerationConfig.F.CellHealth.Calculate(
                    l * ChunkSize.Y
                )
            );
        }

        return _cellHealths[level];
    }

    private GodotMeshBuilder CreatePreparedMeshBuilderForVoxelGrid(VoxelGrid voxelGrid)
    {
        var meshBuilder = new GodotMeshBuilder();
        MarchingCubes.Generate(voxelGrid, meshBuilder, Config.Instance.MarchingCubesOptions);
        return meshBuilder;
    }

    public VoxelGrid? TryGetVoxelGrid(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                return chunk.VoxelGrid;
            }
            return null;
        }
    }

    VoxelGrid IVoxelGridContainer.GetOrCreateVoxelGrid(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            return GetOrGenerateVoxelGrid(chunkPosition);
        }
    }

    public void UnloadAll() { }
}

public class Chunk(VoxelGrid voxelGrid)
{
    public VoxelGrid VoxelGrid { get; } = voxelGrid;
    public static Vector3I Size =>
        Config.Instance.GenerationConfig.TerrainGenerationConfig.ChunkSize;
    public static int SizeFlat => Size.Product();

    public static IEnumerable<T> ForEachCellInChunk<T>(Func<Vector3I, T?> map)
    {
        for (var x = 0; x < Size.X; x++)
        {
            for (var y = 0; y < Size.Y; y++)
            {
                for (var z = 0; z < Size.Z; z++)
                {
                    var result = map(new(x, y, z));
                    if (result != null)
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}
