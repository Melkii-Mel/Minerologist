using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

public class Terrain : ITerrain<VoxelGrid, GodotMeshBuilder>, IVoxelGridContainer
{
    public static readonly int OffsetX = -Chunk.Size.X / 2;
    public static readonly int OffsetY = -Chunk.Size.Y / 2;
    public static readonly int OffsetZ = -Chunk.Size.Z / 2;

    public static readonly int SurfaceChunkYLevel = 0;

    private readonly Random _rng;

    private readonly OpenSimplexNoise _noise;

    private readonly ConcurrentDictionary<Vector3I, Chunk> _chunks = [];

    // TODO : Cleanup when entity is removed
    public readonly Dictionary<Vector3I, List<Entity>> GlobalTileSupportedEntitiesIndex = [];
    public readonly Dictionary<Entity, List<Vector3I>> GlobalEntitySupportingTilesIndex = [];
    public readonly Dictionary<MeshInstance, Entity> GlobalMeshInstanceEntityMap = [];

    private readonly object _lock = new();

    private readonly WorldChunkCoordinatesConverter _worldChunkCoordinatesConverter;

    private readonly TerrainMeshContainer _terrainMeshContainer;
    private readonly EntityNodesContainer _entityNodesContainer;

    public Vector3I ChunkSize => Config.Instance.GenerationConfig.ChunkSize;

    public int ChunkSizeFlat => ChunkSize.Product();

    public event Action<Vector3I>? ChunkChanged;
    public event Action<Entity>? EntityRemoved;

