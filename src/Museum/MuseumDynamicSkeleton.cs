using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

public class MuseumDynamicSkeleton
{
    public MuseumRoot Root { get; }

    private readonly string _skeletonName;
    private readonly MeshInstance _dynamicSkeletonPlaceholder;

    private readonly Dictionary<string, MeshInstance> _meshes;
    private readonly Dictionary<MeshInstance, MeshInstance> _silhouetteOverlays = [];
    private readonly Spatial _skeletonParent;
    private readonly ModelPlacement _placement;

    public MuseumPlacerConfig MuseumPlacerConfig => Config.Instance.MuseumConfig.MuseumPlacerConfig;

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

        var model = MeshCollection.Skeletons[skeletonName];

        _skeletonParent = new Spatial();
        _meshes = [];

        parent.AddChild(_skeletonParent);

        _placement = new ModelPlacement(
            parent,
            _dynamicSkeletonPlaceholder,
            [.. model.Values],
            new Vector3(0.5f, 0f, 0.5f)
        );

        _placement.Apply(
            SelectRelevant(MuseumPlacerConfig.Scales),
            SelectRelevant(MuseumPlacerConfig.RotationsDegrees).ToGdVector3(),
            SelectRelevant(MuseumPlacerConfig.Offsets).ToGdVector3() / 100
        );

        // reparent meshes under _placement.Content, keeping their local transforms

        foreach (var mesh in model)
        {
            var copy = (MeshInstance)mesh.Value.Duplicate();
            _meshes[mesh.Key] = copy;
            _placement.Content.AddChild(copy);
            ApplyShader(copy);
        }

        Reveal(revealedParts);

        /// TEMP
        var initialScale = _placement.Anchor.Scale;
        var clock = 0f;
        root.Root.Update += delta =>
        {
            var rot = _placement.Anchor.Rotation;
            var scale = _placement.Anchor.Scale;
            rot.y += delta;
            scale.y += Mathf.Sin(clock) * delta;
            _placement.Anchor.Rotation = rot;
            // _placement.Anchor.Scale = scale;
            // clock += delta;
        };
        /// /TEMP
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

    private static Vector3[] GetAabbCorners(AABB aabb)
    {
        var min = aabb.Position;
        var max = aabb.End;

        return
        [
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z),
        ];
    }

    private T SelectRelevant<T>(Dictionary<string, T> values)
    {
        if (values.TryGetValue(_skeletonName, out var v))
        {
            return v;
        }
        if (values.TryGetValue(MeshCollection.SKELETONS, out v))
        {
            return v;
        }
        return values[MeshCollection.OTHERS];
    }
}
