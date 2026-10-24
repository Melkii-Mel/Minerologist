using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Path = System.IO.Path;

public class Manifest
{
    private const string MANIFEST_PATH = "res://manifest.json";
    private static string? _root;
    public static string Root
    {
        get =>
            _root
            ?? throw new InvalidOperationException(
                "`Root` static property must be set before accessing Manifest's Instance"
            );
        set => _root = value;
    }
    private static FileSystemDir? _instance;
    public static FileSystemDir Instance =>
        _instance ??= OS.HasFeature("editor") ? CreateAndSaveManifest() : LoadManifest();

    private static JsonSerializerOptions SerializerOptions =>
        new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

    private static FileSystemDir CreateAndSaveManifest()
    {
        var manifest = CreateManifest();
        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        var file = new File();
        file.Open(MANIFEST_PATH, File.ModeFlags.Write);
        file.StoreString(json);
        file.Close();
        return manifest;
    }

    private static FileSystemDir CreateManifest()
    {
        return CreateManifest(Root, Root);
    }

    private static FileSystemDir CreateManifest(string path, string name)
    {
        var manifest = new FileSystemDir(name);
        var directory = OpenDir(path);
        foreach (var subDir in directory.GetDirectories())
        {
            manifest.AddChild(CreateManifest($"{path.TrimEnd('/')}/{subDir}", subDir));
        }
        foreach (var file in directory.GetFiles())
        {
            manifest.AddChild(new FileSystemFile(file, manifest));
        }
        return manifest;
    }

    private static FileSystemDir LoadManifest()
    {
        using var file = new File();

        var error = file.Open(MANIFEST_PATH, File.ModeFlags.Read);
        if (error != Error.Ok)
            throw new System.IO.IOException($"Failed to open '{MANIFEST_PATH}': {error}");

        var json = file.GetAsText();
        file.Close();
        var manifest =
            JsonSerializer.Deserialize<FileSystemDir>(json, SerializerOptions)
            ?? throw new Exception("Cannot deserialize manifest file");
        manifest.SetupConnections();
        return manifest;
    }

    private static DirAccess OpenDir(string path)
    {
        path = "res://" + path;
        var directory =
            DirAccess.Open(path)
            ?? throw new InvalidOperationException($"Path `{path}` does not exist.");
        return directory;
    }
}

public class FileSystemNode(string name, FileSystemDir? parent = null)
{
    public string Name { get; } = name;

    [JsonIgnore]
    public string FullPath => ((Parent?.FullPath + "/") ?? "") + Name;

    [JsonIgnore]
    public FileSystemDir? Parent { get; set; } = parent;

    public void SetParent(FileSystemDir parent)
    {
        if (this is FileSystemDir thisDir)
        {
            Parent?.SubDirs.Remove(thisDir);
            parent.SubDirs.Add(thisDir);
        }
        else if (this is FileSystemFile thisFile)
        {
            Parent?.Files.Remove(thisFile);
            parent.Files.Add(thisFile);
        }
        else
        {
            throw new NotSupportedException(
                "FileSystemNode can either be FileSystemDir or FileSystemFile"
            );
        }
        Parent = parent;
    }

    public string PathRelativeTo(FileSystemDir dir)
    {
        return PathOrDefaultRelativeTo(dir)!;
    }

    public string? PathOrDefaultRelativeTo(FileSystemDir dir)
    {
        var result = Name;
        FileSystemDir? parent = Parent;
        while (parent != dir)
        {
            if (parent is null)
            {
                return null;
            }
            result = parent.Name + "/" + Name;
            parent = parent.Parent;
        }
        return result;
    }
}

public class FileSystemDir(
    string name,
    FileSystemDir? parent = null,
    List<FileSystemDir>? subDirs = null,
    List<FileSystemFile>? files = null
) : FileSystemNode(name, parent)
{
    public List<FileSystemDir> SubDirs { get; } = subDirs ?? [];
    public List<FileSystemFile> Files { get; } = files ?? [];

    public void AddChild(FileSystemNode node)
    {
        node.SetParent(this);
    }

    public void SetupConnections()
    {
        foreach (var subDir in SubDirs)
        {
            subDir.Parent = this;
            subDir.SetupConnections();
        }
        foreach (var file in Files)
        {
            file.Parent = this;
        }
    }

    public IEnumerable<FileSystemDir> SubDirsRecursive()
    {
        foreach (var subDir in SubDirs)
        {
            yield return subDir;
            foreach (var subSubDir in subDir.SubDirsRecursive())
            {
                yield return subSubDir;
            }
        }
    }

    public IEnumerable<FileSystemFile> DirectFiles() => Files;

    public IEnumerable<FileSystemFile> RecursiveFiles()
    {
        foreach (var file in Files)
        {
            yield return file;
        }
        foreach (var subDir in SubDirsRecursive())
        {
            foreach (var file in subDir.DirectFiles())
            {
                yield return file;
            }
        }
    }

    public FileSystemDir? Dir(string dirName)
    {
        foreach (var subDir in SubDirs)
        {
            if (subDir.Name == dirName)
            {
                return subDir;
            }
        }
        return null;
    }
}

public class FileSystemFile(string name, FileSystemDir? parent = null)
    : FileSystemNode(name, parent)
{
    [JsonIgnore]
    public string Ext => Path.GetExtension(Name);

    [JsonIgnore]
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(Name);
}
