using Godot;

public class MuseumStaticSkeleton(string skeletonName, MeshInstance placeholder, Spatial parent)
    : MuseumItem(
        placeholder,
        parent,
        MeshCollection.Skeletons.Get(skeletonName)?.Duplicate()?.Values,
        skeletonName
    )
{
    public override string GroupName => "MUSEUM_STATIC_SKELETON_GROUP";
}
