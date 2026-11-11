// AI-Generated

using System.Collections.Generic;
using Godot;

public class ModelPlacement
{
    private readonly ModelFitResult _fit;
    private readonly Vector3 _anchor;
    private readonly Vector3[] _boundsPoints;

    /// <summary>Move/rotate/scale this. All changes happen around the anchor.</summary>
    public Spatial Anchor { get; } = new();

    /// <summary>Additional transform pivots, ordered from outermost to innermost.</summary>
    public IReadOnlyList<Spatial> Pivots { get; }

    /// <summary>Add the model's meshes here, keeping their original local transforms.</summary>
    public Spatial Content { get; } = new();

    public ModelPlacement(
        Node parent,
        MeshInstance placeholder,
        IReadOnlyCollection<MeshInstance> meshes,
        Vector3 anchor,
        int pivots = 0
    )
    {
        _anchor = anchor;

        parent.AddChild(Anchor);

        var pivotList = new List<Spatial>(pivots);
        Spatial current = Anchor;

        for (var i = 0; i < pivots; i++)
        {
            var pivot = new Spatial();
            current.AddChild(pivot);
            pivotList.Add(pivot);
            current = pivot;
        }

        Pivots = pivotList;
        current.AddChild(Content);

        _fit = ModelFitter.Fit(placeholder, meshes, anchor);

        // Cache the corners of each mesh's AABB in the model's local space.
        var boundsPoints = new List<Vector3>();

        foreach (var mesh in meshes)
        {
            var bounds = mesh.GetAabb();

            for (var x = 0; x <= 1; x++)
            for (var y = 0; y <= 1; y++)
            for (var z = 0; z <= 1; z++)
            {
                var corner = bounds.Position + bounds.Size * new Vector3(x, y, z);
                boundsPoints.Add(mesh.Transform.Xform(corner));
            }
        }

        _boundsPoints = boundsPoints.ToArray();

        Content.Transform = new Transform(Basis.Identity, -_fit.Anchor);

        Apply(1f, Vector3.Zero, Vector3.Zero);
    }

    public void Apply(float extraScale, Vector3 rotationDegrees, Vector3 offset)
    {
        var rotation = new Basis(
            new Vector3(
                Mathf.Deg2Rad(rotationDegrees.x),
                Mathf.Deg2Rad(rotationDegrees.y),
                Mathf.Deg2Rad(rotationDegrees.z)
            )
        );

        var extra = rotation * Basis.Identity.Scaled(Vector3.One * extraScale);
        var basis = extra * _fit.Transform.basis;

        // Calculate the model's AABB after applying its configured rotation and scale.
        var boundsMin = new Vector3(
            float.PositiveInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity
        );

        var boundsMax = new Vector3(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.NegativeInfinity
        );

        foreach (var point in _boundsPoints)
        {
            // Coordinates relative to the selected anchor, after fitting and rotation.
            var transformed = basis.Xform(point - _fit.Anchor);

            boundsMin = Min(boundsMin, transformed);
            boundsMax = Max(boundsMax, transformed);
        }

        var boundsSize = boundsMax - boundsMin;

        // Find the selected normalized anchor in the transformed bounds.
        var transformedAnchor = boundsMin + boundsSize * _anchor;

        // The selected point must remain at its original position in the placeholder.
        var targetAnchor = _fit.Transform.Xform(_fit.Anchor) + offset;

        Anchor.Transform = new Transform(basis, targetAnchor - transformedAnchor);
    }

    private static Vector3 Min(Vector3 a, Vector3 b) =>
        new(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));

    private static Vector3 Max(Vector3 a, Vector3 b) =>
        new(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
}

public static class ModelPlacementAnchorPresets
{
    public static Vector3 Top => new(0.5f, 1, 0.5f);
    public static Vector3 Bottom => new(0.5f, 0, 0.5f);
    public static Vector3 Center => new(0.5f, 0.5f, 0.5f);
}
