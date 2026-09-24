using System.Text;
using System.Text.RegularExpressions;

namespace Cai.Pages;

/// <summary>
/// The producer's prose, turned into the standard's own page nodes.
/// </summary>
/// <remarks>
/// <para>★★ THE CANONICAL SUBSET REFUSES, IT DOES NOT REWRITE. The CMS validates every rich-text value
/// against an exact grammar — <c>p</c>, <c>ul</c>/<c>ol</c> with <c>li</c>, and inside them only text, the
/// five named entities, <c>br</c>, <c>strong</c>, <c>em</c> and <c>a[href]</c> with an https/http/mailto
/// scheme — and REJECTS anything outside it rather than cleaning it up. Markdown handed over raw is a page
/// that fails to publish, which on this site means the survey keeps serving its previous bake and no reader
/// is told why. So this converts, and emits nothing it has not composed itself.</para>
///
/// <para>★★ AND THE PROSE IS NOT OURS. It is written by a model, carried in a signed delivery, and about a
/// repository whose name and contents are attacker-controlled as far as this code is concerned. Every run of
/// text is ESCAPED FIRST and markup composed around it afterwards, so the only tags that can reach a page
/// are the ones named here. The order matters: escaping after composition would escape our own tags, and
/// anything that looked like a fix for that would be a parser differential.</para>
///
/// <para>★ IT IS DELIBERATELY SMALL. Tables, images, footnotes and nested lists have no home in the grammar
/// and none in a survey's two documents; they degrade to their own text, which still reads, rather than
/// pulling in a markdown library whose output would then need validating against the same grammar anyway.</para>
/// </remarks>
internal static partial class ProseFromMarkdown
{
    /// <summary>The most characters one rich-text value may carry — the CMS's own ceiling.</summary>
    /// <remarks>
    /// ★★ A NODE OVER THIS IS REFUSED, AND THE REFUSAL COSTS THE WHOLE SECTION rather than the overflow. A
    /// system overview written from a large codebase's history goes past it, so blocks fill a node until the
    /// next one would not fit and then a new node starts. Held a little under the CMS's 20,000 so a counting
    /// difference between the two implementations cannot be the thing that drops a page.
    /// </remarks>
    public const int NodeCeiling = 19_000;

    /// <summary>The prose as page nodes — headings as headings, everything else as rich text.</summary>
    public static IReadOnlyList<object?> Nodes(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var nodes = new List<object?>();
        var pending = new StringBuilder();

        void Flush()
        {
            if (pending.Length > 0)
            {
                nodes.Add(PageNodes.RichText(pending.ToString()));
                pending.Clear();
            }
        }

        void Add(string block)
        {
            // ★ ONE BLOCK PER NODE IS THE FALLBACK, NOT THE RULE: a paragraph belongs in the same node as
            //   the paragraph before it, or the page becomes a stack of one-line islands.
            if (pending.Length > 0 && pending.Length + block.Length > NodeCeiling)
            {
                Flush();
            }

            foreach (var piece in Fit(block))
            {
                if (pending.Length > 0 && pending.Length + piece.Length > NodeCeiling)
                {
                    Flush();
                }

                pending.Append(piece);
            }
        }

        foreach (var block in Blocks(markdown.ReplaceLineEndings("\n")))
        {
            if (block.Heading is { } heading)
            {
                Flush();
                nodes.Add(PageNodes.Heading(heading.Level, heading.Text));
                continue;
            }

            Add(block.Html!);
        }

        Flush();
        return nodes;
    }

    /// <summary>One heading, or one composed block of canonical HTML.</summary>
    private readonly record struct Block((int Level, string Text)? Heading, string? Html);

