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
        _currentVisuals?.Rotation +=
            Vector3.One * delta * MuseumDescriptionPopupConfig.MeshRotationSpeed;
    }

    private void CreatePopup(MeshInstance meshInstance)
    {
        var placer = Root.Building.Placer;

        var isDiggable = false;
        var skeleton = placer.MeshSkeletonMap.TryGetValue(meshInstance, out var name);

        if (!skeleton)
        {
            isDiggable = placer.MeshDiggableMap.TryGetValue(meshInstance, out name);
        }
        if (!skeleton && !isDiggable)
        {
            return;
        }

        _currentVisuals = (Spatial)meshInstance.Duplicate();

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

        var mesh = (MeshInstance)meshInstance.Duplicate();
        SpatialAabbUtils.FitInsideRect(mesh, meshFrame.RectSize);
        meshContainer.AddChild(_currentVisuals);

        var fontSize = MuseumDescriptionPopupConfig.FontSize;
        var textColor = MuseumDescriptionPopupConfig.TextColor;
        // TODO: Uncomment when fonts are ready
        // var fontPath = System.IO.Path.Combine(
        //     "res://" + Manifest.Instance.Dir("Fonts").FullPath,
        //     MuseumDescriptionPopupConfig.FontName + ".ttf"
        // );
        var textLabel = new TextLabel(textLabelNode);
        textLabel.Apply(
            new()
            {
                BaseColor = textColor,
                BaseFontSize = fontSize,
                // FontPath = fontPath,
            }
        );
        var descriptionsCollection = Root.DescriptionsCollection;
        textLabel.MdText = skeleton
            ? descriptionsCollection.SkeletonGetOrDefault(name)
            : descriptionsCollection.OtherGetOrDefault(name);
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
