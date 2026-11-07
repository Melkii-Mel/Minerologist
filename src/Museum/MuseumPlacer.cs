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
    public readonly Dictionary<string, StaticMuseumItem> MuseumItems = [];
    public readonly Dictionary<string, MuseumDiggable> Diggables = [];
    public readonly Dictionary<string, MuseumStaticSkeleton> OtherSkeletons = [];
    public readonly Dictionary<MeshInstance, string> MeshDiggableMap = [];
    public readonly Dictionary<MeshInstance, string> MeshSkeletonMap = [];

    public MuseumPlacer(Spatial building, MuseumRoot root)
    {
        Root = root;

        _dynamicSkeletonPlaceholder = building.GetChildOfTypeByName<MeshInstance>(
            PlacerConfig.MainSkeletonPlaceholderName
        )!;

        _dynamicSkeletonParent = new();
        _dynamicSkeletonPlaceholder.GetParent().AddChild(_dynamicSkeletonParent);
        _otherSkeletonsParent = building.GetChildOfTypeByName<Spatial>(
            PlacerConfig.OtherSkeletonsParentName
        )!;
        _diggablesParent = building.GetChildOfTypeByName<Spatial>(
            PlacerConfig.DiggablesParentName
        )!;

        CollectChildren(_otherSkeletonsParent, _otherSkeletonPlaceholders = []);
        CollectChildren(_diggablesParent, _diggablesPlaceholders = []);

        ReplacePlaceholders();
        InitSkeleton();
    }

    private void InitSkeleton()
    {
        ReplaceCurrentSkeleton(Root.State.CurrentSkeleton, null);
    }

    private void ReplacePlaceholders()
    {
        _dynamicSkeletonPlaceholder.GetParent().RemoveChild(_dynamicSkeletonPlaceholder);

        foreach (var skeletonName in _otherSkeletonPlaceholders.Keys)
        {
            var placeholder = _otherSkeletonPlaceholders[skeletonName];
            var node = new MuseumStaticSkeleton(
                skeletonName,
                placeholder,
                _otherSkeletonsParent,
                Root
            );
            OtherSkeletons[skeletonName] = node;
            MuseumItems[skeletonName] = node;
            MeshSkeletonMap[node.MeshInstance] = skeletonName;
            _otherSkeletonsParent.AddChild(node.MeshInstance);
            _otherSkeletonsParent.RemoveChild(placeholder);
        }

        foreach (var diggableName in _diggablesPlaceholders.Keys)
        {
            var placeholder = _diggablesPlaceholders[diggableName];
            var node = new MuseumDiggable(diggableName, placeholder, _diggablesParent, Root);
            Diggables[diggableName] = node;
            MuseumItems[diggableName] = node;
            MeshDiggableMap[node.MeshInstance] = diggableName;
            _diggablesParent.AddChild(node.MeshInstance);
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

    public void ReplaceCurrentSkeleton(string newSkeletonName, string? oldSkeletonName)
    {
        if (oldSkeletonName != null)
        {
            if (OtherSkeletons.TryGetValue(oldSkeletonName, out var skeleton))
            {
                skeleton.InceraseNumber();
            }
        }
        {
            for (var i = _dynamicSkeletonParent.GetChildCount() - 1; i > -1; i--)
                _dynamicSkeletonParent.RemoveChild(_dynamicSkeletonParent.GetChild(i));
        }
        _dynamicSkeleton = new MuseumDynamicSkeleton(
            newSkeletonName,
            _dynamicSkeletonPlaceholder,
            _dynamicSkeletonParent,
            Root
        );
    }

    public void UpdateItem(string id)
    {
        if (MuseumItems.TryGetValue(id, out var item))
        {
            item.InceraseNumber();
        }
        else
        {
            Out.Error($"Museum does not contain a placeholder for `{id}`");
        }
    }
}
