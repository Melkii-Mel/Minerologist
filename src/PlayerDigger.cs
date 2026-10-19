using Godot;

public class PlayerDigger
{
    public Config Config => Config.Instance;
    public PlayerVisualsConfig VisualsConfig => Config.PlayerVisualsConfig;
    public float DigRange => Config.PlayerStatsConfig.DigRange;
    public Vector3 ToolOffset => VisualsConfig.ToolOffset.ToGdVector3() / 100f;
    public float DigRadius => Config.PlayerStatsConfig.DigRadius;

    // TODO: Add to config
    public float DigDamage => 1000;
    public float DistanceDamageScaling => 0.7f;
    public float DamageChange => 0.8f;
    public float DamageScaleMin => 0.5f;
    public float DamageScaleMax => 2f;

    private readonly Node _parent;

    private readonly MeshInstance _model;
    private readonly Camera _camera;
    private readonly CollisionObject _playerCollider;
    private readonly Terrain _terrain;

    private System.Random _rng;

    public PlayerDigger(
        Node parent,
        Camera camera,
        CollisionObject player,
        Terrain terrain,
        System.Random? rng = null
    )
    {
        _rng = rng ?? new();

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
        var radius = Mathf.CeilToInt(DigRadius + (float)_rng.NextDouble());
        var radiusSquared = radius * radius;
        var globalPosition = Vector3I.FromGdVector3(worldPosition);

        var multipliers = new float[radiusSquared + 1];

        for (int z = -radius; z <= radius; z++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    var offsetDistanceSquared = x * x + y * y + z * z;
                    if (offsetDistanceSquared > radiusSquared || _rng.NextDouble() > DamageChange)
                    {
                        continue;
                    }

                    var offset = new Vector3I(x, y, z);
                    var len = offset.Length();

                    if (multipliers[offsetDistanceSquared] == 0)
                    {
                        multipliers[offsetDistanceSquared] = Mathf.Pow(DistanceDamageScaling, len);
                    }
                    var damageMultiplier = multipliers[offsetDistanceSquared];
                    var randomMultiplier = RandomUtils.FromRange(
                        DamageScaleMin,
                        DamageScaleMax,
                        _rng
                    );

                    Dig(
                        globalPosition + offset,
                        delta * DigDamage * damageMultiplier * randomMultiplier
                    );
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
