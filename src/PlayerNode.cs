using System.Linq;
using Godot;

public class PlayerNode : KinematicBody
{
    public Spatial Center { get; private set; } = null!;
    public Spatial Collector { get; private set; } = null!;

    public override void _Ready()
    {
        var collisionShape = GetChildren().OfType<CollisionShape>().FirstOrDefault();
        Center = collisionShape;
        Collector = new Spatial() { Position = Center.Position / 2 };
    }
}
