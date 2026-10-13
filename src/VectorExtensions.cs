using Godot;

public static class VectorExtensions
{
    public static Vector3 Min(this Vector3 a, Vector3 b)
    {
        return a.LengthSquared() < b.LengthSquared() ? a : b;
    }

    public static Vector3 Max(this Vector3 a, Vector3 b)
    {
        return a.LengthSquared() > b.LengthSquared() ? a : b;
    }

    public static Vector2 Min(this Vector2 a, Vector2 b)
    {
        return a.LengthSquared() < b.LengthSquared() ? a : b;
    }

    public static Vector2 Max(this Vector2 a, Vector2 b)
    {
        return a.LengthSquared() > b.LengthSquared() ? a : b;
    }
}
