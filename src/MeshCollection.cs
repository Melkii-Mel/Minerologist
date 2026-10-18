using System;
using System.Collections.Generic;
using Godot;
using Path = System.IO.Path;

/// <summary>
/// Represents all diggable entities dynamically loaded from the model assets.
///
/// Skeletons are grouped by their GLB filename. Each MeshInstance in a skeleton
/// is identified by its node name and hierarchy within the scene.
///
/// Other diggables are grouped by their top-level directory under the models
/// directory. The directory hierarchy below the group directory and the GLB
/// filename form the prefix of each entity's ID.
///
/// For example:
///
///     .../Ores/Copper.glb
///         Rock     -> Copper.Rock
///
///     .../Ores/Copper/Crystal.glb
///         Rock     -> Copper.Crystal.Rock
///
/// Multiple MeshInstances in the same GLB are identified by their node names:
///
///     .../Ores/RPS.glb
///         Rock     -> RPS.Rock
///         Paper    -> RPS.Paper
///
/// If multiple objects would receive the same ID, all instances are assigned
/// numeric suffixes starting at zero:
///
///     Rock, Rock -> RPS.Rock.0, RPS.Rock.1
///
/// Node hierarchy is included in the ID recursively. A child node's name is
/// appended to the names of its ancestors, so nested MeshInstances are uniquely
/// identified by their path within the scene.
///
/// </summary>
public static class MeshCollection
{
    public static bool Initialized { get; private set; }

    public static MeshGroups Skeletons { get; private set; } = [];
    public static MeshGroups OtherDiggables { get; private set; } = [];
    public static MeshGroup OtherMeshes { get; private set; } = [];

    public const string OTHER_MESHES_PATH = "res://Assets/Models/Others";
    public const string OTHER_DIGGABLES_PATH = "res://Assets/Models/Diggables/Others";
    public const string SKELETONS_PATH = "res://Assets/Models/Diggables/Skeletons";
    public const string MODELS_EXTENSION = ".glb";

    public static void Initialize()
    {
        if (Initialized)
        {
            return;
        }
        Initialized = true;

        LoadDir(SKELETONS_PATH, groupByFile: true, Skeletons);
        LoadDir(OTHER_DIGGABLES_PATH, groupByFile: false, OtherDiggables);
        var otherMeshes = new MeshGroups { [OTHER_MESHES_PATH] = [] };
        LoadDir(OTHER_MESHES_PATH, groupByFile: false, otherMeshes, groupName: OTHER_MESHES_PATH);
        OtherMeshes = otherMeshes[OTHER_MESHES_PATH];
        GD.Print("Other loaded meshes: ", string.Join(", ", OtherMeshes.Keys));
    }

    private static void LoadDir(
        string path,
        bool groupByFile,
        MeshGroups destination,
        string? prefix = null,
        string? groupName = null,
        HashSet<string>? except = null
    )
    {
        var directory = DirAccess.Open(path);
        if (directory == null)
        {
            GD.PrintErr($"Path `{path}` does not exist.");
            return;
        }
        foreach (var subDir in directory.GetDirectories())
        {
            if (except?.Contains(subDir) ?? false)
            {
                continue;
            }
            LoadDir(
                Path.Combine(path, subDir),
                groupByFile,
                destination,
                prefix: groupName == null ? null : JoinName(prefix, subDir),
                groupName: groupName ?? subDir,
                except
            );
        }
        foreach (var file in directory.GetFiles())
        {
            if (Path.GetExtension(file) != MODELS_EXTENSION)
            {
                continue;
            }
            var filename = Path.GetFileNameWithoutExtension(file);
            var key = groupByFile
                ? filename
                : groupName ?? throw new Exception("Top-level files are not allowed");
            if (!destination.TryGetValue(key, out var _))
            {
                destination[key] = [];
            }
            LoadFile(Path.Combine(path, file), destination[key], JoinName(prefix, filename));
        }
    }

    private static void LoadFile(string filePath, MeshGroup destination, string? prefix)
    {
        var scene = GD.Load<PackedScene>(filePath);
        var spatial = (Spatial)scene.Instance();
        foreach (var child in spatial.GetChildren())
        {
            HandleNode((Node)child, destination, prefix);
        }
    }

    private static void HandleNode(Node node, MeshGroup container, string? prefix)
    {
        if (node is MeshInstance mesh)
        {
            var id = JoinName(prefix, mesh.Name);
            if (container.ContainsKey(id))
            {
                var item = container[id];
                container.Remove(id);
                var newId = JoinName(id, "0");
                container.Add(newId, item);
            }
            var i = 0;
            while (container.ContainsKey(JoinName(id, i.ToString())))
            {
                i++;
            }
            if (i > 0)
            {
                id = JoinName(id, i.ToString());
            }
            container.Add(id, mesh);
        }
        foreach (var child in node.GetChildren())
        {
            var childNode = (Node)child;
            HandleNode((Node)child, container, JoinName(prefix, childNode.Name));
        }
    }

    private static string JoinName(string? prefix, string name)
    {
        return prefix != null ? $"{prefix}.{name}" : name;
    }
}

public class MeshGroup : Dictionary<string, MeshInstance> { }

public class MeshGroups : Dictionary<string, MeshGroup> { }
