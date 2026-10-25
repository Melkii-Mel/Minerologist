using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static DiggerSound;

public class DiggerSoundPlayer
{
    private readonly Dictionary<DiggerSound, AudioPlayer> _players = [];

    private readonly DiggerSound[] _diggerSounds;
    private bool _stuck;

    private readonly Random _rng = new();

    private bool _on = false;

    public DiggerSoundPlayer(Session session)
    {
        session.Update += Update;
        _diggerSounds = [.. Enum.GetValues(typeof(DiggerSound)).Cast<DiggerSound>()];

        foreach (var sound in _diggerSounds)
        {
            var player = SoundPlayer.CreateUnmanagedPlayer(
                AudioCollection.RandomSound(sound.ToString())
            );
            _players[sound] = player;
        }

        EnableWithLoop(DiggerAirflow);
        EnableWithLoop(DiggerAirflowStuck);
        EnableWithLoop(DiggerMotor);

        MotorPitchSet(PitchOff);
        ShiftMotorPitch(0);
    }

    public void Update(float delta)
    {
        IngestRocks(delta);
        ShiftMotorPitch(delta);
        AdjustAirflow(delta);
    }

    public void Start()
    {
        if (_on)
        {
            return;
        }
        _on = true;
        RandomSound(DiggerOn);
    }

    public void StateAir()
    {
        UnstuckIfStuck();
        MotorPitchSet(PitchLow);
        RockIngestionSet(RockIngestionSpeedNone);
        AirflowSet(DiggerAirflow);
    }

    public void StateWall()
    {
        StuckIfNotStuck();
        MotorPitchSet(PitchHigh);
        RockIngestionSet(RockIngestionSpeedStuck);
        AirflowSet(DiggerAirflowStuck);
    }

    public void StateGround()
    {
        UnstuckIfStuck();
        MotorPitchSet(PitchNormal);
        RockIngestionSet(RockIngestionSpeedNormal);
        AirflowSet(DiggerAirflow);
    }

    public void StateDiggable()
    {
        MotorPitchSet(PitchHigh);
        UnstuckIfStuck();
        RockIngestionSet(RockIngestionSpeedStuck);
        AirflowSet(DiggerAirflowStuck);
    }

    public void StatePush()
    {
        MotorPitchSet(PitchBackwards);
        RockIngestionSet(RockIngestionSpeedNone);
    }

    public void Stop()
    {
        if (!_on)
        {
            return;
        }
        _on = false;
        if (!RandomSound(DiggerOff))
        {
            RandomSound(DiggerOn);
        }
        UnstuckIfStuck();
        MotorPitchSet(PitchOff);
        RockIngestionSet(RockIngestionSpeedNone);
        AirflowSet(null);
    }

    private void StuckIfNotStuck()
    {
        if (_stuck)
        {
            return;
        }
        _stuck = true;
        RandomSound(DiggerStuck);
    }

    private void UnstuckIfStuck()
    {
        if (!_stuck)
        {
            return;
        }
        _stuck = false;
        RandomSound(DiggerUnstuck);
    }

    private const float MotorPitchTransitionSpeed = 0.75f;
    private const float PitchHigh = 2;
    private const float PitchNormal = 1;
    private const float PitchLow = 0.5f;
    private const float PitchOff = 0;
    private const float PitchBackwards = -1f;
    private float _currentMotorPitch = 0;
    private float _targetMotorPitch = 0;

    private void MotorPitchSet(float pitch)
    {
        _targetMotorPitch = pitch;
    }

    private const float RockIngestionSpeedNormal = 4;
    private const float RockIngestionSpeedStuck = 1f;
    private const float RockIngestionSpeedNone = 0;
    private const float RockIngestionRandomness = 0.5f;
    private float _rockIngestionSpeed = 0;
    private float _rockIngestionTimer = 0;

    private void RockIngestionSet(float speed)
    {
        _rockIngestionSpeed = speed;
    }

    private const float AirflowVolumeMultiplier = 0.3f;
    private const float AirflowTransitionDuration = 0.2f;
    private float _airflowTransition = 1;
    private float _airflowVolume = 0;
    private float _targetAirflowVolume = 0;
    private DiggerSound _airflow = DiggerAirflow;

    private void AirflowSet(DiggerSound? airflow)
    {
        if (!airflow.HasValue)
        {
            _targetAirflowVolume = 0;
            return;
        }
        _targetAirflowVolume = 1;
        if (airflow == _airflow)
        {
            return;
        }
        _airflowTransition = 0;
        _airflow = airflow.Value;
    }

    private void ShiftMotorPitch(float delta)
    {
        MoveTowards(ref _currentMotorPitch, _targetMotorPitch, delta * MotorPitchTransitionSpeed);
        var player = _players[DiggerMotor];
        player.PitchScale = Mathf.Max(Mathf.Abs(_currentMotorPitch), 0.001f);
        player.VolumeDb = VolumeToDb(
            Mathf.Max(
                Mathf.Min(Mathf.Abs(_currentMotorPitch * 2), 1),
                _targetMotorPitch < 0 ? 1 : 0
            )
        );
    }

    private void MoveTowards(ref float v, float to, float delta)
    {
        v = v > to ? Mathf.Max(v - delta, to) : Mathf.Min(v + delta, to);
    }

    private void IngestRocks(float delta)
    {
        _rockIngestionTimer += delta;
        if (_rockIngestionSpeed == 0)
        {
            return;
        }
        var duration = 1 / _rockIngestionSpeed;
        if (_rockIngestionTimer > duration)
        {
            _rockIngestionTimer -=
                duration
                * (
                    1
                    - (RockIngestionRandomness / 2)
                    + (float)_rng.NextDouble() * RockIngestionRandomness
                );
            RandomSound(DiggerRockIngestion, 0.5f);
        }
    }

    private void AdjustAirflow(float delta)
    {
        MoveTowards(ref _airflowVolume, _targetAirflowVolume, delta / AirflowTransitionDuration);
        MoveTowards(ref _airflowTransition, 1, delta / AirflowTransitionDuration);

        var primaryPlayer = _players[_airflow];
        var secondaryPlayer = _players[
            _airflow == DiggerAirflow ? DiggerAirflowStuck : DiggerAirflow
        ];
        primaryPlayer.VolumeDb = VolumeToDb(
            _airflowVolume * _airflowTransition * AirflowVolumeMultiplier
        );
        secondaryPlayer.VolumeDb = VolumeToDb(
            _airflowVolume * (1 - _airflowTransition) * AirflowVolumeMultiplier
        );
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log(Mathf.Clamp(volume, 0.001f, 1f)) / Mathf.Log(10f);
    }

    private void EnableWithLoop(DiggerSound sound)
    {
        var player = _players[sound];
        var stream = (AudioStreamSample)player.Stream;
        stream.LoopBegin = 0;
        stream.LoopEnd = stream.Data.Length / (stream.Stereo ? 4 : 2);
        stream.LoopMode = AudioStreamSample.LoopModeEnum.Forward;
        player.Play();
    }

    private bool RandomSound(DiggerSound sound, float volume = 1)
    {
        var stream = AudioCollection.RandomSound(sound.ToString());
        if (stream is null)
        {
            return false;
        }
        var player = SoundPlayer.Play(stream);
        player.VolumeDb = VolumeToDb(volume);
        player.PlaybackFinished += () => player.VolumeDb = 0;
        return true;
    }
}

public enum DiggerSound
{
    DiggerMotor,
    DiggerAirflow,
    DiggerAirflowStuck,
    DiggerRockIngestion,
    DiggerStuck,
    DiggerUnstuck,
    DiggerOn,
    DiggerOff,
}
