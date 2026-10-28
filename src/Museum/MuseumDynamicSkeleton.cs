using System;
using Godot;

public class MuseumDynamicSkeleton : Node
{
    private readonly string _skeletonName;
    private readonly MeshInstance _dynamicSkeletonPlaceholder;

    // TODO:
    public MuseumDynamicSkeleton(string skeletonName, MeshInstance dynamicSkeletonPlaceholder)
    {
        _skeletonName = skeletonName;
        _dynamicSkeletonPlaceholder = dynamicSkeletonPlaceholder;
    }

    // TODO:
    public void Reveal(string id) { }
}
