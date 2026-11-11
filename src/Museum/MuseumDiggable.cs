using Godot;

public class MuseumDiggable(
    string diggableName,
    MeshInstance placeholder,
    Spatial parent,
    MuseumRoot root
)
    : StaticMuseumItem(
        placeholder,
        parent,
        (
            (MeshInstance?)MeshCollection.OtherDiggablesFlat.Get(diggableName)?.Duplicate()
        ).ToArrayNullable(),
        diggableName,
        root.State.OtherDiggablesAmounts,
        root
    )
{
    public override string GroupName => "Left";
}
