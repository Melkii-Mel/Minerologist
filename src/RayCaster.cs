using System.Collections.Generic;
using Godot;

public static class RayCaster
{
    public static IEnumerable<RayHit> Cast(
        World world,
        Vector3 from,
        Vector3 to,
        IEnumerable<CollisionObject>? exclude = null,
        uint collisionMask = int.MaxValue,
        CollisionBehavior physicsBodyCollisionBehavior = CollisionBehavior.Stop,
        CollisionBehavior areaCollisionBehavior = CollisionBehavior.Ignore
    )
    {
        var toExclude = new Godot.Collections.Array();
        var currentFrom = from;
        var spaceState = world.DirectSpaceState;

        if (exclude != null)
        {
            foreach (var collider in exclude)
            {
                toExclude.Add(collider);
            }
        }

        while (true)
        {
            var hit = spaceState.IntersectRay(
                currentFrom,
                to,
                toExclude,
                collisionMask: collisionMask,
                collideWithBodies: physicsBodyCollisionBehavior != CollisionBehavior.Ignore,
                collideWithAreas: areaCollisionBehavior != CollisionBehavior.Ignore
            );

            if (hit.Count == 0)
            {
                break;
            }

            var rayHit = new RayHit(
                (CollisionObject)hit["collider"],
                (Vector3)hit["position"],
                (Vector3)hit["normal"],
                (int)hit["shape"]
            );

            yield return rayHit;

            if (
                rayHit.Collider is PhysicsBody
                    && physicsBodyCollisionBehavior == CollisionBehavior.Stop
                || rayHit.Collider is Area && areaCollisionBehavior == CollisionBehavior.Stop
            )
            {
                break;
            }

            toExclude.Add(hit["collider"]);
            currentFrom = (Vector3)hit["position"];
        }
    }
}

public readonly record struct RayHit(
    CollisionObject Collider,
    Vector3 Position,
    Vector3 Normal,
    int ShapeIndex
);

public enum CollisionBehavior
{
    Pass,
    Ignore,
    Stop,
}
