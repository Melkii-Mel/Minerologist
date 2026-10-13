using System;
using Godot;

public class Terrain
{
    public const int OFFSET_X = -Chunk.SIZE_X / 2;
    public const int OFFSET_Y = -Chunk.SIZE_Y / 2;
    public const int OFFSET_Z = -Chunk.SIZE_Z / 2;
    public const int SURFACE_CHUNK_Y_INDEX = 0;

    private readonly Random _rng = new();

    private readonly OpenSimplexNoise _noise;

    public Terrain()
    {
        var noiseConfig = Config.Instance.GenerationConfig.NoiseConfig;
        _noise = new OpenSimplexNoise
        {
            Seed = (int)GD.Randi(),
            Octaves = noiseConfig.Octaves,
            Period = noiseConfig.Period,
            Persistence = noiseConfig.Persistence,
        };
    }

    public Chunk? GenerateChunk(Vector3I chunkPos)
    {
        var terrainChunk = GenerateTerrainChunk(chunkPos);
        if (terrainChunk is null)
            return null;
        var entitiesChunk = GenerateEntitiesChunk(chunkPos);
        return new Chunk(terrainChunk, entitiesChunk);
    }

    public TerrainChunk? GenerateTerrainChunk(Vector3I chunkPos)
    {
        if (chunkPos.Y + 1 < SURFACE_CHUNK_Y_INDEX)
        {
            return null;
        }
        return new TerrainChunk(chunkPos.Y < SURFACE_CHUNK_Y_INDEX);
    }

    public EntitiesChunk GenerateEntitiesChunk(Vector3I chunkPos)
    {
        var result = new EntitiesChunk();
        Chunk.ForEachCellInChunk(localPos =>
        {
            var sample = _noise.GetNoise3dv(ToWorldPos(chunkPos, localPos));
            if (sample > Config.Instance.GenerationConfig.NoiseConfig.SpawnThreshold)
            {
                var prefab = EntityPrefab.Random(_rng);
                var sizeConfig = Config.Instance.RemnantsSizeConfig;
                var size = prefab.IsSkeleton ? sizeConfig.Skeleton : sizeConfig.Others;
                result.Entities.Add(
                    localPos,
                    new(
                        prefab,
                        result,
                        localPos,
                        RandomUtils.Rotation(_rng),
                        RandomUtils.FromRange(size.Min, size.Max, _rng)
                    )
                );
            }
        });
        return result;
    }

    private Block GetBlock(Vector3I chunkIndex, int blockIndex, Direction direction)
    {
        var x = blockIndex % Chunk.SIZE_X;
        var z = (blockIndex - x) % (Chunk.SIZE_X * Chunk.SIZE_Z) / Chunk.SIZE_X;
        var y = (blockIndex - Chunk.SIZE_X * z - x) / Chunk.SIZE_X / Chunk.SIZE_Z;
        Vector3I delta = direction switch
        {
            Direction.Top => new(0, -1, 0),
            Direction.Bottom => new(0, 1, 0),
            Direction.Behind => new(0, 0, -1),
            Direction.Front => new(0, 0, 1),
            Direction.Left => new(-1, 0, 0),
            Direction.Right => new(1, 0, 0),
            _ => throw new NotSupportedException(),
        };
        x += delta.X;
        y += delta.Y;
        z += delta.Z;
        if (x >= Chunk.SIZE_X)
        {
            chunkIndex.X += 1;
            x -= Chunk.SIZE_X;
        }
        else if (y >= Chunk.SIZE_Y)
        {
            chunkIndex.Y += 1;
            y -= Chunk.SIZE_Y;
        }
        else if (z >= Chunk.SIZE_Z)
        {
            chunkIndex.Z += 1;
            z -= Chunk.SIZE_Z;
        }
        else if (x < 0)
        {
            chunkIndex.X -= 1;
            x += Chunk.SIZE_X;
        }
        else if (y < 0)
        {
            chunkIndex.Y -= 1;
            y += Chunk.SIZE_Y;
        }
        else if (z < 0)
        {
            chunkIndex.Z -= 1;
            z += Chunk.SIZE_Z;
        }
        if (!Chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk = GenerateTerrainChunk(chunkIndex);
        }
        return chunk?.Blocks[z * Chunk.SIZE_Y * Chunk.SIZE_X + y * Chunk.SIZE_X + x]
            ?? new Block(false);
    }

    public Vector3 ToWorldPos(Vector3I chunkIndex, Vector3I cellIndex)
    {
        var absolute =
            new Vector3I(
                chunkIndex.X * Chunk.SIZE_X,
                chunkIndex.Y * Chunk.SIZE_Y,
                chunkIndex.Z * Chunk.SIZE_Z
            ) + cellIndex;
        return absolute * Config.Instance.WorldConfig.CellSize;
    }
}

public enum Direction
{
    Top,
    Bottom,
    Behind,
    Front,
    Left,
    Right,
}

public class Chunk(TerrainChunk terrainChunk, EntitiesChunk objectsChunk)
{
    public const int SIZE_X = 20;
    public const int SIZE_Y = 20;
    public const int SIZE_Z = 20;
    public const int SIZE = SIZE_X * SIZE_Y * SIZE_Z;

    public readonly TerrainChunk TerrainChunk = terrainChunk;
    public readonly EntitiesChunk ObjectsChunk = objectsChunk;

    public static void ForEachCellInChunk(Action<Vector3I> action)
    {
        for (var x = 0; x < SIZE_X; x++)
        {
            for (var y = 0; y < SIZE_Y; y++)
            {
                for (var z = 0; z < SIZE_Z; z++)
                {
                    action(new(x, y, z));
                }
            }
        }
    }
}

public class TerrainChunk
{
    public Block[] Blocks = new Block[Chunk.SIZE];

    public TerrainChunk(bool air = false)
    {
        for (var i = 0; i < Chunk.SIZE; i++)
        {
            Blocks[i] = new Block(air);
        }
    }
}

public record struct Block(bool IsAir);
