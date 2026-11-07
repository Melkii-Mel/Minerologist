// AI-generated

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Markdown rendering settings. Godot 3's RichTextLabel has no [size], [quote], [list] or [hr]
/// tags, so those are emulated with fonts, [indent], [color] and glyphs.
/// </summary>
public record MdConfig
{
    /// <summary>
    /// Font size of each heading level relative to the base font size (index 0 = H1).
    /// Levels past the end reuse the last entry. Consumed by <c>TextLabel</c> to generate the
    /// heading fonts; the converter itself only looks at <see cref="HeadingFonts"/>.
    /// </summary>
    public float[] HeadingSizeMultipliers { get; init; } = [2f, 1.5f, 1.25f, 1.1f, 1f, 0.9f];

    /// <summary>
    /// Font resource path per heading level (index 0 = H1). Levels past the end reuse the last
    /// entry. A null/empty entry, or an empty array, falls back to a plain [b].
    /// Normally filled in by <c>TextLabel</c> from <see cref="HeadingSizeMultipliers"/>.
    /// </summary>
    public string?[] HeadingFonts { get; init; } = [];

    public string QuoteColor { get; init; } = "#9aa0a6";

    public string BulletGlyph { get; init; } = "•";

    public string RuleGlyph { get; init; } = new string('─', 20);

    /// <summary>
    /// Replacement for a literal '['. "[lb]" only works on Godot versions that support it;
    /// if yours prints it literally, use something like "[\u200B" instead.
    /// </summary>
    public string LeftBracket { get; init; } = "[lb]";

    /// <summary>Replacement for a literal ']'.</summary>
    public string RightBracket { get; init; } = "[rb]";

    /// <summary>
    /// What happens to BBCode typed directly into the Markdown. Code spans and code blocks
    /// are always escaped, and a backslash-escaped bracket ("\[") is always literal.
    /// </summary>
    public BbcodeHandling Bbcode { get; init; } = BbcodeHandling.AllowKnownTags;

    /// <summary>
    /// Tag names that pass through when <see cref="Bbcode"/> is <see cref="BbcodeHandling.AllowKnownTags"/>.
    /// Add your own RichTextEffect keywords here.
    /// </summary>
    public string[] AllowedTags { get; init; } =
    [
        "b",
        "i",
        "u",
        "s",
        "code",
        "center",
        "right",
        "fill",
        "indent",
        "url",
        "img",
        "font",
        "table",
        "cell",
        "color",
        "lb",
        "rb",
        "wave",
        "tornado",
        "shake",
        "fade",
        "rainbow",
    ];
}

public enum BbcodeHandling
{
    /// <summary>Every '[' and ']' in the text is escaped; BBCode in the Markdown shows literally.</summary>
    Escape,

    /// <summary>
    /// Tags listed in <see cref="MdConfig.AllowedTags"/> pass through, everything else is escaped,
    /// so "array[0]" and "[note]" still show literally.
    /// </summary>
    AllowKnownTags,

    /// <summary>Brackets are never escaped. You are responsible for valid BBCode.</summary>
    Passthrough,
}

