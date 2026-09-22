using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The per-language and per-country corpus pages, and the two indexes over them.
/// </summary>
/// <remarks>
/// <para>★★ A GROUP PAGE IS THE SHEET, SCOPED — the same fold, the same denominators, the same words. Two
/// pages about one corpus that compute their shares differently are two claims, and a reader comparing a
/// row on the sheet with the page it links to would be comparing two arithmetics.</para>
/// <para>★★ AND THE SET OF PAGES IS THE SET THE SHEET LINKS. One function decides both, so a row cannot
/// link a page nobody built and a page cannot exist that nothing points at.</para>
/// </remarks>
public sealed class CorpusGroupPagesTests
{
    [Fact]
    public void A_page_exists_for_exactly_the_groups_the_sheet_links()
    {
        var records = Corpus();

        var built = CorpusGroupPages.Build(records, TakenAt)
            .Select(p => $"/{p.Path}/")
            .ToHashSet(StringComparer.Ordinal);
        var linked = PageText.Hrefs(PageText.Json(CorpusSheetBuilder.Build(records, TakenAt)))
            .Where(h => h.Contains("/language/", StringComparison.Ordinal)
                     || h.Contains("/country/", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(linked);
        foreach (var href in linked)
        {
            Assert.Contains(href, built);
        }
    }

    /// <summary>★★ One arithmetic: the page states the share its row on the sheet states.</summary>
    [Fact]
    public void A_language_page_states_the_same_reading_as_its_row_on_the_sheet()
    {
        var records = Corpus();

        var cut = CorpusCuts.ByLanguage(records, TakenAt).Single(c => c.Key == "csharp");
        var json = PageText.Json(Page(CorpusGroupPages.Build(records, TakenAt), "language/csharp"));

        // ★ A stat cell carries the reading SPLIT — the value in the display slot and the population
        //   beneath it — so the page is checked for both halves rather than for the joined headline, which
        //   no renderer emits.
        var split = cut.Reading.VulnAffectedShare!.Split();
        Assert.Contains(split.Lead, json, StringComparison.Ordinal);
        Assert.Contains(split.Support, json, StringComparison.Ordinal);
    }

    /// <summary>★ A country page carries the caveat, because a country places an OWNER.</summary>
    [Fact]
    public void A_country_page_says_that_a_country_places_an_owner()
    {
        var json = PageText.Json(Page(CorpusGroupPages.Build(Corpus(), TakenAt), "country/denmark"));

        Assert.Contains("places an owner", json, StringComparison.Ordinal);
        Assert.Contains("never a legal entity", json, StringComparison.Ordinal);
    }

    [Fact]
    public void The_indexes_link_exactly_the_pages_that_were_built()
    {
        var records = Corpus();
        var pages = CorpusGroupPages.Build(records, TakenAt);
        var built = pages.Select(p => $"/{p.Path}/").ToHashSet(StringComparer.Ordinal);

        foreach (var index in new[] { "state-of-the-corpus/languages", "state-of-the-corpus/countries" })
        {
            var json = PageText.Json(pages.Single(p => p.Path == index));
            var hrefs = PageText.Hrefs(json)
                .Where(h => h.Contains("/language/", StringComparison.Ordinal)
                         || h.Contains("/country/", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(hrefs);
            foreach (var href in hrefs)
            {
                Assert.Contains(href, built);
            }
        }
    }

    /// <summary>★ A language nobody could place is not a page, and the index says how many that is.</summary>
    [Fact]
    public void The_language_index_says_how_much_of_the_reading_it_covers()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Group("csharp", "Denmark", 6, "acme"));
        records.AddRange(Group(null, null, 4, "beta"));

        var pages = CorpusGroupPages.Build(records, TakenAt);
        var json = PageText.Json(pages.Single(p => p.Path == "state-of-the-corpus/languages"));

        Assert.Contains("6 of 10", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static List<SurveyRecord> Corpus()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Group("csharp", "Denmark", 12, "acme"));
        records.AddRange(Group("go", "Denmark", 6, "beta"));
        records.AddRange(Group("rust", "Norway", 2, "gamma"));
        return records;
    }

    private static List<SurveyRecord> Group(string? language, string? country, int count, string owner) =>
        [.. Enumerable.Range(0, count).Select(i => Record(language, country, $"{owner}-{i}"))];

    private static SurveyPage Page(IReadOnlyList<SurveyPage> pages, string suffix) =>
        pages.Single(p => p.Path.EndsWith(suffix, StringComparison.Ordinal));



    private static SurveyRecord Record(string? language, string? country, string owner)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        var affected = ordinal % 3 == 0;
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_group_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = $"{owner}/repo-{ordinal}",
                Host = "github.com",
                Languages = language is null ? null : new SubjectLanguages { Primary = language },
                Origin = country is null
                    ? new SubjectOrigin { Declared = false, UnresolvedReason = "blank" }
                    : new SubjectOrigin { Country = country, Declared = true, ResolvedBy = "countryName" },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                ProductionLoc = 1000,
                SecurityReading = new SecurityReading
                {
                    VulnMeasurable = true,
                    VulnAffected = affected,
                    Findings = affected ? 1 : 0,
                    DisclosureMeasured = true,
                    DisclosurePolicy = !affected,
                },
            },
        }]);
    }
}
