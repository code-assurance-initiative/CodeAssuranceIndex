namespace Cai.Pages.Publishing;

/// <summary>
/// A <see cref="Figure"/> as a reader meets it in a stat cell: the reading in the display slot, and the
/// population it was taken over in the small line beneath. Obtained only from <see cref="Figure.Split"/>, and
/// only ever as BOTH halves at once.
/// </summary>
/// <remarks>
/// <para>★★ THE DEFECT THIS EXISTS FOR. Every stat on every corpus page was built as
/// <c>Stat(figure.Headline(), label)</c>, and the display slot is an <c>&lt;h2&gt;</c> — so the whole sentence,
/// "76.8% of surveys whose dependencies a scanner could resolve (1,161 of 1,511)", was set in heading type.
/// Four lines of it, in every cell, on every page. The pages carried the right information and read as a wall
/// of text: "these pages should give an overview, not an headache".</para>
///
/// <para>★★ WHY IT IS ONE OBJECT AND NOT TWO METHODS. The obvious fix is a <c>Value()</c> returning
/// <c>"76.8%"</c> — and that is precisely the naked number <see cref="Figure"/> was built to make unwritable.
/// A share whose denominator has been left behind still reads perfectly well, which is why the defect is an
/// ABSENCE and why the reflection sweeps in <c>Kennel.SharedKernel.Tests</c> assert that no member of
/// <see cref="Figure"/> hands out a string that fails to name its population. Returning the two halves as one
/// value keeps that guarantee intact rather than exempting anything from it: there IS no member that yields the
/// lead alone, so a caller who wants the big number is handed the small line with it, in the same expression,
/// and <c>PageNodes.Stat</c> is the one place that puts each half in its slot.</para>
///
/// <para>★ A FLOOR KEEPS ITS HEDGE IN THE LEAD, not in the support. "76.8%" without its denominator is an
/// imprecise claim; "240" without "at least" is a DIFFERENT claim — a censored count printed as a measurement.
/// The hedge is part of the reading, the way the percent sign is part of a share, so it travels in
/// <see cref="Lead"/> and the size of the blind spot travels in <see cref="Support"/>. Neither half of that can
/// be dropped, because neither half can be obtained on its own.</para>
/// </remarks>
public sealed record FigureSplit
{
    /// <summary>Built by <see cref="Figure.Split"/> and by nothing else, so the two halves always agree.</summary>
    internal FigureSplit(string lead, string support)
    {
        Lead = lead;
        Support = support;
    }

    /// <summary>
    /// The reading for the display slot, and nothing else in it: <c>76.8%</c>, <c>3,542</c>, <c>49.5</c>,
    /// <c>at least 240</c>. Short enough to be read at a glance, which is the whole point of a stat cell.
    /// </summary>
    public string Lead { get; }

    /// <summary>
    /// The population, for the small line beneath: <c>1,161 of 1,511 surveys whose dependencies a scanner
    /// could resolve</c>, <c>across 3,464 surveyed repositories</c>, <c>surveys carrying this advisory — a
    /// further 1,204 could not be checked</c>. Always names the basis; for a floor it also states how much of
    /// the population nobody could look at.
    /// </summary>
    public string Support { get; }
}
