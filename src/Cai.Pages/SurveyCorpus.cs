
namespace Cai.Pages;

/// <summary>
/// The measured corpus, cut the one way the standard's pages cut it: by the language its subjects are
/// written in.
/// </summary>
/// <remarks>
/// <para>★★ THE FIELDS ARE COMPUTED ONCE AND BOTH PAGES READ THE SAME ONES. The index over the guides and
/// the guides themselves are one grouping seen twice, never two groupings that happen to agree. When they
/// were two, the index linked every language that merely slugified and a language with four subjects got
/// a link to a page that was never built — a 404 reached from the corpus's own front door.</para>
/// <para>★ A SUBJECT WHOSE PRODUCER NAMED NO LANGUAGE IS NOT A FIELD CALLED NOTHING. It is counted in the
/// corpus and grouped under nothing, and the index says how many those are rather than letting the column
/// quietly come up short.</para>
/// </remarks>
public static class SurveyCorpus
{
    /// <summary>The path segment every field guide lives under: <c>surveys/lang/{language}</c>.</summary>
    public const string LanguageSegment = "lang";

    /// <summary>How many measured subjects a language needs before it gets a guide of its own.</summary>
    /// <remarks>
    /// ★ Below this a "field" is one or two projects, and a page describing it would be reading a trend
    /// into a coincidence. Carried over from the producer's implementation with its reason, because the
    /// reason is the part that stops somebody lowering it to fill the index out.
    /// </remarks>
    public const int MinimumSubjectsForGuide = 5;

    /// <summary>How many ten-point bins the score axis is cut into for the distribution strip.</summary>
    private const int DistributionBins = 10;

    /// <summary>
    /// The address of a language's field guide, or null when it does not have one.
    /// </summary>
    /// <remarks>
    /// ★★ THE ONE FUNCTION. Both halves of the decision live here — enough subjects, AND a language that
    /// reduces to a path segment — so no caller can satisfy one and assume the other. A null answer means
    /// "there is no page", and every consumer means the same thing by it.
    /// </remarks>
    public static string? GuidePathFor(string? language, int subjectCount) =>
        subjectCount >= MinimumSubjectsForGuide && SurveyAddress.Segment(language) is { } slug
            ? $"{SurveyPageBuilder.Root}/{LanguageSegment}/{slug}"
            : null;

    /// <summary>
    /// Every language the corpus has measured, largest field first.
    /// </summary>
    /// <param name="records">One record per measured subject.</param>
    /// <returns>The fields, each carrying the shape of its own distribution and whether it has a guide.</returns>
    public static IReadOnlyList<LanguageField> Fields(IEnumerable<SurveyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return [.. records
            .Select(r => (Record: r, Language: Primary(r)))
            .Where(x => x.Language is not null)
            .GroupBy(x => x.Language!, StringComparer.Ordinal)
            .Select(g => LanguageField.Of(g.Key, [.. g.Select(x => x.Record)]))
            .OrderByDescending(f => f.Subjects.Count)
            .ThenBy(f => f.Language, StringComparer.Ordinal)];
    }

    /// <summary>The language a subject's most recent delivery names, normalised — or null when none does.</summary>
    private static string? Primary(SurveyRecord record)
    {
        var primary = record.Latest.Subject.Languages?.Primary;
        return string.IsNullOrWhiteSpace(primary) ? null : primary.Trim().ToLowerInvariant();
    }

    /// <summary>The score every subject in a set was last measured at.</summary>
    internal static List<double> Scores(IEnumerable<SurveyRecord> records) =>
        [.. records.Select(r => r.Latest.Verdict.Cai)];

    /// <summary>The middle of a set of readings.</summary>
    internal static double Median(List<double> values) => Quantile(values, 0.5);

    /// <summary>
    /// The <paramref name="p"/>-quantile of <paramref name="values"/>, by linear interpolation between the
    /// two neighbouring order statistics.
    /// </summary>
    /// <remarks>
    /// One method for the median and the quartiles, so the three numbers on a row cannot be computed by two
    /// different conventions and disagree at the edges. Sorts in place — the callers own their lists.
    /// </remarks>
    internal static double Quantile(List<double> values, double p)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        if (values.Count == 1)
        {
            return values[0];
        }

        var position = Math.Clamp(p, 0, 1) * (values.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper ? values[lower] : values[lower] + ((values[upper] - values[lower]) * (position - lower));
    }

