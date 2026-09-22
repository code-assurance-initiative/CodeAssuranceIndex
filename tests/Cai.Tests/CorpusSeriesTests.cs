using Cai.Pages;
using Cai.Pages.Publishing;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// §5: the corpus median over time, drawn through the readings that were recorded at the time.
/// </summary>
/// <remarks>
/// <para>★★ ONLY A MEDIAN IS DRAWN. The site ships one chart and its axis is the fixed assurance band
/// scale: a count of codebases plotted on it lands thousands of units above the box, and a share lands
/// inside it and LOOKS right while being inked good or bad in a direction the corpus never claimed — on a
/// vulnerability share, inverted outright. So the corpus's own growth is stated as two dated figures and
/// never as a line.</para>
/// <para>★★ AND A TREND OVER ONE POINT IS NOT A TREND. A flat line through a single point reads as "no
/// change", which is a claim; the truth is "not enough readings", which is not.</para>
/// </remarks>
public sealed class CorpusSeriesTests
{
    [Fact]
    public void A_line_over_one_reading_is_not_a_line()
    {
        Assert.Null(CorpusSeries.Medians([Reading("2026-09-20", 3400, 52.6)]));
    }

    [Fact]
    public void Two_readings_are_the_fewest_that_can_differ()
    {
        Assert.NotNull(CorpusSeries.Medians([Reading("2026-09-19", 3390, 52.5), Reading("2026-09-20", 3400, 52.6)]));
    }

    /// <summary>
    /// ★ A line reads left to right through time, and the store may hand the rows over in any order.
    /// Getting this backwards draws a growing corpus as a shrinking one and a rising median as a fall —
    /// while every figure on the page beside it stays correct, which is what makes it the mistake a reader
    /// cannot catch.
    /// </summary>
    [Fact]
    public void The_line_is_built_oldest_first_whatever_order_the_rows_arrive_in()
    {
        var series = CorpusSeries.Medians(
            [Reading("2026-09-21", 3405, 52.7), Reading("2026-09-19", 3390, 52.5), Reading("2026-09-20", 3400, 52.6)]);

        Assert.NotNull(series);
        Assert.Equal("[52.5,52.6,52.7]", series.Points());
    }

    /// <summary>★ One day is one point, so a re-run cannot put two marks on one date.</summary>
    [Fact]
    public void One_day_is_one_point()
    {
        var series = CorpusSeries.Medians(
            [
                Reading("2026-09-20", 3400, 52.6, hour: 2),
                Reading("2026-09-20", 3402, 52.8, hour: 14),
                Reading("2026-09-21", 3405, 52.9),
            ]);

        Assert.NotNull(series);
        // The LATEST reading of the day is the day's point.
        Assert.Equal("[52.8,52.9]", series.Points());
    }

    /// <summary>
    /// ★★ A READING THAT CANNOT STATE THE QUANTITY IS SKIPPED, NEVER ZEROED. Reading a null as a zero draws
    /// a line rising from nothing at the moment we started looking, and publishes our own release history
    /// as somebody else's improvement.
    /// </summary>
    [Fact]
    public void A_reading_with_no_median_is_not_a_point_at_zero()
    {
        var series = CorpusSeries.Medians(
            [
                // ★ A REAL ROW, NOT AN EMPTY CORPUS. 1,200 codebases were read that day and the median was
                //   not recorded — a reading taken before the column existed. The first draft used a reading
                //   of an EMPTY corpus, which the "no codebases" half of the guard filtered on its own, so
                //   the test passed with the null-skip removed and was asserting nothing about it.
                Reading("2026-09-18", 1200, null),
                Reading("2026-09-19", 3390, 52.5),
                Reading("2026-09-20", 3400, 52.6),
            ]);

        Assert.NotNull(series);
        Assert.Equal("[52.5,52.6]", series.Points());
        Assert.Equal(2, series.Count);
    }

    /// <summary>
    /// ★★ ONE POINT A WEEK, each the newest reading on or before its own date. The corpus is read nightly
    /// and its median sits still for weeks, so a mark per reading put a mark wherever the median happened to
    /// change — and the spacing of the marks then drew the data's volatility rather than the passage of
    /// time. The claim that the line is sampled travels with it.
    /// </summary>
    [Fact]
    public void The_line_is_sampled_to_one_point_a_week()
    {
        // Twenty-two daily readings: three whole weeks and a day.
        var readings = Enumerable.Range(0, 22)
            .Select(i => Reading($"2026-09-{1 + i:00}", 3000 + i, 50 + (i * 0.1)))
            .ToList();

        var weekly = CorpusSeries.Medians(readings)!.Weekly();

        Assert.Equal("weekly", weekly.Sampling);
        // 1 Sep, 8 Sep, 15 Sep, 22 Sep — the last grid point covers the final day.
        Assert.Equal("[50,50.7,51.4,52.1]", weekly.Points());
    }

    /// <summary>★ The chart is handed VALUES, never headlines: a renderer formats, a producer does not.</summary>
    [Fact]
    public void The_points_are_values_not_sentences()
    {
        var series = CorpusSeries.Medians([Reading("2026-09-19", 3390, 52.5), Reading("2026-09-20", 3400, 52.6)]);

        Assert.DoesNotContain("across", series!.Points(), StringComparison.Ordinal);
        Assert.DoesNotContain("codebase", series.Points(), StringComparison.Ordinal);
    }

    /// <summary>★ Both ends stated in words, each carrying the population it was taken over.</summary>
    [Fact]
    public void Both_ends_of_the_line_are_stated_as_figures()
    {
        var series = CorpusSeries.Medians([Reading("2026-09-19", 3390, 52.5), Reading("2026-09-21", 3405, 52.7)])!;

        Assert.Contains("52.5", series.First.Headline(), StringComparison.Ordinal);
        Assert.Contains("3,390", series.First.Headline(), StringComparison.Ordinal);
        Assert.Contains("52.7", series.Last.Headline(), StringComparison.Ordinal);
        Assert.Contains("3,405", series.Last.Headline(), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static DatedReading Reading(string day, int codebases, double? median, int hour = 2) =>
        new(DateTimeOffset.Parse($"{day}T{hour:00}:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            codebases,
            median);
}
