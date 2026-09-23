using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// The per-language and per-country corpus pages, and the two indexes over them.
/// </summary>
/// <remarks>
/// <para>★★ A GROUP PAGE IS THE SHEET, SCOPED. The same fold, the same denominators, the same words — a
/// <see cref="CorpusSlice"/> holds a <see cref="CorpusReading"/> over its own codebases, built by the same
/// function the whole corpus is. Two pages about one corpus computing their shares differently are two
/// claims, and a reader comparing a row on the sheet with the page it links to would be comparing two
/// arithmetics.</para>
///
/// <para>★★ AND THE SET OF PAGES IS THE SET THE SHEET LINKS, because both read
/// <see cref="CorpusSlice.Path"/> — the one decision, made in <see cref="CorpusCuts"/>. A row cannot link
/// a page nobody built, and a page cannot exist that nothing points at.</para>
/// </remarks>
public static class CorpusGroupPages
{
    /// <summary>The address of the language index.</summary>
    public static string PathForLanguageIndex() => $"{CorpusSheetBuilder.Root}/languages";

    /// <summary>The address of the country index.</summary>
    public static string PathForCountryIndex() => $"{CorpusSheetBuilder.Root}/countries";

    /// <summary>Every group page this reading can publish, with the two indexes.</summary>
    public static IReadOnlyList<SurveyPage> Build(IReadOnlyList<SurveyRecord> records, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(records);

        var reading = CorpusReading.From(records, takenAt);
        var languages = CorpusCuts.ByLanguage(records, takenAt).Where(c => c.Path is not null).ToList();
        var countries = CorpusCuts.ByCountry(records, takenAt).Where(c => c.Path is not null).ToList();

        var pages = new List<SurveyPage>();
        pages.AddRange(languages.Select(c => GroupPage(c, GroupKind.Language)));
        pages.AddRange(countries.Select(c => GroupPage(c, GroupKind.Country)));

        if (languages.Count > 0)
        {
            pages.Add(Index(GroupKind.Language, languages, reading));
        }

        if (countries.Count > 0)
        {
            pages.Add(Index(GroupKind.Country, countries, reading));
        }

        return pages;
    }

    private enum GroupKind
    {
        Language,
        Country,
    }

    // ── one group ────────────────────────────────────────────────────────────────────────────────────────────

