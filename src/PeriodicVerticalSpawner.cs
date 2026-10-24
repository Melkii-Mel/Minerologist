using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract class PeriodicVerticalSpawner<TItem>(int count)
{
    public abstract float UnitHeight { get; }

    public float BorderDiameter =>
        Config.Instance.GenerationConfig.ChunkSize.X
        * Config.Instance.GenerationConfig.HoleRadiusChunks
        * 2
        * Config.Instance.GenerationConfig.BorderPadding;

    private int HoleSize => count;

    private int _lastVerticalPosition = int.MinValue;

    private readonly Dictionary<int, TItem> _instances = [];

    public void Update(Vector3 worldPosition)
    {
        Update(worldPosition.ToVector3I());
    }

    public void Update(Vector3I worldPosition)
    {
        var verticalPoistion = (int)(worldPosition.Y / UnitHeight);
        if (verticalPoistion == _lastVerticalPosition)
        {
            return;
        }
        _lastVerticalPosition = verticalPoistion;
        var required = new HashSet<int>();
        for (var i = -Mathf.FloorToInt(HoleSize / 2f); i < Mathf.CeilToInt(HoleSize / 2f); i++)
        {
            var position = verticalPoistion + i;
            required.Add(position);
            if (!_instances.ContainsKey(position))
            {
                var item = AddItem((int)(position * UnitHeight), worldPosition);
                _instances[position] = item;
            }
        }
        foreach (var position in _instances.Keys.ToArray())
        {
            if (!required.Contains(position))
            {
                RemoveItem(_instances[position]);
                _instances.Remove(position);
            }
        }
    }

    public abstract TItem AddItem(int verticalGlobalPosition, Vector3I worldPosition);
    public abstract void RemoveItem(TItem item);
}
