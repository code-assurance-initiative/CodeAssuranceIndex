using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// The advisory and package pages, and the two indexes over them.
/// </summary>
/// <remarks>
/// <para>★★ EVERY COUNT ON THESE PAGES IS A FLOOR AND SAYS SO. An advisory survives only in a survey's
/// readable list of affected locations, and that list stops without the count above it changing — so what
/// a survey was SEEN to carry is what fitted. The cut bites hardest on the codebases carrying the most
/// vulnerabilities, so the error is not noise: it is systematic and always in one direction.
/// <see cref="Figure.Floor"/> carries the bound and the blind spot together, so every renderer states
/// both — the hedge is the type's job rather than a sentence a later caller can leave out.</para>
///
/// <para>★★ EXACTLY ONE SHARE HERE IS EXACT: the one taken over the surveys nothing was withheld from. Its
/// numerator and denominator are counted over the same thing. It UNDERSTATES how widespread an advisory is
/// — a survey with a complete list is by construction a survey with fewer findings — and understating
/// something in a way a reader can see is a different object from a censored count that cannot say
/// so.</para>
/// </remarks>
public static class CorpusAdvisoryPages
{
    /// <summary>
    /// Surveys an advisory or a package must be seen in before it gets a page.
    /// </summary>
    /// <remarks>
    /// ★ THREE. Below it a page is a list of two codebases with an advisory's name at the top, which is a
    /// page about two codebases. Lower than the language gate because the unit is different: an advisory
    /// seen in three codebases is three independent sightings of one fact, while five codebases in a
    /// language are five samples of a field.
    /// </remarks>
    public const int MinimumSurveys = 3;

    /// <summary>The address of the advisory index.</summary>
    public static string PathForAdvisoryIndex() => $"{CorpusSheetBuilder.Root}/advisories";

    /// <summary>The address of the package index.</summary>
    public static string PathForPackageIndex() => $"{CorpusSheetBuilder.Root}/packages";

    /// <summary>The address of one advisory's page, or null when it has none. ★ The one decision.</summary>
    public static string? PathForAdvisory(string? advisoryId, int surveys) =>
        surveys >= MinimumSurveys && SurveyAddress.Segment(advisoryId) is { } slug
            ? $"{CorpusSheetBuilder.Root}/advisory/{slug}"
            : null;

    /// <summary>The address of one package's page, or null when it has none. ★ The one decision.</summary>
    public static string? PathForPackage(string? package, int surveys) =>
        surveys >= MinimumSurveys && SurveyAddress.Segment(package) is { } slug
            ? $"{CorpusSheetBuilder.Root}/package/{slug}"
            : null;

    /// <summary>Every advisory and package page this reading can publish, with their indexes.</summary>
    public static IReadOnlyList<SurveyPage> Build(CorpusReading reading, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var cut = reading.Advisories;
        var pages = new List<SurveyPage>();

        var advisories = new List<(string Id, AdvisoryStat Stat, string Path)>();
        foreach (var (id, stat) in cut.ByAdvisory.OrderByDescending(kv => kv.Value.Surveys).ThenBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (PathForAdvisory(id, stat.Surveys) is not { } path)
            {
                continue;
            }

            advisories.Add((id, stat, path));
            pages.Add(AdvisoryPage(id, path, stat, cut, takenAt));
        }

        var packages = new List<(string Name, PackageStat Stat, string Path)>();
        foreach (var (name, stat) in cut.ByPackage.OrderByDescending(kv => kv.Value.Surveys).ThenBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (PathForPackage(name, stat.Surveys) is not { } path)
            {
                continue;
            }

            packages.Add((name, stat, path));
            pages.Add(PackagePage(name, path, stat, cut, reading, takenAt));
        }

        if (advisories.Count > 0)
        {
            pages.Add(AdvisoryIndex(advisories, cut, takenAt));
        }

        if (packages.Count > 0)
        {
            pages.Add(PackageIndex(packages, cut, takenAt));
        }

        return pages;
    }

    // ── one advisory ─────────────────────────────────────────────────────────────────────────────────────────