public static class MdToBbCode
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .Build();

    private static readonly MdConfig DefaultConfig = new();

    private static readonly char[] BracketChars = ['[', ']'];

    public static string Convert(string md, MdConfig? config = null)
    {
        if (string.IsNullOrEmpty(md))
            return string.Empty;

        var document = Markdown.Parse(md, Pipeline);
        return new Renderer(config ?? DefaultConfig).Render(document);
    }

    /// <summary>
    /// Makes arbitrary text safe to embed in BBCode (e.g. placeholder values) by replacing
    /// '[' and ']' with the configured substitutes.
    /// </summary>
    public static string Escape(string text, MdConfig? config = null)
    {
        if (text.IndexOfAny(BracketChars) < 0)
            return text;

        config ??= DefaultConfig;
        var sb = new StringBuilder(text.Length + 8);

        foreach (var c in text)
        {
            switch (c)
            {
                case '[':
                    sb.Append(config.LeftBracket);
                    break;
                case ']':
                    sb.Append(config.RightBracket);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private sealed class Renderer
    {
        private readonly StringBuilder _sb = new();
        private readonly MdConfig _config;

        private readonly Regex? _tagPattern;

        public Renderer(MdConfig config)
        {
            _config = config;

            if (config.Bbcode == BbcodeHandling.AllowKnownTags && config.AllowedTags.Length > 0)
            {
                // Matches "[/b]" and "[b]", "[color=red]", "[wave amp=50]" for allowed names only.
                var names = string.Join("|", Array.ConvertAll(config.AllowedTags, Regex.Escape));
                _tagPattern = new Regex($@"\[(?:/(?:{names})|(?:{names})(?:[= ][^\[\]]*)?)\]");
            }
        }

        public string Render(MarkdownDocument document)
        {
            RenderBlocks(document);
            return _sb.ToString().Trim();
        }

        // Every block ends with a newline, and a blank line is added between siblings.
        private void RenderBlocks(ContainerBlock container)
        {
            var first = true;

            foreach (var block in container)
            {
                if (!first)
                    _sb.Append('\n');

                first = false;
                RenderBlock(block);
            }
        }

        private void RenderBlock(Block block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    RenderHeading(heading);
                    break;

                case ParagraphBlock paragraph:
                    RenderInlines(paragraph.Inline);
                    _sb.Append('\n');
                    break;

                case ListBlock list:
                    RenderList(list);
                    break;

                case QuoteBlock quote:
                    RenderQuote(quote);
                    break;

                case CodeBlock codeBlock: // includes FencedCodeBlock
                    RenderCode(codeBlock);
                    break;

                case ThematicBreakBlock:
                    _sb.Append("[center]").Append(_config.RuleGlyph).Append("[/center]\n");
                    break;

                case ContainerBlock container:
                    RenderBlocks(container);
                    break;

                case LeafBlock leaf:
                    // Anything else (e.g. raw HTML blocks): keep the text instead of dropping it.
                    if (leaf.Inline != null)
                        RenderInlines(leaf.Inline);
                    else
                        _sb.Append(Escape(leaf.Lines.ToString()));

                    _sb.Append('\n');
                    break;
            }
        }

        private void RenderHeading(HeadingBlock heading)
        {
            var fonts = _config.HeadingFonts;
            var font =
                fonts.Length > 0
                    ? fonts[Math.Max(1, Math.Min(heading.Level, fonts.Length)) - 1]
                    : null;

            var hasFont = !string.IsNullOrWhiteSpace(font);

            _sb.Append(hasFont ? $"[font={font}]" : "[b]");
            RenderInlines(heading.Inline);
            _sb.Append(hasFont ? "[/font]" : "[/b]").Append('\n');
        }

        // Nested lists work because [indent] tags nest.
        private void RenderList(ListBlock list)
        {
            var number = 1;

            if (
                list.IsOrdered
                && int.TryParse(
                    list.OrderedStart,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var start
                )
            )
            {
                number = start;
            }

            _sb.Append("[indent]");

            foreach (var child in list)
            {
                if (child is not ListItemBlock item)
                    continue;

                _sb.Append(list.IsOrdered ? $"{number++}. " : $"{_config.BulletGlyph} ");

                foreach (var itemChild in item)
                    RenderBlock(itemChild);
            }

            _sb.Append("[/indent]");
        }

        private void RenderQuote(QuoteBlock quote)
        {
            _sb.Append("[indent][color=").Append(_config.QuoteColor).Append(']');
            RenderBlocks(quote);
            _sb.Append("[/color][/indent]");
        }

        private void RenderCode(CodeBlock code)
        {
            var text = code.Lines.ToString().TrimEnd('\r', '\n');
            _sb.Append("[code]").Append(Escape(text)).Append("[/code]\n");
        }

        private void RenderInlines(ContainerInline? container)
        {
            if (container == null)
                return;

            // Markdig splits "[color=red]" into several literals ("[", "color=red", "]"), so
            // consecutive plain literals are joined before looking for BBCode tags.
            var run = new StringBuilder();

            foreach (var inline in container)
            {
                if (inline is LiteralInline { IsFirstCharacterEscaped: false } literal)
                {
                    run.Append(literal.Content.ToString());
                    continue;
                }

                FlushRun(run);
                RenderInline(inline);
            }

            FlushRun(run);
        }

        private void FlushRun(StringBuilder run)
        {
            if (run.Length == 0)
                return;

            AppendText(run.ToString());
            run.Clear();
        }

        // Plain document text: escaping depends on MdConfig.Bbcode.
        private void AppendText(string text)
        {
            switch (_config.Bbcode)
            {
                case BbcodeHandling.Passthrough:
                    _sb.Append(text);
                    return;

                case BbcodeHandling.AllowKnownTags when _tagPattern != null:
                    var last = 0;

                    foreach (Match match in _tagPattern.Matches(text))
                    {
                        _sb.Append(Escape(text.Substring(last, match.Index - last)))
                            .Append(match.Value);
                        last = match.Index + match.Length;
                    }

                    _sb.Append(Escape(text.Substring(last)));
                    return;

                default:
                    _sb.Append(Escape(text));
                    return;
            }
        }

        private void RenderInline(Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                {
                    // Only backslash-escaped literals ("\[") get here; RenderInlines handles the
                    // rest. Markdig keeps the backslash in Content, so drop it.
                    var text = literal.Content.ToString();

                    if (literal.IsFirstCharacterEscaped && text.Length > 0)
                        text = text.Substring(1);

                    _sb.Append(Escape(text));
                    break;
                }

                case EmphasisInline emphasis:
                    RenderEmphasis(emphasis);
                    break;

                case CodeInline code:
                    _sb.Append("[code]").Append(Escape(code.Content)).Append("[/code]");
                    break;

                case LineBreakInline:
                    _sb.Append('\n');
                    break;

                case HtmlEntityInline entity: // &amp; &lt; ...
                    _sb.Append(Escape(entity.Transcoded.ToString()));
                    break;

                case HtmlInline html:
                    _sb.Append(Escape(html.Tag));
                    break;

                case AutolinkInline auto:
                    _sb.Append("[url=")
                        .Append(SafeUrl(auto.Url))
                        .Append(']')
                        .Append(Escape(auto.Url))
                        .Append("[/url]");
                    break;

                case LinkInline link:
                    RenderLink(link);
                    break;

                case ContainerInline nested:
                    RenderInlines(nested);
                    break;
            }
        }

        private void RenderEmphasis(EmphasisInline emphasis)
        {
            // Needs bold_font / italics_font set on the RichTextLabel to be visible
            // (TextLabelConfig.BoldFontPath / ItalicFontPath).
            string? tag = emphasis.DelimiterChar switch
            {
                '~' when emphasis.DelimiterCount >= 2 => "s",
                '+' when emphasis.DelimiterCount >= 2 => "u",
                '*' or '_' => emphasis.DelimiterCount >= 2 ? "b" : "i",
                _ => null, // sub/superscript, ==marked== etc. have no Godot 3 equivalent
            };

            if (tag != null)
                _sb.Append('[').Append(tag).Append(']');

            RenderInlines(emphasis);

            if (tag != null)
                _sb.Append("[/").Append(tag).Append(']');
        }

        private void RenderLink(LinkInline link)
        {
            // Images can't be loaded from the web by [img]; show the alt text instead.
            if (link.IsImage || string.IsNullOrEmpty(link.Url))
            {
                RenderInlines(link);
                return;
            }

            // Connect RichTextLabel.meta_clicked to actually open these.
            _sb.Append("[url=").Append(SafeUrl(link.Url!)).Append(']');
            RenderInlines(link);
            _sb.Append("[/url]");
        }

        private string Escape(string text) => MdToBbCode.Escape(text, _config);

        private static string SafeUrl(string url) => url.Replace("]", "%5D");
    }
}
