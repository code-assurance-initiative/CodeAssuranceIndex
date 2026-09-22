using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Phase 6 step 1, continued: the surveys index and a language field guide, diffed against the
/// producer's own goldens.
/// </summary>
/// <remarks>
/// ★ THE INDEX AND A GUIDE ARE DIFFED TOGETHER, because the defect they were both rewritten to prevent
/// lives BETWEEN them — an index that linked every language that merely slugified, to pages nobody built.
/// A golden over either alone cannot see it, and neither can a diff.
/// </remarks>
public sealed class ProducerCatalogueDiffTests
{
    [Fact]
    public void The_index_has_the_same_sections_in_the_same_order()
    {
        Assert.Equal(PageShape.Sections(Producer("index")), PageShape.Sections(Standard(Index())));
    }

    [Fact]
    public void The_index_states_the_same_figures()
    {
        Assert.Equal(PageShape.Stats(Producer("index")), PageShape.Stats(Standard(Index())));
    }

    [Fact]
    public void The_index_carries_the_same_islands()
    {
        Assert.Equal(PageShape.Widgets(Producer("index")), PageShape.Widgets(Standard(Index())));
    }

    [Fact]
    public void The_guide_has_the_same_sections()
    {
        Assert.Equal(PageShape.Sections(Producer("guide-csharp")), PageShape.Sections(Standard(Guide())));
    }

    /// <summary>
    /// The guide's figures, cell for cell — with the vocabulary substitution named below applied.
    /// </summary>
    [Fact]
    public void The_guide_states_the_same_figures()
    {
        var producer = PageShape.Stats(Producer("guide-csharp"))
            .Select(cell => cell.Replace("projects", "codebases", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(producer, PageShape.Stats(Standard(Guide())));
    }

    /// <summary>★★ Below the gate, both publishers build nothing at all — the one function, twice.</summary>
    [Fact]
    public void A_language_below_the_gate_gets_no_page_from_either_publisher()
    {
        Assert.Equal(JsonValueKind.Null, Producer("guide-go-below-the-gate").ValueKind);

        var go = SurveyCorpus.Fields(Corpus()).Single(f => f.Language == "go");
        Assert.Null(FieldGuideBuilder.Build(go));
    }

    /// <summary>
    /// ★★ ONE DELIBERATE WORDING DIFFERENCE, NAMED SO IT CANNOT DRIFT. The producer's index heading was
    /// rewritten from "projects" to "codebases" — a claim about the CONTENTS of the corpus, which had to
    /// move the moment the contents did — and its field guide's title was not, so the two disagree on one
    /// site. The standard says "codebases" on both.
    /// </summary>
    [Fact]
    public void The_guide_says_codebases_where_the_producers_still_says_projects()
    {
        // The whole guide, not only its title: the lede and the stat band say it too.
        Assert.Contains(
            "measured projects",
            JsonSerializer.Serialize(Producer("guide-csharp")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "measured projects",
            JsonSerializer.Serialize(Standard(Guide())),
            StringComparison.Ordinal);

        Assert.Equal("Measured codebases", Producer("index").GetProperty("Title").GetString());
        Assert.Equal("C# projects, measured", Producer("guide-csharp").GetProperty("Title").GetString());

        Assert.Equal("Measured codebases", Index().Title);
        Assert.Equal("C# codebases, measured", Guide().Title);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static JsonElement Producer(string key)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Goldens", "producer-survey-catalogue.json");
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path)).GetProperty(key);
    }

    private static JsonElement Standard(SurveyPage page) =>
        JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            page.Path,
            page.Title,
            page.MetaDescription,
            page.Node,
        }));

    private static SurveyPage Index() => SurveyIndexBuilder.Build(Corpus(), TakenAt);

    private static SurveyPage Guide() =>
        FieldGuideBuilder.Build(SurveyCorpus.Fields(Corpus()).Single(f => f.Language == "csharp"))!;

    /// <summary>The same corpus the producer's golden was taken over: six C# and four Go.</summary>
    private static List<SurveyRecord> Corpus()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Enumerable.Range(0, 6).Select(i => Record("acme", $"service-{i}", "csharp", 60 + i)));
        records.AddRange(Enumerable.Range(0, 4).Select(i => Record("gopher", $"tool-{i}", "go", 40 + i)));
        return records;
    }

    private static SurveyRecord Record(string owner, string name, string language, double score) =>
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
                Languages = new SubjectLanguages { Primary = language, Secondary = ["typescript"] },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z", ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict { Cai = score, Band = "Adequate" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                ProductionLoc = 1500,
                Dimensions = [.. Enumerable.Range(1, 18).Select(d => new DimensionScore($"D{d}", "code-quality", 7.5, 0.95))],
            },
        }]);
}
