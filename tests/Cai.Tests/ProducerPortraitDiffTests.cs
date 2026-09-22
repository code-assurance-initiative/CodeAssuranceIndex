using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Phase 6 step 1: the standard's survey portrait, diffed against the producer's, over one subject.
/// </summary>
/// <remarks>
/// <para>★★ THE DIFFERENCES ARE NAMED, NOT COUNTED. The owner asked that the moved pages be
/// pixel-perfect the same as before; a structural diff is what has to pass first, because a pixel diff
/// over a page whose sections moved tells you only that it moved. Every difference below is either
/// closed or written down here with the reason it stands — nothing is waved through as "close
/// enough".</para>
/// <para>★ THE GOLDEN IS THE PRODUCER'S OWN OUTPUT, captured in its repository while it was still the
/// only implementation (<c>kennel/tests/Kennel.PublicCorpus.Tests/SurveyPortraitGoldenTests.cs</c>) and
/// copied here verbatim. Regenerating it is a deliberate act in the other repository, so this test
/// cannot be made to pass by editing what it compares against.</para>
/// </remarks>
public sealed class ProducerPortraitDiffTests
{
    /// <summary>
    /// The shape of the producer's portrait, section by section — what the standard must reproduce.
    /// </summary>
    [Fact]
    public void The_standard_composes_the_same_sections_in_the_same_order()
    {
        Assert.Equal(PageShape.Sections(Producer()), PageShape.Sections(Standard()));
    }

    /// <summary>★ The same islands, with the same tags — a widget is a contract with the site's theme.</summary>
    [Fact]
    public void The_standard_composes_the_same_islands()
    {
        Assert.Equal(PageShape.Widgets(Producer()), PageShape.Widgets(Standard()));
    }

    /// <summary>
    /// ★ The stat band is what a reader sees first, and it is where a lost figure is least visible.
    /// </summary>
    [Fact]
    public void The_standard_states_the_same_figures_in_the_stat_band()
    {
        // Every cell but the headline, which differs for a reason the owner must rule on — see below.
        Assert.Equal(PageShape.Stats(Producer()).Skip(1), PageShape.Stats(Standard()).Skip(1));
    }

    /// <summary>
    /// ★★ RULED BY THE OWNER, 2026-09-22: PUBLISH ALWAYS, SHOW THE LATEST. The standard's reading is the
    /// correct one and the producer's is the legacy.
    /// </summary>
    /// <remarks>
    /// <para>The producer publishes a repository's PEAK measurement as the face of its page: a regression is
    /// never promoted to the current score, so it shows 72.4 while the newest reading on its own trend line
    /// is 71.0. The owner's words: <i>"it was peak once, today, it should publish always, and show
    /// latest."</i></para>
    /// <para>★ THE TEST STAYS, INVERTED IN MEANING. It no longer holds an open question; it records that the
    /// difference is deliberate and which way it was settled, so a later reader diffing these two pages finds
    /// the ruling instead of rediscovering the question. The standard's page also SAYS which reading it is —
    /// "its most recent published measurement, taken on …" — which is the half that makes the number
    /// checkable.</para>
    /// </remarks>
    [Fact]
    public void The_standard_shows_the_latest_reading_where_the_producer_showed_the_peak()
    {
        var producer = PageShape.Stats(Producer())[0];
        var standard = PageShape.Stats(Standard())[0];

        Assert.StartsWith("72.4 |", producer, StringComparison.Ordinal);
        Assert.StartsWith("71 |", standard, StringComparison.Ordinal);

        // And the difference is exactly peak-versus-latest, not a rounding or a different fold: the peak is
        // in the sequence, and it is not the last reading in it.
        var readings = Deliveries().Select(d => d.Verdict.Cai).ToList();
        Assert.Equal(72.4, readings.Max());
        Assert.Equal(71.0, readings[^1]);

        // ★ And the page says WHICH reading it is, beside the number. A latest-reading headline that did not
        //   say so would be the peak's claim with a different value in it.
        Assert.Contains(
            "most recent published measurement",
            JsonSerializer.Serialize(Standard()),
            StringComparison.Ordinal);
    }

    /// <summary>★ And the page is at the same address, under the same title.</summary>
    [Fact]
    public void The_standard_publishes_it_at_the_same_address()
    {
        var producer = Producer();

        Assert.Equal(producer.GetProperty("Path").GetString(), Page().Path);
        Assert.Equal(producer.GetProperty("Title").GetString(), Page().Title);
    }

    // ---------------------------------------------------------------- fixtures

    private static JsonElement Producer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Goldens", "producer-survey-portrait.json");
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
    }

    private static JsonElement Standard() =>
        JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            Page().Path,
            Page().Title,
            Page().MetaDescription,
            Page().Node,
        }));

    private static SurveyPage Page() => SurveyPageBuilder.Build(SurveyRecord.From(Deliveries()))!;

    /// <summary>
    /// The same subject the producer's golden was taken over, as the deliveries that carry it.
    /// </summary>
    /// <remarks>
    /// ★ FOUR READINGS, because the golden's card carries four and the portrait's trend, its
    /// "measurements over time" count and its dates are all read off the sequence.
    /// </remarks>
    private static IReadOnlyList<DeliveryPayload> Deliveries()
    {
        double[] history = [61.2, 68.1, 72.4, 71.0];
        string[] scanned =
        [
            "2026-05-02T09:00:00Z", "2026-06-15T09:00:00Z", "2026-07-20T09:00:00Z", "2026-08-30T09:00:00Z",
        ];

        return [.. history.Select((score, i) => new DeliveryPayload
        {
            DeliveryId = $"cd_portrait_{i}",
            IssuedAt = scanned[i],
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = "acme/checkout-api",
                Host = "github.com",
                Commit = "3f9a1c2",
                Languages = new SubjectLanguages { Primary = "csharp", Secondary = ["typescript"] },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = scanned[i], ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict
            {
                Cai = score,
                Band = "Strong",
                Lenses =
                [
                    new DeliveryLens { Lens = "codeHealth", Score = 74.1, Band = "Strong" },
                    new DeliveryLens { Lens = "architecture", Score = 69.8, Band = "Adequate" },
                    new DeliveryLens { Lens = "maturity", Score = 70.2, Band = "Strong" },
                    new DeliveryLens { Lens = "productionReadiness", Score = 66.5, Band = "Adequate" },
                    new DeliveryLens { Lens = "securityCompliance", Score = 80.0, Band = "Strong" },
                ],
            },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                ProductionLoc = 1500,
                Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
            },
        })];
    }
}
