using System.Text.Json;
using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// Composes the index over the field guides — the way in for a reader who arrived without a language in
/// mind.
/// </summary>
/// <remarks>
/// <para>★★ IT PUBLISHES TWO MEDIANS, AND SAYS THEY ARE TWO. This page showed "53.5 · median across
/// languages" — the median of the per-language medians, correctly labelled — while another true reading of
/// the same corpus is 52.6, the median CAI across the codebases themselves. Both are right, they are taken
/// over different populations, and a reader who met one here and the other elsewhere concluded that one of
/// them was a lie. That has already happened. Publishing one and hiding the other does not resolve it
/// either, because the other keeps being computed by anyone who has the data; publishing both, each naming
/// its own population, is the only version a reader can check.</para>
/// <para>★★ EVERY LINK ON THIS PAGE IS A PAGE THAT WAS BUILT. The rows come from
/// <see cref="SurveyCorpus.Fields"/> and each one's link is its own <see cref="LanguageField.GuidePath"/> —
/// the same value <see cref="FieldGuideBuilder"/> refuses to build without.</para>
/// </remarks>
public static class SurveyIndexBuilder
{
    /// <summary>
    /// What the corpus count is a count of.
    /// </summary>
    /// <remarks>
    /// ★ "PUBLISHED", AND IT IS NOT DECORATION. The population is the codebases whose owners chose to
    /// publish them, not every codebase ever measured — those are different numbers, and a reader comparing
    /// this index against anything else needs to know which one it is. Adopted from the producer's own
    /// basis, which said so first and which the phase-6 diff caught this one dropping.
    /// </remarks>
    /// <summary>
    /// How much of the index a survey resolved.
    /// </summary>
    /// <remarks>
    /// ★ THE SAME BASIS THE FIELD GUIDE USES, AND IT WAS MISSED HERE when the others were fixed: the
    /// search that found them was truncated with `head` and the visible subset was taken for the whole. A
    /// corpus whose surveys resolve one dimension published "1 dimensions resolved" on the
    /// highest-traffic page there is.
    /// </remarks>
    private static readonly Basis DepthBasis = Basis.Of("dimension", "dimensions");

    internal static readonly Basis CodebasesBasis = Basis.Of(
        "published measured codebase",
        "published measured codebases");

    /// <summary>
    /// The population the OTHER median is taken over.
    /// </summary>
    /// <remarks>
    /// Deliberately not "languages": the median of the per-language medians is taken over the languages that
    /// HAVE a field guide, which is fewer than the languages the corpus has measured. A reader who cannot
    /// see which median is which is the reason this page publishes both.
    /// </remarks>
    internal static readonly Basis LanguagesBasis = Basis.Of(
        "language with a field guide",
        "languages with a field guide");

