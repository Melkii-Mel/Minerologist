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
public static class EntitiesCollection
{
    public static bool Initialized { get; private set; }

    public static EntityPrefabGroups Skeletons = [];
    public static EntityPrefabGroups Others = [];

    public const string MODELS_PATH = "res://Assets/Models";
    public const string SKELETONS_PATH = "res://Assets/Models/Skeletons";

    public static void Initialize()
    {
        if (Initialized)
        {
            return;
        }
        Initialized = true;

        LoadDir(SKELETONS_PATH, groupByFile: true, Skeletons, isSkeleton: true);
        LoadDir(MODELS_PATH, groupByFile: false, Others, isSkeleton: false, except: ["Skeletons"]);
    }

    private static void LoadDir(
        string path,
        bool groupByFile,
        EntityPrefabGroups destination,
        bool isSkeleton,
        string? prefix = null,
        string? groupName = null,
        HashSet<string>? except = null
    )
    {
        var directory =
            DirAccess.Open(path) ?? throw new Exception($"Path `{path}` does not exist.");
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
                isSkeleton,
                prefix: groupName == null ? null : JoinName(prefix, subDir),
                groupName: groupName ?? subDir,
                except
            );
        }
        foreach (var file in directory.GetFiles())
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var key = groupByFile
                ? filename
                : groupName ?? throw new Exception("Top-level files are not allowed");
            if (!destination.TryGetValue(key, out var _))
            {
                destination[key] = [];
            }
            LoadFile(
                Path.Combine(path, file),
                destination[key],
                JoinName(prefix, filename),
                isSkeleton
            );
        }
    }

    private static void LoadFile(
        string filePath,
        EntityPrefabGroup destination,
        string? prefix,
        bool isSkeleton
    )
    {
        var scene = GD.Load<PackedScene>(filePath);
        var spatial = (Spatial)scene.Instance();
        foreach (var child in spatial.GetChildren())
        {
            HandleNode((Node)child, destination, prefix, isSkeleton);
        }
    }

    private static void HandleNode(
        Node node,
        EntityPrefabGroup container,
        string? prefix,
        bool isSkeleton
    )
    {
        if (node is MeshInstance mesh)
        {
            var id = JoinName(prefix, mesh.Name);
            if (container.ContainsKey(id))
            {
                var item = container[id];
                container.Remove(id);
                item = new EntityPrefab(JoinName(item.Id, "0"), item.MeshInstance, isSkeleton);
                container.Add(item.Id, item);
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
            var prefab = new EntityPrefab(id, mesh, isSkeleton);
            container.Add(prefab.Id, prefab);
        }
        foreach (var child in node.GetChildren())
        {
            var childNode = (Node)child;
            HandleNode((Node)child, container, JoinName(prefix, childNode.Name), isSkeleton);
        }
    }

    private static string JoinName(string? prefix, string name)
    {
        return prefix != null ? $"{prefix}.{name}" : name;
    }
}

public class EntityPrefabGroup : Dictionary<string, EntityPrefab> { }

public class EntityPrefabGroups : Dictionary<string, EntityPrefabGroup> { }
