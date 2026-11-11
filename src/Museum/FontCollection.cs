using System.Linq;
using Godot;

public static class FontCollection
{
    public static string FontPath(string fontName)
    {
        var fontPath = new[] { ".ttf", ".otf", ".woff", ".woff2" }
            .Select(extension =>
                System
                    .IO.Path.Combine(
                        "res://" + Manifest.Instance.Dir("Fonts").FullPath,
                        fontName + extension
                    )
                    .Replace('\\', '/')
            )
            .FirstOrDefault(s => ResourceLoader.Exists(s));
        if (string.IsNullOrEmpty(fontPath))
        {
            Out.Error($"Font path is null or empty for name {fontName}");
        }
        return fontPath;
    }
}
