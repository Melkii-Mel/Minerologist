using Godot;

public class MuseumStaticSkeleton : Node
{
    private readonly string _skeletonName;
    private readonly MeshInstance _placeholder;

    public MuseumStaticSkeleton(string skeletonName, MeshInstance placeholder)
    {
        _skeletonName = skeletonName;
        _placeholder = placeholder;
    }
}