    /// <summary>The index over every measured subject, grouped by language.</summary>
    /// <param name="records">One record per measured subject — every language, guide or no guide.</param>
    /// <param name="takenAt">The instant the corpus was read. ★ Not the instant the page was built.</param>
    public static SurveyPage Build(IReadOnlyList<SurveyRecord> records, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(records);

        var fields = SurveyCorpus.Fields(records);
        var codebases = Figure.Count(records.Count, CodebasesBasis, takenAt);

        // ONE VOTE PER LANGUAGE, whatever the size of its field — and only over the languages that have a
        // guide, which is what the basis says out loud. Null when there is none to take it over: the median
        // of nothing is not zero, and Figure refuses to pretend otherwise.
        var withGuides = fields.Where(f => f.GuidePath is not null).ToList();
        var acrossLanguages = withGuides.Count == 0
            ? null
            : Figure.Scalar(
                SurveyCorpus.Median([.. withGuides.Select(f => f.Median)]), withGuides.Count, LanguagesBasis, takenAt);

        // ONE VOTE PER CODEBASE, so the largest fields pull it hardest. Taken over every measured subject,
        // including the ones whose producer named no language — they were measured the same way.
        var acrossCodebases = records.Count == 0
            ? null
            : Figure.Scalar(
                SurveyCorpus.Median(SurveyCorpus.Scores(records)), records.Count, CodebasesBasis, takenAt);

        // The corpus-wide depth is the median of the per-language medians, over the languages that HAVE one:
        // averaging in a zero for a field whose subjects record no depth would report a shallower corpus
        // than was measured.
        var depths = fields.Where(f => f.DepthMedian is not null).Select(f => (double)f.DepthMedian!.Value).ToList();
        var overallDepth = depths.Count == 0 ? (int?)null : SurveyCorpus.MedianDepth([.. depths.Select(d => (int)d)]);

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, "Measured codebases"),
                PageNodes.RichText(
                    $"<p><strong>{PageProse.Escape($"{codebases.Headline()}.")}</strong> "
                    + PageProse.Escape(
                        "Every one was assessed against the Codebase Assurance Index at a pinned commit, "
                        + "under the same rubric and applied the same way, whether or not its source is "
                        + "public. The index is an open standard, so any of these numbers can be re-derived "
                        + "from its survey's evidence. Each language below shows the spread of its "
                        + "codebases' scores rather than one number, next to how much of each codebase the "
                        + "survey actually reached. A score means one thing when the survey resolved a dozen "
                        + "dimensions and another when it resolved two dozen.")
                    + "</p>")),

            // Five figures at most, and the count moves with what can be taken — which is why the band, not
            // this list, decides the rows.
            PageNodes.Band(
                "corpus",
                // ★ THE LABEL ASKS THE BASIS HOW MANY THERE ARE. These two cells put the count in the
                // display slot and the population beneath it, and a population typed as a plural phrase is
                // how an index holding one language published "1" over "languages with a field guide".
                PageNodes.Stat(PageProse.Count(codebases.Population), CodebasesBasis.For(codebases.Population)),
                PageNodes.Stat(PageProse.Count(withGuides.Count), LanguagesBasis.For(withGuides.Count)),
                // ★ EACH MEDIAN'S LABEL IS ITS OWN FIGURE'S BASIS, never a phrase typed beside it: that is
                // what stops the two cells reading as one number and a rounding error.
                acrossLanguages is { } byLanguage
                    ? PageNodes.Stat(
                        PageProse.Score(byLanguage.Value),
                        $"median across {byLanguage.Basis.For(byLanguage.Population)}")
                    : null,
                acrossCodebases is { } byCodebase
                    ? PageNodes.Stat(
                        PageProse.Score(byCodebase.Value),
                        $"median across {byCodebase.Basis.For(byCodebase.Population)}")
                    : null,
                // ★ "TYPICALLY" IS VAGUE ON A PAGE WHOSE ARGUMENT IS THAT A STATISTIC MUST NAME WHAT IT IS.
                // If it is a median, the label says median.
                overallDepth is { } depth
                    ? PageNodes.Stat(
                        PageProse.Count(depth),
                        $"{DepthBasis.For(depth)} resolved, median across the corpus")
                    : null),

            TwoMedians(acrossLanguages, acrossCodebases, codebases),

            // Every language on ONE 0–100 axis, as the DISTRIBUTION of its codebases' scores rather than a
            // single median: sixteen medians in a column is a table nobody compares, and one number per
            // language hides whether a field is tightly clustered or split in two.
            PageNodes.Section(
                null,
                "languages",
                PageNodes.Widget(
                    "cai-language-board",
                    ("heading", "By language"),
                    ("languages", LanguageRows(fields)),
                    ("ungrouped", Ungrouped(fields, records.Count)),
                    ("depth-note", DepthNote(fields)),
                    ("empty-text", "No measured language matches that."))),
        };

        return new SurveyPage(
            SurveyPageBuilder.Root,
            "Measured codebases",
            $"{PageProse.Count(codebases.Population)} codebases measured against the Codebase Assurance "
            + "Index, with their surveys published.",
            PageNodes.Section([.. children]));
    }

    /// <summary>
    /// The note that keeps two true medians from reading as one number and a mistake — or null when the
    /// corpus only has one of them to state.
    /// </summary>
    /// <remarks>
    /// Each reading is rendered by <see cref="Figure.Headline"/>, so it cannot reach the page without the
    /// population it was taken over, and the provenance line dates them both from the one reading instant.
    /// Omitted entirely when either median is missing: with nothing to compare against, a paragraph
    /// explaining a difference would be explaining one that is not on the page.
    /// </remarks>
    private static Dictionary<string, object?>? TwoMedians(Figure? acrossLanguages, Figure? acrossCodebases, Figure codebases)
    {
        if (acrossLanguages is not { } byLanguage || acrossCodebases is not { } byCodebase)
        {
            return null;
        }

        return PageNodes.Section(
            PageNodes.Note,
            "medians",
            PageNodes.Heading(2, "Two medians, and they are not the same statistic"),
            PageNodes.RichText(
                $"<p><strong>{PageProse.Escape(byLanguage.Headline())}</strong> "
                + PageProse.Escape(
                    "is the middle of the per-language medians: every language counts once, whether its "
                    + "field holds four hundred codebases or five.")
                + "</p>"
                + $"<p><strong>{PageProse.Escape(byCodebase.Headline())}</strong> "
                + PageProse.Escape("is the middle of the codebases themselves, so the largest fields pull it hardest.")
                + "</p>"
                + "<p>"
                + PageProse.Escape(
                    "Both are true of the same corpus and they will not agree. A median quoted without the "
                    + "population it was taken over is one a reader cannot check, so each of these carries "
                    + $"its own: {codebases.Provenance()}.")
                + "</p>"));
    }

    /// <summary>One language as four cells: the guide, how many codebases, how their scores are SPREAD, and how deep the survey reached.</summary>
    private static string LanguageRows(IReadOnlyList<LanguageField> fields) =>
        JsonSerializer.Serialize(fields
            .Select(f => new
            {
                name = PageProse.LanguageName(f.Language),
                count = f.Subjects.Count,
                median = Math.Round(f.Median, 1),
                low = Math.Round(f.LowerQuartile, 1),
                high = Math.Round(f.UpperQuartile, 1),
                dist = f.Distribution,
                depth = f.DepthMedian,
                depthOf = f.DepthMeasured,
                // ★ Below the threshold a language still counts toward the corpus and belongs on the board —
                // there is simply no guide to send a reader to, so the row renders WITHOUT a link rather
                // than vanishing and leaving the total short.
                href = f.GuidePath is { } path ? $"/{path}/" : null,
            })
            .ToList());

    /// <summary>
    /// The codebases no language claims, as a figure the board states in its own right.
    /// </summary>
    /// <remarks>
    /// The headline counts every measured codebase; the language rows only count the ones whose producer
    /// named a language. Left implicit, those two numbers simply disagree on the page, and a reader who adds
    /// up the column finds the total short with no explanation.
    /// </remarks>
    private static string? Ungrouped(IReadOnlyList<LanguageField> fields, int total)
    {
        var rest = total - fields.Sum(f => f.Subjects.Count);
        return rest <= 0 ? null : JsonSerializer.Serialize(new { count = rest });
    }

    /// <summary>
    /// What the depth column is, and how much of the board it covers — or null when nothing records it and
    /// the column is not drawn at all.
    /// </summary>
    /// <remarks>
    /// Depth is the one figure on this page a reader has no prior intuition for, and the one most easily
    /// mistaken for a grade. It says how much of the survey resolved, never how good the code is — a shallow
    /// reading of an excellent codebase is a limit of the instrument, not a fault of the project. The
    /// coverage sentence is not optional either: a partial column that does not say so reads as a complete
    /// one.
    /// </remarks>
    private static string? DepthNote(IReadOnlyList<LanguageField> fields)
    {
        var measured = fields.Sum(f => f.DepthMeasured);
        if (measured == 0)
        {
            return null;
        }

        var total = fields.Sum(f => f.Subjects.Count);
        var note = "Depth is how many of the index's dimensions actually resolved on a codebase, taken as "
            + "the median across the language's codebases and read from each one's most recent survey. It "
            + "measures how much of the codebase the survey reached, never how good the code is.";
        if (measured < total)
        {
            note += $" It is recorded for {PageProse.Count(measured)} of {PageProse.Count(total)} codebases "
                + "so far; the rest were last surveyed before per-dimension outcomes were kept, and gain a "
                + "depth the next time they are measured.";
        }

        return note;
    }
}
