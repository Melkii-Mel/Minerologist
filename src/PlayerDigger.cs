using System;
using System.Linq;
using Godot;

public class PlayerDigger
{
    public Config Config => Config.Instance;
    public PlayerVisualsConfig VisualsConfig => Config.PlayerVisualsConfig;
    public int Level => Session.Upgrades.Level;
    public ExpConfig ExpConfig => Config.ExpConfig;
    public float DigRange => (float)ExpConfig.F.DigRange.Calculate(Level);
    public Vector3 ToolOffset => VisualsConfig.ToolOffset.ToGdVector3() / 100f;
    public float DigRadius => (float)ExpConfig.F.DigArea.Calculate(Level);
    public float DigDamage => (float)ExpConfig.F.DigDamage.Calculate(Level);
    public float DigConeRadius => (float)ExpConfig.F.DigConeRadius.Calculate(Level);

    public event Action? DepositItemsStart;
    public event Action? DepositItemsStop;

    public DiggerConfig DiggerConfig => Config.DiggerConfig;
    public float BorderDigDamageMultiplier => DiggerConfig.BorderDigDamageMultiplier;
    public float DistanceDamageScaling => DiggerConfig.DistanceDamageScaling;
    public float DamageChance => DiggerConfig.DamageChance;
    public float DamageScaleMin => DiggerConfig.DamageScaleMin;
    public float DamageScaleMax => DiggerConfig.DamageScaleMax;
    public float DamageTicksPerSecond => DiggerConfig.DamageTicksPerSecond;

    public float DamageTickDuration => 1f / DamageTicksPerSecond;
    private float _damageTimer = 0f;

    private readonly Node _parent;

    private readonly MeshInstance _model;
    private readonly Camera _camera;
    private readonly CollisionObject _playerCollider;
    private readonly Terrain _terrain;

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    private readonly Random _rng;

    public readonly Session Session;

    public PlayerDigger(
        Node parent,
        Camera camera,
        CollisionObject player,
        Terrain terrain,
        Session session,
        Random? rng = null
    )
    {
        Session = session;
        _rng = rng ?? new();

        _parent = parent;
        _camera = camera;
        _playerCollider = player;
        _terrain = terrain;

        _model = CreateModel();
    }

    public void Dig(float delta)
    {
        _damageTimer += delta;
        if (_damageTimer < DamageTickDuration)
        {
            return;
        }
        delta = DamageTickDuration;
        _damageTimer -= DamageTickDuration;

        var rayFrom = _camera.GlobalPosition;
        var direction = -_camera.GlobalTransform.basis.z.Normalized();
        var rayTo = rayFrom + direction * DigRange;
        var pipeHit = false;

        foreach (
            var rayHit in ConeCaster.Cast(
                _camera.GetWorld(),
                rayFrom,
                rayTo,
                Mathf.Deg2Rad(DigConeRadius) / 2,
                exclude: [_playerCollider]
            )
        )
        {
            if (rayHit.Collider.IsInGroup(NodeGroups.TERRAIN))
            {
                Dig(rayHit.Position, delta);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.DIGGABLE))
            {
                DigDiggable(rayHit, delta);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.PIPE))
            {
                DepositItemsStart?.Invoke();
                pipeHit = true;
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.BORDER))
            {
                Dig(rayHit.Position, delta * BorderDigDamageMultiplier);
            }
        }
        if (!pipeHit)
        {
            DepositItemsStop?.Invoke();
        }
    }

    public void Idle(float _)
    {
        DepositItemsStop?.Invoke();
    }

    private void DigDiggable(RayHit rayHit, float delta)
    {
        if (
            !_terrain.GlobalMeshInstanceEntityMap.TryGetValue(
                (MeshInstance)rayHit.Collider.GetParent(),
                out var entity
            )
        )
        {
            return;
        }
        var entitySupportingTiles = _terrain.GlobalEntitySupportingTilesIndex[entity];
        var count = Mathf.Min(
            (float)ExpConfig.F.DigOutSpeed.Calculate(Level),
            entitySupportingTiles.Length
        );
        for (var i = 0; i < count; i++)
        {
            Dig(
                entitySupportingTiles[_rng.Next(entitySupportingTiles.Count())].ToGdVector3(),
                delta
            );
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
                    if (offsetDistanceSquared > radiusSquared || _rng.NextDouble() > DamageChance)
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
