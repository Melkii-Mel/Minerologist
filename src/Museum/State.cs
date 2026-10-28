using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public record MuseumState
{
    public const string CURRENT_SKELETON_UNINITIALIZED = "UNINITIALIZED";

    public Dictionary<string, int> SkeletonAmounts { get; set; } = [];
    public Dictionary<string, int> OtherDiggablesAmounts { get; set; } = [];
    public string CurrentSkeleton { get; set; } = "UNINITIALIZED";
    public Dictionary<string, int> CurrentSkeletonPiecesAmounts { get; set; } = [];
    public Dictionary<string, int> SkeletonsAmounts = [];

    public const string SavePath = "melkii_mel/minerologist/saves/0.json";
    public const string BackupSuffix = ".bak";

    public static MuseumState LoadOrCreate()
    {
        return Load() ?? new();
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

    public static void Save(MuseumState state)
    {
        var json = JsonSerializer.Serialize(state);
        SaveMain(json);
        SaveBackup(json);
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
