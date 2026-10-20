using System.Collections.Generic;
using Tomlyn.Serialization;

public sealed record Config
{
    public ConfigConfig ConfigConfig { get; init; } = new();
    public PlayerStatsConfig PlayerStatsConfig { get; init; } = new();
    public PlayerVisualsConfig PlayerVisualsConfig { get; init; } = new();
    public UpgradesConfig UpgradesConfig { get; init; } = new();
    public GenerationConfig GenerationConfig { get; init; } = new();
    public ControlsConfig ControlsConfig { get; init; } = new();
    public WorldConfig WorldConfig { get; init; } = new();
    public RemnantsConfig RemnantsConfig { get; init; } = new();
    public RemnantsSizeConfig RemnantsSizeConfig { get; init; } = new();
    public MarchingCubesOptions MarchingCubesOptions { get; init; } = new();
    public ExpConfig ExpConfig { get; init; } = new();

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
        ExpConfig.Initialize();
    }
}

public record RemnantsSizeConfig
{
    public Dictionary<string, Size> Sizes { get; init; } =
        new() { ["Skeleton"] = new(10.0f, 12.0f), ["Others"] = new(0.5f, 1.5f) };
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
    public int HoleRadiusChunks { get; init; } = 6;
    public bool XRay { get; init; } = false;
    public Vector3I ChunkSize { get; init; } = new(8, 8, 8);
    public float CellHealth { get; init; } = 100.0f;
    public float CellHealthScaling { get; init; } = 0.3f;
    public int VerticalGenerationDistanceChunks { get; init; } = 3;
}

public sealed record UpgradesConfig;

public sealed record PlayerStatsConfig
{
    public float Acceleration { get; init; } = 200.0f;
    public float Deceleration { get; init; } = 400.0f;
    public FallingObjectConfig FallingObjectConfig { get; init; } = new();
}

public sealed record PlayerVisualsConfig
{
    public string ToolModel { get; init; } = "Tools/Default";
    public Vector3I ToolOffset { get; init; } = Vector3I.Zero;
    public float SwingAmplitudeX { get; init; } = 0.0f;
    public float SwingAmplitudeY { get; init; } = 0.0f;
    public float SwingFrequency { get; init; } = 1.0f;
}

public sealed record PlayerDiggingStats;

public sealed record NoiseConfig
{
    public int Octaves { get; init; } = 2;
    public float Period { get; init; } = 0.5f;
    public float Persistence { get; init; } = 0.8f;
    public float SpawnThreshold { get; init; } = 0.8f;
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
    public string DigDamage { get; init; } = "100 * lvl";
    public string DigArea { get; init; } = "2 + log(lvl, 2)";
    public string DigRange { get; init; } = "5 + lvl * 2";
    public string DigOutSpeed { get; init; } = "1 + log(lvl, 2)";
    public string ExpMultiplier { get; init; } = "1";
    public string JumpVelocity { get; init; } = "25 + lvl * 2";
    public string MaxVelocity { get; init; } = "15 + lvl * 2";
    public Dictionary<string, float> ExpPerType { get; init; } =
        new() { ["Skeleton"] = 10, ["Others"] = 20 };
    public Dictionary<string, float> ExpOverrides { get; init; } = [];

    [TomlIgnore]
    public CompiledExpFormulas F { get; private set; } = null!;

    public void Initialize()
    {
        F = new(this);
    }
}

public sealed record CompiledExpFormulas
{
    public DynamicFormula ExpReq { get; private set; }
    public DynamicFormula DigDamage { get; private set; }
    public DynamicFormula DigArea { get; private set; }
    public DynamicFormula DigRange { get; private set; }
    public DynamicFormula DigOutSpeed { get; private set; }
    public DynamicFormula ExpMultiplier { get; private set; }
    public DynamicFormula JumpVelocity { get; private set; }
    public DynamicFormula MaxVelocity { get; private set; }

    public CompiledExpFormulas(ExpConfig c)
    {
        ExpReq = new(c.ExpReq);
        DigDamage = new(c.DigDamage);
        DigArea = new(c.DigArea);
        DigRange = new(c.DigRange);
        DigOutSpeed = new(c.DigOutSpeed);
        ExpMultiplier = new(c.ExpMultiplier);
        JumpVelocity = new(c.JumpVelocity);
        MaxVelocity = new(c.MaxVelocity);
    }
}

public sealed record ConfigConfig
{
    public bool WriteConfigTemplate { get; init; } = true;
    public bool WriteMergedConfig { get; init; } = true;
    public string ConfigTemplatePath { get; init; } = "Assets/config.template.toml";
    public string MergedConfigPath { get; init; } = "Assets/config.merged.toml";
}
