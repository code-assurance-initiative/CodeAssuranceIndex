using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A count of one is never published under a plural noun.
/// </summary>
/// <remarks>
/// <para>★★ THIS IS THE DEFECT <see cref="Cai.Pages.Publishing.Basis"/> EXISTS TO MAKE UNWRITABLE, and
/// it came back in the places a stat cell takes its label as a bare string. A corpus holding exactly one
/// of something published "1 languages", "1 advisories", "1 dated readings"; the answer was a type that
/// refuses to be built without both forms, because English pluralisation is not derivable from a suffix
/// and chopping an "s" turns "advisories" into "advisorie".</para>
///
/// <para>★★ AND ONE OF THESE HITS EVERY NEWLY-SURVEYED REPOSITORY. A subject's first delivery is its only
/// delivery, so the very first page published about any codebase read "1 measurements over time" — the
/// most common state a portrait can be in, not an edge case. Found by LOOKING at a rendered field guide,
/// which showed "1 dimensions resolved, typically" beside it.</para>
///
/// <para>★ THE OTHER STAT LABELS ARE SAFE BY THEIR GATES rather than by luck — a field guide needs five
/// codebases and a country page ten — and they are left alone rather than converted, because a basis on a
/// count that cannot be one is ceremony. These two can be one.</para>
/// </remarks>
public sealed class CountsNameTheirOwnPluralTests
{
    /// <summary>★★ The first page ever published about a codebase.</summary>
    [Fact]
    public void A_subject_measured_once_has_one_measurement_over_time()
    {
        var json = Json(SurveyPageBuilder.Build(SurveyRecord.From([Reading("2026-08-30T09:00:00Z", 72.4)])));

        Assert.Contains("measurement over time", json, StringComparison.Ordinal);
        Assert.DoesNotContain("1 measurements", json, StringComparison.Ordinal);
        Assert.DoesNotContain(">measurements over time<", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_subject_measured_twice_has_measurements_over_time()
    {
        var json = Json(SurveyPageBuilder.Build(SurveyRecord.From(
            [Reading("2026-06-30T09:00:00Z", 61.0), Reading("2026-08-30T09:00:00Z", 72.4)])));

        Assert.Contains("measurements over time", json, StringComparison.Ordinal);
    }

    /// <summary>★ A survey that resolved a single dimension is a real, degraded survey.</summary>
    [Fact]
    public void A_field_whose_surveys_resolved_one_dimension_says_dimension()
    {
        var json = Json(FieldGuideBuilder.Build(Field(dimensions: 1)));

        Assert.Contains("dimension resolved", json, StringComparison.Ordinal);
        Assert.DoesNotContain("1 dimensions", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_whose_surveys_resolved_several_says_dimensions()
    {
        var json = Json(FieldGuideBuilder.Build(Field(dimensions: 18)));

        Assert.Contains("dimensions resolved", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE SAME DEFECT ON THE INDEX, MISSED BY MY OWN FIX because the search that found the others was
    /// truncated with `head` and I treated the visible subset as complete. The surveys index states the
    /// corpus-wide depth in exactly the same words, and a corpus whose surveys resolve one dimension
    /// publishes it on the highest-traffic page there is.
    /// </summary>
    [Fact]
    public void The_index_says_dimension_when_the_corpus_resolved_one()
    {
        var json = Json(SurveyIndexBuilder.Build(Corpus(dimensions: 1), TakenAt));

        Assert.Contains("dimension resolved", json, StringComparison.Ordinal);
        Assert.DoesNotContain("1 dimensions", json, StringComparison.Ordinal);
    }

    [Fact]
    public void The_index_says_dimensions_when_the_corpus_resolved_several()
    {
        var json = Json(SurveyIndexBuilder.Build(Corpus(dimensions: 18), TakenAt));

        Assert.Contains("dimensions resolved", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return PageText.Json(page);
    }

    private static List<SurveyRecord> Corpus(int dimensions) =>
        [.. Enumerable.Range(0, 6)
            .Select(i => SurveyRecord.From([Reading("2026-08-30T09:00:00Z", 60 + i, dimensions, i)]))];

    private static LanguageField Field(int dimensions)
    {
        var records = Enumerable.Range(0, 6)
            .Select(i => SurveyRecord.From([Reading("2026-08-30T09:00:00Z", 60 + i, dimensions, i)]))
            .ToList();

        return SurveyCorpus.Fields(records).Single();
    }

    private static DeliveryPayload Reading(string at, double cai, int dimensions = 18, int ordinal = 0) => new()
    {
        DeliveryId = $"cd_plural_{ordinal}_{at}",
        IssuedAt = at,
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject
        {
            Repository = $"acme/service-{ordinal}",
            Host = "github.com",
            Commit = "3f9a1c2",
            Languages = new SubjectLanguages { Primary = "csharp" },
        },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
        Measurement = new DeliveryMeasurement { ScannedAt = at, ProductionLoc = 9000 },
        Verdict = new DeliveryVerdict { Cai = cai, Band = "Strong" },
        Evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 9000,
            Dimensions = [.. Enumerable.Range(1, dimensions)
                .Select(d => new DimensionScore($"D{d}", "code-quality", 7.5, 0.95))],
        },
    };
}
