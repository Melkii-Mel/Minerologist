using Godot;

public class ItemsDepositorAnimator
{
    public ItemsDepositorAnimator(Spatial root, ItemsDepositor depositor, Spatial player)
    {
        depositor.Deposited += entityPrefab =>
        {
            var pipe = depositor.CurrentPipe;
            if (pipe == null)
            {
                return;
            }

            var length = pipe.GetAabb().Size.y;
            var pipeEntrancePosition = pipe.GlobalTransform.Xform(new Vector3(0, length * 0.5f, 0));

            var arrivalPoint = new Spatial();
            root.AddChild(arrivalPoint);
            arrivalPoint.GlobalPosition = pipeEntrancePosition;

            var arrivalDirection = pipe.GlobalPosition - pipeEntrancePosition.Normalized();

            var entity = Entity.RandomFromPrefab(Vector3I.Zero, entityPrefab);
            entity.DestroyCollider();
            entity.MeshInstance.Scale /= 30;
            var mesh = entity.MeshInstance;
            root.AddChild(mesh);
            mesh.GlobalPosition = player.GlobalPosition;

            Animations.AddAnimation(
                new AnimationEntityFlyWithArc(
                    mesh,
                    arrivalPoint,
                    arrivalDirection,
                    maxApproachDistance: 30
                ),
                onFinish: () =>
                {
                    entity.MeshInstance.QueueFree();
                    arrivalPoint.QueueFree();
                }
            );
            Animations.AddAnimation(
                new AnimationRotateRandomly(mesh),
                easingFunction: new EFLinear()
            );
            Animations.AddAnimation(new AnimationScale(mesh, 30));
        };
    }
}
