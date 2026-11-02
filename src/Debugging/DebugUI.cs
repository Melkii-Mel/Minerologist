using Godot;

public class DebugUI : Node
{
    private readonly CanvasLayer _uiLayer;
    private readonly Control _uiRoot;
    private readonly Label _fps;

    public DebugUI(Root root, bool visible)
    {
        root.AddChild(this);
        _uiLayer = new() { Layer = 100 };
        _uiRoot = new()
        {
            Visible = visible,
            AnchorRight = 1,
            AnchorBottom = 1,
        };

        _fps = new Label
        {
            RectMinSize = new Vector2(160, 30),
            Align = Label.AlignEnum.Left,
            Valign = Label.VAlign.Top,
            RectPosition = new Vector2(12, 12),
        };

        _uiRoot.AddChild(_fps);
        _uiLayer.AddChild(_uiRoot);
        AddChild(_uiLayer);

        var monitor = new FrameRateMonitor(s => _fps.Text = s);
        root.Update += monitor.Update;
    }

    public override void _Input(InputEvent @event)
    {
        if (
            @event is InputEventKey eventKey
            && eventKey.Pressed
            && eventKey.Scancode == (int)KeyList.F3
        )
        {
            ToggleUi();
        }
    }

    public void ToggleUi()
    {
        _uiRoot.Visible = !_uiRoot.Visible;
    }
}
