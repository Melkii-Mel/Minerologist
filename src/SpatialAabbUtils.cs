using System;
using Godot;

public static class SpatialAabbUtils
{
    public static void FitInsideRect(MeshInstance meshInstance, Vector2 size)
    {
        var bounds = meshInstance.GetAabb().Size * meshInstance.Scale;

        var dimensions = new[] { bounds.x, bounds.y, bounds.z };

        Array.Sort(dimensions);

        var scale = Mathf.Min(size.x / dimensions[2], size.y / dimensions[1]);

        meshInstance.Scale *= scale;
    }
}
