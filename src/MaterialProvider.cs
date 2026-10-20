using Godot;

public class MaterialProvider
{
    // TODO : Make configurable
    public static Material CreateTerrainMaterial()
    {
        var noise = new OpenSimplexNoise
        {
            Period = 8,
            Octaves = 4,
            Persistence = 0.8f,
            Lacunarity = 10,
        };

        var noiseTexture = new NoiseTexture
        {
            Width = 256,
            Height = 256,
            Noise = noise,
        };

        var spatialMaterial = new SpatialMaterial
        {
            AlbedoColor = new Color(Config.Instance.WorldConfig.TerrainColor),
            AlbedoTexture = noiseTexture,

            Uv1Triplanar = true,
            Uv1Scale = new Vector3(0.1f, 0.1f, 0.1f),
        };

        return spatialMaterial;
    }
}
