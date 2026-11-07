using Godot;

public class MuseumUiOverlay : Object
{
    public MuseumCameraConfig MuseumCameraConfig => Config.Instance.MuseumConfig.MuseumCameraConfig;
    public MuseumRoot Root { get; }

    public MuseumUiOverlay(MuseumRoot root)
    {
        var ui = SceneLoader.Instantiate<Control>("MuseumUiOverlay");
        root.RootUi.AddChild(ui);

        var buttons = ui.GetChildByName("Buttons")!.GetChildrenOfType<Button>();
        foreach (var button in buttons)
        {
            var name = button.Name;
            button.Connect("pressed", this, nameof(CameraButtonClick), [name]);
            Control parent = button;
            while (parent.GetParent() is Control control)
            {
                parent = control;
                parent.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
        }

        Root = root;
    }

    private void CameraButtonClick(string name)
    {
        var camera = Root.Camera;
        camera.SetMode(name);
        Root.MagneticPointer3d.CurrentTargetGroup = name;
    }
}
