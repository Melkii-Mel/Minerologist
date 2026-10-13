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
    public float TimeScale { get; set; }
    private TerrainStreamer<MeshContainer> _terrainStreamer;

    public event Action<float>? OnUpdate;
    public event Action<float>? OnPhysicsUpdate;

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
        Player = new(this, Root3d);
        Inventory = new(this);
        Upgrades = new(this);
        bindUpdate(Update);
        bindPhysicsUpdate(PhysicsUpdate);
        _terrainStreamer = new(
            new MeshContainer(),
            new SpatialMaterial(),
            Config.Instance.GenerationConfig.ChunkSize,
            Config.Instance.GenerationConfig.HoleRadiusChunks,
            new TerrainGenerator()
        );
        RestartSession();
    }

    private void Update(float delta)
    {
        delta *= TimeScale;
        if (delta == 0)
            return;
        OnUpdate?.Invoke(delta);
    }

    private void PhysicsUpdate(float delta)
    {
        delta *= TimeScale;
        if (delta == 0)
            return;
        OnPhysicsUpdate?.Invoke(delta);
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
