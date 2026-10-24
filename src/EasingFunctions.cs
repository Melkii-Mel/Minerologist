using Godot;

public abstract class EasingFunction
{
    public abstract float Evalueate(float t);
}

public class EFLinear : EasingFunction
{
    public override float Evalueate(float t) => t;
}

public class EFEaseInOut(float exponent) : EasingFunction
{
    public override float Evalueate(float t) =>
        t < 0.5f
            ? Mathf.Pow(2f, exponent - 1f) * Mathf.Pow(t, exponent)
            : 1f - Mathf.Pow(2f, exponent - 1f) * Mathf.Pow(1f - t, exponent);
}

public class EFEaseIn(float exponent) : EasingFunction
{
    public override float Evalueate(float t) => Mathf.Pow(t, exponent);
}

public class EFEaseOut(float exponent) : EasingFunction
{
    public override float Evalueate(float t) => 1f - Mathf.Pow(1f - t, exponent);
}
