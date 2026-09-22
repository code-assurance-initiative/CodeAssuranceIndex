using System.Text.Json;
using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>One recorded reading of the corpus, as a page needs it.</summary>
/// <remarks>
/// ★ THE PAGE NEVER SEES A DATABASE ROW. This is the shape the trend is drawn through; where the readings
/// are kept is the registry's business, and a page builder that knew would be a page builder that could
/// recompute one.
/// </remarks>
/// <param name="TakenAt">The instant the corpus was read.</param>
/// <param name="Codebases">Published measured codebases at that instant.</param>
/// <param name="MedianCai">The median CAI across them, or null when there were none. ★ Null is not zero.</param>
public sealed record DatedReading(DateTimeOffset TakenAt, int Codebases, double? MedianCai);

/// <summary>
/// A quantity read across the dated readings — the line, and both of its ends in words.
/// </summary>
/// <remarks>
/// <para>★★ ONLY A MEDIAN IS DRAWN. The site ships one chart and its axis is the fixed assurance band
/// scale a CAI is defined on. A series of counts plotted on it lands thousands of units above the box; a
/// series of SHARES is worse, because it lands inside and looks correct — and then a vulnerability share
/// rising through 76 is inked healthy green while a disclosure-policy share at 24 is inked critical red.
/// The band vocabulary is a judgement about assurance, and borrowing it colours a share good or bad in a
/// direction the corpus never claimed. On the vulnerability share it is inverted outright. So a count or a
/// share has its movement STATED as the two dated figures it consists of, and the page says why there is
/// no line.</para>
///
/// <para>★★ THE POINTS ARE THE RECORDED READINGS, IN THE ROWS' OWN ORDER. History is never recomputed:
/// deriving a line from today's corpus would draw a line that never happened, and no reader could tell the
/// difference.</para>
///
/// <para>★★ AND A TREND OVER ONE POINT IS NOT A TREND. <see cref="MinimumReadings"/> is two — the fewest
/// observations that can differ. It is not a statistical threshold and does not pretend to be one: a
/// two-point line is a slope with no shape, and the figures beside it say how many readings it is drawn
/// through so a reader can weigh it. What it stops is the shape that says something false — a flat mark
/// under a heading promising to show what happened.</para>
/// </remarks>
public sealed record CorpusSeries
{
    /// <summary>The fewest dated readings a line may be drawn through.</summary>
    public const int MinimumReadings = 2;

    private const int DaysInAWeek = 7;

    private readonly IReadOnlyList<Figure> _points;

    private CorpusSeries(IReadOnlyList<Figure> points, string? sampling = null)
    {
        _points = points;
        Sampling = sampling;
    }

    /// <summary>How the line was sampled — <c>weekly</c>, or null for every point as recorded.</summary>
    /// <remarks>
    /// ★ SET BY <see cref="Weekly"/> AND BY NOTHING ELSE, because the island states the contract to a
    /// reader as fact: one point per week, each the newest reading on or before its own date.
    /// </remarks>
    public string? Sampling { get; }

    /// <summary>How many points the line is drawn through.</summary>
    public int Count => _points.Count;

    /// <summary>The oldest point, with the population it was taken over.</summary>
    public Figure First => _points[0];

    /// <summary>The newest point, with the population it was taken over.</summary>
    public Figure Last => _points[^1];

    /// <summary>
    /// The corpus median across the dated readings — or null when too few of them can state one.
    /// </summary>
    /// <param name="readings">Every recorded reading, in any order.</param>
    /// <remarks>
    /// <para>★ BUILT OLDEST FIRST WHATEVER ORDER THE ROWS ARRIVE IN. A line reads left to right through
    /// time; getting that backwards draws a growing corpus as a shrinking one and a rising median as a
    /// fall, while every figure on the page beside it stays correct — which is what makes it the mistake a
    /// reader cannot catch.</para>
    /// <para>★ ONE DAY IS ONE POINT, reduced to the latest reading of that day, so a re-run cannot put two
    /// marks on one date.</para>
    /// <para>★★ A READING THAT CANNOT STATE THE QUANTITY IS SKIPPED, NEVER ZEROED. Reading a null as a zero
    /// draws a line rising from nothing at the moment we started looking, and publishes our own release
    /// history as somebody else's improvement. The skipped days simply are not points, and
    /// <see cref="Count"/> says how many there are.</para>
    /// </remarks>
    public static CorpusSeries? Medians(IReadOnlyList<DatedReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var points = readings
            .GroupBy(r => r.TakenAt.UtcDateTime.Date)
            .Select(g => g.MaxBy(r => r.TakenAt)!)
            .OrderBy(r => r.TakenAt)
            .Select(Median)
            .OfType<Figure>()
            .ToList();

        return points.Count < MinimumReadings ? null : new CorpusSeries(points);
    }

