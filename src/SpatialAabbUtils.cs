using Godot;

public static class SpatialAabbUtils
{
    public static void FitInsideRect(MeshInstance meshInstance, Vector2 size)
    {
        var node = (MeshInstance)meshInstance.Duplicate();
        var bounds = node.GetAabb().Size * node.Scale;
        var scale = Mathf.Min(size.x / bounds.x, size.y / bounds.y);
        node.Scale *= scale;
    }
}
