using System.Collections.Generic;
using Godot;

public class ModelPlacement
{
    private readonly ModelFitResult _fit;

    /// <summary>Move/rotate/scale this. All changes happen around the anchor.</summary>
    public Spatial Anchor { get; } = new();

    /// <summary>Add the model's meshes here, keeping their original local transforms.</summary>
    public Spatial Content { get; } = new();

    public ModelPlacement(
        Node parent,
        MeshInstance placeholder,
        IReadOnlyCollection<MeshInstance> meshes,
        Vector3 anchor // e.g. new Vector3(0.5f, 0f, 0.5f) = bottom center
    )
    {
        parent.AddChild(Anchor);
        Anchor.AddChild(Content);

        _fit = ModelFitter.Fit(placeholder, meshes, anchor);

        // The content shifts by -anchor so the model's anchor sits on Anchor's origin.
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

        // Where the anchor ended up after the fit, in the parent's space.
        var anchorPosition = _fit.Transform.Xform(_fit.Anchor);

        Anchor.Transform = new Transform(extra * _fit.Transform.basis, anchorPosition + offset);
    }
}
