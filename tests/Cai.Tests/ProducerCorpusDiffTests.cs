using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Phase 6 step 1, concluded: the corpus family, diffed against the producer's own golden.
/// </summary>
/// <remarks>
/// <para>★★ THE STRUCTURE, NOT THE FIGURES — and the reason is in the data, not in the effort. The
/// producer folds its sheet from its OWN metrics database, over every repository it has ever surveyed; the
/// standard folds it from the published deliveries it holds. Those are different populations by
/// construction, so a figure-for-figure diff would report the difference between two corpora and call it a
/// defect in a page. The plan says the same thing from the other end: the aggregate pages are not
/// pixel-comparable, and are verified by node diff over a frozen dataset.</para>
/// <para>★ WHAT MUST MATCH IS EVERYTHING A READER NAVIGATES BY: which pages exist and at what addresses,
/// the sections in order with their appearances, and the islands with their tags. Those are the parts a
/// hand port loses, and losing one is invisible until somebody opens the page.</para>
/// </remarks>
public sealed class ProducerCorpusDiffTests
{
    /// <summary>★★ The same nine addresses. A page family that moved is every link to it broken.</summary>
    [Fact]
    public void The_standard_publishes_the_same_addresses()
    {
        // ★ PINNED TO THE FAMILY'S REAL SIZE. Two empty lists are equal, and a fixture that cleared no gate
        //   would make this test pass while proving nothing — the sheet, four indexes and four detail pages.
        Assert.Equal(9, ProducerPaths().Count);
        Assert.Equal(ProducerPaths(), StandardPaths());
    }

    [Fact]
    public void The_sheet_has_the_same_sections_in_the_same_order()
    {
        Assert.Equal(PageShape.Sections(Producer("state-of-the-corpus")), PageShape.Sections(Standard(Sheet())));
    }

    [Fact]
    public void The_sheet_carries_the_same_islands()
    {
        Assert.Equal(PageShape.Widgets(Producer("state-of-the-corpus")), PageShape.Widgets(Standard(Sheet())));
    }

    [Fact]
    public void The_sheet_is_titled_the_same()
    {
        Assert.Equal(
            Producer("state-of-the-corpus").GetProperty("Title").GetString(),
            Sheet().Title);
    }

    /// <summary>
    /// ★ The trend island's props are a CONTRACT with the site's theme: an undeclared prop is dropped at
    /// render without a word, so a series handed over under a name the island does not read is a chart that
    /// simply does not appear.
    /// </summary>
    [Fact]
    public void The_trend_island_is_handed_the_same_props()
    {
        Assert.Equal(WidgetProps(Producer("state-of-the-corpus"), "cai-trend"), WidgetProps(Standard(Sheet()), "cai-trend"));
    }

    /// <summary>
    /// ★★ THE GROUP PAGES DIFFER FROM THE PRODUCER'S, AND THE DIFFERENCES ARE NAMED HERE RATHER THAN
    /// LEFT TO BE DISCOVERED. Phase 6 diffed the SHEET's sections and the page ADDRESSES; nobody had
    /// diffed a language or country page's own structure, so these went unseen until one was looked at.
    /// </summary>
    /// <remarks>
    /// <para>★★ THE ONE THAT IS NOT COSMETIC: the producer draws a TREND on every language and country
    /// page, and the standard cannot. Its readings store records the corpus-wide reading — codebases and
    /// the median across them — and nothing per group, so there is no per-language or per-country series
    /// to draw. Building one means the recorded reading carrying a series per group, which is a schema
    /// decision and not a page fix. Recorded in the plan; NOT worked around by deriving a line from
    /// today's data, which is exactly the "line that never happened" the whole readings store exists to
    /// prevent.</para>
    /// <para>★ The rest are wording and anchors — a heading of "Denmark" against "Denmark in the corpus",
    /// and a caveat anchored `census` against `what-a-country-is`. They are pinned so a later change has
    /// to mean it.</para>
    /// </remarks>
    [Fact]
    public void The_group_pages_differ_from_the_producers_in_ways_that_are_written_down()
    {
        var producerCountry = PageShape.Sections(Producer("state-of-the-corpus/country/denmark"));
        var ourCountry = PageShape.Sections(Standard(CountryPage()));

        // The producer draws a trend per group; the standard has no per-group series to draw.
        Assert.Contains("-/trend", producerCountry);
        Assert.DoesNotContain("-/trend", ourCountry);
        Assert.DoesNotContain("cai-trend", PageShape.Widgets(Standard(CountryPage())));

        // And the caveat is present on both, under different anchors.
        Assert.Contains("Note/census", producerCountry);
        Assert.Contains("Note/what-a-country-is", ourCountry);

        // The claim itself is what matters, and it survives the rename.
        var json = PageText.Json(CountryPage());
        Assert.Contains("places an owner", json, StringComparison.Ordinal);
        Assert.Contains("never a legal entity", json, StringComparison.Ordinal);
    }

