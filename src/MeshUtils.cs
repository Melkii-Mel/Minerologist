using System;
using System.Collections.Generic;
using Godot;

public record Surface(Triangle[] Triangles, Vector3[] Vertices);

public record struct Triangle(Vector3 A, Vector3 B, Vector3 C);

public static class MeshUtils
{
    public static Surface[] GetMeshData(MeshInstance meshInstance)
    {
        var mesh = meshInstance.Mesh;
        var transform = meshInstance.Transform;
        var surfaces = new Surface[mesh.GetSurfaceCount()];
        for (int surface = 0; surface < surfaces.Length; surface++)
        {
            Godot.Collections.Array arrays = mesh.SurfaceGetArrays(surface);

            var vertices = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            var indices = (int[])arrays[(int)Mesh.ArrayType.Index];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.Xform(vertices[i]);
            }

            Triangle[] triangles;

            if (indices.Length == 0)
            {
                triangles = new Triangle[vertices.Length / 3];

                for (int i = 0; i < vertices.Length; i += 3)
                {
                    triangles[i / 3] = new(vertices[i], vertices[i + 1], vertices[i + 2]);
                }
            }
            else
            {
                triangles = new Triangle[indices.Length / 3];

                for (int i = 0; i < indices.Length; i += 3)
                {
                    triangles[i / 3] = new(
                        vertices[indices[i]],
                        vertices[indices[i + 1]],
                        vertices[indices[i + 2]]
                    );
                }
            }

            surfaces[surface] = new(triangles, vertices);
        }
        return surfaces;
    }

    public static Bounds FindAabb(Surface[] meshData)
    {
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        bool foundVertex = false;

        foreach (var surface in meshData)
        {
            var vertices = surface.Vertices;
            foreach (Vector3 v in vertices)
            {
                min.x = Math.Min(min.x, v.x);
                min.y = Math.Min(min.y, v.y);
                min.z = Math.Min(min.z, v.z);

                max.x = Math.Max(max.x, v.x);
                max.y = Math.Max(max.y, v.y);
                max.z = Math.Max(max.z, v.z);

                foundVertex = true;
            }
        }

        if (!foundVertex)
        {
            return new Bounds(Vector3I.Zero, Vector3I.Zero);
        }

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

    public static bool TestTriangleAabb(Triangle globalTriangle, Bounds globalAabb)
    {
        Vector3 a = globalTriangle.A;
        Vector3 b = globalTriangle.B;
        Vector3 c = globalTriangle.C;

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
}
