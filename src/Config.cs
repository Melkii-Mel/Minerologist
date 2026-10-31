using System;
using System.Collections.Generic;
using Godot;
using Tomlyn.Serialization;

public sealed record Config
{
    public ConfigConfig ConfigConfig { get; init; } = new();
    public PlayerStatsConfig PlayerStatsConfig { get; init; } = new();
    public PlayerVisualsConfig PlayerVisualsConfig { get; init; } = new();
    public GenerationConfig GenerationConfig { get; init; } = new();
    public ControlsConfig ControlsConfig { get; init; } = new();
    public WorldConfig WorldConfig { get; init; } = new();
    public RemnantsConfig RemnantsConfig { get; init; } = new();
    public RemnantsSizeConfig RemnantsSizeConfig { get; init; } = new();
    public MarchingCubesOptions MarchingCubesOptions { get; init; } = new();
    public ExpConfig ExpConfig { get; init; } = new();
    public DiggerConfig DiggerConfig { get; init; } = new();
    public DepositConfig DepositConfig { get; init; } = new();
    public PipeConfig PipeConfig { get; init; } = new();
    public ConeCasterConfig ConeCasterConfig { get; init; } = new();
    public DiggerVisualsConfig DiggerVisualsConfig { get; init; } = new();
    public ExpUiConfig ExpUiConfig { get; init; } = new();
    public CrosshairConfig CrosshairConfig { get; init; } = new();
    public MuseumConfig MuseumConfig { get; init; } = new();
    public ServerConfig ServerConfig { get; init; } = new();

    private static ConfigLoader? _configLoader;
    private static Config? _instance;

    /// <summary>
    /// The current configuration. May be replaced at runtime.
    /// Use this property directly rather than storing intermediate references,
    /// as they may become stale after a configuration update.
    /// </summary>
    public static Config Instance
    {
        get
        {
            _configLoader ??= new ConfigLoader("Assets/config.toml", x => _instance = x);
            _instance ??= _configLoader.LoadConfig();
            return _instance;
        }
    }

    public void Initialize()
    {
        ExpConfig.Init();
        DepositConfig.Init();
        GenerationConfig.Init();
    }
}

public record ServerConfig
{
    public uint PortSeed { get; init; } = 322;
}

public record MuseumConfig
{
    public MuseumBuildingConfig MuseumBuildingConfig { get; init; } = new();
    public MuseumCameraConfig MuseumCameraConfig { get; init; } = new();
    public MuseumPlacerConfig MuseumPlacerConfig { get; init; } = new();
}

public record MuseumPlacerConfig
{
    public string MainSkeletonPlaceholderName { get; init; } = "placeholder_current_skeleton";
    public string OtherSkeletonsParentName { get; init; } = "placeholders_other_skeletons";
    public string DiggablesParentName { get; init; } = "placeholders_diggables";

    public Dictionary<string, Vector3I> RotationsDegrees { get; init; } =
        new()
        {
            ["Мамонт"] = new(0, -75, 0),
            [MeshCollection.SKELETONS] = new(0, -105, 0),
            [MeshCollection.OTHERS] = Vector3I.Zero,
        };
    public Dictionary<string, float> Scales { get; init; } =
        new()
        {
            ["Мамонт"] = 1.5f,
            [MeshCollection.SKELETONS] = 1,
            [MeshCollection.OTHERS] = 1,
        };
    public Dictionary<string, Vector3I> Offsets { get; init; } =
        new()
        {
            ["Мамонт"] = Vector3I.Zero,
            [MeshCollection.SKELETONS] = Vector3I.Zero,
            [MeshCollection.OTHERS] = Vector3I.Zero,
        };
}

