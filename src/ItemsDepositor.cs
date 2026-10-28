using System;
using Godot;

public class ItemsDepositor
{
    private readonly Inventory _inventory;

    private bool _depositEnabled = false;

    private float _depositDuration;
    private float _depositTimer;

    private double CurrentDepositDelay => 1 / DepositPerSecond.Calculate(_depositDuration);

    public DepositFormula DepositPerSecond => Config.Instance.DepositConfig.F.DepositPerSecond;

    public Upgrades Upgrades { get; }

    public event Action<EntityPrefab>? Deposited;

    public MeshInstance? CurrentPipe { get; private set; }

    public ItemsDepositor(Action<Action<float>> bindUpdate, Inventory inventory, Upgrades upgrades)
    {
        _inventory = inventory;
        bindUpdate(Update);
        Upgrades = upgrades;
    }

    public void DepositEnable(MeshInstance pipe)
    {
        CurrentPipe = pipe;
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
        MuseumCommunication.SendItem(entityPrefab);
        Deposited?.Invoke(entityPrefab);
    }
}
