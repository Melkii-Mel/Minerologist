using System.Collections.Generic;

public sealed record Config(
    PlayerStatsConfig PlayerStatsConfig,
    PlayerVisualsConfig PlayerVisualsConfig,
    UpgradesConfig UpgradesConfig,
    GenerationConfig GenerationConfig,
    ControlsConfig ControlsConfig,
    WorldConfig WorldConfig,
    RemnantsConfig RemnantsConfig,
    RemnantsSizeConfig RemnantsSizeConfig,
    MarchingCubesOptions MarchingCubesOptions
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
            _configLoader ??= new ConfigLoader("Assets/config.toml", x => _instance = x);
            _instance ??= _configLoader.LoadConfig();
            return _instance;
        }
    }
}

public record RemnantsSizeConfig(Dictionary<string, Size> Sizes);

public record Size(float Min, float Max);

public sealed record RemnantsConfig(
    Dictionary<string, int> TypeWeights,
    int SkeletonPieceProbabilityWeight
);

public sealed record ControlsConfig(
    float DiggingCameraSensitivityMultiplier,
    float CameraSensitivityX,
    float CameraSensitivityY,
    int CameraMinAngleDegreesY,
    int CameraMaxAngleDegreesY,
    int CoyoteTime,
    int BufferTime
);

public sealed record GenerationConfig(
    NoiseConfig NoiseConfig,
    int HoleRadiusChunks,
    bool XRay,
    Vector3I ChunkSize,
    float CellHealth,
    float CellHealthScaling,
    int VerticalGenerationDistanceChunks
);

public sealed record UpgradesConfig();

public sealed record PlayerStatsConfig(
    float Acceleration,
    float MaxVelocity,
    float Deceleration,
    FallingObjectConfig FallingObjectConfig,
    float JumpVelocity,
    float DigRange,
    float DigRadius
);

public sealed record PlayerVisualsConfig(
    string ToolModel,
    Vector3I ToolOffset,
    float SwingAmplitudeX,
    float SwingAmplitudeY,
    float SwingFrequency
);

public sealed record PlayerDiggingStats();

public sealed record NoiseConfig(
    int Octaves,
    float Period,
    float Persistence,
    float SpawnThreshold
);

public sealed record WorldConfig(float Gravity, float CellSize, string TerrainColor);

public sealed record FallingObjectConfig(float GravityScale, float TerminalVelocity);
