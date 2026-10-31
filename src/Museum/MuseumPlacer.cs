using System;
using System.Collections.Generic;
using Godot;

public class MuseumPlacer
{
    public MuseumRoot Root { get; }
    public MuseumPlacerConfig PlacerConfig => Config.Instance.MuseumConfig.MuseumPlacerConfig;

    private readonly Spatial _dynamicSkeletonParent;
    private readonly MeshInstance _dynamicSkeletonPlaceholder;
    private readonly Spatial _otherSkeletonsParent;
    private readonly Dictionary<string, MeshInstance> _otherSkeletonPlaceholders;
    private readonly Spatial _diggablesParent;
    private readonly Dictionary<string, MeshInstance> _diggablesPlaceholders;

    private MuseumDynamicSkeleton _dynamicSkeleton = null!;
    private readonly Dictionary<string, MuseumDiggable> _diggables = [];

    public MuseumPlacer(Spatial building, MuseumRoot root)
    {
        Root = root;

        _dynamicSkeletonPlaceholder = (MeshInstance)
            building.FindNode(PlacerConfig.MainSkeletonPlaceholderName);

        _dynamicSkeletonParent = new();
        _dynamicSkeletonPlaceholder.GetParent().AddChild(_dynamicSkeletonParent);
        _otherSkeletonsParent = (Spatial)building.FindNode(PlacerConfig.OtherSkeletonsParentName);
        _diggablesParent = (Spatial)building.FindNode(PlacerConfig.DiggablesParentName);

        CollectChildren(_otherSkeletonsParent, _otherSkeletonPlaceholders = []);
        CollectChildren(_diggablesParent, _diggablesPlaceholders = []);

        ReplacePlaceholders();
        InitSkeleton();
    }

    private void InitSkeleton()
    {
        ReplaceCurrentSkeleton(Root.State.CurrentSkeleton);
    }

    private void ReplacePlaceholders()
    {
        _dynamicSkeletonPlaceholder.GetParent().RemoveChild(_dynamicSkeletonPlaceholder);

        foreach (var skeletonName in _otherSkeletonPlaceholders.Keys)
        {
            var placeholder = _otherSkeletonPlaceholders[skeletonName];
            var node = new MuseumStaticSkeleton(skeletonName, placeholder);
            _otherSkeletonsParent.AddChild(node);
            _otherSkeletonsParent.RemoveChild(placeholder);
        }

        foreach (var diggableName in _diggablesPlaceholders.Keys)
        {
            var placeholder = _diggablesPlaceholders[diggableName];
            var node = new MuseumDiggable(diggableName, placeholder);
            _diggables[diggableName] = node;
            _diggablesParent.AddChild(node);
            _diggablesParent.RemoveChild(placeholder);
        }
    }

    private void CollectChildren(Spatial parent, Dictionary<string, MeshInstance> to)
    {
        foreach (var child in parent.GetChildren())
        {
            var meshInstance = (MeshInstance)child;
            to[meshInstance.Name] = meshInstance;
        }
    }

    public void UpdateCurrentSkeleton(string id)
    {
        _dynamicSkeleton.Reveal(id);
    }

    public void ReplaceCurrentSkeleton(string skeletonName)
    {
        for (var i = _dynamicSkeletonParent.GetChildCount() - 1; i > -1; i--)
        {
            _dynamicSkeletonParent.RemoveChild(_dynamicSkeletonParent.GetChild(i));
        }
        _dynamicSkeleton = new MuseumDynamicSkeleton(
            skeletonName,
            _dynamicSkeletonPlaceholder,
            _dynamicSkeletonParent,
            Root
        );
    }

    public void UpdateItem(string id)
    {
        _diggables[id].InceraseNumber();
    }
}
