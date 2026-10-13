using Godot;

public static class NoiseGenerator
{
    public static VoxelGrid Generate(IVoxelGridContainer container, Bounds bounds, int seed = 0)
    {
        var noise = new OpenSimplexNoise
        {
            Seed = seed,
            Octaves = 4,
            Period = 16f,
            Persistence = 0.5f,
        };

        var size = bounds.Size;
        var pos = bounds.Position;

        var grid = new VoxelGrid(pos, size, container);

        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                for (var z = 0; z < size.Z; z++)
                {
                    var value = noise.GetNoise3d(pos.X + x, pos.Y + y, pos.Z + z);
                    grid.Set(x, y, z, value);
                }
            }
        }

        return grid;
    }

    public static VoxelGrid GenerateTerrain(
        IVoxelGridContainer container,
        Bounds bounds,
        int seed = 0
    )
    {
        const float caveThreshold = 0.2f;
        var terrainNoise = new OpenSimplexNoise
        {
            Seed = seed,
            Octaves = 4,
            Period = 80f,
            Persistence = 0.5f,
        };

        var caveNoise = new OpenSimplexNoise
        {
            Seed = seed + 1,
            Octaves = 3,
            Period = 20f,
            Persistence = 0.5f,
        };

        var size = bounds.Size;
        var pos = bounds.Position;

        var grid = new VoxelGrid(pos, size, container);

        const float baseHeight = 40f;
        const float mountainHeight = 40f;

        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                for (var z = 0; z < size.Z; z++)
                {
                    var worldX = pos.X + x;
                    var worldY = pos.Y + y;
                    var worldZ = pos.Z + z;

                    // Large-scale terrain shape.
                    var terrain = terrainNoise.GetNoise2d(worldX, worldZ);

                    var surfaceHeight = baseHeight + terrain * mountainHeight;

                    // Positive below the surface, negative above it.
                    var density = surfaceHeight - worldY;

                    // 3D noise creates volumetric deformation.
                    var cave = caveNoise.GetNoise3d(worldX, worldY, worldZ);

                    // Only carve underground, sufficiently far below the surface.
                    if (worldY < surfaceHeight - 5f && cave > caveThreshold)
                    {
                        density = Mathf.Min(density, -(cave - caveThreshold) * 20f);
                    }

                    grid.Set(x, y, z, density);
                }
            }
        }

        return grid;
    }
}
