using Godot;

public static class GravityAcceleration
{
    public static float Accelerate(
        float currentVerticalVelocity,
        float delta,
        FallingObjectConfig fallingObjectConfig
    )
    {
        currentVerticalVelocity -=
            Config.Instance.WorldConfig.Gravity * fallingObjectConfig.GravityScale * delta;
        return Mathf.Max(currentVerticalVelocity, -fallingObjectConfig.TerminalVelocity);
    }
}
