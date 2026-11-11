using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public record MuseumState
{
    public const string CURRENT_SKELETON_UNINITIALIZED = "UNINITIALIZED";

    public Dictionary<string, int> OtherDiggablesAmounts { get; set; } = [];
    public Dictionary<string, int> SkeletonsAmounts { get; set; } = [];
    public string CurrentSkeleton { get; set; } = CURRENT_SKELETON_UNINITIALIZED;
    public Dictionary<string, int> CurrentSkeletonPiecesAmounts { get; set; } = [];

    public const string SavePath = "melkii_mel/minerologist/saves/0.json";
    public const string BackupSuffix = ".bak";

    public void CompleteSkeleton(MuseumRoot root)
    {
        SkeletonsAmounts[CurrentSkeleton]++;
        ReplaceSkeleton(root);
    }

    public void ReplaceSkeleton(MuseumRoot root)
    {
        CurrentSkeleton = root.SkeletonSelector.SelectSkeleton();
        CurrentSkeletonPiecesAmounts = MeshCollection
            .Skeletons[CurrentSkeleton]
            .Keys.ToDictionary(k => k, _ => 0);
    }

    public static MuseumState LoadOrCreate(MuseumRoot root)
    {
        var state = Load() ?? new MuseumState();

        state.SkeletonsAmounts ??= new();
        state.OtherDiggablesAmounts ??= new();

        foreach (
            var key in MeshCollection
                .Skeletons.Keys.Concat(root.DescriptionsCollection.SkeletonDescriptions.Keys)
                .Except(["NotCollected"])
        )
        {
            if (!state.SkeletonsAmounts.ContainsKey(key))
            {
                state.SkeletonsAmounts[key] = 0;
            }
        }

        foreach (
            var key in MeshCollection
                .OtherDiggables.SelectMany(d => d.Value.Keys)
                .Concat(root.DescriptionsCollection.OtherDescriptions.Keys)
        )
        {
            if (!state.OtherDiggablesAmounts.ContainsKey(key))
            {
                state.OtherDiggablesAmounts[key] = 0;
            }
        }

        return state;
    }

    public static MuseumState? Load()
    {
        var (path, backupPath) = GetAndCreatePath();
        if (!File.Exists(path) && !File.Exists(backupPath))
        {
            return null;
        }
        MuseumState? state;
        try
        {
            var json = File.ReadAllText(path);
            state = JsonSerializer.Deserialize<MuseumState>(json);
            SaveBackup(json);
        }
        catch
        {
            var json = File.ReadAllText(backupPath);
            state = JsonSerializer.Deserialize<MuseumState>(json);
            SaveMain(json);
        }
        return state;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this);
        SaveMain(json);
        SaveBackup(json);
    }

    public void AutoSave(MuseumRoot museumRoot)
    {
        museumRoot.Root.Update += _ => Save();
    }

    private static void SaveMain(string json)
    {
        var path = GetAndCreatePath().Path;
        File.WriteAllText(path, json);
    }

    private static void SaveBackup(string json)
    {
        var path = GetAndCreatePath().BackupPath;
        File.WriteAllText(path, json);
    }

    private static (string Path, string BackupPath) GetAndCreatePath()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SavePath
        );

        var backupPath = path + BackupSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        return (path, backupPath);
    }
}
