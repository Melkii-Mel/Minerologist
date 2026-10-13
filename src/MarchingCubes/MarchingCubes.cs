using System.Collections.Generic;
using Godot;

public static class MarchingCubes
{
    public static void Generate(
        IScalarField field,
        IMeshBuilder mesh,
        MarchingCubesOptions? options = null
    )
    {
        var size = field.Size;
        var generatedVertices = new Dictionary<Vector3, int>();
        var cube = new float[8];
        var edgeVertices = new int[12];
        options ??= new();
        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                for (var z = 0; z < size.Z; z++)
                {
                    HandleVoxel(
                        field,
                        x,
                        y,
                        z,
                        mesh,
                        options,
                        generatedVertices,
                        cube,
                        edgeVertices
                    );
                }
            }
        }
    }

    private static void HandleVoxel(
        IScalarField field,
        int x,
        int y,
        int z,
        IMeshBuilder mesh,
        MarchingCubesOptions options,
        Dictionary<Vector3, int> generatedVertices,
        float[] cube,
        int[] edgeVertices
    )
    {
        var configurationIndex = 0;

        for (var i = 0; i < 8; i++)
        {
            var corner = CubeTables.CornerTable[i];

            var value = field.Sample(x + corner[0], y + corner[1], z + corner[2]);

            cube[i] = value;

            if (value > options.IsoLevel)
            {
                configurationIndex |= 1 << i;
            }
        }

        if (configurationIndex == 0 || configurationIndex == 255)
            return;

        for (var i = 0; i < 12; i++)
            edgeVertices[i] = -1;

        var triangles = CubeTables.TriTable[configurationIndex];

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var edge0 = triangles[i];

            if (edge0 == -1)
                break;

            var edge1 = triangles[i + 1];
            var edge2 = triangles[i + 2];

            var a = GetEdgeVertex(
                edge0,
                edgeVertices,
                cube,
                x,
                y,
                z,
                field,
                mesh,
                options,
                generatedVertices
            );

            var b = GetEdgeVertex(
                edge1,
                edgeVertices,
                cube,
                x,
                y,
                z,
                field,
                mesh,
                options,
                generatedVertices
            );

            var c = GetEdgeVertex(
                edge2,
                edgeVertices,
                cube,
                x,
                y,
                z,
                field,
                mesh,
                options,
                generatedVertices
            );

            mesh.AddTriangle(a, b, c);
        }
    }

    private static int GetEdgeVertex(
        int edge,
        int[] edgeVertices,
        float[] cube,
        int x,
        int y,
        int z,
        IScalarField field,
        IMeshBuilder mesh,
        MarchingCubesOptions options,
        Dictionary<Vector3, int>? generatedVertices
    )
    {
        var cached = edgeVertices[edge];

        if (cached != -1)
            return cached;

        var corners = CubeTables.EdgeTable[edge];

        var cornerA = CubeTables.CornerTable[corners[0]];
        var cornerB = CubeTables.CornerTable[corners[1]];

        var positionA = new Vector3I(x + cornerA[0], y + cornerA[1], z + cornerA[2]);

        var positionB = new Vector3I(x + cornerB[0], y + cornerB[1], z + cornerB[2]);

        var sampleA = cube[corners[0]];
        var sampleB = cube[corners[1]];

        var (position, t) = options.Interpolator.Interpolate(
            positionA.ToGdVector3(),
            positionB.ToGdVector3(),
            sampleA,
            sampleB,
            options.IsoLevel
        );

        if (options.WeldVertices && generatedVertices!.TryGetValue(position, out var existingIndex))
        {
            edgeVertices[edge] = existingIndex;
            return existingIndex;
        }

        var normal = Vector3.Zero;

        if (options.GenerateNormals)
        {
            normal = options.NormalMode switch
            {
                NormalMode.InterpolatedGradient => -GetGradient(field, positionA)
                    .LinearInterpolate(GetGradient(field, positionB), t)
                    .Normalized(),

                NormalMode.TrilinearGradient => -GetTrilinearGradient(cube, position, x, y, z)
                    .Normalized(),

                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(options.NormalMode),
                    options.NormalMode,
                    "Unsupported normal mode."
                ),
            };
        }

        var uv = Vector2.Zero;

        var index = mesh.AddVertex(position, normal, uv);

        edgeVertices[edge] = index;

        if (options.WeldVertices)
        {
            generatedVertices![position] = index;
        }

        return index;
    }

    private static Vector3 GetGradient(IScalarField field, Vector3I position)
    {
        return new Vector3(
            field.Sample(position.X + 1, position.Y, position.Z)
                - field.Sample(position.X - 1, position.Y, position.Z),
            field.Sample(position.X, position.Y + 1, position.Z)
                - field.Sample(position.X, position.Y - 1, position.Z),
            field.Sample(position.X, position.Y, position.Z + 1)
                - field.Sample(position.X, position.Y, position.Z - 1)
        );
    }

    private static Vector3 GetTrilinearGradient(float[] cube, Vector3 position, int x, int y, int z)
    {
        var px = position.x - x;
        var py = position.y - y;
        var pz = position.z - z;

        var gradient = Vector3.Zero;

        for (var i = 0; i < 8; i++)
        {
            var corner = CubeTables.CornerTable[i];

            var cx = corner[0];
            var cy = corner[1];
            var cz = corner[2];

            var fx = cx == 1 ? px : 1f - px;
            var fy = cy == 1 ? py : 1f - py;
            var fz = cz == 1 ? pz : 1f - pz;

            var value = cube[i];

            gradient.x += (cx == 1 ? 1f : -1f) * fy * fz * value;
            gradient.y += (cy == 1 ? 1f : -1f) * fx * fz * value;
            gradient.z += (cz == 1 ? 1f : -1f) * fx * fy * value;
        }

        return gradient;
    }
}
