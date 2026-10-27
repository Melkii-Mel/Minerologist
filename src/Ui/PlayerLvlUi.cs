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
        _lvlTextLabel.AddColorOverride("default_color", new(UiConfig.TextColor));
        _lvlTextLabel.FitContentHeight = true;
        _progressBar.AddChild(_lvlTextLabel);
        _lvlTextLabel.SetAnchorsAndMarginsPreset(Control.LayoutPreset.VcenterWide);
        _lvlTextLabel.MarginLeft = -UiConfig.Radius / 2;
        _lvlTextLabel.MarginRight = UiConfig.Radius / 2;
        _lvlTextLabel.BbcodeEnabled = true;

        _progressBar.RectPosition = new Vector2(100, session.RootUi.RectSize.y - 100);
    }

    private void Update(float delta)
    {
        _progressBar.SetProgress((float)_uiExp);
        _lvlTextLabel.BbcodeText = $"[center]{(float)_uiLvl}";
        _lvlTextLabel.PushAlign(RichTextLabel.Align.Center);

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
