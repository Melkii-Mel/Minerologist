using Godot;

public class PlayerDigger
{
    public Config Config => Config.Instance;
    public PlayerVisualsConfig VisualsConfig => Config.PlayerVisualsConfig;
    public float DigRange => Config.PlayerStatsConfig.DigRange;
    public Vector3 ToolOffset => VisualsConfig.ToolOffset.ToGdVector3() / 100f;
    public float DigRadius => Config.PlayerStatsConfig.DigRadius;

    // TODO: Add DigDamage config
    public float DigDamage => 100000;

    private readonly Node _parent;

    private readonly MeshInstance _model;
    private readonly Camera _camera;
    private readonly CollisionObject _playerCollider;
    private readonly Terrain _terrain;

    public PlayerDigger(Node parent, Camera camera, CollisionObject player, Terrain terrain)
    {
        _parent = parent;
        _camera = camera;
        _playerCollider = player;
        _terrain = terrain;

        _model = CreateModel();
    }

    public void Dig(float delta)
    {
        var rayFrom = _camera.GlobalPosition;
        var direction = -_camera.GlobalTransform.basis.z.Normalized();
        var rayTo = rayFrom + direction * DigRange;

        foreach (
            var collisionObject in RayCaster.Cast(
                _camera.GetWorld(),
                rayFrom,
                rayTo,
                exclude: [_playerCollider]
            )
        )
        {
            if (collisionObject.Collider.IsInGroup(NodeGroups.DIGGABLE))
            {
                Dig(collisionObject.Position, delta);
            }
        }
    }

    private MeshInstance CreateModel()
    {
        if (MeshCollection.OtherMeshes.TryGetValue(VisualsConfig.ToolModel, out var diggerModel))
        {
            diggerModel = (MeshInstance)diggerModel.Duplicate();
            _parent.AddChild(diggerModel);
            diggerModel.Position = ToolOffset;
            return diggerModel;
        }
        else
        {
            GD.PrintErr("Tool model not found");
            return new MeshInstance();
        }
    }

    public void Dig(Vector3 worldPosition, float delta)
    {
        var radius = Mathf.CeilToInt(DigRadius);
        var radiusSquared = radius * radius;
        var globalPosition = Vector3I.FromGdVector3(worldPosition);

        for (int z = -radius; z <= radius; z++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y + z * z > radiusSquared)
                    {
                        continue;
                    }

                    var offset = new Vector3I(x, y, z);

                    Dig(globalPosition + offset, delta * DigDamage);
                }
            }
        }
        _terrain.FlushChunkChangeEvents();
    }

    private void Dig(Vector3I point, float damage)
    {
        if (_terrain.TryChangeCellValue(point, -damage, notify: false, onlyIfContactsAir: true)) { }
    }
}
