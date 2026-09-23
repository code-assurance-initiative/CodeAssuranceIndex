using System.Globalization;
using System.Text.Json;
using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// Composes a language's field guide — what the codebases measured in one language are like, before which
/// ones there are.
/// </summary>
/// <remarks>
/// <para>★★ IT ANSWERS "WHAT ARE THESE PROJECTS LIKE" FIRST. That is the whole reason a group page beats a
/// list of links: a flat column of several hundred addresses is a directory, and a reader who wants "the
/// largest Go projects that scored Strong" could only get there by reading every row of one.</para>
/// <para>★ ONE BUILDER FOR EVERY PRODUCER, exactly as the portrait is. A field holding subjects measured by
/// two producers is one page with one shape; who measured each subject is content on the subject's own
/// page.</para>
/// </remarks>
public static class FieldGuideBuilder
{
    /// <summary>How much of the index a survey resolved.</summary>
    private static readonly Basis DepthBasis = Basis.Of("dimension", "dimensions");

    /// <summary>The guide for a field, or null when that field has no guide.</summary>
    /// <remarks>
    /// ★ THE NULL COMES FROM <see cref="LanguageField.GuidePath"/> AND NOWHERE ELSE. A second condition
    /// here — even one that agreed today — is how the index came to link pages that were never built.
    /// </remarks>
    public static SurveyPage? Build(LanguageField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (field.GuidePath is not { } path)
        {
            return null;
        }

        var name = PageProse.LanguageName(field.Language);
        var title = $"{name} codebases, measured";
        var ranked = field.Subjects;
        var totalLoc = ranked.Sum(r => (long)r.Latest.Evidence.ProductionLoc);

        var children = new List<object?>
        {
            PageNodes.Section(PageNodes.Heading(1, title), PageNodes.RichText(Lede(field, name))),
            PageNodes.Band(
                "field",
                PageNodes.Stat(PageProse.Count(ranked.Count), "measured codebases"),
                PageNodes.Stat(
                    PageProse.Score(field.Median),
                    $"median CAI · half between {PageProse.Score(field.LowerQuartile)} and "
                    + $"{PageProse.Score(field.UpperQuartile)}"),
                // How deep the surveys of this field reached. Omitted rather than zeroed while no subject
                // here records it — a band of three wraps 2+1 between roughly 560 and 720px, the band lays
                // that out, and the omission stays an omission.
                // ★ THE LABEL ASKS THE BASIS HOW MANY THERE ARE. A survey that resolved a single
                //   dimension is a real, degraded survey, and a hardcoded plural published
                //   "1 dimensions resolved, typically" — which is the defect Basis exists to prevent.
                field.DepthMedian is { } depth
                    ? PageNodes.Stat(
                        PageProse.Count(depth),
                        field.DepthMeasured == ranked.Count
                            ? $"{DepthBasis.For(depth)} resolved, typically"
                            : $"{DepthBasis.For(depth)} resolved, typically · recorded for "
                                + PageProse.Count(field.DepthMeasured))
                    : null,
                totalLoc > 0 ? PageNodes.Stat(PageProse.Compact(totalLoc), "lines in total") : null),

            // The codebases, with the shape of the field drawn over them and a way through them.
            PageNodes.Section(
                null,
                "projects",
                PageNodes.Widget(
                    "cai-survey-list",
                    ("heading", "The codebases"),
                    ("projects", SubjectRows(ranked)),
                    ("empty-text", $"No {name} codebase here matches that."))),
        };

        return new SurveyPage(path, title, Description(name, ranked.Count, field.Median), PageNodes.Section([.. children]));
    }

    /// <summary>
    /// The lede, which characterises the FIELD rather than introducing a list.
    /// </summary>
    /// <remarks>
    /// ★ THE MIDDLE HALF IS STATED WITH THE MEDIAN for the same reason the board draws the whole
    /// distribution: a median alone cannot distinguish a field that clusters around it from one split at
    /// both ends.
    /// </remarks>
    private static string Lede(LanguageField field, string name)
    {
        var count = field.Subjects.Count;
        return $"<p><strong>{PageProse.Escape($"{name} · {PageProse.Count(count)} measured codebases.")}</strong> "
            + PageProse.Escape(
                $"The median score across them is {PageProse.Score(field.Median)}, with half of them "
                + $"between {PageProse.Score(field.LowerQuartile)} and {PageProse.Score(field.UpperQuartile)}."
                + " Each was measured at a pinned commit, and every page below carries the date its score"
                + " was taken.")
            + "</p>";
    }

    /// <summary>
    /// The rows the survey-list island renders: one per subject, as data.
    /// </summary>
    /// <remarks>
    /// <c>day</c> is what a reader sees and <c>at</c> is what the by-date sort uses. They are separate on
    /// purpose: "21 July 2026" sorted as text orders by its leading digit, and a sort control that silently
    /// produces the wrong order is worse than one that is not offered.
    /// </remarks>
    private static string SubjectRows(IReadOnlyList<SurveyRecord> ranked) =>
        JsonSerializer.Serialize(ranked
            .Select(r =>
            {
                var latest = r.Latest;
                var measured = MeasuredAt(latest.Measurement.ScannedAt ?? latest.IssuedAt);
                var slash = latest.Subject.Repository.LastIndexOf('/');
                return new
                {
                    owner = slash > 0 ? latest.Subject.Repository[..slash] : "",
                    name = slash > 0 ? latest.Subject.Repository[(slash + 1)..] : latest.Subject.Repository,
                    score = Math.Round(latest.Verdict.Cai, 1),
                    loc = latest.Evidence.ProductionLoc,
                    day = measured is { } at ? PageProse.Day(at) : null,
                    at = measured?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                    href = SurveyAddress.For(latest.Subject.Host, latest.Subject.Repository) is { } p ? $"/{p}/" : null,
                };
            })
            .Where(row => row.href is not null)
            .ToList());

    private static DateTimeOffset? MeasuredAt(string? instant) =>
        DateTimeOffset.TryParse(instant, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)
            ? when
            : null;

    private static string Description(string name, int count, double median) =>
        $"{PageProse.Count(count)} {name} codebases measured against the Codebase Assurance Index, "
        + $"median score {PageProse.Score(median)}.";
}