    public Terrain(Node parent, int? seed = null)
    {
        _worldChunkCoordinatesConverter = new(Config.Instance.GenerationConfig.ChunkSize);

        _rng = seed.HasValue ? new(seed.Value) : new();

        var noiseConfig = Config.Instance.GenerationConfig.NoiseConfig;
        _noise = new OpenSimplexNoise
        {
            Seed = (int)GD.Randi(),
            Octaves = noiseConfig.Octaves,
            Period = noiseConfig.Period,
            Persistence = noiseConfig.Persistence,
            Lacunarity = noiseConfig.Lacunarity,
        };

        _terrainMeshContainer = new(MaterialProvider.CreateTerrainMaterial());
        _entityNodesContainer = new();
        parent.AddChild(_terrainMeshContainer);
        parent.AddChild(_entityNodesContainer);
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

        var currentHealth = chunk.VoxelGrid.Sample(localPos);

        // TODO : var minHealth = Config.Instance.GenerationConfig.MinCellHealth;
        var minHealth = -100;

        if (onlyIfContactsAir)
        {
            for (var x = -1; x < 2; x++)
            {
                for (var y = -1; y < 2; y++)
                {
                    for (var z = -1; z < 2; z++)
                    {
                        var offset = new Vector3I(x, y, z);

                        if (offset == Vector3I.Zero)
                        {
                            continue;
                        }

                        var value = TryGetCellValue(worldPosition + new Vector3I(x, y, z));
                        if (
                            !value.HasValue
                            || value.Value <= Config.Instance.MarchingCubesOptions.IsoLevel
                        )
                        {
                            goto BREAK;
                        }
                    }
                }
            }
            return false;
        }
        BREAK:

        var newHealth = currentHealth + delta;

        // TODO : if (newHealth < Config.Instance.GenerationConfig.CellHealthThreshold) {
        var healthDepleted = newHealth < 10;

        if (healthDepleted)
        {
            newHealth = Mathf.Clamp(
                newHealth,
                minHealth - 1,
                Config.Instance.MarchingCubesOptions.IsoLevel
            );
        }

        chunk.VoxelGrid.Set(localPos, newHealth);
        InvokeOrCollectChunkChange(chunk.VoxelGrid.ChunkPosition, notify);

        foreach (var neighbor in chunk.VoxelGrid.ExistingNeighbors())
        {
            if (
                neighbor?.UpdateBorderIfInvalid()
                ?? false && _terrainMeshContainer.ContainsMesh(neighbor.ChunkPosition)
            )
            {
                InvokeOrCollectChunkChange(neighbor.ChunkPosition, notify);
            }
        }

        if (
            healthDepleted
            && GlobalTileSupportedEntitiesIndex.TryGetValue(worldPosition, out var entities)
        )
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                var e = entities[i];
                if (e.Chunk.UpdateEntity(e))
                {
                    EntityRemoved?.Invoke(e);
                    foreach (var tile in GlobalEntitySupportingTilesIndex[e])
                    {
                        var list = GlobalTileSupportedEntitiesIndex[tile];
                        list.Remove(e);
                        if (list.Count == 0)
                        {
                            GlobalTileSupportedEntitiesIndex.Remove(tile);
                        }
                    }
                    GlobalEntitySupportingTilesIndex.Remove(e);
                }
            }
        }

        return true;
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
        if (!_chunks.TryGetValue(chunkPosition, out _))
        {
            new Chunk(GetOrGenerateVoxelGrid(chunkPosition), null);
        }

        var chunk = _chunks[chunkPosition];

        var entitiesChunk = GenerateEntitiesChunk(
            chunkPosition,
            empty: chunkPosition.Y >= SurfaceChunkYLevel
        );

        chunk.Entities = entitiesChunk;
    }

    public Vector3I ChunkToWorld(Vector3I chunkPosition)
    {
        return _worldChunkCoordinatesConverter.ChunkToWorld(chunkPosition);
    }

    public EntitiesChunk GenerateEntitiesChunk(Vector3I chunkPos, bool empty)
    {
        var globalPos = ChunkToWorld(chunkPos);
        var entities = empty
            ? []
            : Chunk.ForEachCellInChunk(localPos =>
            {
                var globalCellPos = globalPos + localPos;
                var sample = _noise.GetNoise3dv(globalCellPos.ToGdVector3()) / 2 + 0.5;
                if (sample > Config.Instance.GenerationConfig.NoiseConfig.SpawnThreshold)
                {
                    return CreateRandomEntity(globalCellPos);
                }
                return null;
            });
        var result = new EntitiesChunk(chunkPos, entities, this);
        foreach (var kvp in result.EntitySupportingTiles)
        {
            GlobalEntitySupportingTilesIndex.Add(kvp.Key, kvp.Value);
            kvp.Key.Chunk = result;
            GlobalMeshInstanceEntityMap.Add(kvp.Key.MeshInstance, kvp.Key);
        }
        foreach (var kvp in result.SupportingTileEntities)
        {
            if (GlobalTileSupportedEntitiesIndex.TryGetValue(kvp.Key, out var e))
            {
                e.AddRange(kvp.Value);
            }
            else
            {
                GlobalTileSupportedEntitiesIndex.Add(kvp.Key, [.. kvp.Value]);
            }
        }
        return result;
    }

    public void Load(Vector3I chunkPosition)
    {
        var chunk = _chunks[chunkPosition];
        _terrainMeshContainer.Commit(
            chunkPosition,
            CreatePreparedMeshBuilderForVoxelGrid(chunk.VoxelGrid)
        );
        if (chunk.Entities == null)
        {
            Generate(chunkPosition);
        }
        _entityNodesContainer.Load(chunkPosition, chunk.Entities!);
    }

    public void Unload(Vector3I chunkPosition)
    {
        _terrainMeshContainer.Unload(chunkPosition);
        _entityNodesContainer.Unload(chunkPosition);
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
            ChunkOfValues(
                chunkPosition.Y < 0
                    ? Config.Instance.GenerationConfig.CellHealth
                        * Mathf.Pow(
                            1 + Config.Instance.GenerationConfig.CellHealthScaling,
                            -chunkPosition.Y
                        )
                    : 0,
                ChunkSizeFlat
            )
        );
        _chunks[chunkPosition] = new Chunk(grid, null);
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

    private GodotMeshBuilder CreatePreparedMeshBuilderForVoxelGrid(VoxelGrid voxelGrid)
    {
        var meshBuilder = new GodotMeshBuilder();
        MarchingCubes.Generate(voxelGrid, meshBuilder, Config.Instance.MarchingCubesOptions);
        return meshBuilder;
    }

    private Entity CreateRandomEntity(Vector3I globalPos)
    {
        var prefab = EntityPrefab.Random(_rng);
        var sizeConfig = Config.Instance.RemnantsSizeConfig;
        var sizeRange = sizeConfig.Sizes[prefab.Type];
        var rotation = RandomUtils.Rotation(_rng);
        var scale = RandomUtils.FromRange(sizeRange.Min, sizeRange.Max, _rng);

        return new Entity(prefab, globalPos, rotation, scale);
    }

    public VoxelGrid CreateVoxelGrid(Vector3I chunkPosition)
    {
        lock (_lock)
        {
            return GetOrGenerateVoxelGrid(chunkPosition);
        }
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
            return TryGetVoxelGrid(chunkPosition) ?? CreateVoxelGrid(chunkPosition);
        }
    }
}

public class Chunk(VoxelGrid voxelGrid, EntitiesChunk? entities)
{
    public EntitiesChunk? Entities { get; set; } = entities;
    public VoxelGrid VoxelGrid { get; } = voxelGrid;
    public static Vector3I Size => Config.Instance.GenerationConfig.ChunkSize;
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
