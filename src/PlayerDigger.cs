using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class PlayerDigger
{
    public Config Config => Config.Instance;
    public PlayerVisualsConfig VisualsConfig => Config.PlayerVisualsConfig;
    public int Level => Session.Upgrades.Level;
    public ExpConfig ExpConfig => Config.ExpConfig;
    public float DigRange => (float)ExpConfig.F.DigRange.Calculate(Level);
    public float DigRadius => (float)ExpConfig.F.DigArea.Calculate(Level);
    public float DigDamage => (float)ExpConfig.F.DigDamage.Calculate(Level);
    public float DigConeRadius => (float)ExpConfig.F.DigConeRadius.Calculate(Level);

    public event Action<MeshInstance>? DepositItemsStart;
    public event Action? DepositItemsStop;

    public DiggerConfig DiggerConfig => Config.DiggerConfig;
    public float BorderDigDamageMultiplier => DiggerConfig.BorderDigDamageMultiplier;
    public float DistanceDamageScaling => DiggerConfig.DistanceDamageScaling;
    public float DamageChance => DiggerConfig.DamageChance;
    public float DamageScaleMin => DiggerConfig.DamageScaleMin;
    public float DamageScaleMax => DiggerConfig.DamageScaleMax;
    public float DamageTicksPerSecond => DiggerConfig.DamageTicksPerSecond;
    public float BuildupSeconds => DiggerConfig.DiggingBuildupSeconds;

    public float DamageTickDuration => 1f / DamageTicksPerSecond;
    private float _damageTimer = 0f;
    private float _digTimer = 0f;

    private readonly Node _parent;

    private readonly Camera _camera;
    private readonly CollisionObject _playerCollider;
    private readonly Terrain _terrain;

    private readonly WorldChunkCoordinatesConverter _coordinatesConverter = new();

    private readonly Random _rng;

    public readonly Session Session;

    public bool _isIdle = true;

    private readonly DiggerSoundPlayer _soundPlayer;

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

        new PlayerDiggerVisuals(Session, () => _isIdle);
        _soundPlayer = new DiggerSoundPlayer(session);
    }

    public void Update(float delta, bool digging)
    {
        _isIdle = !digging;
        var hits = Hit().ToArray();
        Session.Crosshair.Active = digging;
        if (digging)
        {
            Dig(delta, hits);
            _soundPlayer.Start();
        }
        else
        {
            Idle(delta);
            _soundPlayer.Stop();
        }
    }

    public void Dig(float delta, IEnumerable<RayHit> hits)
    {
        _damageTimer += delta;
        _digTimer += delta;
        if (_damageTimer < DamageTickDuration)
        {
            return;
        }
        delta = DamageTickDuration;
        _damageTimer -= DamageTickDuration;
        var damageMultiplier = Mathf.Min(1, _digTimer / BuildupSeconds);
        delta *= damageMultiplier;

        var pipeHit = false;
        var hitSomething = false;

        foreach (var rayHit in hits)
        {
            hitSomething = true;
            if (rayHit.Collider.IsInGroup(NodeGroups.TERRAIN))
            {
                _soundPlayer.StateGround();
                Dig(rayHit.Position, delta);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.DIGGABLE))
            {
                _soundPlayer.StateDiggable();
                DigDiggable(rayHit, delta);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.PIPE))
            {
                _soundPlayer.StatePush();
                DepositItemsStart?.Invoke((MeshInstance)rayHit.Collider.GetParent());
                pipeHit = true;
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.BORDER))
            {
                _soundPlayer.StateWall();
                Dig(rayHit.Position, delta * BorderDigDamageMultiplier);
            }
            else
            {
                _soundPlayer.StateAir();
            }
        }
        if (!pipeHit)
        {
            DepositItemsStop?.Invoke();
        }
        if (!hitSomething)
        {
            _soundPlayer.StateAir();
        }
    }

    public void Idle(float _)
    {
        DepositItemsStop?.Invoke();
        _digTimer = 0;
    }

    private IEnumerable<RayHit> Hit()
    {
        var rayFrom = _camera.GlobalPosition;
        var direction = -_camera.GlobalTransform.basis.z.Normalized();
        var rayTo = rayFrom + direction * DigRange;

        var hitSomething = false;

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
            hitSomething = true;
            if (rayHit.Collider.IsInGroup(NodeGroups.TERRAIN))
            {
                SetCrosshairMode(CrosshairMode.Ground);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.DIGGABLE))
            {
                SetCrosshairMode(CrosshairMode.Diggable);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.PIPE))
            {
                SetCrosshairMode(CrosshairMode.Pipe);
            }
            else if (rayHit.Collider.IsInGroup(NodeGroups.BORDER))
            {
                SetCrosshairMode(CrosshairMode.Air);
            }
            else
            {
                SetCrosshairMode(CrosshairMode.Air);
            }
            yield return rayHit;
        }

        if (!hitSomething)
        {
            SetCrosshairMode(CrosshairMode.Air);
        }

        void SetCrosshairMode(CrosshairMode mode)
        {
            Session.Crosshair.CrosshairMode = mode;
        }
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
                delta * (float)ExpConfig.F.DigOutDamage.Calculate(Level)
            );
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
