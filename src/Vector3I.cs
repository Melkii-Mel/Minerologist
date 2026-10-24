using System;
using Godot;

public record struct Vector3I(int X, int Y, int Z)
{
    public Vector3I(int x)
        : this(x, x, x) { }

    public Vector3I(Vector3 gdVector3)
        : this((int)gdVector3.x, (int)gdVector3.y, (int)gdVector3.z) { }

    public static Vector3I operator +(Vector3I a, Vector3I b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3I operator *(Vector3I a, int b) => new(a.X * b, a.Y * b, a.Z * b);

    public static Vector3I operator *(Vector3I a, Vector3I b) =>
        new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    public static Vector3 operator *(Vector3I a, float b) => new(a.X * b, a.Y * b, a.Z * b);

    public static readonly Vector3I Zero = new(0, 0, 0);
    public static readonly Vector3I One = new(1, 1, 1);

    public static Vector3I operator -(Vector3I a)
    {
        return new Vector3I(-a.X, -a.Y, -a.Z);
    }

    public static Vector3I operator -(Vector3I a, Vector3I b)
    {
        return a + (-b);
    }

    public static Vector3 operator *(float a, Vector3I b)
    {
        return b * a;
    }

    public readonly Vector3 ToGdVector3()
    {
        return new(X, Y, Z);
    }

    public static Vector3I FromGdVector3(Vector3 vec)
    {
        return new((int)vec.x, (int)vec.y, (int)vec.z);
    }

    public readonly bool IsNeighborOf(Vector3I other)
    {
        var dx = Math.Abs(X - other.X);
        var dy = Math.Abs(Y - other.Y);
        var dz = Math.Abs(Z - other.Z);

        return dx <= 1 && dy <= 1 && dz <= 1 && (dx != 0 || dy != 0 || dz != 0);
    }

    public readonly int Product()
    {
        return X * Y * Z;
    }

    public readonly int LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    public readonly float Length()
    {
        return Mathf.Sqrt(LengthSquared());
    }

    public Vector3I(int[] values)
        : this(values[0], values[1], values[2]) { }
}

public static class Vector3Extensions
{
    public static Vector3I ToVector3I(this Vector3 v)
    {
        return new Vector3I((int)v.x, (int)v.y, (int)v.z);
    }

    public static Vector3 ToVector3(this Vector3I v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
}