    /// <summary>
    /// The line sampled to one point a week, each the newest reading on or before its own date.
    /// </summary>
    /// <remarks>
    /// ★★ THE PRODUCER OF THE LINE IS THE ONLY THING THAT CAN SAMPLE IT. The island is handed values on an
    /// index-based axis and the dates live here. Left to itself it collapsed runs of identical medians, so
    /// a mark sat wherever the median CHANGED and the spacing of the marks was a picture of the data's
    /// volatility rather than of the passage of time — "very unevenly distributed", and no reader could
    /// decode them. The corpus is read nightly and its median sits still for weeks, which is exactly the
    /// shape that produces.
    /// </remarks>
    public CorpusSeries Weekly()
    {
        var sampled = new List<Figure>();
        var last = _points[^1].TakenAt.UtcDateTime.Date;
        var next = 0;
        Figure? newest = null;

        for (var week = _points[0].TakenAt.UtcDateTime.Date; ; week = week.AddDays(DaysInAWeek))
        {
            while (next < _points.Count && _points[next].TakenAt.UtcDateTime.Date <= week)
            {
                newest = _points[next++];
            }

            // ★ Never null: the grid starts ON the oldest reading's own day, so the first pass through this
            //   loop has already taken it. The bang says that rather than hoping for it.
            sampled.Add(newest!);

            if (week >= last)
            {
                break;
            }
        }

        return new CorpusSeries(sampled, "weekly");
    }

    /// <summary>
    /// The points as the island's JSON, oldest first.
    /// </summary>
    /// <remarks>
    /// ★ SERIALISED, NOT FORMATTED. A producer never chooses how a figure is rendered, which keeps the
    /// chart's numbers identical to the ones in the sentence above it. It is the figure's VALUE, not its
    /// headline: a ratio's value is a fraction, so a share series would arrive at the island a hundred
    /// times too small — one more reason only a scalar is ever drawn.
    /// </remarks>
    public string Points() => JsonSerializer.Serialize(_points.Select(p => p.Value));

    /// <summary>What a median is taken over.</summary>
    internal static readonly Basis CodebasesBasis = Basis.Of(
        "published measured codebase",
        "published measured codebases");

    /// <summary>
    /// The size of the corpus across the dated readings — the population the median was taken over.
    /// </summary>
    /// <remarks>
    /// ★★ STATED, NEVER DRAWN. A count plotted on an axis defined by the assurance band cutlines lands
    /// thousands of units above the box, its end label reads a codebase count to one decimal place, and its
    /// end dot is inked as though the corpus had scored Exemplary. This series exists so the corpus's own
    /// growth can be said in words beside the line, with both ends dated.
    /// </remarks>
    public static CorpusSeries? Sizes(IReadOnlyList<DatedReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var points = readings
            .GroupBy(r => r.TakenAt.UtcDateTime.Date)
            .Select(g => g.MaxBy(r => r.TakenAt)!)
            .OrderBy(r => r.TakenAt)
            .Where(r => r.MedianCai is not null && r.Codebases > 0)
            .Select(r => Figure.Count(r.Codebases, CodebasesBasis, r.TakenAt))
            .ToList();

        return points.Count < MinimumReadings ? null : new CorpusSeries(points);
    }

    private static Figure? Median(DatedReading reading) =>
        reading.MedianCai is { } median && reading.Codebases > 0
            ? Figure.Scalar(median, reading.Codebases, CodebasesBasis, reading.TakenAt)
            : null;
}
