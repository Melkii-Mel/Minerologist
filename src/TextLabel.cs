using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
/// Wraps a RichTextLabel: Markdown (or raw BBCode) in, styled text out, with {placeholder}
/// substitution. Godot 3 has no [size] tag, so heading sizes come from DynamicFont resources
/// generated from <see cref="MdConfig.HeadingSizeMultipliers"/> and cached under user://.
/// </summary>
public class TextLabel
{
    private const string FontCacheDir = "user://md_fonts";

    private static readonly Regex PlaceholderPattern = new(@"\{(!?)(\w+)\}", RegexOptions.Compiled);

    public RichTextLabel Node { get; }

    /// <summary>
    /// Markdown source. Setting it converts to BBCode and updates the label.
    /// </summary>
    public string? MdText
    {
        get => _mdText;
        set
        {
            _mdText = value;
            SetBbcode(MdToBbCode.Convert(value ?? "", _renderConfig));
        }
    }

    /// <summary>
    /// BBCode template, before placeholder substitution. Setting it directly shows the BBCode
    /// as-is (placeholders applied) and clears <see cref="MdText"/>, since the two would
    /// otherwise disagree.
    /// </summary>
    public string BbcodeText
    {
        get => _bbcodeText;
        set
        {
            _mdText = null;
            SetBbcode(value);
        }
    }

    private string? _mdText;
    private string _bbcodeText = "";

    private MdConfig _mdConfig = new();
    private MdConfig _renderConfig = new(); // _mdConfig + generated heading fonts

    // Stored as given; escaping is decided per use by the {name} / {!name} syntax.
    private readonly Dictionary<string, string> _placeholders = new();

    public TextLabel(
        RichTextLabel? node = default,
        TextLabelConfig? config = default,
        MdConfig? mdConfig = default
    )
    {
        Node = node ?? new RichTextLabel();
        Node.BbcodeEnabled = true;
        Apply(config ?? new TextLabelConfig(), mdConfig);
    }

    /// <summary>
    /// Applies fonts and colors to the label and re-renders the current text.
    /// Pass <paramref name="mdConfig"/> to change the Markdown settings too; omit it to keep them.
    /// </summary>
    public void Apply(TextLabelConfig config, MdConfig? mdConfig = null)
    {
        _mdConfig = mdConfig ?? _mdConfig;

        var size = Math.Max(1, Mathf.RoundToInt(config.BaseFontSize));

        // Without a FontPath, reuse whatever font the label already has so heading sizes
        // can still be derived from it.
        var regular =
            LoadFontData(config.FontPath) ?? (Node.GetFont("normal_font") as DynamicFont)?.FontData;
        var bold = LoadFontData(config.BoldFontPath);
        var italic = LoadFontData(config.ItalicFontPath);
        var boldItalic = LoadFontData(config.BoldItalicFontPath);
        var mono = LoadFontData(config.MonoFontPath);

        // Overrides are only added when a font exists; the theme default stays otherwise.
        SetFont("normal_font", regular, size);
        SetFont("bold_font", bold, size);
        SetFont("italics_font", italic, size);
        SetFont("bold_italics_font", boldItalic, size);
        SetFont("mono_font", mono, size);

        Node.AddColorOverride("default_color", config.BaseColor);

        _renderConfig = _mdConfig with
        {
            HeadingFonts = BuildHeadingFonts(
                bold ?? regular,
                size,
                _mdConfig.HeadingSizeMultipliers
            ),
        };

        if (_mdText != null)
            SetBbcode(MdToBbCode.Convert(_mdText, _renderConfig));
        else
            Refresh();
    }

    /// <summary>
    /// Sets the value of a placeholder (name given without braces) and re-renders.
    /// In the text, <c>{name}</c> inserts the value literally (brackets escaped) and
    /// <c>{!name}</c> inserts it as-is, so the value may contain BBCode.
    /// Unknown placeholders are left untouched.
    /// </summary>
    public void Set(string placeholder, string value)
    {
        _placeholders[placeholder.Trim('{', '}')] = value;
        Refresh();
    }

    private void SetBbcode(string bbcode)
    {
        _bbcodeText = bbcode ?? "";
        Refresh();
    }

    // Pushes template + placeholders to the label. Assigning BbcodeText re-parses the tags,
    // which is also what makes the label pick up new fonts.
    private void Refresh() => Node.BbcodeText = ApplyPlaceholders(_bbcodeText);

    // One pass, so a value that itself contains "{other}" is not substituted again.
    private string ApplyPlaceholders(string bbcode)
    {
        if (_placeholders.Count == 0)
            return bbcode;

        return PlaceholderPattern.Replace(
            bbcode,
            m =>
            {
                if (!_placeholders.TryGetValue(m.Groups[2].Value, out var value))
                    return m.Value;

                var raw = m.Groups[1].Length > 0; // {!name}
                return raw ? value : MdToBbCode.Escape(value, _renderConfig);
            }
        );
    }

    private void SetFont(string name, DynamicFontData? data, int size)
    {
        if (data == null)
            return;

        Node.AddFontOverride(name, new DynamicFont { FontData = data, Size = size });
    }

    private static DynamicFontData? LoadFontData(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (!ResourceLoader.Exists(path))
        {
            Out.Error($"Font not found: {path}");
            return null;
        }

        return GD.Load<DynamicFontData>(path);
    }

    // BBCode's [font=...] can only reference a resource path, so each heading font is saved
    // as a .tres under user:// (named by level, size and source font) and reused afterwards.
    private static string?[] BuildHeadingFonts(
        DynamicFontData? data,
        int baseSize,
        float[] multipliers
    )
    {
        if (data == null || multipliers.Length == 0)
            return [];

        _ = new Directory().MakeDirRecursive(FontCacheDir);

        var sourceHash = data.ResourcePath.MD5Text().Substring(0, 8);
        var paths = new string?[multipliers.Length];

        for (var i = 0; i < multipliers.Length; i++)
        {
            var size = Math.Max(1, Mathf.RoundToInt(baseSize * multipliers[i]));
            var path = $"{FontCacheDir}/h{i + 1}_{size}_{sourceHash}.tres";

            if (!ResourceLoader.Exists(path))
            {
                var font = new DynamicFont { FontData = data, Size = size };

                if (ResourceSaver.Save(path, font) != Error.Ok)
                {
                    GD.PushWarning($"TextLabel: could not save heading font {path}");
                    continue; // null entry -> that level falls back to [b]
                }
            }

            paths[i] = path;
        }

        return paths;
    }
}

public record TextLabelConfig
{
    public float BaseFontSize { get; init; } = 12;
    public Color BaseColor { get; init; } = Colors.Black;

    /// <summary>
    /// Regular font (.ttf/.otf). Null keeps the label's current font.
    /// </summary>
    public string? FontPath { get; init; }

    // Optional: emphasis and inline code are invisible in Godot 3 without these.
    public string? BoldFontPath { get; init; }
    public string? ItalicFontPath { get; init; }
    public string? BoldItalicFontPath { get; init; }
    public string? MonoFontPath { get; init; }
}
