using Godot;

public class MaterialProvider
{
    // TODO: Make configurable
    public static Material CreateTerrainMaterial()
    {
        var noise = new OpenSimplexNoise
        {
            Period = 16,
            Octaves = 4,
            Persistence = 0.8f,
            Lacunarity = 3,
        };

        var noiseTexture = new NoiseTexture
        {
            Width = 256,
            Height = 256,
            Noise = noise,
            Seamless = true,
        };

        var spatialMaterial = new SpatialMaterial
        {
            AlbedoColor = new Color(Config.Instance.WorldConfig.TerrainColor),
            AlbedoTexture = noiseTexture,

            Uv1Triplanar = true,
            FlagsWorldTriplanar = true,
            Uv1Scale = new Vector3(0.1f, 0.1f, 0.1f),
        };

        return spatialMaterial;
    }
}
