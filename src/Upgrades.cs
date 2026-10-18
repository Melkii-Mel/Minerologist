public class Upgrades
{
    private readonly Session _session;

    public Upgrades(Session session)
    {
        _session = session;
    }

    internal void Reset()
    {
        Godot.GD.PrintErr("Upgrades reset is not implemented");
    }
}
