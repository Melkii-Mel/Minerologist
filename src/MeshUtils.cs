using System;
using System.Collections.Generic;
using Godot;

public static class MeshUtils
{
    public static Bounds FindAabb(Mesh mesh, Transform transform)
    {
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        bool foundVertex = false;

        for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
        {
            var arrays = mesh.SurfaceGetArrays(surface);
            var vertices = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];

            foreach (Vector3 vertex in vertices)
            {
                Vector3 p = transform.Xform(vertex);

                min.x = Math.Min(min.x, p.x);
                min.y = Math.Min(min.y, p.y);
                min.z = Math.Min(min.z, p.z);

                max.x = Math.Max(max.x, p.x);
                max.y = Math.Max(max.y, p.y);
                max.z = Math.Max(max.z, p.z);

                foundVertex = true;
            }
        }

        if (!foundVertex)
            return new Bounds(Vector3I.Zero, Vector3I.Zero);

        var position = new Vector3I(
            (int)Math.Floor(min.x),
            (int)Math.Floor(min.y),
            (int)Math.Floor(min.z)
        );

        var end = new Vector3I(
            (int)Math.Ceiling(max.x),
            (int)Math.Ceiling(max.y),
            (int)Math.Ceiling(max.z)
        );

        return new Bounds(position, end - position);
    }

    public static IEnumerable<Vector3I> GetCandidatePoints(Bounds globalAabb)
    {
        Vector3I min = globalAabb.Position;
        Vector3I max = min + globalAabb.Size;

        for (int x = min.X; x < max.X; x++)
        for (int y = min.Y; y < max.Y; y++)
        for (int z = min.Z; z < max.Z; z++)
        {
            yield return new Vector3I(x, y, z);
        }
    }

    public static bool TestTriangleAabb(
        (Vector3, Vector3, Vector3) globalTriangle,
        Bounds globalAabb
    )
    {
        Vector3 a = globalTriangle.Item1;
        Vector3 b = globalTriangle.Item2;
        Vector3 c = globalTriangle.Item3;

        Vector3 boxMin = globalAabb.Position.ToGdVector3();
        Vector3 boxMax = (globalAabb.Position + globalAabb.Size).ToGdVector3();

        Vector3 center = (boxMin + boxMax) * 0.5f;
        Vector3 halfSize = (boxMax - boxMin) * 0.5f;

        // Translate the triangle so that the box is centered at the origin.
        a -= center;
        b -= center;
        c -= center;

        Vector3 e0 = b - a;
        Vector3 e1 = c - b;
        Vector3 e2 = a - c;

        // Box face normals.
        if (Separated(new Vector3(1, 0, 0), a, b, c, halfSize))
            return false;

        if (Separated(new Vector3(0, 1, 0), a, b, c, halfSize))
            return false;

        if (Separated(new Vector3(0, 0, 1), a, b, c, halfSize))
            return false;

        // Triangle face normal.
        if (Separated(e0.Cross(e1), a, b, c, halfSize))
            return false;

        // Nine axes formed by crossing each triangle edge with each box axis.
        Vector3[] edges = [e0, e1, e2];

        Vector3[] boxAxes = [new(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1)];

        foreach (Vector3 edge in edges)
        {
            foreach (Vector3 axis in boxAxes)
            {
                if (Separated(edge.Cross(axis), a, b, c, halfSize))
                    return false;
            }
        }

        return true;
    }

    private static bool Separated(Vector3 axis, Vector3 a, Vector3 b, Vector3 c, Vector3 halfSize)
    {
        // A zero-length axis cannot separate the triangle and the box.
        if (axis.LengthSquared() < 1e-12f)
            return false;

        float pa = a.Dot(axis);
        float pb = b.Dot(axis);
        float pc = c.Dot(axis);

        float min = Math.Min(pa, Math.Min(pb, pc));
        float max = Math.Max(pa, Math.Max(pb, pc));

        float radius =
            halfSize.x * Math.Abs(axis.x)
            + halfSize.y * Math.Abs(axis.y)
            + halfSize.z * Math.Abs(axis.z);

        return min > radius || max < -radius;
    }

    public static IEnumerable<(Vector3, Vector3, Vector3)> GetTriangles(
        Mesh mesh,
        Transform globalTransform
    )
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
                    Vector3 a = globalTransform.Xform(vertices[i]);
                    Vector3 b = globalTransform.Xform(vertices[i + 1]);
                    Vector3 c = globalTransform.Xform(vertices[i + 2]);

                    yield return (a, b, c);
                }
            }
            else
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = globalTransform.Xform(vertices[indices[i]]);
                    Vector3 b = globalTransform.Xform(vertices[indices[i + 1]]);
                    Vector3 c = globalTransform.Xform(vertices[indices[i + 2]]);

                    yield return (a, b, c);
                }
            }
        }
    }
}
