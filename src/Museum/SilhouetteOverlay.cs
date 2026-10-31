using Godot;

public static class SilhouetteOverlay
{
    private const string ShaderCode =
        @"
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_back;

uniform float expansion : hint_range(0.0, 0.1) = 0.002;
uniform vec4 silhouette_color : hint_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float mix_level : hint_range(0.0, 1.0) = 0.0;

void vertex()
{
    VERTEX += NORMAL * expansion;
}

void fragment()
{
    ALBEDO = silhouette_color.rgb;
    ALPHA = silhouette_color.a * mix_level;
}
";

    public static MeshInstance Create(
        MeshInstance original,
        float scaleIncrease = 0.002f,
        float mixLevel = 1,
        Color? color = null
    )
    {
        var overlay = new MeshInstance
        {
            Name = original.Name + "_Silhouette",
            Mesh = original.Mesh,
            Transform = original.Transform,
            CastShadow = GeometryInstance.ShadowCastingSetting.Off,
            Visible = original.Visible,
        };

        if (original.Skeleton != null && original.Skeleton != "")
        {
            var skeleton = original.GetNodeOrNull(original.Skeleton);

            if (skeleton != null)
            {
                var parent = original.GetParent();

                if (parent != null && skeleton.IsInsideTree())
                {
                    overlay.Skeleton = parent.GetPathTo(skeleton);
                }
            }
        }

        var shader = new Shader { Code = ShaderCode };

        var material = new ShaderMaterial { Shader = shader };

        material.SetShaderParam("silhouette_color", color ?? Colors.Black);

        material.SetShaderParam("mix_level", Mathf.Clamp(mixLevel, 0.0f, 1.0f));

        material.SetShaderParam("expansion", scaleIncrease);

        overlay.MaterialOverride = material;

        original.GetParent().AddChild(overlay);

        return overlay;
    }

    public static void SetMixLevel(MeshInstance overlay, float mixLevel)
    {
        var material = overlay?.MaterialOverride as ShaderMaterial;
        material?.SetShaderParam("mix_level", Mathf.Clamp(mixLevel, 0.0f, 1.0f));
    }
}
