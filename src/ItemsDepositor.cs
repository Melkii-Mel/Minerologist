using System;

public class ItemsDepositor
{
    public Inventory _inventory;

    private bool _depositEnabled = false;

    private float _currentDepositSpeed;
    private float _currentDepositTimer;

    private float CurrentDepositDelay
    {
        get => 1 / _currentDepositSpeed;
        set => _currentDepositSpeed = 1 / value;
    }

    // TODO : Move to config
    public float MinDepositItemsPerSecond => 1;
    public float MaxDepositItemsPerSecond => 5;
    public float DeltaDepositItemsPerSecond => 1;

    public Upgrades Upgrades { get; }

    public ItemsDepositor(Action<Action<float>> bindUpdate, Inventory inventory, Upgrades upgrades)
    {
        _inventory = inventory;
        Upgrades = upgrades;
        bindUpdate(Update);
    }

    public void DepositEnable()
    {
        _depositEnabled = true;
        _currentDepositSpeed = MinDepositItemsPerSecond;
        _currentDepositTimer = 0;
    }

    public void DepositDisable()
    {
        _depositEnabled = false;
        _currentDepositSpeed = 0;
        _currentDepositTimer = 0;
    }

    public void Update(float delta)
    {
        if (_depositEnabled)
        {
            _currentDepositTimer += delta;
            while (_currentDepositTimer > CurrentDepositDelay)
            {
                _currentDepositTimer -= CurrentDepositDelay;
                Deposit();
            }
            _currentDepositSpeed += DeltaDepositItemsPerSecond * delta;
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
    }
}
