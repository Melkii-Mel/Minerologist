// AI-generated

using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

public static class MdToBbCode
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .Build();

    public static string Convert(string md)
    {
        if (string.IsNullOrEmpty(md))
            return string.Empty;

        var document = Markdown.Parse(md, Pipeline);
        var output = new StringBuilder();

        foreach (var block in document)
            RenderBlock(block, output);

        return output.ToString().Trim();
    }

    private static void RenderBlock(Block block, StringBuilder output)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var size = heading.Level switch
                {
                    1 => 24,
                    2 => 20,
                    _ => 16,
                };

                output.Append("[b][size=").Append(size).Append("]");

                RenderInlines(heading.Inline, output);

                output.AppendLine("[/size][/b]");
                break;

            case ParagraphBlock paragraph:
                RenderInlines(paragraph.Inline, output);
                output.AppendLine();
                break;

            case ListBlock list:
                RenderList(list, output);
                break;

            case QuoteBlock quote:
                output.AppendLine("[quote]");

                foreach (var child in quote)
                    RenderBlock(child, output);

                output.AppendLine("[/quote]");
                break;

            case FencedCodeBlock code:
                RenderCode(code, output);
                break;

            case CodeBlock code:
                RenderCode(code, output);
                break;

            case ThematicBreakBlock:
                output.AppendLine("[hr]");
                break;
        }
    }

    private static void RenderList(ListBlock list, StringBuilder output)
    {
        output.AppendLine("[list]");

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
                continue;

            output.Append("[*]");

            foreach (var child in listItem)
                RenderBlock(child, output);

            output.AppendLine();
        }

        output.AppendLine("[/list]");
    }

    private static void RenderCode(CodeBlock code, StringBuilder output)
    {
        output.Append("[code]\n");
        output.Append(code.Lines.ToString());
        output.Append("\n[/code]\n");
    }

    private static void RenderInlines(ContainerInline? container, StringBuilder output)
    {
        if (container == null)
        {
            return;
        }

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    output.Append(literal.Content.ToString());
                    break;

                case EmphasisInline emphasis:
                    var tag = emphasis.DelimiterChar switch
                    {
                        '~' => "s",
                        '+' => "u",
                        '_' when emphasis.DelimiterCount == 2 => "b",
                        '*' when emphasis.DelimiterCount == 2 => "b",
                        '_' => "i",
                        _ => "i",
                    };

                    output.Append('[').Append(tag).Append(']');

                    RenderInlines(emphasis, output);

                    output.Append("[/").Append(tag).Append(']');
                    break;

                case CodeInline code:
                    output.Append("[code]").Append(code.Content).Append("[/code]");
                    break;

                case LineBreakInline:
                    output.Append('\n');
                    break;

                case ContainerInline nested:
                    RenderInlines(nested, output);
                    break;
            }
        }
    }
}
