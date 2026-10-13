using System.Collections.Generic;

public sealed record Config(
    PlayerStatsConfig PlayerStatsConfig,
    UpgradesConfig UpgradesConfig,
    GenerationConfig GenerationConfig,
    ControlsConfig ControlsConfig,
    WorldConfig WorldConfig,
    RemnantsConfig RemnantsConfig,
    RemnantsSizeConfig RemnantsSizeConfig
)
{
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
            _configLoader ??= new ConfigLoader("NOT_IMPLEMENTED", x => _instance = x);
            _instance ??= _configLoader.LoadConfig();
            return _instance;
        }
    }
}

public record RemnantsSizeConfig(Size Skeleton, Size Others);

public record Size(float Min, float Max);

public sealed record RemnantsConfig(
    Dictionary<string, int> TypeWeights,
    int SkeletonPieceProbabilityWeight
);

public sealed record ControlsConfig(float CameraSensitivityX, float CameraSensitivityY);

public sealed record GenerationConfig(
    NoiseConfig NoiseConfig,
    int HoleRadiusChunks,
    bool XRay,
    Vector3I ChunkSize,
    float CellHealth,
    float CellHealthScaling
);

public sealed record UpgradesConfig();

public sealed record PlayerStatsConfig(
    float Acceleration,
    float MaxVelocity,
    float Deceleration,
    FallingObjectConfig FallingObjectConfig
);

public sealed record NoiseConfig(
    int Octaves,
    float Period,
    float Persistence,
    float SpawnThreshold
);

public sealed record WorldConfig(float Gravity, float CellSize);

public sealed record FallingObjectConfig(float GravityScale, float TerminalVelocity);
