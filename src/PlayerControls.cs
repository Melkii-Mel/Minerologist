using Godot;

public class PlayerControls
{
    private readonly Player _player;
    private readonly Session _session;
    private int ControllerId => _session.SessionContainer.ControllerId;
    private bool _handleInputs = false;

    private float RestartDuration => Config.Instance.ControlsConfig.RestartDuration;
    private float _restartDuration = 0;

    public PlayerControls(Player player)
    {
        _session = player.Session;
        _player = player;

        _session.Input += OnInput;
        _session.Update += OnUpdate;
    }

    private void OnUpdate(float delta)
    {
        if (Input.IsKeyPressed((int)KeyList.R))
        {
            _restartDuration += delta;
            if (_restartDuration > RestartDuration)
            {
                _session.RestartSession();
            }
        }
        else
        {
            _restartDuration = 0;
        }
    }

    public void StartInputHandling()
    {
        _handleInputs = true;
        if (ControllerId == -1)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public void StopInputHandling()
    {
        _handleInputs = false;
        if (ControllerId == -1)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    private void OnInput(InputEvent e)
    {
        if (!_handleInputs)
        {
            return;
        }
        // If session controls are bound to the keyboard
        if (ControllerId == -1)
        {
            OnMouseKeyboardInput(e);
        }
        else
        {
            OnJoypadInput(e);
        }
    }

    private void OnMouseKeyboardInput(InputEvent e)
    {
        var directionX = 0;
        var directionY = 0;
        if (Input.IsKeyPressed((int)KeyList.A))
        {
            directionX -= 1;
        }
        if (Input.IsKeyPressed((int)KeyList.W))
        {
            directionY -= 1;
        }
        if (Input.IsKeyPressed((int)KeyList.S))
        {
            directionY += 1;
        }
        if (Input.IsKeyPressed((int)KeyList.D))
        {
            directionX += 1;
        }
        var jumping =
            e is InputEventKey keyEvent
            && keyEvent.Pressed
            && !keyEvent.Echo
            && keyEvent.Scancode == (int)KeyList.Space;

        var digging = Input.IsMouseButtonPressed((int)ButtonList.Left);
        var spin = Vector2.Zero;
        if (e is InputEventMouseMotion mouseMotion)
        {
            spin = mouseMotion.Relative;
        }

        _player.Jumping = jumping;
        _player.Digging = digging;
        _player.Direction = new Vector2(directionX, directionY).Normalized();
        _player.Spin =
            spin
            * (digging ? Config.Instance.ControlsConfig.DiggingCameraSensitivityMultiplier : 1);
    }

    private void OnJoypadInput(InputEvent e)
    {
        var leftStick = Vector2.Zero;
        var rightStick = Vector2.Zero;
        var dPad = Vector2.Zero;
        var jumping = false;
        var digging = false;
        if (e.Device != ControllerId)
            return;

        if (e is InputEventJoypadMotion motion)
        {
            switch (motion.Axis)
            {
                case (int)JoystickList.AnalogLx:
                    leftStick.x = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogLy:
                    leftStick.y = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogRx:
                    rightStick.x = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogRy:
                    rightStick.y = motion.AxisValue;
                    break;
            }
        }
        else if (e is InputEventJoypadButton button)
        {
            var v = button.Pressed ? 1 : 0;
            switch (button.ButtonIndex)
            {
                case (int)JoystickList.Button12:
                    dPad.y += v;
                    break;

                case (int)JoystickList.Button13:
                    dPad.y -= v;
                    break;

                case (int)JoystickList.Button14:
                    dPad.x -= v;
                    break;

                case (int)JoystickList.Button15:
                    dPad.x += v;
                    break;
                case (int)JoystickList.Button0:
                    jumping = button.Pressed;
                    break;
                case (int)JoystickList.Button2:
                    digging = button.Pressed;
                    break;
            }
        }

        _player.Jumping = jumping;
        _player.Digging = digging;
        _player.Direction = leftStick + dPad;
        if (_player.Direction.LengthSquared() > 1)
            _player.Direction = _player.Direction.Normalized();
        _player.Spin =
            rightStick
            * (digging ? Config.Instance.ControlsConfig.DiggingCameraSensitivityMultiplier : 1);
    }
}
