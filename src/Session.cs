using System;
using Godot;

public class Session
{
    public readonly SessionContainer SessionContainer;
    public readonly Spatial Root3d;
    public readonly Control RootUi;
    public readonly Inventory Inventory;
    public readonly Player Player;
    public readonly Upgrades Upgrades;
    public float TimeScale { get; set; } = 1;
    public readonly TerrainStreamer<VoxelGrid, GodotMeshBuilder> TerrainStreamer;
    public readonly Terrain Terrain;

    public event Action<float>? Update;
    public event Action<float>? PhysicsUpdate;
    public event Action<InputEvent>? Input;

    public Session(
        SessionContainer container,
        Spatial root3d,
        Control rootUi,
        Action<Action<float>> bindUpdate,
        Action<Action<float>> bindPhysicsUpdate
    )
    {
        SessionContainer = container;

        Root3d = root3d;
        RootUi = rootUi;

        Terrain = new Terrain(root3d);

        Player = new(this, Root3d, Vector3.Zero, Terrain);

        Inventory = new(this);

        Upgrades = new(this);

        bindUpdate(OnUpdate);
        bindPhysicsUpdate(OnPhysicsUpdate);

        var genConfig = Config.Instance.GenerationConfig;

        TerrainStreamer = new(
            genConfig.ChunkSize,
            genConfig.HoleRadiusChunks,
            genConfig.VerticalGenerationDistanceChunks,
            Terrain
        );

        Update += _ =>
        {
            var duration = new DurationDebug();
            TerrainStreamer.Update(Player.VerticalOriginPosition);
            //GD.Print(duration.TotalMilliseconds());
        };

        RestartSession();
    }

    private void OnUpdate(float delta)
    {
        delta *= TimeScale;
        if (delta == 0)
            return;
        Update?.Invoke(delta);
    }

    private void OnPhysicsUpdate(float delta)
    {
        delta *= TimeScale;
        if (delta == 0)
            return;
        PhysicsUpdate?.Invoke(delta);
    }

    public void RestartSession()
    {
        Player.ResetPlayerStats();
        ResetUpgrades();
        ClearInventory();
        ResetTerrain();
    }

    private void ResetUpgrades()
    {
        Upgrades.Reset();
    }

    private void ClearInventory() { }

    private void ResetTerrain() { }
}
