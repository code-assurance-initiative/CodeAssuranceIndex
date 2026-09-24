using System.Text.Json;
using Cai.Pages;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The producer's prose, turned into the standard's own nodes.
/// </summary>
/// <remarks>
/// <para>★★ THE CANONICAL SUBSET REFUSES, IT DOES NOT REWRITE. The CMS validates every rich-text value
/// against an exact grammar — <c>p</c>, <c>ul</c>/<c>ol</c>/<c>li</c>, and inside them only text, the five
/// named entities, <c>br</c>, <c>strong</c>, <c>em</c> and <c>a[href]</c> — and rejects anything else
/// rather than cleaning it. A markdown document handed over raw is a page that fails to publish, which on
/// this site means the survey keeps serving its previous bake and nobody is told why.</para>
///
/// <para>★★ AND THE PROSE IS NOT OURS. It is written by a model, carried in a signed delivery, and about a
/// repository whose name and contents are attacker-controlled as far as this code is concerned. Every run
/// of text is escaped BEFORE any markup is composed around it, so the only tags that can appear are the
/// ones this converter chose to emit.</para>
/// </remarks>
public sealed class ProseFromMarkdownTests
{
    private static string Html(object? node) =>
        ((Dictionary<string, object?>)node!)["html"]!.ToString()!;

    private static string Type(object? node) =>
        ((Dictionary<string, object?>)node!)["type"]!.ToString()!;

    [Fact]
    public void Paragraphs_separated_by_a_blank_line_become_separate_paragraphs()
    {
        var nodes = ProseFromMarkdown.Nodes("First thing.\n\nSecond thing.");

        Assert.Single(nodes);
        Assert.Equal("<p>First thing.</p><p>Second thing.</p>", Html(nodes[0]));
    }

    [Fact]
    public void A_wrapped_paragraph_is_one_paragraph()
    {
        var nodes = ProseFromMarkdown.Nodes("A sentence that was\nwrapped by the writer.");

        Assert.Equal("<p>A sentence that was wrapped by the writer.</p>", Html(nodes[0]));
    }

    /// <summary>★ A heading becomes a HEADING NODE, not bold text: the site sets the standard's own heading
    /// scale, and a paragraph pretending to be one stops moving when the theme does.</summary>
    [Fact]
    public void A_markdown_heading_becomes_a_heading_node()
    {
        var nodes = ProseFromMarkdown.Nodes("## What this is\n\nA payments API.");

        Assert.Equal(2, nodes.Count);
        Assert.Equal("heading", Type(nodes[0]));
        Assert.Equal("What this is", ((Dictionary<string, object?>)nodes[0]!)["text"]);
        // ★ Level 3: the section it lands in already carries the page's h2, and a prose heading that
        //   outranks its own section reads as a new section to a screen reader walking the outline.
        Assert.Equal(3, ((Dictionary<string, object?>)nodes[0]!)["level"]);
        Assert.Equal("<p>A payments API.</p>", Html(nodes[1]));
    }

    [Fact]
    public void Bullets_become_a_list()
    {
        var nodes = ProseFromMarkdown.Nodes("- One\n- Two\n* Three");

        Assert.Equal("<ul><li>One</li><li>Two</li><li>Three</li></ul>", Html(nodes[0]));
    }

    [Fact]
    public void Numbered_items_become_an_ordered_list()
    {
        var nodes = ProseFromMarkdown.Nodes("1. First\n2. Second");

        Assert.Equal("<ol><li>First</li><li>Second</li></ol>", Html(nodes[0]));
    }

    [Fact]
    public void Emphasis_and_links_survive_as_the_inline_subset()
    {
        var nodes = ProseFromMarkdown.Nodes(
            "It is **important** and *notable*, see [the rubric](https://codeassuranceindex.info/rubric/).");

        Assert.Equal(
            "<p>It is <strong>important</strong> and <em>notable</em>, see "
            + "<a href=\"https://codeassuranceindex.info/rubric/\">the rubric</a>.</p>",
            Html(nodes[0]));
    }

