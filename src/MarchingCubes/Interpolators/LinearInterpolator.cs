using Godot;

public sealed class LinearVertexInterpolator : IVertexInterpolator
{
    public (Vector3 Vec, float T) Interpolate(
        Vector3 p1,
        Vector3 p2,
        float v1,
        float v2,
        float isoLevel
    )
    {
        float t = (isoLevel - v1) / (v2 - v1);
        return (p1 + (p2 - p1) * t, t);
    }
}
