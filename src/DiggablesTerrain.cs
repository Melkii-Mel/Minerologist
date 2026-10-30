using System;
using System.Collections.Generic;
using Godot;

public class DiggablesTerrain : ITerrain<byte, byte>
{
    public readonly Dictionary<Vector3I, List<Entity>> GlobalTileSupportedEntitiesIndex = [];
    public readonly Dictionary<Entity, Vector3I[]> GlobalEntitySupportingTilesIndex = [];
    public readonly Dictionary<MeshInstance, Entity> GlobalMeshInstanceEntityMap = [];
    public readonly Dictionary<Vector3I, EntitiesChunk> EntitiesChunks = [];

    private static readonly EntitiesGenerationConfig EntitiesGenerationConfig = Config
        .Instance
        .GenerationConfig
        .EntitiesGenerationConfig;

    public Vector3I ChunkSize => EntitiesGenerationConfig.ChunkSize;

    public event Action<Vector3I>? ChunkChanged;

    private readonly EntityNodesContainer _entityNodesContainer;

    private readonly WorldChunkCoordinatesConverter _worldChunkCoordinatesConverter = new(
        EntitiesGenerationConfig.ChunkSize
    );

    private readonly OpenSimplexNoise _noise;

    private readonly Random _rng;

    public DiggablesTerrain(Node parent, Random? rng = null)
    {
        _rng = rng ?? new();

        _entityNodesContainer = new();

        parent.AddChild(_entityNodesContainer);
        var noiseConfig = EntitiesGenerationConfig.NoiseConfig;
        _noise = new OpenSimplexNoise
        {
            Seed = (int)GD.Randi(),
            Octaves = noiseConfig.Octaves,
            Period = noiseConfig.Period,
            Persistence = noiseConfig.Persistence,
            Lacunarity = noiseConfig.Lacunarity,
        };
    }

    private EntitiesChunk GenerateEntitiesChunk(Vector3I chunkPosition)
    {
        return GenerateEntitiesChunk(
            chunkPosition,
            empty: chunkPosition.Y >= Terrain.SurfaceChunkYLevel
        );
    }

    public EntitiesChunk GenerateEntitiesChunk(Vector3I chunkPos, bool empty)
    {
        var globalPos = _worldChunkCoordinatesConverter.ChunkToWorld(chunkPos);
        // TODO: Configure noise here
        var entities = new List<Entity>();
        if (!empty)
        {
            for (var x = 0; x < ChunkSize.X; x++)
            {
                for (var y = 0; y < ChunkSize.Y; y++)
                {
                    for (var z = 0; z < ChunkSize.Z; z++)
                    {
                        var globalCellPos = globalPos + new Vector3I(x, y, z);
                        var sample = _noise.GetNoise3dv(globalCellPos.ToGdVector3()) / 2 + 0.5;
                        if (sample > EntitiesGenerationConfig.NoiseConfig.SpawnThreshold)
                        {
                            entities.Add(CreateRandomEntity(globalCellPos));
                        }
                    }
                }
            }
        }
        var result = new EntitiesChunk([.. entities]);
        foreach (var e in result.Entities)
        {
            GlobalEntitySupportingTilesIndex.Add(e, e.SupportingTiles);
            GlobalMeshInstanceEntityMap.Add(e.MeshInstance, e);
            foreach (var tile in e.SupportingTiles)
            {
                if (GlobalTileSupportedEntitiesIndex.TryGetValue(tile, out var e1))
                {
                    e1.Add(e);
                }
                else
                {
                    GlobalTileSupportedEntitiesIndex[tile] = [e];
                }
            }
        }
        return result;
    }

    private Entity CreateRandomEntity(Vector3I globalPos)
    {
        var prefab = EntityPrefab.Random(_rng);
        return Entity.RandomFromPrefab(globalPos, prefab);
    }

    public void Generate(Vector3I chunkPosition)
    {
        var chunk = GenerateEntitiesChunk(chunkPosition);
        EntitiesChunks[chunkPosition] = chunk;
    }

    public void PrepareLoad(Vector3I chunkPosition)
    {
        _entityNodesContainer.Load(chunkPosition, EntitiesChunks[chunkPosition]);
    }

    public void ExecuteLoad(Vector3I chunkPosition) { }

    public void Unload(Vector3I chunkPosition)
    {
        _entityNodesContainer.Unload(chunkPosition);
    }

    public bool IsGenerated(Vector3I chunkPosition)
    {
        return EntitiesChunks.ContainsKey(chunkPosition);
    }

    public byte PrepareUpdate(Vector3I chunkPosition) => 0;

    public byte ExecuteUpdate(Vector3I chunkPosition, byte snapshot) => 0;

    public void FinalizeUpdate(Vector3I chunkPosition, byte result) { }
}