    private static SurveyPage CountryPage() =>
        CorpusGroupPages.Build(Corpus(), TakenAt).Single(p => p.Path.EndsWith("country/denmark", StringComparison.Ordinal));

    // ---------------------------------------------------------------- readers

    private static List<string> WidgetProps(JsonElement page, string tag)
    {
        var found = new List<string>();
        Walk(page.GetProperty("Node"), n =>
        {
            if (n.TryGetProperty("type", out var t) && t.GetString() == "widget"
                && n.TryGetProperty("tag", out var actual) && actual.GetString() == tag
                && n.TryGetProperty("props", out var props))
            {
                found.AddRange(props.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
            }
        });
        return found;
    }

    private static void Walk(JsonElement node, Action<JsonElement> visit)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            visit(node);
            foreach (var property in node.EnumerateObject())
            {
                Walk(property.Value, visit);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                Walk(item, visit);
            }
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 2, 0, 0, TimeSpan.Zero);

    private static List<string> ProducerPaths() =>
        [.. Family().EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];

    private static List<string> StandardPaths() =>
        [.. new[] { Sheet() }
            .Concat(CorpusGroupPages.Build(Corpus(), TakenAt))
            .Concat(CorpusAdvisoryPages.Build(CorpusReading.From(Corpus(), TakenAt), TakenAt))
            .Select(p => p.Path)
            .Order(StringComparer.Ordinal)];

    private static JsonElement Family()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Goldens", "producer-corpus-family.json");
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
    }

    private static JsonElement Producer(string path) => Family().GetProperty(path);

    private static JsonElement Standard(SurveyPage page) =>
        JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            page.Path,
            page.Title,
            page.MetaDescription,
            page.Node,
        }));

    private static SurveyPage Sheet() => CorpusSheetBuilder.Build(Corpus(), TakenAt, History());

    /// <summary>Three readings, matching the producer golden's own history.</summary>
    private static List<DatedReading> History() =>
    [
        new(TakenAt.AddDays(-30), 6, 49.8),
        new(TakenAt.AddDays(-14), 8, 51.2),
        new(TakenAt, 10, 52.6),
    ];

    /// <summary>
    /// The corpus the standard folds: six C# and four Go, all Danish-owned, one advisory seen in four.
    /// </summary>
    /// <remarks>
    /// ★ SHAPED TO CLEAR THE SAME GATES THE PRODUCER'S SNAPSHOT CLEARS, so the two produce the same page
    /// SET — five for csharp, four for go (below the language gate), ten codebases in Denmark (at the
    /// country gate), one advisory in four surveys (above the advisory gate). A corpus that cleared
    /// different gates would differ in which pages exist, which is the one thing this diff is for.
    /// </remarks>
    private static List<SurveyRecord> Corpus()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Enumerable.Range(0, 6).Select(i => Record("acme", $"service-{i}", "csharp", 60 + i, advisory: i < 4)));
        records.AddRange(Enumerable.Range(0, 4).Select(i => Record("gopher", $"tool-{i}", "go", 40 + i, advisory: false)));
        return records;
    }

    private static SurveyRecord Record(string owner, string name, string language, double score, bool advisory) =>
        SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_{owner}_{name}",
            IssuedAt = "2026-08-30T09:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = $"{owner}/{name}",
                Host = "github.com",
                Commit = "3f9a1c2",
                Languages = new SubjectLanguages { Primary = language },
                Origin = new SubjectOrigin { Country = "Denmark", Declared = true, ResolvedBy = "countryName" },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z", ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict { Cai = score, Band = "Adequate" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                ProductionLoc = 1500,
                Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
                SecurityReading = new SecurityReading
                {
                    VulnMeasurable = true,
                    VulnAffected = advisory,
                    Findings = advisory ? 1 : 0,
                    DisclosureMeasured = true,
                    DisclosurePolicy = !advisory,
                    SecretsHistoryMeasured = true,
                    SupplyChainMeasurable = true,
                    Sbom = !advisory,
                    AdvisoriesRead = true,
                    AdvisoryListComplete = true,
                    Advisories = advisory
                        ? [new VisibleAdvisory { AdvisoryId = "CVE-2026-0001", Package = "left-pad", PackageVersion = "1.3.0" }]
                        : [],
                },
            },
        }]);
}
