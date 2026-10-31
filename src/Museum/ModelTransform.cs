using Godot;

public static class ModelTransform
{
    public static Transform ApplyOffsets(
        Transform baseTransform,
        Vector3 anchor,
        float additionalScale,
        Vector3 rotationDegrees,
        Vector3 positionOffset
    )
    {
        var anchorPosition = baseTransform.Xform(anchor);

        var rotation = new Basis(Deg2Rad(rotationDegrees));

        var scale = Basis.Identity.Scaled(Vector3.One * additionalScale);

        var additionalBasis = rotation * scale;

        var basis = additionalBasis * baseTransform.basis;

        var origin =
            anchorPosition
            + additionalBasis.Xform(baseTransform.origin - anchorPosition)
            + positionOffset;

        return new Transform(basis, origin);
    }

    private static Vector3 Deg2Rad(Vector3 vec)
    {
        return new(Mathf.Deg2Rad(vec.x), Mathf.Deg2Rad(vec.y), Mathf.Deg2Rad(vec.z));
    }
}
