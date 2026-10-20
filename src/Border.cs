using System.Collections.Generic;
using Godot;

public class Border(Node parent, MeshInstance meshInstance)
{
    private readonly Node _parent = parent;
    private readonly MeshInstance _meshInstance = meshInstance;

    private Vector3I ChunkSize => Config.Instance.GenerationConfig.ChunkSize;

    // TODO : Add customizable padding
    private float Width =>
        Config.Instance.GenerationConfig.ChunkSize.X
        * Config.Instance.GenerationConfig.HoleRadiusChunks
        * 2
        * 0.88f;
    private int HoleSize => 4;

    // TODO : Customizable Offset
    private Vector2 Offset => new(ChunkSize.X / 2, ChunkSize.Z / 2);

    private readonly Dictionary<int, MeshInstance> _instances = [];

    private int _lastVerticalPosition = int.MinValue;

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    public void Update(Vector3 gdWorldPosition)
    {
        var worldPosition = gdWorldPosition.ToVector3I();
        var chunkPosition = _coordinatesConverter.WorldToChunk(worldPosition);
        var verticalPoistion = chunkPosition.Y;
        if (verticalPoistion == _lastVerticalPosition)
        {
            return;
        }
        _lastVerticalPosition = verticalPoistion;
        var aabb = _meshInstance.GetAabb();
        var size = aabb.Size;
        var scale = Width / size.x;
        var height = (size * scale).y;
        var required = new HashSet<int>();
        for (var i = -Mathf.FloorToInt(HoleSize / 2f); i < Mathf.CeilToInt(HoleSize / 2f); i++)
        {
            var position = verticalPoistion + i;
            required.Add(position);
            if (!_instances.ContainsKey(position))
            {
                AddWall(position);
            }
        }
        foreach (var position in _instances.Keys)
        {
            if (!required.Contains(position))
            {
                RemoveWall(position);
            }
        }

        void AddWall(int position)
        {
            var instance = (MeshInstance)_meshInstance.Duplicate();
            _parent.AddChild(instance);
            instance.Scale = new(scale, scale, scale);
            instance.Position = new(
                worldPosition.X + Offset.x,
                height * position,
                worldPosition.Z + Offset.y
            );
            var staticBody = new StaticBody();
            instance.AddChild(staticBody);
            var shape = new CollisionShape() { Shape = instance.Mesh.CreateTrimeshShape() };
            staticBody.AddChild(shape);
        }

        void RemoveWall(int position)
        {
            var instance = _instances[position];
            _instances.Remove(position);
            instance.QueueFree();
        }
    }
}
