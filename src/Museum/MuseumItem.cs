using System;
using System.Collections.Generic;
using Godot;

// TODO: Dark silhouette shader until discovered
public abstract class MuseumItem : MeshInstance
{
    public abstract string GroupName { get; }
    private const string InvisibleShaderCode =
        @"
        shader_type spatial;
        render_mode unshaded, depth_draw_never, shadows_disabled;

        void fragment() {
            ALBEDO = vec3(0.0);
            ALPHA = 0.0;
        }";

    private static readonly Shader InvisibleShader = new() { Code = InvisibleShaderCode };
    private static readonly ShaderMaterial InvisibleShaderMaterial = new()
    {
        Shader = InvisibleShader,
    };

    public static MuseumPlacerConfig MuseumPlacerConfig =>
        Config.Instance.MuseumConfig.MuseumPlacerConfig;

    public MeshInstance MeshInstance { get; }

    private readonly MeshInstance _placeholder;

    public MuseumItem(
        MeshInstance placeholder,
        Spatial parent,
        IReadOnlyCollection<MeshInstance>? meshes,
        string name
    )
    {
        _placeholder = placeholder;

        MeshInstance = (MeshInstance)_placeholder.Duplicate();
        MeshInstance.MaterialOverride = InvisibleShaderMaterial;
        MeshInstance.CreateTrimeshCollision();
        MeshInstance.GetChild(0).AddToGroup(GroupName);

        if (meshes != null)
        {
            foreach (var mesh in meshes)
            {
                MeshInstance.AddChild(mesh);
            }

            var placement = new ModelPlacement(
                parent,
                placeholder,
                meshes,
                ModelPlacementAnchorPresets.Bottom
            );
            placement.Apply(
                MuseumDynamicSkeleton.SelectRelevant(MuseumPlacerConfig.Scales, name),
                MuseumDynamicSkeleton
                    .SelectRelevant(MuseumPlacerConfig.RotationsDegrees, name)
                    .ToGdVector3(),
                MuseumDynamicSkeleton.SelectRelevant(MuseumPlacerConfig.Offsets, name).ToGdVector3()
                    / 100
            );
        }
        else
        {
            Out.Error($"Model for {name} not found");
        }
    }

    // TODO:
    public void InceraseNumber()
    {
        Out.Error("InceraseNumber not implemented");
    }
}
