using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The question this whole move exists to answer: if a SECOND producer publishes to the standard, does
/// its page render like the first one's?
/// </summary>
/// <remarks>
/// <para>★★ THIS TEST IS WRITTEN BEFORE THE BUILDER AND IS EXPECTED TO FAIL. Until the standard composes
/// its own pages, the answer is no: a producer sends a NODE TREE — sections, widgets, URLs, titles and
/// prose — and the CMS only styles it. Two producers would publish structurally different pages under one
/// standard, and the shared theme would make them look like one site while they were never one page.</para>
/// <para>★ WHAT "THE SAME" MEANS HERE. Everything except who is credited. The address, the structure, the
/// headings, the widgets and the words must be identical; the producer's name, scanner and scanner
/// version are the ONLY things allowed to differ, because attribution is the one fact that genuinely is
/// the producer's.</para>
/// <para>See <c>docs/plans/cai-owns-its-pages.md</c>.</para>
/// </remarks>
public sealed class SurveyPageProducerNeutralityTests
{
    private const string Subject = "acme/checkout-api";

    [Fact]
    public void Two_producers_measuring_the_same_subject_publish_the_same_page()
    {
        var first = Record("watchdog.canine.dev", "watchdog-surveyor", "3.1.0");
        var second = Record("assay.example.org", "other-surveyor", "0.9.2");

        var a = SurveyPageBuilder.Build(first);
        var b = SurveyPageBuilder.Build(second);

        Assert.Equal(a.Path, b.Path);
        Assert.Equal(a.Title, b.Title);
        Assert.Equal(a.MetaDescription, b.MetaDescription);
        Assert.Equal(Anonymised(a, first), Anonymised(b, second));
    }

    [Fact]
    public void The_address_is_the_standards_own_and_names_the_subject_not_the_producer()
    {
        var page = SurveyPageBuilder.Build(Record("watchdog.canine.dev", "watchdog-surveyor", "3.1.0"));

        Assert.StartsWith(SurveyPageBuilder.Root + "/", page.Path, StringComparison.Ordinal);
        Assert.Contains("checkout-api", page.Path, StringComparison.Ordinal);
        Assert.DoesNotContain("watchdog", page.Path, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ★ AND THE PRODUCER IS NAMED ON THE PAGE. Neutral does not mean anonymous: a reader is entitled to
    /// know who took the measurement, which is exactly why attribution is the one permitted difference.
    /// </summary>
    [Fact]
    public void The_page_says_who_measured_it()
    {
        var page = SurveyPageBuilder.Build(Record("assay.example.org", "other-surveyor", "0.9.2"));

        Assert.Contains("assay.example.org", Json(page.Node), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static SurveyRecord Record(string producer, string scanner, string scannerVersion)
    {
        // Two readings, so the page has a trajectory to draw — and so this test would catch a builder
        // that took the climb from the producer instead of deriving it from the sequence.
        var earlier = Payload(producer, scanner, scannerVersion, "2026-05-02T09:00:00Z", 6.4);
        var latest = Payload(producer, scanner, scannerVersion, "2026-07-01T10:32:04Z", 7.5);
        return SurveyRecord.From([earlier, latest]);
    }

    private static DeliveryPayload Payload(
        string producer, string scanner, string scannerVersion, string issuedAt, double codeHealth)
    {
        var evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            Commit = "3f9a1c2",
            QualityBar = "production",
            AnalyzableProjects = 3,
            ProductionLoc = 1500,
            Dimensions =
            [
                new DimensionScore("D1", "code-quality", codeHealth, 0.95),
                new DimensionScore("D3", "code-quality", 8.2, 0.95),
                new DimensionScore("D5", "architecture", 7.1, 0.95),
                new DimensionScore("D9", "testing", 7.0, 0.85),
                new DimensionScore("D30", "security", 7.6, 0.90),
            ],
        };

        var request = new DeliveryBuildRequest
        {
            DeliveryId = $"cd_{producer}_{issuedAt}",
            IssuedAt = issuedAt,
            Subject = new DeliverySubject { Repository = Subject, Commit = "3f9a1c2", Host = "github.com" },
            Producer = new DeliveryProducer { Name = producer, Scanner = scanner, ScannerVersion = scannerVersion },
        };

        return DeliveryTestHelp.Build(evidence, request);
    }

    /// <summary>The page with every trace of WHO measured it replaced, so only the rest can differ.</summary>
    private static string Anonymised(SurveyPage page, SurveyRecord record)
    {
        var producer = record.Latest.Producer;
        var json = Json(page.Node);
        json = json.Replace(producer.Name, "«producer»", StringComparison.Ordinal);
        if (producer.Scanner is { Length: > 0 } scanner)
        {
            json = json.Replace(scanner, "«scanner»", StringComparison.Ordinal);
        }

        if (producer.ScannerVersion is { Length: > 0 } version)
        {
            json = json.Replace(version, "«scannerVersion»", StringComparison.Ordinal);
        }

        // The delivery id carries the producer in this fixture; it is provenance, not page content.
        return json.Replace(record.Latest.DeliveryId, "«deliveryId»", StringComparison.Ordinal);
    }

    private static string Json(IReadOnlyDictionary<string, object?> node) =>
        JsonSerializer.Serialize(node, new JsonSerializerOptions { WriteIndented = false });
}
