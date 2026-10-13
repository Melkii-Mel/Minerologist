using System;
using System.Collections.Generic;
using Godot;

public static class MeshUtils
{
    public static Bounds FindAabb(Mesh mesh, Vector3I globalPosition)
    {
        throw new NotImplementedException();
    }

    public static IEnumerable<Vector3I> GetCandidatePoints(Bounds aabb)
    {
        throw new NotImplementedException();
    }

    public static Mesh Transformed(Mesh mesh, Vector3 offset, Vector3 rotation, float scale)
    {
        throw new NotImplementedException();
    }

    public static bool TestTriangleAabb((Vector3, Vector3, Vector3) triangle, Bounds aabb)
    {
        throw new NotImplementedException();
    }

    public static IEnumerable<(Vector3, Vector3, Vector3)> GetTriangles(Mesh mesh)
    {
        for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
        {
            Godot.Collections.Array arrays = mesh.SurfaceGetArrays(surface);

            var vertices = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            var indices = (int[])arrays[(int)Mesh.ArrayType.Index];

            if (indices.Length == 0)
            {
                for (int i = 0; i < vertices.Length; i += 3)
                {
                    Vector3 a = vertices[i];
                    Vector3 b = vertices[i + 1];
                    Vector3 c = vertices[i + 2];

                    yield return (a, b, c);
                }
            }
            else
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]];
                    Vector3 b = vertices[indices[i + 1]];
                    Vector3 c = vertices[indices[i + 2]];

                    yield return (a, b, c);
                }
            }
        }
    }
}
