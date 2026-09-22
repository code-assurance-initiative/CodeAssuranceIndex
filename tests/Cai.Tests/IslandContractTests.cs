using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Every island the standard composes is one the site actually ships, handed only props it declares.
/// </summary>
/// <remarks>
/// <para>★★ THE CMS DOES NOT VALIDATE THIS, AND THAT IS THE WHOLE REASON THE TEST EXISTS. Imprint's own
/// tests say it out loud: <i>"a syndicated page does not validate its tags against the manifest"</i>. So a
/// page naming a widget the site does not ship is ACCEPTED, published, and renders as nothing — and an
/// undeclared prop is dropped at render without a word, which is worse, because the page looks right and
/// is missing a number.</para>
/// <para>★ THE MANIFEST IS FROZEN FROM THE SITE'S OWN <c>widgets/manifest.json</c> rather than restated
/// here. A list retyped in this repository would agree with itself for ever and with the site only by
/// luck; when the site adds or renames a prop, this copy is what has to be refreshed, deliberately.</para>
/// </remarks>
public sealed class IslandContractTests
{
    /// <summary>
    /// ★ WHAT THIS TEST IS ACTUALLY LOOKING AT, pinned — because "every island is shipped" is true of no
    /// islands at all, and a fixture that cleared no gate would pass all three of these while checking
    /// nothing.
    /// </summary>
    [Fact]
    public void The_corpus_under_test_composes_every_island_the_standard_has()
    {
        var pages = EveryPage();
        var tags = Islands().Select(i => i.Tag).Distinct().Order(StringComparer.Ordinal).ToList();

        Assert.Equal(23, pages.Count);
        Assert.Equal(
            [
                "cai-band-scale", "cai-figure-band", "cai-language-board", "cai-lens-gauges",
                "cai-link-cards", "cai-share-bars", "cai-survey-list", "cai-trend",
            ],
            tags);
    }

    [Fact]
    public void Every_island_on_every_page_is_one_the_site_ships()
    {
        var shipped = Shipped();

        foreach (var (page, tag, _) in Islands())
        {
            Assert.True(
                shipped.ContainsKey(tag),
                $"page '{page}' names the island <{tag}>, which the site does not ship. A syndicated page "
                + "is not validated against the manifest, so this publishes and renders as nothing.");
        }
    }

    [Fact]
    public void No_island_is_handed_a_prop_it_does_not_read()
    {
        var shipped = Shipped();

        foreach (var (page, tag, prop) in Islands())
        {
            Assert.True(
                shipped.TryGetValue(tag, out var declared) && declared.Contains(prop),
                $"page '{page}' hands <{tag}> a prop '{prop}' it does not declare. An undeclared prop is "
                + "dropped at render without a word, so the page looks right and is missing a value.");
        }
    }

    /// <summary>
    /// ★★ AND NOT ONE OF THEM IS GIVEN SOMEWHERE TO FETCH FROM. A widget that CAN fetch will, and what it
    /// fetches is whichever card the gallery hands back — which is how one project's figures came to be
    /// printed under a thousand repositories' names. Several shipped islands DO declare <c>api-base</c>;
    /// the standard's pages hand every island its numbers instead.
    /// </summary>
    [Fact]
    public void No_island_on_any_page_is_given_an_api_base()
    {
        Assert.DoesNotContain(Islands(), i => i.Prop == "api-base");
    }

    // ---------------------------------------------------------------- readers

    private static Dictionary<string, HashSet<string>> Shipped()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Goldens", "imprint-cai-widgets.json");
        var raw = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(path))!;
        return raw.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    /// <summary>Every (page, island, prop) the standard composes for a corpus that clears every gate.</summary>
    private static List<(string Page, string Tag, string Prop)> Islands()
    {
        var found = new List<(string, string, string)>();

        foreach (var page in EveryPage())
        {
            var json = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(page.Node));
            Walk(json, node =>
            {
                if (!node.TryGetProperty("type", out var type) || type.GetString() != "widget")
                {
                    return;
                }

                var tag = node.GetProperty("tag").GetString()!;
                if (!node.TryGetProperty("props", out var props))
                {
                    return;
                }

                foreach (var prop in props.EnumerateObject())
                {
                    found.Add((page.Path, tag, prop.Name));
                }
            });
        }

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

    /// <summary>
    /// Every page the standard can publish, over a corpus shaped to clear every gate.
    /// </summary>
    /// <remarks>
    /// ★ A GATE NOT CLEARED IS A PAGE NOT BUILT, AND A PAGE NOT BUILT IS AN ISLAND NOT CHECKED. The corpus
    /// carries enough C# for a field guide, too little Go for one, enough codebases in one country, an
    /// advisory seen often enough for a page, and a history long enough to draw a line.
    /// </remarks>
    private static List<SurveyPage> EveryPage()
    {
        var records = Corpus();
        var pages = new List<SurveyPage>();

        pages.AddRange(records.Select(SurveyPageBuilder.Build).OfType<SurveyPage>());
        pages.Add(SurveyIndexBuilder.Build(records, TakenAt));
        pages.AddRange(SurveyCorpus.Fields(records).Select(FieldGuideBuilder.Build).OfType<SurveyPage>());
        pages.Add(CorpusSheetBuilder.Build(records, TakenAt, History()));
        pages.AddRange(CorpusGroupPages.Build(records, TakenAt));
        pages.AddRange(CorpusAdvisoryPages.Build(CorpusReading.From(records, TakenAt), TakenAt));

        return pages;
    }

    private static List<DatedReading> History() =>
    [
        new(TakenAt.AddDays(-30), 6, 49.8),
        new(TakenAt.AddDays(-14), 8, 51.2),
        new(TakenAt, 12, 52.6),
    ];

    private static List<SurveyRecord> Corpus()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Enumerable.Range(0, 8).Select(i => Record("acme", $"service-{i}", "csharp", 60 + i, i < 4)));
        records.AddRange(Enumerable.Range(0, 4).Select(i => Record("gopher", $"tool-{i}", "go", 40 + i, false)));
        return records;
    }

    private static SurveyRecord Record(string owner, string name, string language, double score, bool advisory)
    {
        // ★ TWO READINGS EACH, so the portrait draws its trend: a one-reading subject has no trend island,
        //   and an island that is never composed is an island this test cannot check.
        DeliveryPayload Reading(int ordinal, string scannedAt, double cai) => new()
        {
            DeliveryId = $"cd_{owner}_{name}_{ordinal}",
            IssuedAt = scannedAt,
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = $"{owner}/{name}",
                Host = "github.com",
                Commit = "3f9a1c2",
                Languages = new SubjectLanguages { Primary = language, Secondary = ["typescript"] },
                Origin = new SubjectOrigin { Country = "Denmark", Declared = true, ResolvedBy = "countryName" },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = scannedAt, ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict
            {
                Cai = cai,
                Band = "Adequate",
                Lenses = [new DeliveryLens { Lens = "codeHealth", Score = cai, Band = "Adequate" }],
            },
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
                    SecretsHistoryAffected = advisory,
                    SupplyChainMeasurable = true,
                    Sbom = !advisory,
                    AdvisoriesRead = true,
                    AdvisoryListComplete = true,
                    Advisories = advisory
                        ? [new VisibleAdvisory { AdvisoryId = "CVE-2026-0001", Package = "left-pad", PackageVersion = "1.3.0" }]
                        : [],
                },
            },
        };

        return SurveyRecord.From(
            [Reading(0, "2026-06-30T09:00:00Z", score - 5), Reading(1, "2026-08-30T09:00:00Z", score)]);
    }
}
