using Godot;

public class Player
{
    public readonly Session Session;
    private readonly PlayerNode _playerNode;
    public Vector2 HVelocity { get; set; }
    public float VVelocity { get; set; }
    public Vector2 Direction { get; set; }
    public Vector2 Spin { get; set; }

    private PlayerStatsConfig PlayerStatsConfig => Config.Instance.PlayerStatsConfig;
    private ControlsConfig ControlsConfig => Config.Instance.ControlsConfig;

    public bool Jumping { get; set; }
    public bool Digging { get; set; }

    public Player(Session session, Node parent)
    {
        Session = session;
        Session.OnPhysicsUpdate += PhysicsUpdate;
        _playerNode = SceneLoader.Instantiate<PlayerNode>("Player");
        parent.AddChild(_playerNode);
    }

    public void PhysicsUpdate(float delta)
    {
        UpdateRotation();
        UpdateHVelocity(delta);
        UpdateVVelocity(delta);
        AdjustVelocity(UpdatePosition(delta));
    }

    private void UpdateRotation()
    {
        var sensitivityX = ControlsConfig.CameraSensitivityX;
        var sensitivityY = ControlsConfig.CameraSensitivityY;
        _playerNode.Rotation += new Vector3(-Spin.y * sensitivityX, Spin.x * sensitivityY, 0);
        Spin = Vector2.Zero;
    }

    private Vector3 UpdatePosition(float delta)
    {
        var positionDelta = new Vector3(HVelocity.x, VVelocity, HVelocity.y);
        return _playerNode.MoveAndSlide(positionDelta * delta);
    }

    private void AdjustVelocity(Vector3 adjustedVelocity)
    {
        HVelocity = new Vector2(adjustedVelocity.x, adjustedVelocity.z);
        VVelocity = adjustedVelocity.y;
    }

    private void UpdateHVelocity(float delta)
    {
        if (Direction.LengthSquared() > 1)
            Direction = Direction.Normalized();
        if (Direction == Vector2.Zero)
        {
            var deltaVelocity = -HVelocity.Normalized() * PlayerStatsConfig.Deceleration * delta;
            if (HVelocity.LengthSquared() < deltaVelocity.LengthSquared())
                HVelocity = Vector2.Zero;
            else
                HVelocity -= deltaVelocity;
            return;
        }
        var isOppositeDirection = HVelocity.Dot(Direction) < 0;
        var acceleration = Mathf.Max(
            PlayerStatsConfig.Acceleration,
            isOppositeDirection ? PlayerStatsConfig.Deceleration : 0
        );
        HVelocity = HVelocity.Min(HVelocity + Direction * acceleration * delta);
    }

    private void UpdateVVelocity(float delta)
    {
        if (!_playerNode.IsOnFloor())
        {
            VVelocity = GravityAcceleration.Accelerate(
                VVelocity,
                delta,
                PlayerStatsConfig.FallingObjectConfig
            );
        }
        else
        {
            VVelocity = 0;
        }
    }

    public void ResetPlayerStats()
    {
        ResetPosition();
        ResetRotation();
        ResetVelocity();
    }

    public void ResetPosition()
    {
        _playerNode.Position = Vector3.Zero;
    }

    public void ResetRotation()
    {
        _playerNode.Rotation = Vector3.Zero;
    }

    public void ResetVelocity()
    {
        HVelocity = Vector2.Zero;
        VVelocity = 0;
    }
}
