using System;
using Godot;

/// <summary>
/// Applies a deterministic, randomly varying rotational velocity over an animation's progress.
/// </summary>
public class AnimationRotateRandomly : AnimationInstance
{
    private const int Segments = 2;

    private readonly Spatial _target;
    private readonly Vector3 _initialRotation;

    private readonly Vector3[] _angularVelocities = GenerateAngularVelocities(Segments + 1);
    private readonly Vector3[] _accumulatedRotations;

    public AnimationRotateRandomly(Spatial target)
    {
        _target = target;
        _initialRotation = target.GlobalRotation;
        _accumulatedRotations = IntegrateAngularVelocities();
    }

    protected override void ApplyInternal(float completion)
    {
        float scaled = Mathf.Clamp(completion, 0f, 1f) * Segments;
        int segment = Math.Min((int)scaled, Segments - 1);
        float u = scaled - segment;

        Vector3 velocityStart = _angularVelocities[segment];
        Vector3 velocityEnd = _angularVelocities[segment + 1];

        Vector3 rotationOffset =
            _accumulatedRotations[segment]
            + (velocityStart * u + (velocityEnd - velocityStart) * (0.5f * u * u)) / Segments;

        var globalTransform = _target.GlobalTransform;
        globalTransform.basis = new Basis(_initialRotation + rotationOffset).Scaled(
            globalTransform.basis.Scale
        );

        _target.GlobalTransform = globalTransform;
    }

    private static Vector3[] GenerateAngularVelocities(int count)
    {
        var random = new Random();
        var velocities = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            velocities[i] = new Vector3(
                RandomRange(random, -4f, 4f),
                RandomRange(random, -4f, 4f),
                RandomRange(random, -4f, 4f)
            );
        }

        return velocities;
    }

    private static float RandomRange(Random random, float min, float max) =>
        min + (float)random.NextDouble() * (max - min);

    private Vector3[] IntegrateAngularVelocities()
    {
        var rotations = new Vector3[Segments + 1];
        float dt = 1f / Segments;

        for (int i = 0; i < Segments; i++)
        {
            rotations[i + 1] =
                rotations[i] + (_angularVelocities[i] + _angularVelocities[i + 1]) * (0.5f * dt);
        }

        return rotations;
    }
}
