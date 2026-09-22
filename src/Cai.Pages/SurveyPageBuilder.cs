using System.Globalization;
using Cai.Delivery;

namespace Cai.Pages;

/// <summary>
/// Composes the page the standard publishes about a measured subject.
/// </summary>
/// <remarks>
/// <para>★★ ONE BUILDER FOR EVERY PRODUCER. That is the whole reason it lives here: whoever measured the
/// code, the page about it has the same address, the same structure and the same words, and differs only
/// in the numbers and in who is credited with taking them. Until this existed a producer sent a NODE TREE
/// and the CMS only styled it, so a second producer would have published a structurally different page
/// under one standard — and the shared theme would have made them look like one site while they were
/// never one page.</para>
/// <para>★★ NO WIDGET ON THIS PAGE IS GIVEN AN API BASE. Every island is handed its numbers as props. A
/// widget that CAN fetch will fetch, and what it fetches is whichever card the gallery hands back — the
/// hero — which is how another project's figures came to be printed under a thousand repositories' names.
/// Carried over from the producer's implementation together with the reason, because the reason is the
/// part that stops it happening again.</para>
/// </remarks>
public static class SurveyPageBuilder
{
    /// <summary>The first path segment every survey page lives under.</summary>
    /// <remarks>
    /// NOT <c>registry</c>: the registry stores and distributes signed deliveries. These pages are the
    /// public catalogue of subjects that have been measured and whose owners chose to publish.
    /// </remarks>
    public const string Root = "surveys";

    /// <summary>The page for one measured subject, or null when the subject cannot be addressed.</summary>
    public static SurveyPage? Build(SurveyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var latest = record.Latest;
        var subject = latest.Subject;
        if (SurveyAddress.For(subject.Host, subject.Repository) is not { } path)
        {
            return null;
        }

        var verdict = latest.Verdict;
        var title = subject.Repository;
        var sections = new List<object?>
        {
            PageNodes.Section(PageNodes.Heading(1, title)),

            // The numbers, in the theme's strongest treatment. A BAND rather than a raw grid: how many
            // cells fit one row is the SITE's decision and moves with the viewport, so handing a grid a
            // list publishes a filled slab at some widths.
            PageNodes.Band("measurement", [.. Stats(record)]),

            // Where the score sits on the fixed scale.
            PageNodes.Section(PageNodes.Widget(
                "cai-band-scale",
                ("score", Number(verdict.Cai)),
                ("heading", $"Where {Number(verdict.Cai)} sits on the scale"),
                ("caption", $"{verdict.Band} — the most recent published measurement, taken {Day(latest.IssuedAt)}."))),
        };

        // How the score moved, as a line — derived from the SEQUENCE this record holds, never supplied.
        // Two producers each computing "the climb" their own way would put two different trend lines
        // under one standard.
        if (record.Deliveries.Count > 1)
        {
            var series = string.Join(",", record.Deliveries.Select(d => Number(d.Verdict.Cai)));
            sections.Add(PageNodes.Section(null, "trend", PageNodes.Widget(
                "cai-trend",
                ("heading", "How the score moved"),
                ("series", series),
                ("first-date", Day(record.Deliveries[0].IssuedAt)),
                ("last-date", Day(latest.IssuedAt)),
                ("caption", TrendCaption(record)))));
        }

        // What was measured, lens by lens. The gauges ARE the lens table, not a second copy of it, and
        // the numbers travel as an attribute so a reader who never runs the script still finds them.
        if (latest.Verdict.Lenses.Count > 0)
        {
            var gauges = string.Join(
                ";",
                verdict.Lenses.Select(l => $"{l.Lens}|{Number(l.Score)}|{l.Band}"));
            sections.Add(PageNodes.Section(null, "lenses", PageNodes.Widget(
                "cai-lens-gauges",
                ("heading", "What was measured, lens by lens"),
                ("lenses", gauges))));
        }

        sections.Add(About(record));

        return new SurveyPage(path, title, MetaDescription(record), PageNodes.Section([.. sections]));
    }

