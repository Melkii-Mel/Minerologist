using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class MuseumDynamicSkeleton
{
    public MuseumRoot Root { get; }

    private readonly string _skeletonName;
    private readonly MeshInstance _dynamicSkeletonPlaceholder;

    private readonly Dictionary<string, MeshInstance> _meshes;
    private readonly Dictionary<MeshInstance, MeshInstance> _silhouetteOverlays = [];
    private readonly Spatial _skeletonParent;
    public ModelPlacement Placement { get; }

    public Transform ExternalTransform { get; }

    public MuseumPlacerConfig MuseumPlacerConfig => Config.Instance.MuseumConfig.MuseumPlacerConfig;

    public Spatial AnimationPivot { get; }

    // TODO: Juicify
    public MuseumDynamicSkeleton(
        string skeletonName,
        MeshInstance dynamicSkeletonPlaceholder,
        Spatial parent,
        MuseumRoot root
    )
    {
        _skeletonName = skeletonName;
        _dynamicSkeletonPlaceholder = dynamicSkeletonPlaceholder;
        Root = root;

        var revealedParts = Root
            .State.CurrentSkeletonPiecesAmounts.Where(a => a.Value > 0)
            .Select(a => a.Key)
            .ToArray();

        var model = MeshCollection.Skeletons[skeletonName].Duplicate();

        _skeletonParent = new Spatial();
        _meshes = [];

        parent.AddChild(_skeletonParent);

        Placement = new ModelPlacement(
            parent,
            _dynamicSkeletonPlaceholder,
            [.. model.Values],
            new Vector3(0.5f, 0f, 0.5f),
            pivots: 1
        );

        AnimationPivot = Placement.Pivots[0];

        UpdateModelTransform();

        // reparent meshes under _placement.Content, keeping their local transforms

        foreach (var mesh in model)
        {
            var copy = (MeshInstance)mesh.Value.Duplicate();
            _meshes[mesh.Key] = copy;
            Placement.Content.AddChild(copy);
            ApplyShader(copy);
        }

        Reveal(revealedParts);

        var initialScale = Placement.Anchor.Scale;
        root.Root.Update += delta =>
        {
            UpdateModelTransform();
        };
    }

    private void UpdateModelTransform()
    {
        Placement.Apply(
            SelectRelevant(MuseumPlacerConfig.Front.Scales),
            SelectRelevant(MuseumPlacerConfig.Front.RotationsDegrees).ToGdVector3(),
            SelectRelevant(MuseumPlacerConfig.Front.Offsets).ToGdVector3() / 100
        );
    }

    // TODO: Juicify
    public void Reveal(params string[] ids)
    {
        foreach (var mesh in ids.Select(id => _meshes[id]))
        {
            RemoveShader(mesh);
        }
    }

    private void ApplyShader(MeshInstance original)
    {
        var overlay = SilhouetteOverlay.Create(original, mixLevel: 0.4f);
        _silhouetteOverlays[original] = overlay;
        original.Visible = false;
    }

    private void RemoveShader(MeshInstance original)
    {
        original.Visible = true;
        SilhouetteOverlay.SetMixLevel(_silhouetteOverlays[original], 0);
    }

    private T SelectRelevant<T>(Dictionary<string, T> values)
    {
        return SelectRelevant(values, _skeletonName);
    }

    public static T SelectRelevant<T>(Dictionary<string, T> values, string name)
    {
        if (values.TryGetValue(name, out var v))
        {
            return v;
        }
        if (
            MeshCollection.Skeletons.ContainsKey(name)
            && values.TryGetValue(MeshCollection.SKELETONS, out v)
        )
        {
            return v;
        }
        return values[MeshCollection.OTHERS];
    }
}
