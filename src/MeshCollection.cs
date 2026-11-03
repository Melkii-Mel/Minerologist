using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

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
    public static MeshGroup OtherDiggablesFlat { get; private set; } = [];
    public static MeshGroup OtherMeshes { get; private set; } = [];

    public const string MODELS = "Models";
    public const string OTHERS = "Others";
    public const string DIGGABLES = "Diggables";
    public const string SKELETONS = "Skeletons";
    public const string MODELS_EXTENSION = ".glb";

    public static void Initialize()
    {
        if (Initialized)
        {
            return;
        }
        Initialized = true;

        var modelsManifest = Manifest.Instance.Dir(MODELS);
        var otherMeshesManifest = modelsManifest.Dir(OTHERS);
        var diggablesManifest = modelsManifest.Dir(DIGGABLES);
        var skeletonsManifest = diggablesManifest.Dir(SKELETONS);
        var otherDiggablesManifest = diggablesManifest.Dir(OTHERS);

        LoadDir(skeletonsManifest, groupByFile: true, Skeletons);
        LoadDir(otherDiggablesManifest, groupByFile: false, OtherDiggables);
        OtherDiggablesFlat =
        [
            with(OtherDiggables.SelectMany(d => d.Value).ToDictionary(d => d.Key, d => d.Value)),
        ];
        var otherMeshes = new MeshGroups { [OTHERS] = [] };
        LoadDir(otherMeshesManifest, groupByFile: false, otherMeshes, groupName: OTHERS);
        GD.Print(
            "Other meshes manifest: ",
            string.Join(", ", otherMeshesManifest.RecursiveFiles().Select(f => f.Ext))
        );
        OtherMeshes = otherMeshes[OTHERS];
        GD.Print("Other loaded meshes: ", ToCodeUnits(string.Join(", ", OtherMeshes.Keys)));
    }

    private static string ToCodeUnits(string value)
    {
        return string.Join(" ", value.Select(c => ((int)c).ToString("X4")));
    }

    private static void LoadDir(
        FileSystemDir dir,
        bool groupByFile,
        MeshGroups destination,
        string? groupName = null
    )
    {
        if (groupName == null)
        {
            if (groupByFile)
            {
                foreach (var file in dir.RecursiveFiles().Where(f => f.Ext == MODELS_EXTENSION))
                {
                    destination[file.NameWithoutExtension] = [];
                    LoadFile(
                        "res://" + file.FullPath,
                        destination[file.NameWithoutExtension],
                        null
                    );
                }
            }
            else
            {
                foreach (var subDir in dir.SubDirs)
                {
                    LoadDir(subDir, groupByFile, destination, groupName: subDir.Name);
                }
                foreach (var file in dir.Files.Where(f => f.Ext == MODELS_EXTENSION))
                {
                    throw new Exception("Top-level files are not allowed");
                }
            }
        }
        else
        {
            destination[groupName] = [];
            foreach (var file in dir.RecursiveFiles().Where(f => f.Ext == MODELS_EXTENSION))
            {
                GD.Print(file.PathRelativeTo(dir));
                LoadFile(
                    "res://" + file.FullPath,
                    destination[groupName],
                    System.IO.Path.GetFileNameWithoutExtension(
                        file.PathRelativeTo(dir).Replace('/', '.')
                    )
                );
            }
        }
    }

    private static void LoadFile(string filePath, MeshGroup destination, string? prefix)
    {
        var scene = GD.Load<PackedScene>(filePath);
        var spatial = (Spatial)scene.Instance();
        if (spatial.GetChildCount() == 0)
        {
            return;
        }
        var joinName = spatial.GetChildCount() > 1 || spatial.GetChild(0).GetChildCount() > 0;
        foreach (var child in spatial.GetChildren())
        {
            HandleNode((Node)child, destination, prefix, joinName);
        }
    }

    private static void HandleNode(Node node, MeshGroup container, string? prefix, bool joinName)
    {
        if (node is MeshInstance mesh)
        {
            var id = (joinName || prefix == null) ? JoinName(prefix, mesh.Name) : prefix;
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
            HandleNode((Node)child, container, JoinName(prefix, childNode.Name), joinName);
        }
    }

    private static string JoinName(string? prefix, string name)
    {
        return prefix != null ? $"{prefix}.{name}" : name;
    }
}

public class MeshGroup : Dictionary<string, MeshInstance>
{
    public MeshGroup() { }

    public MeshGroup(Dictionary<string, MeshInstance> dict)
        : base(dict) { }

    public MeshGroup Duplicate()
    {
        return
        [
            with(this.ToDictionary(kvp => kvp.Key, kvp => (MeshInstance)kvp.Value.Duplicate())),
        ];
    }
}

public class MeshGroups : Dictionary<string, MeshGroup> { }
