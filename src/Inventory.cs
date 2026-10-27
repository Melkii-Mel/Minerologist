using System;
using System.Collections.Generic;

public class Inventory
{
    private readonly List<ItemStack?> _itemStacks = [];

    public event Action<InventoryActionSlotSet>? InventoryUpdated;

    public void AddItem(Entity entity)
    {
        var prefab = entity.Prefab;
        var itemStack = new ItemStack(prefab);
        for (var i = 0; i < _itemStacks.Count; i++)
        {
            if (_itemStacks[i] == null)
            {
                _itemStacks[i] = itemStack;
                InventoryUpdated?.Invoke(new InventoryActionSlotSet(i, itemStack));
                return;
            }
        }
        _itemStacks.Add(itemStack);
        InventoryUpdated?.Invoke(new InventoryActionSlotSet(_itemStacks.Count - 1, itemStack));
    }

    public ItemStack? FlushItem()
    {
        if (_itemStacks.Count == 0)
        {
            return null;
        }
        ShiftItemsToFillFirstSlot();
        var removed = _itemStacks[0];
        _itemStacks[0] = null;
        InventoryUpdated?.Invoke(new InventoryActionSlotSet(0, null));
        ShiftItemsToFillFirstSlot();
        return removed;
    }

    public void ShiftItemsToFillFirstSlot()
    {
        var removedCount = 0;

        if (_itemStacks[0] != null)
        {
            return;
        }

        while (_itemStacks.Count > 0 && _itemStacks[0] == null)
        {
            _itemStacks.RemoveAt(0);
            removedCount++;
        }

        for (var i = 0; i < _itemStacks.Count; i++)
        {
            InventoryUpdated?.Invoke(new InventoryActionSlotSet(i + removedCount, null));
            InventoryUpdated?.Invoke(new InventoryActionSlotSet(i, _itemStacks[i]));
        }
    }

    public void Clear()
    {
        _itemStacks.Clear();
    }
}

public record struct ItemStack(EntityPrefab Item);

public record InventoryActionSlotSet(int SlotIndex, ItemStack? ItemStack);
