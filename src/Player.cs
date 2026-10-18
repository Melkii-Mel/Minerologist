using System;
using Godot;

public class Player
{
    public Vector2 Direction { get; set; }
    public Vector2 Spin { get; set; }
    public bool Jumping
    {
        get =>
            _lastJumpInput + TimeSpan.FromMilliseconds(ControlsConfig.BufferTime) >= DateTime.Now
            && (_lastActualJump - _lastJumpInput).TotalMilliseconds < 0;
        set => _lastJumpInput = value ? DateTime.Now : _lastJumpInput;
    }
    public bool IsOnGround =>
        _lastGroundTime + TimeSpan.FromMilliseconds(ControlsConfig.CoyoteTime) >= DateTime.Now
        && (_lastGroundTime - _lastActualJump).TotalMilliseconds > 0;
    public bool Digging { get; set; }
    private DateTime _lastJumpInput = DateTime.MinValue;
    private DateTime _lastActualJump = DateTime.MinValue;
    private DateTime _lastGroundTime = DateTime.MinValue;

    public readonly Session Session;
    private readonly PlayerNode _playerNode;
    private readonly Camera _playerCameraNode;
    private readonly PlayerDigger _digger;
    public Vector2 HVelocity { get; set; }
    public float VVelocity { get; set; }

    public Vector3 VerticalOriginPosition =>
        new(_horizontalOrigin.x, _playerNode.GlobalPosition.y, _horizontalOrigin.z);
    private Vector3 _horizontalOrigin;

    private PlayerStatsConfig PlayerStatsConfig => Config.Instance.PlayerStatsConfig;
    private ControlsConfig ControlsConfig => Config.Instance.ControlsConfig;

    public Vector3 DefaultPlayerPosition { get; private set; } = new(0, 10, 0);

    public Player(Session session, Node parent, Vector3 origin, Terrain terrain)
    {
        Session = session;
        Session.PhysicsUpdate += PhysicsUpdate;
        _playerNode = SceneLoader.Instantiate<PlayerNode>("Player");
        _playerCameraNode = _playerNode.GetNode<Camera>("Camera");
        parent.AddChild(_playerNode);

        var inputControls = new PlayerControls(this);
        inputControls.StartInputHandling();

        _horizontalOrigin = origin;

        _digger = new PlayerDigger(_playerNode, _playerCameraNode, _playerNode, terrain);
    }

    public void PhysicsUpdate(float delta)
    {
        UpdateRotation();
        UpdateHVelocity(delta);
        AdjustVelocity(UpdatePosition());
        UpdateVVelocity(delta);
        UpdateLastGroundTime();
        if (Digging)
        {
            _digger.Dig(delta);
        }
    }

    private void UpdateLastGroundTime()
    {
        if (_playerNode.IsOnFloor())
        {
            _lastGroundTime = DateTime.Now;
        }
    }

    private void UpdateRotation()
    {
        var sensitivityX = ControlsConfig.CameraSensitivityX;
        var sensitivityY = ControlsConfig.CameraSensitivityY;
        _playerCameraNode.Rotation += new Vector3(-Spin.y * sensitivityX, 0, 0);
        _playerCameraNode.RotationDegrees = new Vector3(
            Mathf.Clamp(
                _playerCameraNode.RotationDegrees.x,
                ControlsConfig.CameraMinAngleDegreesY,
                ControlsConfig.CameraMaxAngleDegreesY
            ),
            0,
            0
        );
        _playerNode.Rotation += new Vector3(0, -Spin.x * sensitivityY, 0);
        Spin = Vector2.Zero;
    }

    private Vector3 UpdatePosition()
    {
        var velocity = new Vector3(HVelocity.x, VVelocity, HVelocity.y);
        // TODO: floor max angle and stopOnSlope as configurables
        return _playerNode.MoveAndSlide(
            velocity,
            Vector3.Up,
            stopOnSlope: true,
            floorMaxAngle: Mathf.Deg2Rad(60)
        );
    }

    private void AdjustVelocity(Vector3 adjustedVelocity)
    {
        HVelocity = new Vector2(adjustedVelocity.x, adjustedVelocity.z);
        VVelocity = adjustedVelocity.y;
    }

    private void UpdateHVelocity(float delta)
    {
        var direction = Direction.Rotated(-_playerNode.Rotation.y);
        if (Direction.LengthSquared() > 1)
            Direction = Direction.Normalized();
        if (direction == Vector2.Zero)
        {
            var deltaVelocity = -HVelocity.Normalized() * PlayerStatsConfig.Deceleration * delta;
            if (HVelocity.LengthSquared() < deltaVelocity.LengthSquared())
                HVelocity = Vector2.Zero;
            else
                HVelocity += deltaVelocity;
            return;
        }
        var isOppositeDirection = HVelocity.Dot(direction) < 0;
        var acceleration = Mathf.Max(
            PlayerStatsConfig.Acceleration,
            isOppositeDirection ? PlayerStatsConfig.Deceleration : 0
        );
        HVelocity += direction * acceleration * delta;
        if (
            HVelocity.LengthSquared()
            > PlayerStatsConfig.MaxVelocity * PlayerStatsConfig.MaxVelocity
        )
        {
            HVelocity = HVelocity.Normalized() * PlayerStatsConfig.MaxVelocity;
        }
    }

    private void UpdateVVelocity(float delta)
    {
        VVelocity = GravityAcceleration.Accelerate(
            VVelocity,
            delta,
            PlayerStatsConfig.FallingObjectConfig
        );
        if (Jumping && IsOnGround)
        {
            _lastActualJump = DateTime.Now;
            VVelocity = PlayerStatsConfig.JumpVelocity;
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
        _playerNode.Position = DefaultPlayerPosition;
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
