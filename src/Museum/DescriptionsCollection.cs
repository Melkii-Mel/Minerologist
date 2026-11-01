using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DescriptionsCollection
{
    public readonly Dictionary<string, string> SkeletonDescriptions;
    public readonly Dictionary<string, string> OtherDescriptions;

    public DescriptionsCollection()
    {
        var manifest = Manifest.Instance.Dir("Descriptions");
        SkeletonDescriptions = Load(manifest.Dir("Skeletons"));
        OtherDescriptions = Load(manifest.Dir("Others"));
    }

    private Dictionary<string, string> Load(FileSystemDir dir)
    {
        return dir.DirectFiles()
            .ToDictionary(
                f => f.NameWithoutExtension,
                f =>
                {
                    using var file = new Godot.File();
                    var error = file.Open(f.FullPath, Godot.File.ModeFlags.Read);
                    if (error != Godot.Error.Ok)
                    {
                        throw new IOException($"Failed to open {f.FullPath}: {error}");
                    }
                    var md = file.GetAsText();
                    return MdToBbCode.Convert(md);
                }
            );
    }
}
