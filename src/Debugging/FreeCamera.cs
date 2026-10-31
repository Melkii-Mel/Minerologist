using Godot;

public class FreeCamera : Camera
{
    [Export]
    public float MoveSpeed = 20.0f;

    [Export]
    public float FastMultiplier = 8.0f;

    [Export]
    public float SlowMultiplier = 0.25f;

    [Export]
    public float MouseSensitivity = 0.002f;

    private bool _mouseCaptured = true;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && _mouseCaptured)
        {
            RotateY(-mouseMotion.Relative.x * MouseSensitivity);

            Rotation = new Vector3(
                Mathf.Clamp(
                    Rotation.x - mouseMotion.Relative.y * MouseSensitivity,
                    -Mathf.Deg2Rad(89.0f),
                    Mathf.Deg2Rad(89.0f)
                ),
                Rotation.y,
                Rotation.z
            );
        }
        else if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Scancode == (uint)KeyList.Escape)
            {
                _mouseCaptured = false;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == (int)ButtonList.Left && !_mouseCaptured)
            {
                _mouseCaptured = true;
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public override void _Process(float delta)
    {
        var direction = Vector3.Zero;

        if (Input.IsKeyPressed((int)KeyList.W))
            direction -= Transform.basis.z;

        if (Input.IsKeyPressed((int)KeyList.S))
            direction += Transform.basis.z;

        if (Input.IsKeyPressed((int)KeyList.A))
            direction -= Transform.basis.x;

        if (Input.IsKeyPressed((int)KeyList.D))
            direction += Transform.basis.x;

        if (Input.IsKeyPressed((int)KeyList.Space))
            direction += Vector3.Up;

        if (Input.IsKeyPressed((int)KeyList.C))
            direction -= Vector3.Up;

        if (direction.LengthSquared() > 0.0f)
            direction = direction.Normalized();

        var speed = MoveSpeed;

        if (Input.IsKeyPressed((int)KeyList.Shift))
            speed *= FastMultiplier;

        if (Input.IsKeyPressed((int)KeyList.Control))
            speed *= SlowMultiplier;

        GlobalTranslate(direction * speed * delta);
    }
}