public record MuseumCameraConfig
{
    public Vector3I Offset { get; init; } = Vector3I.Zero;
    public float Fov { get; init; } = 93;
    public Dictionary<string, Vector3I> Offsets { get; init; } =
        new()
        {
            ["Left"] = Vector3I.Zero,
            ["Center"] = Vector3I.Zero,
            ["Right"] = Vector3I.Zero,
        };
    public Dictionary<string, float> Rotations { get; init; } =
        new()
        {
            ["Left"] = 90,
            ["Center"] = 0,
            ["Right"] = -90,
        };
    public float PositionTransitionSpeed { get; init; } = 10;
    public float RotationTransitionSpeed { get; init; } = 45;
}

public record MuseumBuildingConfig
{
    public Vector3I Offset { get; init; } = Vector3I.Zero;
}

public record CrosshairConfig
{
    public Dictionary<
        string,
        Dictionary<string, CrosshairPartState>
    > CrosshairModeConfigs { get; init; } =
        new()
        {
            ["Air"] = new()
            {
                ["Left"] = new(0, 0, 45),
                ["Right"] = new(0, 0, -45),
                ["Top"] = new(0, 0, -45),
                ["Bottom"] = new(0, 0, 45),
            },
            ["Diggable"] = new()
            {
                ["Left"] = new(-1.1f, 0, 0),
                ["Right"] = new(1.1f, 0, 0),
                ["Top"] = new(0, -1.1f, 90),
                ["Bottom"] = new(0, 1.1f, 90),
            },
            ["Ground"] = new()
            {
                ["Left"] = new(-0.6f, -0.1f, -90),
                ["Right"] = new(0.6f, 0.1f, -90),
                ["Top"] = new(0.1f, -0.6f, -180),
                ["Bottom"] = new(-0.1f, 0.6f, -180),
            },
            ["Pipe"] = new()
            {
                ["Left"] = new(0, 0, 0),
                ["Right"] = new(0, 0, 0),
                ["Top"] = new(0, 0, 0),
                ["Bottom"] = new(0, 0, 0),
            },
        };

    public float RelativeTransitionSpeed { get; init; } = 10;
    public float MinRotationSpeedDegrees { get; init; } = 30;
    public float MinOffsetTransitionSpeed { get; init; } = 1;
    public float MinScaleTransitionSpeed { get; init; } = 1;
    public float CrosshairPartSizeX { get; init; } = 100;
    public float CrosshairPartSizeY { get; init; } = 15;
    public float ActiveScale { get; init; } = 0.8f;
    public float InactiveScale { get; init; } = 0.5f;
}

public record ExpUiConfig
{
    public float LvlVelocity { get; init; } = 1;
    public float MinVelocity { get; init; } = 0.01f;
    public string BgColor { get; init; } = "#DBA25D";
    public string MainColor { get; init; } = "#FFDF94";
    public string TextColor { get; init; } = "#4A1E00";
    public float Radius { get; init; } = 30;
    public float Width { get; init; } = 8;
}

public record ConeCasterConfig
{
    public int Rings { get; init; } = 3;
    public int RaysPerRing { get; init; } = 8;
}

public record PipeConfig
{
    public float UnitHeight { get; init; } = 15;
    public float Scale { get; init; } = 3;
    public float GenerationChance { get; init; } = 1;
    public bool AlwaysGenerateTop { get; init; } = true;
    public int GenerationRange { get; init; } = 15;
}

public record DepositConfig
{
    public string DepositPerSecond => "deposit_duration * 2";

    [TomlIgnore]
    public DepositConfigFormulas F { get; private set; } = null!;

    public void Init()
    {
        F = new(this);
    }
}

public record DepositConfigFormulas
{
    public DepositFormula DepositPerSecond { get; }

    public DepositConfigFormulas(DepositConfig c)
    {
        DepositPerSecond = new(c.DepositPerSecond);
    }
}

public record DiggerConfig
{
    public float BorderDigDamageMultiplier { get; init; } = 0.2f;
    public float DistanceDamageScaling { get; init; } = 0.7f;
    public float DamageChance { get; init; } = 0.8f;
    public float DamageScaleMin { get; init; } = 0.5f;
    public float DamageScaleMax { get; init; } = 2.0f;
    public float DamageTicksPerSecond { get; init; } = 60.0f;
    public float DiggingBuildupSeconds { get; init; } = 0.5f;
}

