using System.Collections.Generic;
using Godot;

public class GodotMeshBuilder : IMeshBuilder
{
    private readonly List<Vector3> _vertices = [];
    private readonly List<Vector3> _normals = [];
    private readonly List<Vector2> _uvs = [];
    private readonly List<int> _indices = [];

    public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        var index = _vertices.Count;

        _vertices.Add(position);
        _normals.Add(normal);
        _uvs.Add(uv);

        return index;
    }

    public void AddTriangle(int a, int b, int c)
    {
        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);
    }

    public ArrayMesh? Commit()
    {
        if (_vertices.Count == 0)
        {
            return null;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = _normals.ToArray();
        arrays[(int)Mesh.ArrayType.TexUv] = _uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = _indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        return mesh;
    }

    public void Reset()
    {
        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _indices.Clear();
    }
}