    /// <summary>
    /// A set of scores as counts per ten-point bin on the fixed 0–100 axis.
    /// </summary>
    /// <remarks>
    /// Counts, not percentages: the renderer scales them, and a reader who wants the shape of a field gets
    /// the real one — including a field that is split in two rather than clustered around its median.
    /// </remarks>
    internal static IReadOnlyList<int> Distribution(IEnumerable<double> scores)
    {
        var bins = new int[DistributionBins];
        foreach (var score in scores)
        {
            var index = Math.Clamp((int)(score / (100.0 / DistributionBins)), 0, DistributionBins - 1);
            bins[index]++;
        }

        return bins;
    }

    /// <summary>
    /// The median depth of the subjects that HAVE one, as a whole number of dimensions.
    /// </summary>
    /// <remarks>
    /// Rounded away from zero rather than truncated: with an even number of subjects the median can land on
    /// a half, and "12.5 dimensions" is not a thing that can resolve. A dimension count is a count.
    /// </remarks>
    internal static int MedianDepth(IEnumerable<int> depths) =>
        (int)Math.Round(Median([.. depths.Select(d => (double)d)]), MidpointRounding.AwayFromZero);
}

/// <summary>
/// One language's field: every subject measured in it, and the shape of their scores.
/// </summary>
/// <remarks>
/// ★ THE SPREAD TRAVELS WITH THE MEDIAN, always. A median alone cannot distinguish a field clustered
/// around it from one split at both ends, and two languages with the same median and different spreads are
/// different fields.
/// </remarks>
public sealed record LanguageField
{
    private LanguageField(
        string language,
        IReadOnlyList<SurveyRecord> subjects,
        string? guidePath,
        double median,
        double lowerQuartile,
        double upperQuartile,
        IReadOnlyList<int> distribution,
        int? depthMedian,
        int depthMeasured)
    {
        Language = language;
        Subjects = subjects;
        GuidePath = guidePath;
        Median = median;
        LowerQuartile = lowerQuartile;
        UpperQuartile = upperQuartile;
        Distribution = distribution;
        DepthMedian = depthMedian;
        DepthMeasured = depthMeasured;
    }

    /// <summary>The language identifier, normalised — <c>csharp</c>, never <c>C#</c>.</summary>
    public string Language { get; }

    /// <summary>Every subject measured in it, highest score first.</summary>
    public IReadOnlyList<SurveyRecord> Subjects { get; }

    /// <summary>Where this field's guide lives, or null when it has none. ★ The single decision.</summary>
    public string? GuidePath { get; }

    /// <summary>The middle score of the field.</summary>
    public double Median { get; }

    /// <summary>The bottom of the middle half.</summary>
    public double LowerQuartile { get; }

    /// <summary>The top of the middle half.</summary>
    public double UpperQuartile { get; }

    /// <summary>The field's scores as counts per ten-point bin on the 0–100 axis.</summary>
    public IReadOnlyList<int> Distribution { get; }

    /// <summary>
    /// How many of the index's dimensions typically resolved, or null when no subject here records it.
    /// </summary>
    /// <remarks>
    /// ★ NULL RATHER THAN ZERO. A survey that did not record its depth is not a survey that reached
    /// nothing, and a zero in this cell reads as the instrument having failed on the whole field.
    /// </remarks>
    public int? DepthMedian { get; }

    /// <summary>How many subjects the depth reading was taken over.</summary>
    public int DepthMeasured { get; }

    /// <summary>Builds the field for a language from the subjects measured in it.</summary>
    internal static LanguageField Of(string language, IReadOnlyList<SurveyRecord> subjects)
    {
        var ranked = subjects
            .OrderByDescending(r => r.Latest.Verdict.Cai)
            .ThenBy(r => r.Latest.Subject.Repository, StringComparer.Ordinal)
            .ToList();

        var scores = SurveyCorpus.Scores(ranked);
        var depths = ranked.Select(Depth).OfType<int>().ToList();

        return new LanguageField(
            language,
            ranked,
            SurveyCorpus.GuidePathFor(language, ranked.Count),
            SurveyCorpus.Median([.. scores]),
            SurveyCorpus.Quantile([.. scores], 0.25),
            SurveyCorpus.Quantile([.. scores], 0.75),
            SurveyCorpus.Distribution(scores),
            depths.Count == 0 ? null : SurveyCorpus.MedianDepth(depths),
            depths.Count);
    }

    /// <summary>How many dimensions the subject's most recent survey resolved, or null when it records none.</summary>
    private static int? Depth(SurveyRecord record)
    {
        var count = record.Latest.Evidence.Dimensions.Count;
        return count == 0 ? null : count;
    }
}