    /// <summary>
    /// Walk the document a line at a time, gathering runs of like lines into blocks.
    /// </summary>
    /// <remarks>
    /// ★ A LINE BREAK INSIDE A PARAGRAPH IS NOT A PARAGRAPH BREAK. Writers wrap; markdown says a blank line
    /// separates paragraphs and a single newline is a space. Treating every newline as a break turned one
    /// sentence into six paragraphs on the first document this was pointed at.
    /// </remarks>
    private static IEnumerable<Block> Blocks(string markdown)
    {
        var lines = markdown.Split('\n');
        var paragraph = new List<string>();
        var items = new List<string>();
        var ordered = false;
        var fenced = false;

        Block? DrainParagraph()
        {
            if (paragraph.Count == 0)
            {
                return null;
            }

            var text = string.Join(' ', paragraph);
            paragraph.Clear();
            return new Block(null, $"<p>{Inline(text)}</p>");
        }

        Block? DrainList()
        {
            if (items.Count == 0)
            {
                return null;
            }

            var tag = ordered ? "ol" : "ul";
            var body = string.Concat(items.Select(i => $"<li>{Inline(i)}</li>"));
            items.Clear();
            return new Block(null, $"<{tag}>{body}</{tag}>");
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            if (FenceLine().IsMatch(line.TrimStart()))
            {
                // ★ The fence markers go and the code stays. A survey's prose quotes a signature or a
                //   command now and then, and dropping the block would lose the point of the paragraph
                //   above it; there is no `code` in the grammar, so it reads as text.
                fenced = !fenced;
                continue;
            }

            if (fenced)
            {
                paragraph.Add(line);
                continue;
            }

            if (line.Trim().Length == 0)
            {
                if (DrainParagraph() is { } p) { yield return p; }
                if (DrainList() is { } l) { yield return l; }
                continue;
            }

            if (ThematicBreak().IsMatch(line.Trim()))
            {
                if (DrainParagraph() is { } p) { yield return p; }
                if (DrainList() is { } l) { yield return l; }
                continue;
            }

            if (HeadingLine().Match(line.TrimStart()) is { Success: true } heading)
            {
                if (DrainParagraph() is { } p) { yield return p; }
                if (DrainList() is { } l) { yield return l; }

                // ★ LEVEL 3, WHATEVER THE DOCUMENT SAYS. The section this lands in already carries the
                //   page's own h2; a prose heading that outranks it reads as a new top-level section to
                //   anyone walking the outline, and the outline is how a screen reader navigates.
                var hashes = heading.Groups["hashes"].Value.Length;
                yield return new Block((hashes <= 2 ? 3 : 4, heading.Groups["text"].Value.Trim()), null);
                continue;
            }

            if (BulletLine().Match(line) is { Success: true } bullet)
            {
                if (DrainParagraph() is { } p) { yield return p; }
                if (ordered && items.Count > 0 && DrainList() is { } switched) { yield return switched; }
                ordered = false;
                items.Add(bullet.Groups["text"].Value);
                continue;
            }

            if (NumberedLine().Match(line) is { Success: true } numbered)
            {
                if (DrainParagraph() is { } p) { yield return p; }
                if (!ordered && items.Count > 0 && DrainList() is { } switched) { yield return switched; }
                ordered = true;
                items.Add(numbered.Groups["text"].Value);
                continue;
            }

            if (items.Count > 0)
            {
                // A continuation line under a list item belongs to that item, not to a new paragraph.
                items[^1] = $"{items[^1]} {line.Trim()}";
                continue;
            }

            // ★ A blockquote keeps its words. The redactor's publication notice IS a blockquote, and it
            //   exists to be read: a reader who cannot tell a redacted changelog from a thin one concludes
            //   the analysis is thin.
            paragraph.Add(QuoteLine().Replace(line.TrimStart(), string.Empty).Trim());
        }

        if (DrainParagraph() is { } lastParagraph) { yield return lastParagraph; }
        if (DrainList() is { } lastList) { yield return lastList; }
    }

    /// <summary>
    /// One block, cut into pieces no larger than the ceiling.
    /// </summary>
    /// <remarks>
    /// ★ A SINGLE BLOCK CAN EXCEED IT ON ITS OWN, and splitting between blocks cannot help. Broken at a
    /// space so a word is never cut in half, and the tags are re-closed around each piece so every piece is
    /// independently valid — a half-open paragraph would be refused exactly like an oversized one.
    /// </remarks>
    private static IEnumerable<string> Fit(string block)
    {
        if (block.Length <= NodeCeiling)
        {
            yield return block;
            yield break;
        }

        // Only paragraphs reach this length in practice; a list is cut between its items by the same rule
        // applied to the whole element, which is why the open/close tags are read off the block itself.
        var open = block.StartsWith("<ul>", StringComparison.Ordinal) ? "<ul>"
            : block.StartsWith("<ol>", StringComparison.Ordinal) ? "<ol>"
            : "<p>";
        var close = $"</{open[1..^1]}>";
        var inner = block[open.Length..^close.Length];
        var room = NodeCeiling - open.Length - close.Length;

        var at = 0;
        while (at < inner.Length)
        {
            var take = Math.Min(room, inner.Length - at);
            if (at + take < inner.Length)
            {
                var space = inner.LastIndexOf(' ', at + take - 1, take);
                if (space > at)
                {
                    take = space - at;
                }
            }

            yield return $"{open}{inner.Substring(at, take).Trim()}{close}";
            at += take;
            while (at < inner.Length && inner[at] == ' ')
            {
                at++;
            }
        }
    }

