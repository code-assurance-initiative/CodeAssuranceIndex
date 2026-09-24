using Cai.Pages;
using Cai.Pages.Publishing;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A share is published as a whole percent, and rounding never claims none or all.
/// </summary>
/// <remarks>
/// <para>★★ ONE DECIMAL CLAIMED A PRECISION THE MEASUREMENT DOES NOT HAVE. "65.5% carry a
/// known-vulnerable component" reads as though the tenth means something; it is 184 of 281, and the
/// next scan moves it by more than a tenth. Owner's ruling, 2026-09-24: publish whole percents.</para>
///
/// <para>★★ AND THE TWO RAILS THAT MAKE THAT HONEST. Rounding to an integer is where a share starts
/// lying at the ends: three of seven hundred is 0.43%, and "0%" says NOBODY when the answer is three.
/// Six hundred and ninety-nine of seven hundred is 99.86%, and "100%" says EVERY ONE when one is
/// missing. Those are the two statements this index must never make by accident, so they are the two
/// the renderer refuses to make: <c>&lt;1%</c> and <c>&gt;99%</c>. A true zero and a true whole still
/// print as 0% and 100%, because then they are the answer rather than a rounding of it.</para>
/// </remarks>
public sealed class SharesArePublishedAsWholePercentTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 24, 18, 7, 0, TimeSpan.Zero);

    private static readonly Basis Surveys = Basis.Of("survey", "surveys");

    [Fact]
    public void An_ordinary_share_is_a_whole_percent()
    {
        Assert.StartsWith("65%", Figure.Ratio(184, 281, Surveys, At).Headline(), StringComparison.Ordinal);
    }

    /// <summary>★★ Three of seven hundred is not nobody.</summary>
    [Fact]
    public void A_share_too_small_to_round_up_is_not_published_as_none()
    {
        var headline = Figure.Ratio(3, 700, Surveys, At).Headline();

        Assert.StartsWith("<1%", headline, StringComparison.Ordinal);
        Assert.DoesNotContain("0% of", headline, StringComparison.Ordinal);
    }

    /// <summary>★★ And one missing out of seven hundred is not every one.</summary>
    [Fact]
    public void A_share_just_short_of_all_is_not_published_as_all()
    {
        var headline = Figure.Ratio(699, 700, Surveys, At).Headline();

        Assert.StartsWith(">99%", headline, StringComparison.Ordinal);
        Assert.DoesNotContain("100%", headline, StringComparison.Ordinal);
    }

    /// <summary>★ A true zero and a true whole are answers, not roundings.</summary>
    [Fact]
    public void Nobody_and_everybody_are_stated_plainly()
    {
        Assert.StartsWith("0%", Figure.Ratio(0, 700, Surveys, At).Headline(), StringComparison.Ordinal);
        Assert.StartsWith("100%", Figure.Ratio(700, 700, Surveys, At).Headline(), StringComparison.Ordinal);
    }

    /// <summary>★ The stat-cell split carries the same percent as the headline it supports.</summary>
    [Fact]
    public void The_split_and_the_headline_agree()
    {
        var figure = Figure.Ratio(184, 281, Surveys, At);

        Assert.Equal("65%", figure.Split().Lead);
        Assert.StartsWith("65%", figure.Headline(), StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE MASTHEAD SHOWS THE DAY, THE CITATION NOTE KEEPS THE INSTANT. The sheet is republished
    /// hourly, so a timestamp where a reader meets it changed all day and told them nothing; the reading
    /// it describes is a day's reading. But `DayAndTime` exists because the day alone cannot tell two
    /// readings apart — the surveys index once published 4,065, 4,075 and 4,078 over one morning, each
    /// stamped with the same day. So the minute stays exactly where it does that work: the append-only
    /// note at the foot that says this is a citable observation.
    /// </summary>
    [Fact]
    public void The_sheet_shows_the_day_and_keeps_the_instant_where_it_is_cited()
    {
        var json = PageText.Json(CorpusSheetBuilder.Build(
            [], new DateTimeOffset(2026, 9, 24, 18, 7, 0, TimeSpan.Zero), []));

        Assert.Contains("24 September 2026. Each figure below", json, StringComparison.Ordinal);
        Assert.Contains("measured 24 September 2026, 18:07 UTC", json, StringComparison.Ordinal);
    }

    /// <summary>★★ A SCORE IS NOT A SHARE and keeps its decimal — 78.2 is a reading on a 0–100 scale.</summary>
    [Fact]
    public void A_score_still_carries_its_decimal()
    {
        Assert.StartsWith("78.2", Figure.Score(78.2, 40, Surveys, At).Headline(), StringComparison.Ordinal);
    }
}
