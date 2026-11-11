using Godot;

public class MuseumDebug
{
    public MuseumRoot Root { get; }
    public MuseumState State => Root.State;
    public MuseumPlacer Placer => Root.Building.Placer;

    public MuseumDebug(MuseumRoot root)
    {
        Root = root;
        Root.Root.Input += Input;
    }

    private void Input(InputEvent @event)
    {
        if (@event is InputEventKey key)
        {
            if (key.Pressed && key.Scancode == (int)KeyList.N && key.Control)
            {
                var oldSkeleton = State.CurrentSkeleton;
                State.CompleteSkeleton(Root);
                Placer.ReplaceCurrentSkeleton(State.CurrentSkeleton, oldSkeleton);
                foreach (var diggable in MeshCollection.OtherDiggablesFlat.Keys)
                {
                    Root.DiggablesReceiver.AddDiggable(new("", diggable));
                }
            }
        }
    }
}
