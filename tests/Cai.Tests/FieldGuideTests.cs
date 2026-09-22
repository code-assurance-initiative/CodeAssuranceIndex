using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The language field guides and the index over them.
/// </summary>
/// <remarks>
/// <para>★★ ONE FUNCTION DECIDES WHETHER A LANGUAGE HAS A GUIDE. In the producer's implementation the
/// index and the builder each decided it, and the index linked every language that merely SLUGIFIED — so
/// a language with four subjects got a link to a page nobody ever built: a 404 reached from the corpus's
/// own front door. Here the decision is <see cref="LanguageField.GuidePath"/>, computed once per field and
/// consumed by both.</para>
/// <para>★★ THE INDEX PUBLISHES TWO MEDIANS AND SAYS THEY ARE TWO. The median of the per-language medians
/// and the median across the subjects themselves are both true readings of one corpus, taken over
/// different populations, and a reader who met one here and the other elsewhere concluded one of them was
/// a lie. That has already happened.</para>
/// </remarks>
public sealed class FieldGuideTests
{
    /// <summary>
    /// ★ Below the minimum a "field" is one or two projects, and a page describing it would be reading a
    /// trend into a coincidence.
    /// </summary>
    [Fact]
    public void A_language_below_the_minimum_gets_no_guide()
    {
        var fields = SurveyCorpus.Fields(Subjects("go", 4, 50));

        var go = Assert.Single(fields);
        Assert.Equal(4, go.Subjects.Count);
        Assert.Null(go.GuidePath);
        Assert.Null(FieldGuideBuilder.Build(go));
    }

    [Fact]
    public void A_language_at_the_minimum_gets_one()
    {
        var go = Assert.Single(SurveyCorpus.Fields(Subjects("go", 5, 50)));

        Assert.Equal("surveys/lang/go", go.GuidePath);
        Assert.NotNull(FieldGuideBuilder.Build(go));
    }

    /// <summary>★★ The 404 guard: every link on the index is a page that was built.</summary>
    [Fact]
    public void The_index_links_exactly_the_guides_that_were_built()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 6, 90));
        records.AddRange(Subjects("go", 4, 30));
        records.AddRange(Subjects("rust", 5, 70));

        var fields = SurveyCorpus.Fields(records);
        var built = fields
            .Select(FieldGuideBuilder.Build)
            .OfType<SurveyPage>()
            .Select(p => $"/{p.Path}/")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var linked = PageText.Hrefs(Json(SurveyIndexBuilder.Build(records, TakenAt)))
            .Where(h => h.Contains("/lang/", StringComparison.Ordinal))
            .OrderBy(h => h, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["/surveys/lang/csharp/", "/surveys/lang/rust/"], built);
        Assert.Equal(built, linked);
    }

    /// <summary>★★ Both readings, each naming the population it was taken over.</summary>
    [Fact]
    public void The_index_publishes_two_medians_each_naming_its_population()
    {
        // csharp: six subjects at 90. go: five at 10,20,30,40,50 — median 30.
        // Across LANGUAGES: median(90, 30) = 60. Across SUBJECTS: the 6th of eleven = 90.
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 6, 90));
        records.AddRange(Subjects("go", [10, 20, 30, 40, 50]));

        var json = Json(SurveyIndexBuilder.Build(records, TakenAt));

        Assert.Contains("median across languages with a field guide", json, StringComparison.Ordinal);
        Assert.Contains("median across measured codebases", json, StringComparison.Ordinal);
        Assert.Contains("60.0", json, StringComparison.Ordinal);
        Assert.Contains("90.0", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ A corpus holding exactly one field guide published "1 languages with a field guide". The label
    /// asks the basis how many there are; it is never a plural phrase typed beside a number.
    /// </summary>
    [Fact]
    public void One_language_with_a_guide_is_named_in_the_singular()
    {
        var json = Json(SurveyIndexBuilder.Build(Subjects("csharp", 5, 80), TakenAt));

        Assert.Contains("language with a field guide", json, StringComparison.Ordinal);
        Assert.DoesNotContain("languages with a field guide", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ The middle half beside the median, for the same reason the board draws the whole distribution: a
    /// median alone cannot distinguish a field clustered around it from one split at both ends.
    /// </summary>
    [Fact]
    public void A_guide_states_the_middle_half_beside_the_median()
    {
        var field = Assert.Single(SurveyCorpus.Fields(Subjects("rust", [10, 20, 30, 40, 50])));

        var json = Json(FieldGuideBuilder.Build(field));

        Assert.Contains("median CAI", json, StringComparison.Ordinal);
        Assert.Contains("30.0", json, StringComparison.Ordinal);
        Assert.Contains("between 20.0 and 40.0", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ A 1.0 delivery names no language. That is a producer saying nothing, not a language called
    /// nothing — it is counted in the corpus and grouped under no field.
    /// </summary>
    [Fact]
    public void Subjects_whose_producer_named_no_language_are_not_grouped_under_a_blank_field()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 5, 80));
        records.AddRange(Subjects(null, 3, 40));

        var fields = SurveyCorpus.Fields(records);

        Assert.Equal(["csharp"], fields.Select(f => f.Language));
        // Still part of the corpus: the count the index leads with is every measured codebase.
        Assert.Contains("8 measured codebases", Json(SurveyIndexBuilder.Build(records, TakenAt)), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static List<SurveyRecord> Subjects(string? language, int count, double score) =>
        Subjects(language, [.. Enumerable.Repeat(score, count)]);

    private static List<SurveyRecord> Subjects(string? language, IReadOnlyList<double> scores) =>
        [.. scores.Select((s, i) => SurveyRecord.From([Reading(language, i, s)]))];

    private static DeliveryPayload Reading(string? language, int ordinal, double score) => new()
    {
        DeliveryId = $"cd_{language ?? "none"}_{ordinal}",
        IssuedAt = "2026-09-01T10:00:00Z",
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject
        {
            Repository = $"acme/{language ?? "unnamed"}-{ordinal}",
            Host = "github.com",
            Commit = "3f9a1c2",
            Languages = language is null ? null : new SubjectLanguages { Primary = language },
        },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
        Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z", ProductionLoc = 1500 },
        Verdict = new DeliveryVerdict { Cai = score, Band = "Strong" },
        Evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 1500,
            Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
        },
    };

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return PageText.Json(page);
    }

}
