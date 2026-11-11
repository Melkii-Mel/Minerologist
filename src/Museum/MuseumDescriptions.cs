using System.Linq;
using System.Text.RegularExpressions;
using Godot;

// TODO: No description until discovered
public class MuseumDescriptions : Object
{
    public MuseumDescriptionPopupConfig MuseumDescriptionPopupConfig =>
        Config.Instance.MuseumConfig.MuseumDescriptionPopupConfig;

    private Spatial? _currentVisuals;

    public MuseumRoot Root { get; }

    public MuseumDescriptions(MuseumRoot root)
    {
        Root = root;
        root.MagneticPointer3d.MeshClicked += CreatePopup;
        root.Root.Update += Update;
    }

    private void Update(float delta)
    {
        _currentVisuals?.RotationDegrees +=
            delta * MuseumDescriptionPopupConfig.MeshRotationSpeed.ToGdVector3();
    }

    private void CreatePopup(MeshInstance meshInstance)
    {
        var placer = Root.Building.Placer;

        var isDiggable = false;
        var skeleton = placer.MeshSkeletonMap.TryGetValue(meshInstance, out var name);
        var amount = 0;

        if (!skeleton)
        {
            isDiggable = placer.MeshDiggableMap.TryGetValue(meshInstance, out name);
        }
        if (!skeleton && !isDiggable)
        {
            return;
        }
        amount = skeleton
            ? Root.State.SkeletonsAmounts[name]
            : Root.State.OtherDiggablesAmounts[name];

        _currentVisuals = (Spatial?)placer.MuseumItems[name].ModelPlacement?.Anchor.Duplicate();
        _currentVisuals?.Position = Vector3.Zero;

        var popup = SceneLoader.Instantiate<Control>("MuseumDescriptionPopup");
        Root.RootUi.AddChild(popup);

        var meshContainer = popup.GetChildOfTypeByName<Spatial>("MeshContainer")!;
        var meshFrame = popup.GetChildOfTypeByName<Control>("MeshFrame")!;
        var textLabelNode = popup.GetChildOfTypeByName<RichTextLabel>("TextLabel")!;
        var titleTextLabelNode = popup.GetChildOfTypeByName<RichTextLabel>("TitleTextLabel")!;
        var bgPanel = popup.GetChildOfTypeByName<Panel>("BgPanel")!;
        var closeButton = popup.GetChildOfTypeByName<Button>("Close")!;

        bgPanel.Connect("gui_input", this, nameof(OnBackgroundInput), [popup]);
        closeButton.Connect("pressed", this, nameof(OnCloseButtonPressed), [popup]);

        if (_currentVisuals != null)
        {
            var camera = new Camera();
            meshContainer.AddChild(camera);
            meshContainer.AddChild(_currentVisuals);

            _currentVisuals.Position = new(0, 0, -10);

            camera.SetOrthogonal(1, 0, 1000);
            camera.Current = true;
            camera.LookAt(_currentVisuals.GlobalPosition, Vector3.Up);
        }

        var textLabel = new TextLabel(textLabelNode);
        var titleTextLabel = new TextLabel(titleTextLabelNode);
        MuseumDescriptionPopupConfig.BodyFontConfig.Apply(textLabel);
        MuseumDescriptionPopupConfig.TitleFontConfig.Apply(titleTextLabel);

        var descriptionsCollection = Root.DescriptionsCollection;
        var text = Root.DescriptionsCollection.GetOrDefault(
            amount > 0 ? name : "NotCollected",
            skeleton
                ? descriptionsCollection.SkeletonDescriptions
                : descriptionsCollection.OtherDescriptions
        );
        var titleText = "";
        var bodyText = text;
        var match = Regex.Match(
            text,
            @"\[center\]\\?\s*\\?#\s*(.*?)\\?\s*\[/center\]\\?\s*(.*)",
            RegexOptions.Singleline
        );

        if (match.Success)
        {
            titleText = match.Groups[1].Value.Trim();
            bodyText = match.Groups[2].Value.Trim();
        }

        titleTextLabel.MdText = titleText;
        textLabel.MdText = bodyText;
    }

    private void OnBackgroundInput(InputEvent inputEvent, Control popup)
    {
        if (
            inputEvent is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == (int)ButtonList.Left
        )
        {
            popup.QueueFree();
            _currentVisuals = null;
        }
    }

    private void OnCloseButtonPressed(Control popup)
    {
        popup.QueueFree();
        _currentVisuals = null;
    }
}
