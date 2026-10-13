using System;
using Godot;

public class PlayerControls
{
    private readonly Player _player;
    private readonly Session _session;
    private int ControllerId => _session.SessionContainer.ControllerId;

    public PlayerControls(Player player, Action<Action<InputEvent>> subscribe)
    {
        _session = player.Session;
        _player = player;

        subscribe(Input);
    }

    private void Input(InputEvent e)
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
        _player.Spin = rightStick;
    }
}
