using Godot;

public static class SceneLoader
{
    public static T Instantiate<T>(string name)
        where T : Node
    {
        var path = $"res://Assets/Scenes/{name}.tscn";

        if (!ResourceLoader.Exists(path))
        {
            path = $"res://Assets/Scenes/{name}.glb";
        }

        var scene = ResourceLoader.Load<PackedScene>(path);
        return (T)scene.Instance();
    }
}