    private static SurveyPage AdvisoryPage(
        string id, string path, AdvisoryStat stat, AdvisoryCut cut, DateTimeOffset takenAt)
    {
        // ★ No branch for the uncensored case: a Floor with a zero blind spot IS a Count, decided by the
        //   factory rather than by a caller remembering to choose.
        var seen = Figure.Floor(
            stat.Surveys,
            cut.SurveysTruncated,
            Basis.Of("survey carrying this advisory", "surveys carrying this advisory"),
            takenAt);
        var amongComplete = Share(
            stat.SurveysAmongComplete,
            cut.SurveysComplete,
            Basis.Of("survey whose advisory list was complete", "surveys whose advisory list was complete"),
            takenAt);
        var inherited = Share(
            stat.SurveysInherited,
            stat.Surveys,
            Basis.Of("survey where this advisory was visible", "surveys where this advisory was visible"),
            takenAt);

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, id),
                // ★ ONE SENTENCE, AND ONLY THE REASON. The figure says "at least" and names its own blind
                //   spot; repeating that here is how a caveat comes to live in three places at once. What no
                //   figure can carry is WHY the count is short.
                PageNodes.RichText(PageProse.Paragraph(
                    "The advisories a survey carries survive only in its brief's rendered list of affected "
                    + "locations, and that list stops after a fixed number of entries without the count above "
                    + "it changing — so what a survey was seen to carry is what fitted."))),

            PageNodes.FigureBand(
                "reach",
                "lead",
                "Reach",
                null,
                null,
                PageNodes.Reading(
                    seen,
                    "seen carrying it",
                    "A floor, not a total. The cut bites hardest on the codebases with the most "
                    + "vulnerabilities, so this count is not short by a random amount — it is short in one "
                    + "direction."),
                amongComplete is null
                    ? null
                    : PageNodes.Tally(
                        amongComplete,
                        "of the surveys nothing was withheld from",
                        "The one share here taken over a population nothing was withheld from, and the only "
                        + "one that can be read as a statement about the corpus: its numerator and its "
                        + "denominator are counted over the same thing."
                        + "\n\nIt understates how widespread this is — a survey with a complete list is by "
                        + "construction a survey with fewer findings — and understating something in a way a "
                        + "reader can see is a different object from a censored count that cannot say so."),
                inherited is null
                    ? null
                    : PageNodes.Tally(
                        inherited,
                        "pulled it in through something else",
                        "The codebase did not choose this package: something it depends on did. That changes "
                        + "who can fix it and how, and it is read from the brief's own words rather than "
                        + "inferred.")),

            PageNodes.Section(
                "note",
                "package",
                PageNodes.Heading(2, "The package it is raised against"),
                PageNodes.RichText(PageProse.Paragraph(
                    stat.Packages <= 1
                        ? $"Seen against {PageProse.Escape(stat.Package)}."
                        : $"Seen against {PageProse.Escape(stat.Package)} and "
                            + $"{PageProse.Count(stat.Packages - 1)} other package "
                            + $"{(stat.Packages == 2 ? "name" : "names")}. One advisory id reaching the corpus "
                            + "under more than one package name is stated rather than resolved by picking "
                            + "one."))),
        };

        return new SurveyPage(
            path,
            id,
            $"{id} was seen in at least {PageProse.Count(stat.Surveys)} measured codebases — a floor, with the "
            + "size of its own blind spot stated.",
            PageNodes.Section([.. children]));
    }

    // ── one package ──────────────────────────────────────────────────────────────────────────────────────────

    private static SurveyPage PackagePage(
        string name, string path, PackageStat stat, AdvisoryCut cut, CorpusReading reading, DateTimeOffset takenAt)
    {
        var seen = Figure.Floor(
            stat.Surveys,
            cut.SurveysTruncated,
            Basis.Of("survey carrying this package with an advisory against it", "surveys carrying this package with an advisory against it"),
            takenAt);
        var amongComplete = Share(
            stat.SurveysAmongComplete,
            cut.SurveysComplete,
            Basis.Of("survey whose advisory list was complete", "surveys whose advisory list was complete"),
            takenAt);

        // The advisories raised against it, named — a package page that does not name them asks a reader to
        // take "vulnerable" on trust.
        var raised = reading.Advisories.ByAdvisory
            .Where(kv => string.Equals(kv.Value.Package, name, StringComparison.Ordinal))
            .OrderByDescending(kv => kv.Value.Surveys)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, name),
                PageNodes.RichText(PageProse.Paragraph(
                    "Every figure here counts codebases where this package was SEEN carrying an advisory. A "
                    + "codebase whose advisory list was cut short may carry it without being counted."))),

            PageNodes.FigureBand(
                "reach",
                "lead",
                "Reach",
                null,
                null,
                PageNodes.Reading(seen, "seen carrying it", null),
                amongComplete is null
                    ? null
                    : PageNodes.Tally(amongComplete, "of the surveys nothing was withheld from", null)),

            raised.Count == 0
                ? null
                : PageNodes.Section(
                    PageNodes.Note,
                    "advisories",
                    PageNodes.Heading(2, "Advisories raised against it"),
                    PageNodes.RichText(
                        "<ul>"
                        + string.Join("", raised.Select(kv =>
                            PathForAdvisory(kv.Key, kv.Value.Surveys) is { } advisoryPath
                                ? $"<li>{PageProse.Link($"/{advisoryPath}/", kv.Key)}</li>"
                                : $"<li>{PageProse.Escape(kv.Key)}</li>"))
                        + "</ul>")),
        };

        return new SurveyPage(
            path,
            name,
            $"{name} was seen carrying an advisory in at least {PageProse.Count(stat.Surveys)} measured "
            + "codebases — a floor, with the size of its own blind spot stated.",
            PageNodes.Section([.. children]));
    }

    // ── the two indexes ──────────────────────────────────────────────────────────────────────────────────────

    private static SurveyPage AdvisoryIndex(
        IReadOnlyList<(string Id, AdvisoryStat Stat, string Path)> rows, AdvisoryCut cut, DateTimeOffset takenAt)
    {
        var held = Held(cut.ByAdvisory.Count, rows.Count, Basis.Of("advisory this reading could see", "advisories this reading could see"), takenAt);

        return new SurveyPage(
            PathForAdvisoryIndex(),
            "Advisories seen across the corpus",
            $"{PageProse.Count(rows.Count)} advisories seen in at least {MinimumSurveys} measured codebases "
            + "each, every count a floor.",
            PageNodes.Section(
                PageNodes.Section(
                    PageNodes.Heading(1, "Advisories seen across the corpus"),
                    PageNodes.RichText(Preamble(cut, held, "advisory", "advisories"))),
                PageNodes.Section(
                    null,
                    "advisories",
                    PageNodes.RichText(
                        "<ul>"
                        + string.Join("", rows.Select(r =>
                            $"<li>{PageProse.Link($"/{r.Path}/", r.Id)} — seen in at least "
                            + $"{PageProse.Count(r.Stat.Surveys)}</li>"))
                        + "</ul>"))));
    }

    private static SurveyPage PackageIndex(
        IReadOnlyList<(string Name, PackageStat Stat, string Path)> rows, AdvisoryCut cut, DateTimeOffset takenAt)
    {
        var held = Held(cut.ByPackage.Count, rows.Count, Basis.Of("package this reading could see", "packages this reading could see"), takenAt);

        return new SurveyPage(
            PathForPackageIndex(),
            "Vulnerable packages seen across the corpus",
            $"{PageProse.Count(rows.Count)} packages seen carrying an advisory in at least {MinimumSurveys} "
            + "measured codebases each, every count a floor.",
            PageNodes.Section(
                PageNodes.Section(
                    PageNodes.Heading(1, "Vulnerable packages seen across the corpus"),
                    PageNodes.RichText(Preamble(cut, held, "package", "packages"))),
                PageNodes.Section(
                    null,
                    "packages",
                    PageNodes.RichText(
                        "<ul>"
                        + string.Join("", rows.Select(r =>
                            $"<li>{PageProse.Link($"/{r.Path}/", r.Name)} — seen in at least "
                            + $"{PageProse.Count(r.Stat.Surveys)}</li>"))
                        + "</ul>"))));
    }

    /// <summary>
    /// What the index is an index OF, and how much of it is not on the page.
    /// </summary>
    /// <remarks>
    /// ★★ THE HELD-BACK COUNT IS NOT OPTIONAL. A table of counts presented alone reads as a census of the
    /// corpus while being a census of the part big enough to print, and the count of what was held back is
    /// what stops it saying that.
    /// </remarks>
    private static string Preamble(AdvisoryCut cut, Figure? held, string one, string many)
    {
        // ★ The article comes from the noun, because the noun is substituted in. See PageProse.An.
        var article = PageProse.An(one);
        var opening = char.ToUpperInvariant(article[0]) + article[1..];

        return PageProse.Paragraph(
            $"Every count here is a floor. {opening} {one} survives only in a survey's readable list of affected "
            + "locations, which stops after a fixed number of entries without the count above it changing, so "
            + $"a survey may carry {article} {one} without being counted for it. "
            + $"{PageProse.Count(cut.SurveysTruncated)} of {PageProse.Count(cut.Surveys)} surveys read for "
            + "advisories had a list cut short that way.")
        + (held is null
            ? string.Empty
            : PageProse.Paragraph(
                $"{PageProse.Escape(held.Headline())} are not listed, because each was seen in fewer than "
                + $"{MinimumSurveys} codebases. Below that a page describes a handful of codebases rather than "
                + $"the {many} they carry. They are absent from the list rather than folded into it."));
    }

    /// <summary>How many of a family have no page — null when all of them do.</summary>
    private static Figure? Held(int total, int published, Basis basis, DateTimeOffset takenAt) =>
        total == 0 || total == published ? null : Figure.Ratio(total - published, total, basis, takenAt);

    /// <summary>A share, or null when there is nothing to take it over.</summary>
    private static Figure? Share(int numerator, int denominator, Basis basis, DateTimeOffset takenAt) =>
        denominator == 0 || numerator == 0 ? null : Figure.Ratio(numerator, denominator, basis, takenAt);
}
