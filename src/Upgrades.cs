using System;

public class Upgrades
{
    public ExpConfig ExpConfig => Config.Instance.ExpConfig;

    public int Level { get; private set; } = 1;

    public double ExpMultiplier => ExpConfig.F.ExpMultiplier.Calculate(Level);

    public double CurrentExp { get; private set; }

    public double CurrentExpReq => ExpConfig.F.ExpReq.Calculate(Level);

    public event Action? LevelChanged;
    public event Action? CurrentExpChanged;

    public void Reset()
    {
        Godot.GD.PrintErr("Upgrades reset is not implemented");
    }

    public void Upgrade(EntityPrefab entityPrefab)
    {
        var type = entityPrefab.Type;
        var id = entityPrefab.Id;
        var fullName = string.Join("/", type, id);

        var expFound =
            ExpConfig.ExpOverrides.TryGetValue(fullName, out var exp)
            || ExpConfig.ExpPerType.TryGetValue(type, out exp)
            || ExpConfig.ExpPerType.TryGetValue("Others", out exp);

        if (!expFound)
        {
            throw new InvalidOperationException(
                $"No experience value is configured for entity {fullName} of type '{type}', and no 'Others' fallback exists."
            );
        }

        IncreaseExp(exp * ExpMultiplier);
    }

    private void IncreaseExp(double v)
    {
        CurrentExp += v;
        while (CurrentExp > CurrentExpReq)
        {
            CurrentExp -= CurrentExpReq;
            Level += 1;
            LevelChanged?.Invoke();
            Godot.GD.Print($"LEVEL UP! Current LVL: {Level}");
        }
        CurrentExpChanged?.Invoke();
    }
}
