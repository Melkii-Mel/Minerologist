using System;
using System.Collections.Generic;

public class Inventory
{
    private readonly List<ItemStack?> _itemStacks = [];

    public event Action<IInventoryAction>? InventoryUpdated;

    public void AddItem(Entity entity)
    {
        var prefab = entity.Prefab;
        var itemStack = new ItemStack(prefab);
        for (var i = 0; i < _itemStacks.Count; i++)
        {
            var stack = _itemStacks[i];
            if (stack == null)
            {
                _itemStacks[i] = stack;
                InventoryUpdated?.Invoke(new SlotAdded(i));
                return;
            }
        }
        _itemStacks.Add(itemStack);
        InventoryUpdated?.Invoke(new SlotAdded(_itemStacks.Count - 1));
    }

    public ItemStack? FlushItem()
    {
        if (_itemStacks.Count == 0)
        {
            return null;
        }
        ShiftItemsToFillFirstSlot();
        var removed = _itemStacks[0];
        InventoryUpdated?.Invoke(new SlotRemoved(0, removed!.Value));
        ShiftItemsToFillFirstSlot();
        return removed;
    }

    public void ShiftItemsToFillFirstSlot()
    {
        if (_itemStacks.Count == 0)
        {
            return;
        }
        while (_itemStacks[0] == null)
        {
            for (int i = 0; i < _itemStacks.Count - 1; i++)
            {
                _itemStacks[i] = _itemStacks[i + 1];
                InventoryUpdated?.Invoke(new SlotMoved(i + 1, i));
            }
        }
    }
}

public record struct ItemStack(EntityPrefab Item);

public interface IInventoryAction;

public record SlotAdded(int SlotIndex) : IInventoryAction;

public record SlotRemoved(int SlotIndex, ItemStack LastContents) : IInventoryAction;

public record SlotMoved(int OldSlotIndex, int NewSlotIndex) : IInventoryAction;
