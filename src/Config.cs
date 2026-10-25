using System.Collections.Generic;
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

public record ConeCasterConfig
{
    public int Rings { get; init; } = 3;
    public int RaysPerRing { get; init; } = 8;
}

public record PipeConfig
{
    public float UnitHeight { get; init; } = 100;
    public float Scale { get; init; } = 3;
    public float GenerationChance { get; init; } = 1;
    public bool AlwaysGenerateTop { get; init; } = true;
    public int GenerationRange { get; init; } = 4;
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
    public float BorderDigDamageMultiplier { get; init; } = .2f;
    public float DistanceDamageScaling { get; init; } = .7f;
    public float DamageChance { get; init; } = .8f;
    public float DamageScaleMin { get; init; } = .5f;
    public float DamageScaleMax { get; init; } = 2;
    public float DamageTicksPerSecond { get; init; } = 40;
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
}

public sealed record GenerationConfig
{
    public NoiseConfig NoiseConfig { get; init; } = new();
    public int HoleRadiusChunks { get; init; } = 2;
    public bool XRay { get; init; } = false;
    public Vector3I ChunkSize { get; init; } = new(16, 16, 16);
    public string CellHealth { get; init; } = "100 * pow(1.3, depth / 16)";
    public int VerticalGenerationDistanceChunks { get; init; } = 3;
    public float BorderPadding { get; init; } = 0.8f;
    public float MinCellHealthMultiplier { get; init; } = -1;
    public float CellThresholdMultiplier { get; init; } = 0.05f;

    [TomlIgnore]
    public GenerationConfigFormulas F { get; private set; } = null!;
    public int EntityVariationsDetalization { get; init; } = 25;

    public void Init()
    {
        F = new(this);
    }
}

public record GenerationConfigFormulas
{
    public GenerationFormula CellHealth { get; }

    public GenerationConfigFormulas(GenerationConfig c)
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
    public float MaxFloorAngle { get; init; } = 45;
}

public sealed record PlayerVisualsConfig
{
    public float SwingAmplitudeX { get; init; } = 0.0f;
    public float SwingAmplitudeY { get; init; } = 0.0f;
    public float SwingFrequency { get; init; } = 1.0f;
}

public sealed record ToolVisualsConfig
{
    public string ToolModel { get; init; } = "Tools.Default.Plane";
    public Vector3I ToolRotationDegrees { get; init; } = new(0, 180, 0);
    public float ToolScale { get; init; } = 0.8f;
    public Vector3I ToolOffset { get; init; } = new(80, -160, -180);
}

public sealed record ToolAnimationConfig
{
    public float RotationIdleDegrees { get; init; } = 45;
    public float RotationDurationSeconds { get; init; } = 0.5f;
}

public sealed record NoiseConfig
{
    public int Octaves { get; init; } = 2;
    public float Period { get; init; } = 0.5f;
    public float Persistence { get; init; } = 0.8f;
    public float SpawnThreshold { get; init; } = 0.75f;
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
    public string DigDamage { get; init; } = "400 * pow(1.3, lvl)";
    public string DigArea { get; init; } = "1 + log(lvl, 4)";
    public string DigRange { get; init; } = "5 + lvl / 2";
    public string DigOutSpeed { get; init; } = "1 + log(lvl, 2)";
    public string ExpMultiplier { get; init; } = "1";
    public string JumpVelocity { get; init; } = "25 + lvl * 2";
    public string MaxVelocity { get; init; } = "15 + lvl * 2";
    public string DigConeRadius { get; init; } = "20";
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
    public LevelFormula ExpReq { get; private set; }
    public LevelFormula DigDamage { get; private set; }
    public LevelFormula DigArea { get; private set; }
    public LevelFormula DigRange { get; private set; }
    public LevelFormula DigOutSpeed { get; private set; }
    public LevelFormula ExpMultiplier { get; private set; }
    public LevelFormula JumpVelocity { get; private set; }
    public LevelFormula MaxVelocity { get; private set; }
    public LevelFormula DigConeRadius { get; private set; }

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
    }
}

public sealed record ConfigConfig
{
    public bool WriteConfigTemplate { get; init; } = true;
    public bool WriteMergedConfig { get; init; } = true;
    public string ConfigTemplatePath { get; init; } = "Assets/config.template.toml";
    public string MergedConfigPath { get; init; } = "Assets/config.merged.toml";
}
