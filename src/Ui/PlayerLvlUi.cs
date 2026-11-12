using Godot;

public class PlayerLevelUi
{
    private readonly Session _session;

    private int _uiLvl = 1;
    private double _uiExp = 0;

    private Upgrades Upgrades => _session.Upgrades;

    private ExpUiConfig UiConfig => Config.Instance.ExpUiConfig;

    private readonly CircularProgressBar _progressBar;
    private readonly RichTextLabel _lvlTextLabel;
    private readonly TextLabel _textLabel;

    public PlayerLevelUi(Session session)
    {
        _session = session;
        session.Update += Update;
        _progressBar = new(
            UiConfig.Width,
            UiConfig.Radius,
            new(UiConfig.BgColor),
            new(UiConfig.MainColor)
        );
        session.RootUi.AddChild(_progressBar);
        _lvlTextLabel = new();
        _progressBar.AddChild(_lvlTextLabel);
        _textLabel = new TextLabel(
            _lvlTextLabel,
            new()
            {
                BaseColor = new(UiConfig.TextColor),
                BaseFontSize = UiConfig.TextFontSize,
                FontPath = FontCollection.FontPath(UiConfig.FontName),
            }
        )
        {
            BbcodeText = "[center]{lvl}",
        };
        _lvlTextLabel.SetAnchorsAndMarginsPreset(Control.LayoutPreset.VcenterWide);
        _lvlTextLabel.MarginLeft = -UiConfig.Radius / 2;
        _lvlTextLabel.MarginRight = UiConfig.Radius / 2;
        _lvlTextLabel.FitContentHeight = true;
        _lvlTextLabel.ScrollActive = false;
        _lvlTextLabel.MarginTop = -UiConfig.TextFontSize / 2;
        _textLabel.Set("lvl", "1");

        _progressBar.RectPosition = new Vector2(
            UiConfig.OffsetX,
            session.RootUi.RectSize.y - UiConfig.OffsetY
        );
    }

    private void Update(float delta)
    {
        var scale =
            _session.RootUi.RectSize.x / _session.SessionContainer.Root.GetViewport().Size.x;
        _progressBar.RectPosition = new Vector2(
            UiConfig.OffsetX * scale,
            _session.RootUi.RectSize.y - UiConfig.OffsetY * scale
        );
        _progressBar.RectScale = Vector2.One * scale;
        _progressBar.SetProgress((float)_uiExp);
        _textLabel.Set("lvl", _uiLvl.ToString());

        var targetUiExp = Upgrades.CurrentExp / Upgrades.CurrentExpReq;
        var lvl = Upgrades.Level;

        if (lvl == _uiLvl && _uiExp >= targetUiExp - 0.0001f)
        {
            return;
        }

        var velocity = Mathf.Max(
            (float)(lvl - _uiLvl + targetUiExp - _uiExp) * UiConfig.LvlVelocity,
            UiConfig.MinVelocity
        );

        _uiExp += delta * velocity;

        if (lvl == _uiLvl && _uiExp > 1)
        {
            _uiExp = 1;
        }
        else
        {
            _uiLvl += (int)_uiExp;
            _uiExp %= 1;
        }
    }
}