    private static SurveyPage GroupPage(CorpusSlice cut, GroupKind kind)
    {
        var reading = cut.Reading;
        var title = kind == GroupKind.Language
            ? $"{cut.Name} in the corpus"
            : $"{cut.Name} in the corpus";

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, title),
                PageNodes.RichText(PageProse.Paragraph(Lede(cut, kind)))),

            PageNodes.Band(
                "population",
                PageNodes.Stat(PageProse.Count(cut.Codebases), "measured codebases"),
                PageNodes.Stat(PageProse.Score(cut.MedianHeadline), "median CAI across them"),
                kind == GroupKind.Country
                    ? PageNodes.Stat(
                        PageProse.Count(reading.Origins.OwnersConsidered),
                        "accounts that own them")
                    : null,
                PageNodes.Stat(PageProse.Count(reading.VulnMeasurable), "with a dependency graph that resolved")),

            PageNodes.FigureBand(
                "findings",
                "lead",
                "What was found",
                null,
                "Each is taken over its own population, named beneath it. The populations are not the same.",
                reading.VulnAffectedShare is not { } affected
                    ? null
                    : PageNodes.Reading(
                        affected,
                        "carry a known-vulnerable component",
                        "Taken over the codebases here a scanner could resolve, never over all of them: a "
                        + "codebase nobody could scan is unmeasured, not clean."),
                reading.DisclosureWithoutPolicyShare is not { } noPolicy
                    ? null
                    : PageNodes.Reading(
                        noPolicy,
                        "publish no disclosure policy",
                        "Taken over the codebases here where one was looked for."),
                reading.SecretsInHistoryShare is not { } secrets
                    ? null
                    : PageNodes.Reading(secrets, "committed a secret at some point", null),
                reading.SbomShare is not { } sbom
                    ? null
                    : PageNodes.Reading(sbom, "publish an SBOM", null)),

            kind == GroupKind.Country ? CountryCaveat() : LanguageNote(),
        };

        return new SurveyPage(cut.Path!, title, MetaDescription(cut, kind), PageNodes.Section([.. children]));
    }

    private static string Lede(CorpusSlice cut, GroupKind kind) =>
        kind == GroupKind.Language
            ? $"{PageProse.Escape(cut.Name)} — {PageProse.Count(cut.Codebases)} measured codebases, read as "
                + "one group. Every figure below is taken over the population named beneath it, and every "
                + "one of them is a count of codebases rather than of findings."
            : $"{PageProse.Escape(cut.Name)} — {PageProse.Count(cut.Codebases)} measured codebases whose "
                + "owner declared this country, read as one group.";

    /// <summary>★ The caveat travels with every country surface, not only with the sheet's §4.</summary>
    private static Dictionary<string, object?> CountryCaveat() =>
        PageNodes.Section(
            PageNodes.Note,
            "what-a-country-is",
            PageNodes.Heading(2, "What a country is here"),
            PageNodes.RichText(PageProse.Paragraph(
                "A country is normalised from what an account writes about itself in its own profile line, so "
                + "it places an owner. It never places a project, and never a legal entity. Most owners "
                + "cannot be placed at all, so this is a reading of the codebases whose owners said where "
                + "they are, and of nothing wider.")));

    /// <summary>★ And the language surfaces say the opposite fact, which is why they need no caveat.</summary>
    private static Dictionary<string, object?> LanguageNote() =>
        PageNodes.Section(
            PageNodes.Note,
            "what-a-language-is",
            PageNodes.Heading(2, "What a language is here"),
            PageNodes.RichText(PageProse.Paragraph(
                "A language is read off the code by the pass that measured everything else. Nobody declares "
                + "it and no account can decline to, so nothing on this page rests on anybody saying "
                + "anything about themselves.")));

    private static string MetaDescription(CorpusSlice cut, GroupKind kind) =>
        kind == GroupKind.Language
            ? $"{PageProse.Count(cut.Codebases)} {cut.Name} codebases measured against the Codebase Assurance "
                + $"Index, median {PageProse.Score(cut.MedianHeadline)} — and what was found in them."
            : $"{PageProse.Count(cut.Codebases)} measured codebases whose owner declared {cut.Name}, median "
                + $"{PageProse.Score(cut.MedianHeadline)} — and what was found in them.";

    // ── the two indexes ──────────────────────────────────────────────────────────────────────────────────────

    private static SurveyPage Index(GroupKind kind, IReadOnlyList<CorpusSlice> cuts, CorpusReading reading)
    {
        var language = kind == GroupKind.Language;
        var title = language ? "The corpus by language" : "The corpus by country";
        var coverage = CorpusCuts.Coverage(
            cuts,
            reading.Codebases,
            Basis.Of("measured codebase", "measured codebases"),
            reading.TakenAt);

        var rows = cuts.Select(c =>
            $"<li>{PageProse.Link(c.Href!, c.Name)} — {PageProse.Count(c.Codebases)} codebases, median "
            + $"{PageProse.Score(c.MedianHeadline)}</li>");

        return new SurveyPage(
            language ? PathForLanguageIndex() : PathForCountryIndex(),
            title,
            $"{PageProse.Count(cuts.Count)} {(language ? "languages" : "countries")} with enough measured "
            + "codebases to read as a group.",
            PageNodes.Section(
                PageNodes.Section(
                    PageNodes.Heading(1, title),
                    PageNodes.RichText(
                        PageProse.Paragraph(
                            language
                                ? "A language is read off the code, so nothing here rests on anybody "
                                    + "declaring anything."
                                : "A country is normalised from what an account writes about itself in its "
                                    + "own profile line, so it places an owner. It never places a project, "
                                    + "and never a legal entity.")
                        // ★★ THE ROWS DO NOT ADD UP TO THE CORPUS AND THE PAGE SAYS SO, on the index as well
                        //    as on the sheet: a reader who arrives here directly meets the same claim.
                        // ★ The clause after the share is only true when something was left out: at full
                        //   coverage it names two empty classes. See Figure.Remainder.
                        + (coverage is null
                            ? string.Empty
                            : PageProse.Paragraph(
                                $"{PageProse.Escape(coverage.Headline())} are on this page."
                                + (coverage.Remainder is > 0 ? " The rest are " : string.Empty)
                                + (coverage.Remainder is not > 0
                                    ? string.Empty
                                    : language
                                        ? "codebases carrying no primary language this reading could read, "
                                            + "which is a limit of the indexing behind it and never a fact "
                                            + "about the codebases, or languages holding too few codebases "
                                            + "to read as a group."
                                        : "codebases whose owner declared nothing placeable, or countries "
                                            + "holding too few codebases to read as a place."))))),
                PageNodes.Section(
                    null,
                    language ? "languages" : "countries",
                    PageNodes.RichText("<ul>" + string.Join("", rows) + "</ul>"))));
    }
}