    /// <summary>The figures the band states — only the ones this record can actually support.</summary>
    /// <remarks>
    /// ★ A FIGURE THE RECORD CANNOT SUPPORT IS NOT PRINTED AS A ZERO. An absent production-line count is
    /// "not measured", and a zero in its place reads as a measurement of an empty repository.
    /// </remarks>
    private static IEnumerable<object?> Stats(SurveyRecord record)
    {
        var latest = record.Latest;
        yield return PageNodes.Stat(
            Number(latest.Verdict.Cai),
            $"{latest.Verdict.Band} · {Day(latest.IssuedAt)}");

        if (latest.Evidence.ProductionLoc > 0)
        {
            yield return PageNodes.Stat(Compact(latest.Evidence.ProductionLoc), "lines of production code");
        }

        yield return PageNodes.Stat(
            record.Deliveries.Count.ToString(CultureInfo.InvariantCulture),
            "measurements over time");
    }

    /// <summary>What the run recorded, and who recorded it.</summary>
    /// <remarks>
    /// ★★ THE PRODUCER IS NAMED HERE, and this is the ONLY place a page differs between producers.
    /// Neutral does not mean anonymous: a reader is entitled to know who took the measurement, and the
    /// standard's claim is that the METHOD was the same, not that the measurer was.
    /// </remarks>
    private static object About(SurveyRecord record)
    {
        var latest = record.Latest;
        var lines = new List<string>
        {
            $"The score is its most recent published measurement, taken on {Day(latest.IssuedAt)} at a "
            + "pinned commit. It is not a live figure and does not change until the project is measured again.",
        };

        if (latest.Subject.Commit is { Length: > 0 } commit)
        {
            lines.Add($"Measured at commit {PageProse.Escape(commit)} — the exact code this score is about.");
        }

        lines.Add(
            $"Scored under {PageProse.Escape(latest.RubricVersion)} — the same rubric and the same method as "
            + "every other entry in this index.");

        lines.Add($"Measured by {PageProse.Escape(Attribution(latest.Producer))}.");

        return PageNodes.Section("note", null,
            PageNodes.Heading(2, "About this page"),
            PageNodes.RichText("<ul>" + string.Join("", lines.Select(l => $"<li>{l}</li>")) + "</ul>"));
    }

    /// <summary>Who measured it, and with what — the producer's own words for its scanner.</summary>
    private static string Attribution(DeliveryProducer producer) =>
        producer.Scanner is { Length: > 0 } scanner
            ? (producer.ScannerVersion is { Length: > 0 } version
                ? $"{producer.Name} using {scanner} {version}"
                : $"{producer.Name} using {scanner}")
            : producer.Name;

    private static string MetaDescription(SurveyRecord record) =>
        $"{record.Latest.Subject.Repository} scored {Number(record.Latest.Verdict.Cai)} on the Codebase "
        + $"Assurance Index — {record.Latest.Verdict.Band}, measured at a pinned commit under "
        + $"{record.Latest.RubricVersion}, with the survey published in full.";

    private static string TrendCaption(SurveyRecord record)
    {
        var first = record.Deliveries[0];
        var last = record.Latest;
        var delta = last.Verdict.Cai - first.Verdict.Cai;
        var direction = delta >= 0 ? "up" : "down";
        return $"{record.Deliveries.Count} measurements, from {Day(first.IssuedAt)} to {Day(last.IssuedAt)}: "
             + $"{Number(first.Verdict.Cai)} to {Number(last.Verdict.Cai)} — {direction} "
             + $"{Number(Math.Abs(delta))}. Every published measurement of this repository, oldest first.";
    }

    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Compact(int value) =>
        value >= 1000 ? $"{(value / 1000d).ToString("0.#", CultureInfo.InvariantCulture)}k"
                      : value.ToString(CultureInfo.InvariantCulture);

    /// <summary>An RFC 3339 instant as the day a reader sees, or the raw string when it will not parse.</summary>
    private static string Day(string issuedAt) =>
        DateTimeOffset.TryParse(issuedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)
            ? when.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)
            : issuedAt;
}
