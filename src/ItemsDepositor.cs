using System;

public class ItemsDepositor
{
    private readonly Inventory _inventory;
    private readonly Session _session;

    private bool _depositEnabled = false;

    private float _depositDuration;
    private float _depositTimer;

    private double CurrentDepositDelay => 1 / DepositPerSecond.Calculate(_depositDuration);

    public DepositFormula DepositPerSecond => Config.Instance.DepositConfig.F.DepositPerSecond;

    public Upgrades Upgrades { get; }

    public ItemsDepositor(
        Action<Action<float>> bindUpdate,
        Inventory inventory,
        Upgrades upgrades,
        Session session
    )
    {
        _inventory = inventory;
        Upgrades = upgrades;
        _session = session;
        bindUpdate(Update);
    }

    public void DepositEnable()
    {
        if (_depositEnabled)
        {
            return;
        }
        _depositEnabled = true;
        _depositTimer = 0;
        _depositDuration = 0;
    }

    public void DepositDisable()
    {
        _depositEnabled = false;
    }

    public void Update(float delta)
    {
        if (!_depositEnabled)
        {
            return;
        }
        _depositTimer += delta;
        _depositDuration += delta;
        while (_depositTimer > CurrentDepositDelay)
        {
            _depositTimer -= (float)CurrentDepositDelay;
            Deposit();
        }
    }

    private void Deposit()
    {
        var itemStack = _inventory.FlushItem();
        if (!itemStack.HasValue)
        {
            DepositDisable();
            return;
        }
        var entityPrefab = itemStack.Value.Item;
        Upgrades.Upgrade(entityPrefab);
        Server.SendItem(entityPrefab);
        var mesh = Animations.AddAnimation(
            new AnimationEntityFlyWithArc(),
            onFinish: () =>
            {
                mesh.QueueFree();
            }
        );
    }
}
