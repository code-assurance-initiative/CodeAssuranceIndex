using Cai.Pages;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The small writing rules every published page shares — how a language, a count, a day and a sentence are
/// written for a reader.
/// </summary>
public sealed class PageProseTests
{
    [Theory]
    [InlineData("csharp", "C#")]
    [InlineData("fsharp", "F#")]
    [InlineData("vbnet", "VB.NET")]
    [InlineData("cpp", "C++")]
    [InlineData("objectivec", "Objective-C")]
    [InlineData("ocaml", "OCaml")]
    [InlineData("gdscript", "GDScript")]
    [InlineData("vhdl", "VHDL")]
    [InlineData("javascript", "JavaScript")]
    [InlineData("typescript", "TypeScript")]
    [InlineData("php", "PHP")]
    [InlineData("sql", "SQL")]
    [InlineData("html", "HTML")]
    [InlineData("css", "CSS")]
    public void A_known_language_is_written_the_way_its_users_write_it(string id, string written) =>
        Assert.Equal(written, PageProse.LanguageName(id));

    [Theory]
    [InlineData("  CSharp ", "C#")] // the id is matched trimmed and case-insensitively
    [InlineData("TYPESCRIPT", "TypeScript")]
    public void A_known_language_is_recognised_however_it_arrives(string id, string written) =>
        Assert.Equal(written, PageProse.LanguageName(id));

    /// <summary>★ A language this list has not met yet is still a real language: capitalised, never dropped.</summary>
    [Theory]
    [InlineData("python", "Python")]
    [InlineData("rust", "Rust")]
    [InlineData("zig", "Zig")]
    public void An_unlisted_language_passes_through_capitalised(string id, string written) =>
        Assert.Equal(written, PageProse.LanguageName(id));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_language_is_said_to_be_unknown(string? id) =>
        Assert.Equal("Unknown", PageProse.LanguageName(id));

    [Theory]
    [InlineData(999, "999")]
    [InlineData(1_000, "1k")]
    [InlineData(2_150, "2.2k")]
    [InlineData(10_000, "10k")]
    [InlineData(173_400, "173k")]
    [InlineData(2_100_000, "2.1M")]
    [InlineData(3_000_000_000, "3B")]
    public void A_large_count_is_compact_at_a_glance(long value, string written) =>
        Assert.Equal(written, PageProse.Compact(value));

    [Fact]
    public void A_day_month_and_instant_read_the_same_in_every_locale()
    {
        var at = new DateTimeOffset(2026, 9, 15, 2, 33, 0, TimeSpan.FromHours(2)); // 00:33 UTC

        Assert.Equal("15 September 2026", PageProse.Day(at));
        Assert.Equal("15 September 2026, 00:33 UTC", PageProse.DayAndTime(at));
        Assert.Equal("September 2026", PageProse.Month(at));
    }

    [Fact]
    public void Text_is_escaped_so_it_can_never_read_as_markup()
    {
        Assert.Equal("a &amp; b &lt;i&gt;", PageProse.Escape("a & b <i>"));
        Assert.Equal("<p>x &lt; y</p>", PageProse.Paragraph("  x < y  "));
        Assert.Equal(string.Empty, PageProse.Paragraph("   "));
        Assert.Equal("<a href=\"/a?b=1&amp;c=2\">A &amp; B</a>", PageProse.Link("/a?b=1&c=2", "A & B"));
    }

    [Fact]
    public void A_list_leaves_out_blank_items_and_says_nothing_when_all_are_blank()
    {
        Assert.Equal("<ul><li>one</li><li>two</li></ul>", PageProse.List(["one", " ", "two"]));
        Assert.Equal(string.Empty, PageProse.List(["", "  "]));
    }

    [Fact]
    public void The_first_sentence_is_cut_on_a_word_and_says_it_was_cut()
    {
        Assert.Equal("First one.", PageProse.FirstSentence("First one. Second one."));
        Assert.Equal(string.Empty, PageProse.FirstSentence(null));
        Assert.Equal("alpha beta…", PageProse.FirstSentence("alpha beta gamma", maxLength: 12));
    }
}
