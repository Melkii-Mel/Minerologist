using System.Collections.Generic;
using Godot;

public class MuseumUiOverlay : Object
{
    public MuseumCameraConfig MuseumCameraConfig => Config.Instance.MuseumConfig.MuseumCameraConfig;
    public MuseumRoot Root { get; }

    private readonly List<Button> _buttons = [];

    public MuseumUiOverlay(MuseumRoot root)
    {
        var ui = SceneLoader.Instantiate<Control>("MuseumUiOverlay");
        root.RootUi.AddChild(ui);
        ui.Modulate = new(Config.Instance.MuseumConfig.MuseumUiOverlayConfig.ColorModulation);

        var buttons = ui.GetChildByName("Buttons")!.GetChildrenOfType<Button>();
        foreach (var button in buttons)
        {
            var name = button.Name;
            button.Connect("pressed", this, nameof(CameraButtonClick), [name]);
            _buttons.Add(button);
            Control parent = button;
        }

        Root = root;
    }

    private void CameraButtonClick(string name)
    {
        foreach (var button in _buttons)
        {
            button.Visible = name != button.Name;
        }
        var camera = Root.Camera;
        camera.SetMode(name);
        Root.MagneticPointer3d.CurrentTargetGroup = name;
    }
}
