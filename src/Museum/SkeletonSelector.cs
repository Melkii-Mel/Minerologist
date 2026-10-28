using System;
using System.Collections.Generic;
using System.Linq;

public class SkeletonSelector
{
    public MuseumRoot Root { get; }
    public string[] AllSkeletons { get; }
    public Dictionary<string, int> SkeletonsAmounts => Root.State.SkeletonsAmounts;

    readonly Random _rng;

    public SkeletonSelector(MuseumRoot root, Random? rng = null)
    {
        _rng = rng ?? new();
        Root = root;

        AllSkeletons = [.. MeshCollection.Skeletons.Keys];
    }

    public string SelectSkeleton()
    {
        var min = SkeletonsAmounts.Values.Min();
        return RandomUtils.ChooseRandomItem(
            SkeletonsAmounts.Where(a => a.Value > min).Select(kvp => kvp.Key).ToList(),
            _rng
        );
    }
}
