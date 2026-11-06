using Godot;

public class MuseumCamera
{
    private string _modeName;
    private MuseumCameraMode Mode => MuseumCameraConfig.CameraModes[_modeName];

    public Camera Camera { get; private set; }

    private MuseumCameraConfig MuseumCameraConfig =>
        Config.Instance.MuseumConfig.MuseumCameraConfig;

    private Vector3 DefaultOffset => MuseumCameraConfig.Offset.ToGdVector3() / 100;
    private Vector3 DefaultRotationDegrees => MuseumCameraConfig.RotationDegrees.ToGdVector3();

    public MuseumCamera(MuseumRoot root)
    {
        Camera = new Camera();
        root.AddChild(Camera);
        root.Root.Update += Update;
        _modeName = SetMode("Back");
        Camera.Position = MuseumCameraConfig.Offset.ToGdVector3() / 100;
        Camera.RotationDegrees = DefaultRotationDegrees;
        Camera.Fov = MuseumCameraConfig.Fov;
    }

    private void Update(float delta)
    {
        Camera.Fov = MuseumCameraConfig.Fov;

        var currentOffset = Camera.Position - DefaultOffset;
        var currentRotationDegrees = Camera.RotationDegrees - DefaultRotationDegrees;
        Camera.Position =
            DefaultOffset
            + MoveTowards.Calculate(
                currentOffset,
                Mode.Offset.ToGdVector3() / 100,
                MuseumCameraConfig.RelativeTransitionSpeed,
                MuseumCameraConfig.MinPositionTransitionSpeed,
                delta
            );
        Camera.RotationDegrees =
            DefaultRotationDegrees
            + MoveTowards.Calculate(
                currentRotationDegrees,
                Mode.Rotation.ToGdVector3(),
                MuseumCameraConfig.RelativeTransitionSpeed,
                MuseumCameraConfig.MinRotationTransitionSpeed,
                delta
            );
    }

    public string SetMode(string modeName)
    {
        _modeName = modeName;
        return modeName;
    }
}
