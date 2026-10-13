using System.Collections.Generic;

public class TerrainGenerator : ITerrainGenerator
{
    public VoxelGrid Generate(IVoxelGridContainer container, Vector3I chunkPosition)
    {
        var grid = new VoxelGrid(
            chunkPosition,
            container,
            ChunkOfValues(
                chunkPosition.Y > 0 ? Config.Instance.GenerationConfig.CellHealth : 0,
                container.ChunkSizeFlat
            )
        );

        var remnants = new EntitiesChunk(chunkPosition);

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
}
