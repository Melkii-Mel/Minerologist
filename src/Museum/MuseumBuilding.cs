using Godot;

public class MuseumBuilding
{
    public Spatial Building { get; }

    public MuseumPlacer Placer { get; }

    public MuseumBuilding(MuseumRoot root)
    {
        var buildingRoot = SceneLoader.Instantiate<Spatial>("MuseumBuilding");
        Building = (Spatial)buildingRoot.GetChild(0);
        buildingRoot.RemoveChild(Building);
        Building.Position = Config.Instance.MuseumConfig.MuseumBuildingConfig.Offset.ToGdVector3();
        root.AddChild(Building);
        Placer = new(Building, root);
    }
}
