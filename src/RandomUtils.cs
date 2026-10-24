using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class RandomUtils
{
    public static int ChooseWeighted(IReadOnlyList<int> weights, Random rng)
    {
        int total = weights.Sum();

        int roll = rng.Next(total);

        for (int i = 0; i < weights.Count; i++)
        {
            if (roll < weights[i])
                return i;

            roll -= weights[i];
        }

        throw new InvalidOperationException();
    }

    public static T ChooseWeighted<T>(IReadOnlyList<(T Item, int Weight)> itemWeights, Random rng)
    {
        int itemIndex = ChooseWeighted([.. itemWeights.Select(iw => iw.Weight)], rng);
        return itemWeights[itemIndex].Item;
    }

    public static T ChooseRandomItem<T>(IReadOnlyList<T> items, Random rng)
    {
        var roll = rng.Next(items.Count);
        return items[roll];
    }

    public static Vector3 Rotation(Random rng)
    {
        return new Vector3(Float(rng), Float(rng), Float(rng)) * Mathf.Pi * 2;
    }

    public static float Float(Random rng)
    {
        return (float)rng.NextDouble();
    }

    internal static float FromRange(float min, float max, Random rng)
    {
        return (float)rng.NextDouble() * max + min;
    }
}
