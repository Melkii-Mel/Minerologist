using System.Collections.Generic;
using Godot;

public static class ModelFitter
{
    public static ModelFitResult Fit(
        MeshInstance placeholder,
        IEnumerable<MeshInstance> meshes,
        Vector3 anchor
    )
    {
        var placeholderTransform = placeholder.Transform;
        var placeholderAabb = placeholder.GetAabb();

        var rotation = placeholderTransform.basis.Orthonormalized();
        var inverseRotation = rotation.Inverse();

        // Placeholder bounds in its own rotation space.
        var placeholderMin = Vector3.One * float.PositiveInfinity;
        var placeholderMax = Vector3.One * float.NegativeInfinity;

        foreach (var corner in GetCorners(placeholderAabb))
        {
            var point = inverseRotation.Xform(placeholderTransform.Xform(corner));
            placeholderMin = Min(placeholderMin, point);
            placeholderMax = Max(placeholderMax, point);
        }

        var placeholderSize = placeholderMax - placeholderMin;
        var placeholderCenter = (placeholderMin + placeholderMax) / 2;

        // Model bounds in its own (unrotated) local space.
        var modelMin = Vector3.One * float.PositiveInfinity;
        var modelMax = Vector3.One * float.NegativeInfinity;
        var hasMesh = false;

        foreach (var mesh in meshes)
        {
            hasMesh = true;

            foreach (var corner in GetCorners(mesh.GetAabb()))
            {
                var point = mesh.Transform.Xform(corner);
                modelMin = Min(modelMin, point);
                modelMax = Max(modelMax, point);
            }
        }

        if (!hasMesh)
            return new ModelFitResult(Transform.Identity, new AABB(), anchor);

        var modelBounds = new AABB(modelMin, modelMax - modelMin);
        var modelSize = modelBounds.Size;

        const float epsilon = 1e-5f;
        var scale = float.PositiveInfinity;

        for (var i = 0; i < 3; i++)
        {
            if (modelSize[i] > epsilon)
                scale = Mathf.Min(scale, placeholderSize[i] / modelSize[i]);
        }

        if (float.IsInfinity(scale) || float.IsNaN(scale))
            scale = 1f;

        var basis = rotation.Scaled(Vector3.One * scale);

        var modelAnchor = modelBounds.Position + modelSize * anchor;
        var placeholderAnchor = placeholderMin + placeholderSize * anchor;

        var origin = rotation.Xform(placeholderAnchor - modelAnchor * scale);

        return new ModelFitResult(new Transform(basis, origin), modelBounds, modelAnchor);
    }

    private static Vector3[] GetCorners(AABB aabb)
    {
        var p = aabb.Position;
        var s = aabb.Size;

        return
        [
            p,
            p + new Vector3(s.x, 0, 0),
            p + new Vector3(0, s.y, 0),
            p + new Vector3(0, 0, s.z),
            p + new Vector3(s.x, s.y, 0),
            p + new Vector3(s.x, 0, s.z),
            p + new Vector3(0, s.y, s.z),
            p + s,
        ];
    }

    private static Vector3 Min(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
    }

    private static Vector3 Max(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
    }
}
