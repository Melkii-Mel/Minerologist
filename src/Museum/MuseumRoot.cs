using Godot;

public class MuseumRoot : Node
{
    public Root Root { get; }
    public MuseumCamera Camera { get; }
    public MuseumBuilding Building { get; }
    public MuseumState State { get; }
    public DiggablesReceiver DiggablesReceiver { get; }
    public SkeletonSelector SkeletonSelector { get; }
    public MuseumServer Server { get; }

    public MuseumRoot(Root root)
    {
        State = MuseumState.LoadOrCreate();
        Root = root;
        Camera = new(this);
        DiggablesReceiver = new(this);
        SkeletonSelector = new(this);
        Server = new(this);

        if (State.CurrentSkeleton == MuseumState.CURRENT_SKELETON_UNINITIALIZED)
        {
            State.CurrentSkeleton = SkeletonSelector.SelectSkeleton();
        }

        Building = new(this);
    }

    public override void _Ready() { }
}
