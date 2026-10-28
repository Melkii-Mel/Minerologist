using Godot;

public class MuseumCamera
{
    public Camera Camera { get; private set; }

    public Vector3 Offset { get; private set; }

    private MuseumCameraConfig MuseumCameraConfig =>
        Config.Instance.MuseumConfig.MuseumCameraConfig;

    public MuseumCamera(MuseumRoot root)
    {
        Camera = new Camera();
        UpdateCameraConfig();
        root.Root.Update += Update;
    }

    private void Update(float delta)
    {
        UpdateCameraConfig();
    }

    private void UpdateCameraConfig()
    {
        Camera.Position = MuseumCameraConfig.Offset.ToGdVector3();
        Camera.Fov = MuseumCameraConfig.Fov;
    }
}

public enum MuseumCameraPosition
{
    Left,
    Right,
    Center,
}
