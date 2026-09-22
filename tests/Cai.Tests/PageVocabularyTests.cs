using System.Text.Json;
using Cai.Pages;
using Cai.Pages.Publishing;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The authoring vocabulary the standard composes its pages from, held to the node specs the producer's
/// copy emitted before it moved.
/// </summary>
/// <remarks>
/// <para>★★ THE GOLDEN WAS CAPTURED FROM THE OTHER IMPLEMENTATION WHILE IT WAS STILL THE ONLY ONE, for the
/// same reason as the pixel baseline: once the vocabulary lives here there is nothing left to compare
/// against, and "it still produces the same specs" becomes a claim rather than a check. It is byte-for-byte
/// what <c>Kennel.Watchdog.Syndication.PageNodes</c> emitted on 2026-09-22, and every difference is a
/// change to what the CMS receives — i.e. a change to every published page.</para>
/// <para>★ THESE ARE SPECS, NOT MARKUP. A section carries an APPEARANCE the theme styles; nothing here
/// names a colour, a width or a tag. That is what keeps the standard from becoming a second renderer.</para>
/// <para>Phase 3 of <c>docs/plans/cai-owns-its-pages.md</c>.</para>
/// </remarks>
public sealed class PageVocabularyTests
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    private static Dictionary<string, object?> Specimens() => new(StringComparer.Ordinal)
    {
        ["section"] = PageNodes.Section(PageNodes.Heading(2, "Measured codebases")),
        ["section-appearance-anchor"] = PageNodes.Section("stat-band", "masthead", PageNodes.Heading(3, "By language")),
        ["grid"] = PageNodes.Grid(220, PageNodes.Stack(PageNodes.Heading(4, "One"))),
        ["stack"] = PageNodes.Stack(PageNodes.Heading(4, "Cell")),
        ["heading"] = PageNodes.Heading(2, "Two medians"),
        ["richtext"] = PageNodes.RichText("<p>A paragraph with <strong>weight</strong>.</p>"),
        ["eyebrow"] = PageNodes.Eyebrow("§2 Findings"),
        ["stat"] = PageNodes.Stat("6,276", "published measured codebases"),
        ["widget"] = PageNodes.Widget("cai-score-card", ("api-base", "https://app.watchdog.canine.dev"), ("owner", "acme")),
        ["button"] = PageNodes.Button("Read the survey", "/surveys/github/acme/api/"),
        ["marker"] = PageNodes.Marker("55.4", "median across published measured codebases", "The middle of the codebases themselves."),
    };

    [Fact]
    public void The_vocabulary_emits_exactly_what_the_producers_copy_emitted()
    {
        var golden = Path.Combine(AppContext.BaseDirectory, "Goldens", "page-vocabulary.json");
        Assert.True(File.Exists(golden), $"The golden is missing from {golden}; it is the only record of what moved.");

        Assert.Equal(
            File.ReadAllText(golden).ReplaceLineEndings("\n"),
            JsonSerializer.Serialize(Specimens(), Pretty).ReplaceLineEndings("\n"));
    }

    /// <summary>
    /// ★ A SPEC NAMES NO MARKUP. The moment one does, the standard has started rendering — and a page
    /// stops picking up the site's theme changes for free.
    /// </summary>
    [Fact]
    public void No_specimen_carries_markup_a_colour_or_a_pixel_width()
    {
        var json = JsonSerializer.Serialize(Specimens());

        Assert.DoesNotContain("<div", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#", json, StringComparison.Ordinal);      // no hex colour
        Assert.DoesNotContain("class=", json, StringComparison.OrdinalIgnoreCase);
    }
}
