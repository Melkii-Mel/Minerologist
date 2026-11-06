using Godot;

public class MuseumDiggable(string diggableName, MeshInstance placeholder, Spatial parent)
    : MuseumItem(
        placeholder,
        parent,
        (
            (MeshInstance?)MeshCollection.OtherDiggablesFlat.Get(diggableName)?.Duplicate()
        ).ToArrayNullable(),
        diggableName
    )
{
    public override string GroupName => "MUSEUM_DIGGABLE_GROUP";
}
