using System;
using System.Collections.Generic;
using Godot;

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

    public MuseumRoot Root { get; }

    public ModelPlacement? ModelPlacement { get; }

    private readonly MeshInstance _placeholder;
    private readonly string _name;
    private readonly Dictionary<string, int> _amounts;
    private readonly IReadOnlyCollection<MeshInstance> _meshes;

    private TextLabel? _label;
    private Viewport? _counterViewport;

    public StaticMuseumItem(
        MeshInstance placeholder,
        Spatial parent,
        IReadOnlyCollection<MeshInstance>? meshes,
        string name,
        Dictionary<string, int> amounts,
        MuseumRoot root
    )
    {
        Root = root;
        Root.Root.Update += Update;
        _placeholder = placeholder;
        _name = name;
        _amounts = amounts;

        MeshInstance = (MeshInstance)_placeholder.Duplicate();

        MeshInstance.MaterialOverride = InvisibleShaderMaterial;
        MeshInstance.CreateTrimeshCollision();
        MeshInstance.GetChildOfType<StaticBody>()!.AddToGroup(GroupName);

        CreateText();

        if (meshes != null)
        {
            ModelPlacement = new ModelPlacement(
                parent,
                placeholder,
                meshes,
                ModelPlacementAnchorPresets.Bottom
            );

            foreach (var mesh in meshes)
            {
                ModelPlacement.Content.AddChild(mesh);

                _silhouette.Add(SilhouetteOverlay.Create(mesh, mixLevel: 0.8f));

                mesh.Visible = false;
            }

            _meshes = meshes;

            UpdateModelTransform();
        }
        else
        {
            Out.Error($"Model for {name} not found");
            _meshes = [];
        }

        UpdateNumber();
    }

    private void Update(float _)
    {
        UpdateModelTransform();
    }

    private void UpdateModelTransform()
    {
        ModelPlacement?.Apply(
            MuseumDynamicSkeleton.SelectRelevant(MuseumPlacerConfig.Sides.Scales, _name),
            MuseumDynamicSkeleton
                .SelectRelevant(MuseumPlacerConfig.Sides.RotationsDegrees, _name)
                .ToGdVector3(),
            MuseumDynamicSkeleton
                .SelectRelevant(MuseumPlacerConfig.Sides.Offsets, _name)
                .ToGdVector3() / 100
        );
    }

    private void SetCount(int count)
    {
        if (count > 0)
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

        _label?.Set("count", count.ToString());
    }

    public void UpdateNumber()
    {
        if (_amounts.TryGetValue(_name, out var count))
        {
            SetCount(count);
        }
        else
        {
            Out.Error($"Amount for {_name} not found");
        }
    }

    private void CreateText()
    {
        var plane = MeshInstance.GetChildOfTypeByNameRegex<MeshInstance>("^counter_plane");

        if (plane is null)
        {
            Out.Error("counter_plane not found");
            return;
        }

        _counterViewport = new Viewport
        {
            Size = new Vector2(
                MuseumPlacerConfig.CounterViewportWidth,
                MuseumPlacerConfig.CounterViewportHeight
            ),
            TransparentBg = true,
            RenderTargetUpdateMode = Viewport.UpdateMode.Always,
        };

        var label = new RichTextLabel
        {
            RectPosition = Vector2.Zero,
            RectSize = _counterViewport.Size,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
        };

        _label = new(label) { BbcodeText = "\n[center]x{count}[/center]" };
        MuseumPlacerConfig.CounterFontConfig.Apply(_label);

        _counterViewport.AddChild(_label.Node);
        Root.AddChild(_counterViewport);

        plane.MaterialOverride = new SpatialMaterial
        {
            AlbedoTexture = _counterViewport.GetTexture(),
            FlagsTransparent = true,
            ParamsCullMode = Material3D.CullMode.Disabled,
        };
    }
}
