using Godot;

public static class SceneLoader
{
    public static T Instantiate<T>(string name)
        where T : Node
    {
        var scene = (PackedScene)ResourceLoader.Load($"res://Assets/Scenes/{name}.tscn");
        return (T)scene.Instance();
    }
}
