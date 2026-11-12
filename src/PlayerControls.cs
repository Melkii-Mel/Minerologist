using Godot;

public class PlayerControls
{
    private readonly Player _player;
    private readonly Session _session;

    private int ControllerId => _session.SessionContainer.ControllerId;

    private bool _handleInputs;

    private float RestartDuration => Config.Instance.ControlsConfig.RestartDuration;

    private float _restartDuration;

    private Vector2 _leftStick;
    private Vector2 _rightStick;
    private Vector2 _mouseMotion;

    private bool _dPadUp;
    private bool _dPadDown;
    private bool _dPadLeft;
    private bool _dPadRight;

    private bool _jumping;
    private bool _digging;

    private const float StickDeadzone = 0.15f;

    public PlayerControls(Player player)
    {
        _session = player.Session;
        _player = player;

        _session.Input += OnInput;
        _session.Update += OnUpdate;
    }

    private void OnUpdate(float delta)
    {
        if (!_handleInputs || ControllerId != -1)
        {
            _restartDuration = 0;
            return;
        }

        if (Input.IsKeyPressed((int)KeyList.R))
        {
            _restartDuration += delta;

            if (_restartDuration >= RestartDuration)
            {
                _restartDuration = 0;
                _session.SessionContainer.Restart();
            }
        }
        else
        {
            _restartDuration = 0;
        }

        var digging = Input.IsMouseButtonPressed((int)ButtonList.Left);

        _player.Spin =
            _mouseMotion
            * (digging ? Config.Instance.ControlsConfig.DiggingCameraSensitivityMultiplier : 1)
            * new Vector2(
                Config.Instance.ControlsConfig.MouseCameraSensitivityX,
                Config.Instance.ControlsConfig.MouseCameraSensitivityY
            );

        _mouseMotion = Vector2.Zero;
    }

    public void StartInputHandling()
    {
        ResetInputState();

        _handleInputs = true;

        if (ControllerId == -1)
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public void StopInputHandling()
    {
        _handleInputs = false;

        ResetInputState();

        if (ControllerId == -1)
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void ResetInputState()
    {
        _leftStick = Vector2.Zero;
        _rightStick = Vector2.Zero;
        _mouseMotion = Vector2.Zero;

        _dPadUp = false;
        _dPadDown = false;
        _dPadLeft = false;
        _dPadRight = false;

        _jumping = false;
        _digging = false;

        _restartDuration = 0;

        _player.Jumping = false;
        _player.Digging = false;
        _player.Direction = Vector2.Zero;
        _player.Spin = Vector2.Zero;
    }

    private void OnInput(InputEvent e)
    {
        if (!_handleInputs)
            return;

        if (ControllerId == -1)
            OnMouseKeyboardInput(e);
        else
            OnJoypadInput(e);
    }

    private void OnMouseKeyboardInput(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Scancode == (int)KeyList.Space)
        {
            _jumping = keyEvent.Pressed && !keyEvent.Echo;
        }

        var direction = Vector2.Zero;

        if (Input.IsKeyPressed((int)KeyList.A))
            direction.x -= 1;

        if (Input.IsKeyPressed((int)KeyList.D))
            direction.x += 1;

        if (Input.IsKeyPressed((int)KeyList.W))
            direction.y -= 1;

        if (Input.IsKeyPressed((int)KeyList.S))
            direction.y += 1;

        if (direction.LengthSquared() > 1)
            direction = direction.Normalized();

        var digging = Input.IsMouseButtonPressed((int)ButtonList.Left);

        if (e is InputEventMouseMotion mouseMotion)
            _mouseMotion += mouseMotion.Relative;

        _player.Jumping = _jumping;
        _player.Digging = digging;
        _player.Direction = direction;
    }

    private void OnJoypadInput(InputEvent e)
    {
        if (e.Device != ControllerId)
            return;

        if (e is InputEventJoypadMotion motion)
        {
            switch (motion.Axis)
            {
                case (int)JoystickList.AnalogLx:
                    _leftStick.x = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogLy:
                    _leftStick.y = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogRx:
                    _rightStick.x = motion.AxisValue;
                    break;

                case (int)JoystickList.AnalogRy:
                    _rightStick.y = motion.AxisValue;
                    break;
            }
        }
        else if (e is InputEventJoypadButton button)
        {
            var pressed = button.Pressed;

            switch (button.ButtonIndex)
            {
                case (int)JoystickList.Button12:
                    _dPadUp = pressed;
                    break;

                case (int)JoystickList.Button13:
                    _dPadDown = pressed;
                    break;

                case (int)JoystickList.Button14:
                    _dPadLeft = pressed;
                    break;

                case (int)JoystickList.Button15:
                    _dPadRight = pressed;
                    break;

                case (int)JoystickList.Button0:
                    _jumping = pressed;
                    break;

                case (int)JoystickList.Button2:
                    _digging = pressed;
                    break;
            }
        }

        ApplyJoypadState();
    }

    private void ApplyJoypadState()
    {
        var leftStick = ApplyDeadzone(_leftStick);
        var rightStick = ApplyDeadzone(_rightStick);

        var dPad = new Vector2(
            (_dPadRight ? 1 : 0) - (_dPadLeft ? 1 : 0),
            (_dPadDown ? 1 : 0) - (_dPadUp ? 1 : 0)
        );

        var direction = leftStick + dPad;

        if (direction.LengthSquared() > 1)
            direction = direction.Normalized();

        _player.Jumping = _jumping;
        _player.Digging = _digging;
        _player.Direction = direction;

        _player.Spin =
            rightStick
            * (_digging ? Config.Instance.ControlsConfig.DiggingCameraSensitivityMultiplier : 1);
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone = StickDeadzone)
    {
        var length = value.Length();

        if (length <= deadzone)
            return Vector2.Zero;

        return value.Normalized() * Mathf.Min((length - deadzone) / (1 - deadzone), 1);
    }
}
