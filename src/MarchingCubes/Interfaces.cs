using Godot;

public interface IMeshBuilder
{
    int AddVertex(Vector3 position, Vector3 normal, Vector2 uv);
    void AddTriangle(int a, int b, int c);
}

public interface IScalarField
{
    Vector3I Size { get; }
    float Sample(int x, int y, int z);
}

public interface IVertexInterpolator
{
    (Vector3 Vec, float T) Interpolate(Vector3 p1, Vector3 p2, float v1, float v2, float isoLevel);
}
