using System;
using System.Linq;
using Godot;

public class PlayerNode : KinematicBody
{
    public Spatial Center { get; private set; } = null!;
    public Spatial Collector { get; private set; } = null!;

    private int? _freefalSoundIdx = null;

    private const float STEP_DELAY = 0.5f;
    private const float STEP_SOUND_SPEED_THRESHOLD = 1;
    private const float FREEFALL_SOUND_VERTICAL_VELOCITY_THRESHOLD = 70;
    private const float FALL_SOUND_VERTICAL_VELOCITY_THRESHOLD = 20;
    private Vector3 _lastFramePos;
    private float _stepTimer;
    private float _verticalVelocity;

    public override async void _Ready()
    {
        var collisionShape = GetChildren().OfType<CollisionShape>().FirstOrDefault();
        Center = collisionShape;
        Collector = new Spatial() { Position = Center.Position / 2 };

        await ToSignal(GetTree(), "idle_frame");
        await ToSignal(GetTree(), "idle_frame");

        AudioCollection.PlayRandomSound(Sounds.Spawn);
    }

    public override void _Process(float delta)
    {
        var verticalVelocity = -(Position - _lastFramePos).y;
        verticalVelocity /= delta;
        _verticalVelocity = Mathf.Max(_verticalVelocity, verticalVelocity);

        var horizontalVelocitySquared =
            (
                new Vector2(Position.x, Position.z) - new Vector2(_lastFramePos.x, _lastFramePos.z)
            ).LengthSquared() / delta;
        if (IsOnFloor())
        {
            if (_freefalSoundIdx.HasValue)
            {
                AudioCollection.StopPlayingSound(_freefalSoundIdx.Value);
                _freefalSoundIdx = null;
            }
            if (_verticalVelocity > FALL_SOUND_VERTICAL_VELOCITY_THRESHOLD)
            {
                AudioCollection.PlayRandomSound(Sounds.Fall);
            }
            if (horizontalVelocitySquared > STEP_SOUND_SPEED_THRESHOLD * STEP_SOUND_SPEED_THRESHOLD)
            {
                _stepTimer += delta;
                if (_stepTimer > STEP_DELAY)
                {
                    _stepTimer -= STEP_DELAY;
                    AudioCollection.PlayRandomSound(Sounds.Step);
                }
            }
            _verticalVelocity = 0;
        }
        else
        {
            if (_verticalVelocity > FREEFALL_SOUND_VERTICAL_VELOCITY_THRESHOLD)
            {
                if (!_freefalSoundIdx.HasValue)
                {
                    _freefalSoundIdx = AudioCollection.PlayRandomSound(Sounds.Freefall);
                }
            }
        }
        _lastFramePos = Position;
    }
}
