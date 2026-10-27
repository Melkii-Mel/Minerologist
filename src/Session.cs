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
    public InventoryUi InventoryUi { get; }
    public Crosshair Crosshair { get; }

    public readonly TerrainStreamer<VoxelGrid, GodotMeshBuilder> TerrainStreamer;
    public readonly Terrain Terrain;
    public readonly Border Border;
    public readonly Pipes Pipes;
    public readonly ItemsDepositor ItemsDepositor;
    public readonly ItemsDepositorAnimator ItemsDepositorAnimator;

    public event Action<float>? Update;
    public event Action<float>? PhysicsUpdate;
    public event Action<InputEvent>? Input;

    public Session(
        SessionContainer container,
        Spatial root3d,
        Control rootUi,
        Random rng,
        Action<Action<float>> bindUpdate,
        Action<Action<float>> bindPhysicsUpdate,
        Action<Action<InputEvent>> bindInput
    )
    {
        SessionContainer = container;

        Root3d = root3d;
        RootUi = rootUi;

        Terrain = new Terrain(root3d, this);
        Terrain.EntityRemoved += OnEntityRemove;
        GD.Print(string.Join(", ", MeshCollection.OtherMeshes.Keys));
        Border = new Border(root3d, MeshCollection.OtherMeshes["границы.Cylinder001"]);
        Pipes = new Pipes(root3d, MeshCollection.OtherMeshes["труба.Cylinder"], rng);

        Player = new(this, Root3d, Vector3.Zero, Terrain);
        Player.DepositItemsStart += OnDepositItemsStart;
        Player.DepositItemsStop += OnDepositItemsStop;

        Inventory = new();

        Inventory.InventoryUpdated += OnInventoryUpdate;

        InventoryUi = new(this);
        Crosshair = new(this);

        Upgrades = new();

        ItemsDepositor = new(u => Update += u, Inventory, Upgrades);
        ItemsDepositorAnimator = new(root3d, ItemsDepositor, Player.PlayerNode.Center);

        var _ = new PlayerLevelUi(this);

        bindUpdate(OnUpdate);
        bindPhysicsUpdate(OnPhysicsUpdate);
        bindInput(OnInput);

        var genConfig = Config.Instance.GenerationConfig;

        TerrainStreamer = new(
            genConfig.ChunkSize,
            genConfig.HoleRadiusChunks,
            genConfig.VerticalGenerationDistanceChunks,
            Terrain
        );

        SimpleParticles.Initialize(root3d, u => Update += u);

        Update += _ =>
        {
            var duration = new DurationDebug();
            TerrainStreamer.Update(Player.VerticalOriginPosition);
            Border.Update(Player.VerticalOriginPosition);
            Pipes.Update(Player.VerticalOriginPosition);
            //GD.Print(duration.TotalMilliseconds());
        };
    }

    private void OnDepositItemsStart(MeshInstance pipe)
    {
        ItemsDepositor.DepositEnable(pipe);
    }

    private void OnDepositItemsStop()
    {
        ItemsDepositor.DepositDisable();
    }

    private void OnEntityRemove(Entity entity)
    {
        Root3d.AddChild(entity.MeshInstance);
        entity.DestroyCollider();
        Animations.AddAnimation(
            new AnimationEntityFlyTowards(entity.MeshInstance, Player.PlayerNode.Center),
            onFinish: () =>
            {
                entity.MeshInstance.QueueFree();
                Inventory.AddItem(entity);
                // AudioCollection.PlayRandomSound(Sounds.Collect);
            }
        );
        Animations.AddAnimation(
            new AnimationRotateRandomly(entity.MeshInstance),
            easingFunction: new EFLinear()
        );
        Animations.AddAnimation(new AnimationScale(entity.MeshInstance, 0.05f));
    }

    private void OnInventoryUpdate(InventoryActionSlotSet action)
    {
        InventoryUi.Update(action);
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

    private void OnInput(InputEvent e)
    {
        Input?.Invoke(e);
    }
}
