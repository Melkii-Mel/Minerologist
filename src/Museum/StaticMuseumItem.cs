using System;
using System.Collections.Generic;
using Godot;

// TODO: Dark silhouette shader until discovered
public abstract class StaticMuseumItem
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
    private readonly List<MeshInstance> _silhouette = [];

    private readonly MeshInstance _placeholder;
    private readonly string _name;
    private readonly Dictionary<string, int> _amounts;
    private readonly IReadOnlyCollection<MeshInstance> _meshes;
    private TextLabel? _label;

    public StaticMuseumItem(
        MeshInstance placeholder,
        Spatial parent,
        IReadOnlyCollection<MeshInstance>? meshes,
        string name,
        Dictionary<string, int> amounts
    )
    {
        _placeholder = placeholder;
        _name = name;
        _amounts = amounts;
        MeshInstance = (MeshInstance)_placeholder.Duplicate();

        CreateText();

        // MeshInstance.MaterialOverride = InvisibleShaderMaterial;
        MeshInstance.CreateTrimeshCollision();
        MeshInstance.GetChildOfType<StaticBody>()!.AddToGroup(GroupName);

        if (meshes != null)
        {
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
            foreach (var mesh in meshes)
            {
                placement.Content.AddChild(mesh);
            }
            _meshes = meshes;
            foreach (var mesh in meshes ?? [])
            {
                _silhouette.Add(SilhouetteOverlay.Create(mesh, mixLevel: 0.8f));
                mesh.Visible = false;
            }
        }
        else
        {
            Out.Error($"Model for {name} not found");
            _meshes = [];
        }
    }

    private void SetNumber(int number)
    {
        if (number > 0)
        {
            foreach (var silhouette in _silhouette)
            {
                SilhouetteOverlay.SetMixLevel(silhouette, 0);
            }
            foreach (var mesh in _meshes)
            {
                mesh.Visible = true;
            }
        }
        _label?.BbcodeText = $"x{number}";
    }

    public void InceraseNumber()
    {
        SetNumber(_amounts[_name]);
    }

    private void CreateText()
    {
        var plane = MeshInstance.GetChildOfTypeByName<MeshInstance>("counter_plane");
        if (plane is null)
        {
            return;
        }
        var viewport = new Viewport { Size = new Vector2(1024, 512) };
        var label = new TextLabel();
        viewport.AddChild(label.Node);
        plane.MaterialOverride = new SpatialMaterial { AlbedoTexture = viewport.GetTexture() };
    }
}
