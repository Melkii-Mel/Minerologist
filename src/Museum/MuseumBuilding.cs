using Godot;

public class MuseumBuilding
{
    public Spatial Building { get; }

    public MuseumPlacer Placer { get; }

    public MuseumBuilding(MuseumRoot root)
    {
        Building = SceneLoader.Instantiate<Spatial>("MuseumBuilding");
        Building.Position = Config.Instance.MuseumConfig.MuseumBuildingConfig.Offset.ToGdVector3();
        root.AddChild(Building);
        Placer = new(Building, root);
    }
}
