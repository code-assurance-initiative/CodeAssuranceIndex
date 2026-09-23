using System.Globalization;
using System.Text.Json;
using Cai.Delivery;
using Cai.Scoring;

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
                ("caption", $"{verdict.Band} — the most recent published measurement, taken {MeasuredOn(latest)}."))),
        };

        // How the score moved, as a line — derived from the SEQUENCE this record holds, never supplied.
        // Two producers each computing "the climb" their own way would put two different trend lines
        // under one standard.
        if (record.Deliveries.Count > 1)
        {
            // ★★ JSON, NOT A JOINED STRING. The island parses this prop as an array; handed
            //    "61.2,68.1,72.4" it renders a heading and nothing under it — a section that looks
            //    like a design decision rather than a broken one. Four kinds of test passed over
            //    that: the node diff compares TAGS and sections, the island contract compares prop
            //    NAMES, and neither reads a value. A rendered screenshot caught it in one look.
            var series = JsonSerializer.Serialize(record.Deliveries.Select(d => Math.Round(d.Verdict.Cai, 1)));
            sections.Add(PageNodes.Section(null, "trend", PageNodes.Widget(
                "cai-trend",
                ("heading", "How the score moved"),
                ("series", series),
                ("first-date", MeasuredOn(record.Deliveries[0])),
                ("last-date", MeasuredOn(latest)),
                ("caption", TrendCaption(record)))));
        }

        // What was measured, lens by lens. The gauges ARE the lens table, not a second copy of it, and
        // the numbers travel as an attribute so a reader who never runs the script still finds them.
        if (latest.Verdict.Lenses.Count > 0)
        {
            // ★★ THE SAME DEFECT, AND THE SAME LESSON: the island reads {label, value, note}
            //    objects, and a pipe-and-semicolon string renders as nothing at all.
            // ★ The label is the STANDARD's own name for the lens (LensCatalog), not the key and not
            //   the producer's wording — a lens's name is the standard's to give.
            var gauges = JsonSerializer.Serialize(verdict.Lenses.Select(l => new
            {
                label = LensName(l.Lens),
                value = Math.Round(l.Score, 1),
                note = l.Band,
            }));
            sections.Add(PageNodes.Section(null, "lenses", PageNodes.Widget(
                "cai-lens-gauges",
                ("heading", "What was measured, lens by lens"),
                ("lenses", gauges),
                ("footnote", DarkLensNote(verdict)))));
        }

        // ★ AFTER THE MEASUREMENT AND BEFORE THE PROVENANCE. A reader who got this far and liked what
        //   they read has nowhere to go; an invitation that INTERRUPTS the evidence is an advert, and
        //   this page's whole claim is that it is evidence first.
        sections.Add(PageNodes.Section(null, "survey-your-own",
            PageNodes.Heading(2, "Survey your own repository"),
            PageNodes.RichText(PageProse.Paragraph(
                $"{PageProse.Escape(title)} was measured the same way every project in this corpus was: the "
                + "same rubric, at a pinned commit, with the result published in full. Point a surveyor at a "
                + "repository you know and see whether you agree with it."))));

        sections.Add(About(record));

        // And where to go from here, as cards with a mark on each. No producer chooses these: they are
        // the standard's own routes out of its own page.
        sections.Add(PageNodes.Section(null, "elsewhere",
            PageNodes.Widget("cai-link-cards", ("links", Destinations(record)))));

        return new SurveyPage(path, title, MetaDescription(record), PageNodes.Section([.. sections]));
    }

    /// <summary>
    /// The model-aware lenses this survey did not light up, said in the page's own words.
    /// </summary>
    /// <remarks>
    /// ★★ A LENS THAT DID NOT APPLY IS NOT A ZERO, AND NOT A GAP EITHER. A reader who sees five lenses
    /// on one page and six on another is owed the difference between "this codebase's architecture does
    /// not call for it" and "the survey could not read it". Scoring an inapplicable lens zero would mark
    /// a CRUD service down for having no aggregates, which is the measurement saying something false
    /// about the code rather than about itself.
    /// </remarks>
    private static string? DarkLensNote(DeliveryVerdict verdict)
    {
        var lit = verdict.Lenses.Select(l => l.Lens).ToHashSet(StringComparer.Ordinal);
        var dark = LensCatalog.All
            .Where(l => !l.Core && !lit.Contains(l.Key))
            .Select(l => l.DisplayName)
            .ToList();

        if (dark.Count == 0)
        {
            return null;
        }

        var names = dark.Count == 1
            ? $"The {dark[0]} lens"
            : $"The {string.Join(", ", dark.Take(dark.Count - 1))} and {dark[^1]} lenses";
        var verb = dark.Count == 1 ? "stayed dark" : "stayed dark";
        return $"{names} {verb}: this codebase's architecture does not call for them. A lens that does not "
             + "apply is a result, not a gap.";
    }

    /// <summary>Where a reader goes from here.</summary>
    /// <remarks>
    /// ★ THE STANDARD'S OWN ROUTES OUT OF ITS OWN PAGE, chosen here rather than supplied. A producer
    /// that could choose them could point the standard's readers wherever it liked.
    /// </remarks>
    private static string Destinations(SurveyRecord record)
    {
        var subject = record.Latest.Subject;
        var links = new List<object>
        {
            new
            {
                icon = "cai",
                label = "How this project compares",
                note = "Every measured codebase read together — the same rubric, the same instrument",
                href = "/state-of-the-corpus/",
            },
            new
            {
                icon = "cai",
                label = "Verify this score yourself",
                note = "Reproduce the number from the published evidence",
                href = "/verify/",
            },
        };

        if (SourceUrl(subject) is { } source)
        {
            links.Insert(0, new
            {
                icon = "",
                label = "The project's source repository",
                note = $"{subject.Host}/{subject.Repository}",
                href = source,
            });
        }

        return JsonSerializer.Serialize(links);
    }

    /// <summary>The forge address a subject can be read back at, or null when there is none to link.</summary>
    /// <remarks>
    /// ★ NULL IS A REAL ANSWER. A closed repository and an uploaded project have no public page, and a
    /// card linking to a 404 is worse than a card that is not there.
    /// </remarks>
    private static string? SourceUrl(DeliverySubject subject) =>
        subject.Host is { Length: > 0 } host && subject.Repository is { Length: > 0 } repository
            ? $"https://{host}/{repository}"
            : null;

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
            $"{latest.Verdict.Band} · {MeasuredOn(latest)}");

        if (latest.Evidence.ProductionLoc > 0)
        {
            yield return PageNodes.Stat(Compact(latest.Evidence.ProductionLoc), "lines of production code");
        }

        // ★ WHAT THE SURVEY WAS OF. Missing from the first port of this band and caught by diffing against
        //   the producer's own golden: the delivery carries it (subject.languages, MINOR 1.1), and a
        //   portrait that does not say what a codebase is written in makes the reader open the source to
        //   find out.
        if (latest.Subject.Languages?.Primary is { Length: > 0 } primary)
        {
            var others = (latest.Subject.Languages.Secondary ?? []).Select(PageProse.LanguageName).ToList();
            yield return PageNodes.Stat(
                PageProse.LanguageName(primary),
                others.Count > 0 ? $"with {string.Join(", ", others)}" : "primary language");
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
            $"The score is its most recent published measurement, taken on {MeasuredOn(latest)} at a "
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

        // ★ THE APPEARANCE IS THE VOCABULARY'S CONSTANT AND THE ANCHOR IS "about". Ported by hand as a
        //   lower-case literal with no anchor, which the theme does not style and no link can reach — caught
        //   by diffing this page against the producer's own golden rather than by reading it.
        return PageNodes.Section(PageNodes.Note, "about",
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

    /// <summary>
    /// The one thing the chart cannot say about itself.
    /// </summary>
    /// <remarks>
    /// ★★ IT NO LONGER RESTATES THE MOVEMENT, because the island already does. Rendered, the section
    /// carried two near-identical sentences — "3 measurements, from 25 June to 24 August: 54.0 to
    /// 78.2 — up 24.2." from the island, and the same sentence again from here, differing only in
    /// whether it wrote 54 or 54.0. A page that says one thing twice, slightly differently, invites a
    /// reader to look for the difference. Caught by looking at a screenshot; no test could have,
    /// because both halves were individually correct.
    /// <para>★ WHAT SURVIVES IS THE PART THE CHART CANNOT SHOW: that this is EVERY published
    /// measurement rather than a filtered climb. The producer's caption also explained why the
    /// headline could sit above the newest point — that sentence is gone with the rule it described,
    /// now that the page publishes the latest reading rather than the peak.</para>
    /// </remarks>
    private static string TrendCaption(SurveyRecord record) =>
        "Every published measurement of this repository, oldest first — including the ones that went "
        + "down. It is not a best-so-far line.";

    /// <summary>When the code was MEASURED — never when the delivery was signed.</summary>
    /// <remarks>
    /// ★★ THE TWO ARE DAYS APART AND THE PAGE MEANS THE FIRST. The standard signs when a delivery is
    /// pushed; the producer scanned whenever it scanned, and `measurement.scannedAt` says so. Dating a
    /// page by its signature tells a reader the code was looked at on a day nobody looked at it — and
    /// the whole claim of a survey page is that a number is attached to a moment. The fallback to the
    /// signature is honest only because it is the latest the measurement can possibly be.
    /// </remarks>
    private static string MeasuredOn(DeliveryPayload delivery) =>
        Day(delivery.Measurement.ScannedAt is { Length: > 0 } scanned ? scanned : delivery.IssuedAt);

    /// <summary>The standard's own name for a lens — never the key, which is an identifier.</summary>
    /// <remarks>
    /// ★ An unknown key passes through rather than being dropped: a lens this catalogue has not met
    /// yet is still a lens that was measured, and a gauge missing from the page is worse than one
    /// labelled with its key.
    /// </remarks>
    private static string LensName(string key) =>
        LensCatalog.All.FirstOrDefault(l => string.Equals(l.Key, key, StringComparison.Ordinal))?.DisplayName
        ?? key;

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
