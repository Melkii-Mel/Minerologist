using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ConeCaster
{
    public static ConeCasterConfig ConeCasterConfig = Config.Instance.ConeCasterConfig;
    public static int Rings = ConeCasterConfig.Rings;
    public static int RaysPerRing = ConeCasterConfig.RaysPerRing;

    public static IEnumerable<RayHit> Cast(
        World world,
        Vector3 from,
        Vector3 to,
        float halfAngleRadians = Mathf.Pi / 12f,
        IEnumerable<CollisionObject>? exclude = null,
        uint collisionMask = int.MaxValue,
        CollisionBehavior physicsBodyCollisionBehavior = CollisionBehavior.Stop,
        CollisionBehavior areaCollisionBehavior = CollisionBehavior.Ignore
    )
    {
        if (Rings <= 0 || RaysPerRing <= 0)
        {
            yield break;
        }

        var direction = to - from;
        var length = direction.Length();

        if (length <= 0f || halfAngleRadians < 0f || halfAngleRadians >= Mathf.Pi / 2f)
        {
            yield break;
        }

        direction /= length;

        var reference = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;

        var right = direction.Cross(reference).Normalized();
        var up = direction.Cross(right).Normalized();

        var excluded = exclude?.ToList() ?? [];

        var hits = new Dictionary<ulong, RayHit>();

        AddHits(to);

        var baseRadius = length * Mathf.Tan(halfAngleRadians);

        for (var ring = 1; ring <= Rings; ring++)
        {
            var radius = baseRadius * ring / Rings;
            var count = RaysPerRing * ring;

            for (var i = 0; i < count; i++)
            {
                var angle = 2f * Mathf.Pi * i / count;

                var offset = right * (Mathf.Cos(angle) * radius) + up * (Mathf.Sin(angle) * radius);

                AddHits(to + offset);
            }
        }

        var orderedHits = hits.Values.OrderBy(hit => from.DistanceSquaredTo(hit.Position));

        foreach (var hit in orderedHits)
        {
            yield return hit;

            if (
                hit.Collider is PhysicsBody
                    && physicsBodyCollisionBehavior == CollisionBehavior.Stop
                || hit.Collider is Area && areaCollisionBehavior == CollisionBehavior.Stop
            )
            {
                yield break;
            }
        }

        void AddHits(Vector3 rayEnd)
        {
            foreach (
                var hit in RayCaster.Cast(
                    world,
                    from,
                    rayEnd,
                    excluded,
                    collisionMask,
                    physicsBodyCollisionBehavior,
                    areaCollisionBehavior
                )
            )
            {
                var id = hit.Collider.GetInstanceId();

                if (
                    !hits.TryGetValue(id, out var previous)
                    || from.DistanceSquaredTo(hit.Position)
                        < from.DistanceSquaredTo(previous.Position)
                )
                {
                    hits[id] = hit;
                }
            }
        }
    }
}
