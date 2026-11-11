using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DescriptionsCollection
{
    public readonly Dictionary<string, string> SkeletonDescriptions;
    public readonly Dictionary<string, string> OtherDescriptions;
    private readonly string _description404;

    public DescriptionsCollection()
    {
        var manifest = Manifest.Instance.Dir("Descriptions");
        SkeletonDescriptions = Load(manifest.Dir("Skeletons"));
        OtherDescriptions = Load(manifest.Dir("Others"));
        _description404 = LoadFile(manifest.File("404.md"));
    }

    public string SkeletonGetOrDefault(string skeletonName)
    {
        return GetOrDefault(skeletonName, SkeletonDescriptions);
    }

    public string OtherGetOrDefault(string otherName)
    {
        return GetOrDefault(otherName, OtherDescriptions);
    }

    public string GetOrDefault(string name, Dictionary<string, string> descriptions)
    {
        if (descriptions.TryGetValue(name, out var description))
        {
            return description;
        }
        return _description404;
    }

    private Dictionary<string, string> Load(FileSystemDir dir)
    {
        return dir.DirectFiles().ToDictionary(f => f.NameWithoutExtension, LoadFile);
    }

    private string LoadFile(FileSystemFile f)
    {
        using var file = new Godot.File();
        var error = file.Open("res:/" + f.FullPath, Godot.File.ModeFlags.Read);
        if (error != Godot.Error.Ok)
        {
            throw new IOException($"Failed to open {f.FullPath}: {error}");
        }
        var md = file.GetAsText();
        return md;
    }
}
