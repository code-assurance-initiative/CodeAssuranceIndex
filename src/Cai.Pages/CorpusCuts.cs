using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// The corpus cut into the groups its pages are drawn over — by language, and by where its owners say they
/// are.
/// </summary>
/// <remarks>
/// ★★ A CUT IS A CORPUS READING OVER A SUBSET, not a second kind of arithmetic. Every row on §3 and §4 is
/// folded by <see cref="CorpusReading.From"/> exactly as the whole sheet is, so a row cannot state a share
/// over a denominator the sheet would not.
/// </remarks>
public static class CorpusCuts
{
    /// <summary>
    /// Surveys a language must carry a security reading for before it gets a page of its own.
    /// </summary>
    /// <remarks>
    /// ★ FIVE — the same number the field guide uses, for the same reason, over a DIFFERENT population.
    /// Below five a "field" is a coincidence and a page describing one would be reading a trend into two or
    /// three codebases. The two counts are over different populations and neither implies the other: the
    /// field guide counts published codebase pages, this counts surveys carrying a security reading, and a
    /// codebase with no addressable forge path has no page at all — so a language can clear five here and
    /// have fewer than five there.
    /// </remarks>
    public const int MinimumLanguageSurveys = 5;

    /// <summary>
    /// Codebases a country must hold before it gets a page of its own.
    /// </summary>
    /// <remarks>
    /// ★ TEN, AND HIGHER THAN THE LANGUAGE GATE ON PURPOSE. A country page is a statement about a place, and
    /// a place represented by four codebases owned by two accounts is a statement about two accounts. A
    /// country is also a claim an account makes about itself and resolves for about half of them, while a
    /// language is read off the code by the pass that measured everything else: nobody chose it, and no
    /// account can inflate it.
    /// </remarks>
    public const int MinimumCountryCodebases = 10;

    /// <summary>The address of a language's corpus page, or null when it has none. ★ The one decision.</summary>
    public static string? LanguagePathFor(string? language, int surveys) =>
        surveys >= MinimumLanguageSurveys && SurveyAddress.Segment(language) is { } slug
            ? $"{CorpusSheetBuilder.Root}/language/{slug}"
            : null;

    /// <summary>The address of a country's corpus page, or null when it has none. ★ The one decision.</summary>
    public static string? CountryPathFor(string? country, int codebases) =>
        codebases >= MinimumCountryCodebases && SurveyAddress.Segment(country) is { } slug
            ? $"{CorpusSheetBuilder.Root}/country/{slug}"
            : null;

    /// <summary>
    /// The corpus by language, largest field first.
    /// </summary>
    /// <remarks>
    /// ★★ A SUBJECT WHOSE PRODUCER NAMED NO LANGUAGE IS IN NO CUT. It is not a field called "Unknown": a
    /// bucket of subjects nobody could place is a fact about the metadata, never a field of software
    /// engineering, and published as a row it would be our own gap presented as a kind of programming. The
    /// sheet states how much of the reading could be placed in a language at all instead.
    /// <para>★ LARGEST FIRST, which is the order a reader scans and the only ordering here that is not a
    /// judgement. Ordering by the affected share would rank the languages by how badly they come out, on a
    /// page that does not rank anything.</para>
    /// </remarks>
    public static IReadOnlyList<CorpusSlice> ByLanguage(IEnumerable<SurveyRecord> records, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(records);

        return [.. records
            .Select(r => (Record: r, Language: Primary(r)))
            .Where(x => x.Language is not null)
            .GroupBy(x => x.Language!, StringComparer.Ordinal)
            .Select(g =>
            {
                var subjects = g.Select(x => x.Record).ToList();
                var reading = CorpusReading.From(subjects, takenAt);
                return new CorpusSlice(
                    g.Key,
                    PageProse.LanguageName(g.Key),
                    LanguagePathFor(g.Key, reading.SecurityReadings),
                    reading,
                    SurveyCorpus.Median(SurveyCorpus.Scores(subjects)));
            })
            .OrderByDescending(c => c.Codebases)
            .ThenBy(c => c.Key, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The corpus by country, largest first.
    /// </summary>
    /// <remarks>
    /// ★ A COUNTRY NOBODY RESOLVED IS ABSENT, never a row called "unknown" — which is how an unknown bucket
    /// becomes a country on a chart. How many could be placed at all is stated as its own figure.
    /// </remarks>
    public static IReadOnlyList<CorpusSlice> ByCountry(IEnumerable<SurveyRecord> records, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(records);

        return [.. records
            .Select(r => (Record: r, Country: r.Latest.Subject.Origin?.Country))
            .Where(x => x.Country is { Length: > 0 })
            .GroupBy(x => x.Country!, StringComparer.Ordinal)
            .Select(g =>
            {
                var subjects = g.Select(x => x.Record).ToList();
                var reading = CorpusReading.From(subjects, takenAt);
                return new CorpusSlice(
                    g.Key,
                    g.Key,
                    CountryPathFor(g.Key, subjects.Count),
                    reading,
                    SurveyCorpus.Median(SurveyCorpus.Scores(subjects)));
            })
            .OrderByDescending(c => c.Codebases)
            .ThenBy(c => c.Key, StringComparer.Ordinal)];
    }

    /// <summary>
    /// How much of a reading could be placed in a cut at all.
    /// </summary>
    /// <remarks>
    /// ★★ THE ROWS DO NOT ADD UP TO THE CORPUS AND THE PAGE SAYS SO. A table of groups presented alone reads
    /// as a census; a reader who adds up the column and finds the total short is owed the difference, and
    /// the difference is a limit of the metadata rather than a fact about the codebases.
    /// </remarks>
    public static Figure? Coverage(IReadOnlyList<CorpusSlice> cuts, int codebases, Basis basis, DateTimeOffset takenAt) =>
        codebases == 0 ? null : Figure.Ratio(cuts.Sum(c => c.Codebases), codebases, basis, takenAt);

    private static string? Primary(SurveyRecord record)
    {
        var primary = record.Latest.Subject.Languages?.Primary;
        return string.IsNullOrWhiteSpace(primary) ? null : primary.Trim().ToLowerInvariant();
    }
}

/// <summary>One group of the corpus, read exactly as the whole corpus is.</summary>
/// <param name="Key">The group's identifier — a normalised language, or a country name.</param>
/// <param name="Name">What a reader is shown — <c>C#</c>, <c>Denmark</c>.</param>
/// <param name="Path">The group's own page, or null when it has none. ★ The single decision.</param>
/// <param name="Reading">The corpus reading over this group's codebases alone.</param>
/// <param name="MedianHeadline">The middle CAI of this group's codebases.</param>
public sealed record CorpusSlice(
    string Key,
    string Name,
    string? Path,
    CorpusReading Reading,
    double MedianHeadline)
{
    /// <summary>How many codebases are in this group.</summary>
    public int Codebases => Reading.Codebases;

    /// <summary>The link to the group's page, or null when it has none.</summary>
    /// <remarks>★ Derived from <see cref="Path"/> and nowhere else, so a link cannot outrun the page set.</remarks>
    public string? Href => Path is { Length: > 0 } path ? $"/{path}/" : null;
}
