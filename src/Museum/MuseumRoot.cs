using Godot;

public class MuseumRoot : Node
{
    public Root Root { get; }
    public MuseumCamera Camera { get; }
    public MuseumBuilding Building { get; }
    public MuseumDescriptions MuseumDescriptions { get; }
    public MuseumState State { get; }
    public DiggablesReceiver DiggablesReceiver { get; }
    public SkeletonSelector SkeletonSelector { get; }
    public MuseumServer Server { get; }
    public DescriptionsCollection DescriptionsCollection { get; }
    public MagneticPointer3d MagneticPointer3d { get; }
    public MuseumUiOverlay MuseumUiOverlay { get; }

    public Control RootUi { get; internal set; }
    public Node2D Root2d { get; internal set; }
    public Spatial Root3d { get; internal set; }

    public MuseumRoot(Root root)
    {
        RootUi = new();
        Root2d = new();
        Root3d = new();
        RootUi.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
        AddChild(RootUi);
        AddChild(Root2d);
        AddChild(Root3d);

        AddChild(SceneLoader.Instantiate<Node>("Environment"));

        State = MuseumState.LoadOrCreate();
        Root = root;
        Camera = new(this);
        DiggablesReceiver = new(this);
        SkeletonSelector = new(this);
        Server = new(this);
        DescriptionsCollection = new();

        MuseumUiOverlay = new(this);
        MagneticPointer3d = new MagneticPointer3d(this);

        if (State.CurrentSkeleton == MuseumState.CURRENT_SKELETON_UNINITIALIZED)
        {
            State.ReplaceSkeleton(this);
        }

        Building = new(this);

        MuseumDescriptions = new(this);

        State.AutoSave(this);
    }

    public override void _Ready() { }
}
