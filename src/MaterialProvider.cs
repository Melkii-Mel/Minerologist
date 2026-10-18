using Godot;

public class MaterialProvider
{
    public static Material CreateTerrainMaterial()
    {
        var spatialMaterial = new SpatialMaterial
        {
            AlbedoColor = new(new Color(Config.Instance.WorldConfig.TerrainColor)),
        };
        return spatialMaterial;
    }
}
