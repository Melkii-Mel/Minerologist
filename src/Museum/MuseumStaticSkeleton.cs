using Godot;

public class MuseumStaticSkeleton(
    string skeletonName,
    MeshInstance placeholder,
    Spatial parent,
    MuseumRoot root
)
    : StaticMuseumItem(
        placeholder,
        parent,
        MeshCollection.Skeletons.Get(skeletonName)?.Duplicate()?.Values,
        skeletonName,
        root.State.SkeletonsAmounts,
        root
    )
{
    public override string GroupName => "Right";
}