public record RemnantsSizeConfig
{
    public Dictionary<string, Size> Sizes { get; init; } =
        new() { ["Мамонт"] = new(10.0f, 12.0f), ["Others"] = new(0.5f, 1.5f) };
}

public record Size(float Min = 0.5f, float Max = 1.5f);

public sealed record RemnantsConfig
{
    public Dictionary<string, int> TypeWeights { get; init; } = [];
    public Dictionary<string, Dictionary<string, int>> WeightOverrides { get; init; } = [];
    public int SkeletonPieceProbabilityWeight { get; init; } = 10;
}

public sealed record ControlsConfig
{
    public float DiggingCameraSensitivityMultiplier { get; init; } = 0.5f;
    public float CameraSensitivityX { get; init; } = 0.002f;
    public float CameraSensitivityY { get; init; } = 0.002f;
    public int CameraMinAngleDegreesY { get; init; } = -90;
    public int CameraMaxAngleDegreesY { get; init; } = 90;
    public int CoyoteTime { get; init; } = 250;
    public int BufferTime { get; init; } = 250;
    public float RestartDuration { get; init; } = 3;
    public float Fov { get; init; } = 75;
}

public sealed record GenerationConfig
{
    public bool XRay { get; init; } = false;
    public float BorderPadding { get; init; } = 0.8f;
    public TerrainGenerationConfig TerrainGenerationConfig { get; init; } = new();
    public EntitiesGenerationConfig EntitiesGenerationConfig { get; init; } = new();

    public void Init()
    {
        TerrainGenerationConfig.Init();
        EntitiesGenerationConfig.Init();
    }
}

public sealed record TerrainGenerationConfig
{
    public Vector3I ChunkSize { get; init; } = new(8, 8, 8);
    public int HoleRadiusChunks { get; init; } = 2;
    public string CellHealth { get; init; } = "50 * pow(2, (depth / 8 - 1))";
    public int VerticalGenerationDistanceChunks { get; init; } = 6;
    public float MinCellHealthMultiplier { get; init; } = -1.0f;
    public float CellThresholdMultiplier { get; init; } = 0.05f;

    [TomlIgnore]
    public GenerationConfigFormulas F { get; private set; } = null!;

    public void Init()
    {
        F = new(this);
    }
}

public sealed record EntitiesGenerationConfig
{
    public int EntityVariationsDetalization { get; init; } = 25;
    public NoiseConfig NoiseConfig { get; init; } = new();
    public Vector3I ChunkSize { get; init; } = new(4, 4, 4);
    public int LoadRadiusChunks { get; init; } = 2;
    public int UnloadRadiusChunks { get; init; } = 10;
    public int VerticalLoadDistanceChunks { get; init; } = 1;
    public int VerticalUnloadDistanceChunks { get; init; } = 10;

    public void Init() { }
}

public record GenerationConfigFormulas
{
    public GenerationFormula CellHealth { get; }

    public GenerationConfigFormulas(TerrainGenerationConfig c)
    {
        CellHealth = new(c.CellHealth);
    }
}

public sealed record PlayerStatsConfig
{
    public float Acceleration { get; init; } = 200.0f;
    public float Deceleration { get; init; } = 400.0f;
    public FallingObjectConfig FallingObjectConfig { get; init; } = new();
    public bool StopOnSlope { get; init; } = true;
    public float MaxFloorAngle { get; init; } = 45.0f;
}

public sealed record PlayerVisualsConfig
{
    public float SwingAmplitudeX { get; init; } = 0.0f;
    public float SwingAmplitudeY { get; init; } = 0.0f;
    public float SwingFrequency { get; init; } = 1.0f;
}