    /// <summary>
    /// One run of text as canonical inline HTML: escaped first, then emphasis, code spans and links.
    /// </summary>
    /// <remarks>
    /// ★★ THE HREF IS AN ALLOWLIST. The grammar admits https, http and mailto; anything else — a
    /// <c>javascript:</c> or a <c>data:</c> URL — is REFUSED by the CMS and would take the whole page down,
    /// and were it ever accepted it would be a script in a page the standard signs its name to. A link this
    /// will not emit degrades to its own text, which still reads.
    /// </remarks>
    private static string Inline(string text)
    {
        var escaped = PageProse.Escape(text);

        // Code spans first: their content must not then be read as emphasis. There is no `code` in the
        // grammar, so the backticks simply go.
        escaped = CodeSpan().Replace(escaped, m => m.Groups["text"].Value);

        escaped = LinkSpan().Replace(escaped, m =>
        {
            var href = m.Groups["href"].Value.Trim();
            var label = m.Groups["text"].Value;
            return AllowedHref(href) ? $"<a href=\"{href}\">{label}</a>" : label;
        });

        escaped = StrongSpan().Replace(escaped, m => $"<strong>{m.Groups["text"].Value}</strong>");
        escaped = EmphasisSpan().Replace(escaped, m => $"<em>{m.Groups["text"].Value}</em>");
        return escaped;
    }

    /// <summary>The schemes the canonical grammar admits. A relative link is admitted too — the pages this
    /// prose lands in are served from one site, and <c>page:</c> ids are not ours to mint.</summary>
    private static bool AllowedHref(string href) =>
        href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<text>.+)$")]
    private static partial Regex HeadingLine();

    [GeneratedRegex(@"^\s*[-*+]\s+(?<text>.+)$")]
    private static partial Regex BulletLine();

    [GeneratedRegex(@"^\s*\d+[.)]\s+(?<text>.+)$")]
    private static partial Regex NumberedLine();

    [GeneratedRegex(@"^\s*(?:[-*_]\s*){3,}$")]
    private static partial Regex ThematicBreak();

    [GeneratedRegex(@"^(?:```|~~~)")]
    private static partial Regex FenceLine();

    // ★ The RAW line, not the escaped one: the marker is stripped while walking the document and the
    //   escaping happens afterwards, in Inline. Written against `&gt;` first, it matched nothing and every
    //   notice reached the page with its marker still on it.
    [GeneratedRegex(@"^>\s?")]
    private static partial Regex QuoteLine();

    [GeneratedRegex(@"`(?<text>[^`]+)`")]
    private static partial Regex CodeSpan();

    // ★ THE HREF MAY CONTAIN PARENTHESES, and a pattern that stopped at the first one read
    //   `javascript:alert(1)` as the href `javascript:alert(1` — refused, correctly — and then left the
    //   stray `)` in the sentence. One level of nesting is what a URL takes in practice.
    [GeneratedRegex(@"\[(?<text>[^\]]*)\]\((?<href>[^()\s]*(?:\([^()]*\)[^()\s]*)*)\)")]
    private static partial Regex LinkSpan();

    [GeneratedRegex(@"(?<!\w)(?:\*\*|__)(?<text>\S(?:[^*_]*\S)?)(?:\*\*|__)(?!\w)")]
    private static partial Regex StrongSpan();

    [GeneratedRegex(@"(?<!\w)(?:\*|_)(?<text>\S(?:[^*_]*\S)?)(?:\*|_)(?!\w)")]
    private static partial Regex EmphasisSpan();
}