    /// <summary>
    /// ★★ THE HREF IS AN ALLOWLIST, NOT A FILTER. The grammar admits https, http and mailto; a
    /// <c>javascript:</c> or <c>data:</c> href would be REJECTED by the CMS and take the whole page down
    /// with it — and if it were ever accepted it would be a script in a page the standard signs its name to.
    /// A link it will not emit degrades to its own text, which still reads.
    /// </summary>
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("file:///etc/passwd")]
    public void A_link_the_grammar_would_refuse_degrades_to_its_text(string href)
    {
        var nodes = ProseFromMarkdown.Nodes($"Look [here]({href}) now.");

        Assert.Equal("<p>Look here now.</p>", Html(nodes[0]));
    }

    /// <summary>★★ ESCAPED BEFORE ANYTHING IS COMPOSED AROUND IT. The five entities are the only ones the
    /// grammar knows, and a bare <c>&amp;</c> ends a text run just as a <c>&lt;</c> does.</summary>
    [Fact]
    public void Markup_in_the_prose_is_text()
    {
        var nodes = ProseFromMarkdown.Nodes("<script>alert(1)</script> & <b>bold</b>");

        Assert.Equal(
            "<p>&lt;script&gt;alert(1)&lt;/script&gt; &amp; &lt;b&gt;bold&lt;/b&gt;</p>",
            Html(nodes[0]));
    }

    [Fact]
    public void Markup_inside_a_heading_is_text_too()
    {
        var nodes = ProseFromMarkdown.Nodes("# <img src=x onerror=alert(1)>");

        Assert.Equal("heading", Type(nodes[0]));
        Assert.Equal("<img src=x onerror=alert(1)>", ((Dictionary<string, object?>)nodes[0]!)["text"]);
    }

    /// <summary>★ The publication notice the redactor adds is a blockquote of one bold line. It exists to be
    /// READ — a reader who cannot tell a redacted changelog from a thin one concludes the analysis is thin.</summary>
    [Fact]
    public void A_blockquote_keeps_its_words()
    {
        var nodes = ProseFromMarkdown.Nodes("> **Details are withheld.**\n\nAnd then a row.");

        Assert.Equal(
            "<p><strong>Details are withheld.</strong></p><p>And then a row.</p>", Html(nodes[0]));
    }

    [Fact]
    public void A_thematic_break_and_a_fence_leave_no_stray_characters()
    {
        var nodes = ProseFromMarkdown.Nodes("Before.\n\n---\n\n```csharp\nvar x = 1;\n```\n\nAfter.");

        var html = Html(nodes[0]);
        Assert.DoesNotContain("---", html, StringComparison.Ordinal);
        Assert.DoesNotContain("```", html, StringComparison.Ordinal);
        Assert.Contains("var x = 1;", html, StringComparison.Ordinal);
        Assert.Contains("<p>Before.</p>", html, StringComparison.Ordinal);
        Assert.Contains("<p>After.</p>", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ ONE NODE HAS A CEILING AND THE DOCUMENT DOES NOT. The canonical value is capped at 20,000
    /// characters; a system overview written from a large codebase's whole history goes past that, and a
    /// single node would be refused — losing the WHOLE section, not the overflow. So blocks fill a node
    /// until the next one would not fit, and then a new node starts.
    /// </summary>
    [Fact]
    public void A_document_past_the_node_ceiling_is_split_across_nodes()
    {
        var paragraph = new string('a', 900);
        var markdown = string.Join("\n\n", Enumerable.Repeat(paragraph, 40));

        var nodes = ProseFromMarkdown.Nodes(markdown);

        Assert.True(nodes.Count > 1, "36,000 characters cannot travel in one 20,000-character node");
        foreach (var node in nodes)
        {
            Assert.True(
                Html(node).Length <= ProseFromMarkdown.NodeCeiling,
                $"a node carried {Html(node).Length} characters");
        }

        // ★ AND NOTHING IS DROPPED ON THE WAY. A splitter that silently lost a block would read exactly
        //   like a shorter document.
        var all = string.Concat(nodes.Select(Html));
        Assert.Equal(40, all.Split("<p>").Length - 1);
    }

    /// <summary>★ A single block longer than the ceiling cannot be made to fit by splitting BETWEEN blocks,
    /// so it is broken at a word.</summary>
    [Fact]
    public void One_oversized_block_is_broken_rather_than_dropped()
    {
        var markdown = string.Join(' ', Enumerable.Repeat("word", 8000));

        var nodes = ProseFromMarkdown.Nodes(markdown);

        Assert.True(nodes.Count > 1);
        foreach (var node in nodes)
        {
            Assert.True(Html(node).Length <= ProseFromMarkdown.NodeCeiling);
        }

        Assert.Equal(8000, string.Concat(nodes.Select(Html)).Split("word").Length - 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\n  ")]
    public void Nothing_to_say_produces_no_nodes(string? markdown) =>
        Assert.Empty(ProseFromMarkdown.Nodes(markdown));

    /// <summary>★ Whatever comes out must be serialisable as the page spec it claims to be — the same shape
    /// every other node takes.</summary>
    [Fact]
    public void Every_node_serialises_as_a_page_spec()
    {
        var json = JsonSerializer.Serialize(ProseFromMarkdown.Nodes("# Title\n\nBody **here**."));

        Assert.Contains("\"type\":\"heading\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"richtext\"", json, StringComparison.Ordinal);
    }
}