public sealed record DiggerVisualsConfig
{
    public string ToolModel { get; init; } = "Tools.Default.Plane";
    public Vector3I ToolRotationDegrees { get; init; } = new(0, 180, 0);
    public float ToolScale { get; init; } = 0.8f;
    public Vector3I ToolOffset { get; init; } = new(80, -160, -180);
    public DiggerAnimationConfig DiggerAnimationConfig { get; init; } = new();
}

public sealed record DiggerAnimationConfig
{
    public Vector3I AnimationPivotOffset { get; init; } = new(0, 120, 90);
    public Vector3I OffsetIdle { get; init; } = new(20, -30, 20);
    public Vector3I RotationIdleDegrees { get; init; } = new(-740, -20, 0);
    public float RotationDurationSeconds { get; init; } = 0.25f;
}

public sealed record NoiseConfig
{
    public int Octaves { get; init; } = 2;
    public float Period { get; init; } = 0.5f;
    public float Persistence { get; init; } = 0.8f;
    public float SpawnThreshold { get; init; } = 0.76f;
    public float Lacunarity { get; init; } = 10.0f;
}

public sealed record WorldConfig
{
    public float Gravity { get; init; } = 98.0f;
    public float CellSize { get; init; } = 1.0f;
    public string TerrainColor { get; init; } = "#75553A";
}

public sealed record FallingObjectConfig
{
    public float GravityScale { get; init; } = 1.0f;
    public float TerminalVelocity { get; init; } = 200.0f;
}

public sealed record ExpConfig
{
    public string ExpReq { get; init; } = "30 * pow(lvl, 1.5)";
    public string DigDamage { get; init; } = "200 * pow(1.3, lvl)";
    public string DigArea { get; init; } = "0.5 + log(lvl, 4)";
    public string DigRange { get; init; } = "5 + lvl / 2";
    public string DigOutSpeed { get; init; } = "0.5 + log(lvl / 2, 4)";
    public string ExpMultiplier { get; init; } = "1";
    public string JumpVelocity { get; init; } = "20 + lvl * 2";
    public string MaxVelocity { get; init; } = "10 + lvl * 1.5";
    public string DigConeRadius { get; init; } = "20";
    public string DigOutDamage { get; init; } = "0.3";
    public Dictionary<string, float> ExpPerType { get; init; } =
        new() { ["Skeleton"] = 10, ["Others"] = 20 };
    public Dictionary<string, float> ExpOverrides { get; init; } = [];

    [TomlIgnore]
    public LevelFormulas F { get; private set; } = null!;

    public void Init()
    {
        F = new(this);
    }
}

public sealed record LevelFormulas
{
    public LevelFormula ExpReq { get; }
    public LevelFormula DigDamage { get; }
    public LevelFormula DigArea { get; }
    public LevelFormula DigRange { get; }
    public LevelFormula DigOutSpeed { get; }
    public LevelFormula ExpMultiplier { get; }
    public LevelFormula JumpVelocity { get; }
    public LevelFormula MaxVelocity { get; }
    public LevelFormula DigConeRadius { get; }
    public LevelFormula DigOutDamage { get; }

    public LevelFormulas(ExpConfig c)
    {
        ExpReq = new(c.ExpReq);
        DigDamage = new(c.DigDamage);
        DigArea = new(c.DigArea);
        DigRange = new(c.DigRange);
        DigOutSpeed = new(c.DigOutSpeed);
        ExpMultiplier = new(c.ExpMultiplier);
        JumpVelocity = new(c.JumpVelocity);
        MaxVelocity = new(c.MaxVelocity);
        DigConeRadius = new(c.DigConeRadius);
        DigOutDamage = new(c.DigOutDamage);
    }
}

public sealed record ConfigConfig
{
    public bool WriteConfigTemplate { get; init; } = true;
    public bool WriteMergedConfig { get; init; } = true;
    public string ConfigTemplatePath { get; init; } = "Assets/config.template.toml";
    public string MergedConfigPath { get; init; } = "Assets/config.merged.toml";
}
