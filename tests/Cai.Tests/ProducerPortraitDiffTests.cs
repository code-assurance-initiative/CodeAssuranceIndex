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
        Assert.Equal(Anchors(Producer()), Anchors(Standard()));
    }

    /// <summary>★ The same islands, with the same tags — a widget is a contract with the site's theme.</summary>
    [Fact]
    public void The_standard_composes_the_same_islands()
    {
        Assert.Equal(Widgets(Producer()), Widgets(Standard()));
    }

    /// <summary>
    /// ★ The stat band is what a reader sees first, and it is where a lost figure is least visible.
    /// </summary>
    [Fact]
    public void The_standard_states_the_same_figures_in_the_stat_band()
    {
        // Every cell but the headline, which differs for a reason the owner must rule on — see below.
        Assert.Equal(Stats(Producer()).Skip(1), Stats(Standard()).Skip(1));
    }

    /// <summary>
    /// ★★ THE ONE DIFFERENCE THAT CHANGES A PUBLISHED NUMBER, HELD HERE SO IT CANNOT DRIFT SILENTLY.
    /// </summary>
    /// <remarks>
    /// <para>The producer publishes a repository's PEAK measurement as the face of its page: a regression is
    /// never promoted to the current score, so a page can show 72.4 while the newest reading on its own
    /// trend line is 71.0. The standard publishes the LATEST reading, and says so in the words beside it —
    /// "its most recent published measurement, taken on …".</para>
    /// <para>★★ THEY ARE DIFFERENT CLAIMS AND ONLY ONE CAN BE ON THE PAGE. This is not a port defect and it
    /// is not the implementer's to decide: it changes the number every regressed codebase publishes. It is
    /// written down here, and in the plan, for the owner to rule on. Whichever way it goes, this test is
    /// what stops the two quietly diverging in the meantime.</para>
    /// </remarks>
    [Fact]
    public void The_headline_differs_because_the_producer_publishes_the_peak_and_the_standard_the_latest()
    {
        var producer = Stats(Producer())[0];
        var standard = Stats(Standard())[0];

        Assert.StartsWith("72.4 |", producer, StringComparison.Ordinal);
        Assert.StartsWith("71 |", standard, StringComparison.Ordinal);

        // And the difference is exactly peak-versus-latest, not a rounding or a different fold: the peak is
        // in the sequence, and it is not the last reading in it.
        var readings = Deliveries().Select(d => d.Verdict.Cai).ToList();
        Assert.Equal(72.4, readings.Max());
        Assert.Equal(71.0, readings[^1]);
    }

    /// <summary>★ And the page is at the same address, under the same title.</summary>
    [Fact]
    public void The_standard_publishes_it_at_the_same_address()
    {
        var producer = Producer();

        Assert.Equal(producer.GetProperty("Path").GetString(), Page().Path);
        Assert.Equal(producer.GetProperty("Title").GetString(), Page().Title);
    }

    // ---------------------------------------------------------------- readers

    /// <summary>Every section's anchor, in document order — null for an unanchored one.</summary>
    private static List<string> Anchors(JsonElement page) => Sections(Node(page));

    private static List<string> Sections(JsonElement node)
    {
        var found = new List<string>();
        Walk(node, n =>
        {
            if (n.TryGetProperty("type", out var t) && t.GetString() == "section")
            {
                // ★ THE APPEARANCE TOO, NOT ONLY THE ANCHOR. An appearance is what the theme styles the
                //   section BY, so a section carrying the right anchor and the wrong appearance is an
                //   unstyled block at the right address — invisible to a test that reads only anchors, and
                //   exactly what a hand port gets wrong.
                var anchor = n.TryGetProperty("anchor", out var a) && a.ValueKind == JsonValueKind.String
                    ? a.GetString()!
                    : "-";
                var appearance = n.TryGetProperty("appearance", out var ap) && ap.ValueKind == JsonValueKind.String
                    ? ap.GetString()!
                    : "-";
                found.Add($"{appearance}/{anchor}");
            }
        });
        return found;
    }

    private static List<string> Widgets(JsonElement page)
    {
        var found = new List<string>();
        Walk(Node(page), n =>
        {
            if (n.TryGetProperty("type", out var t) && t.GetString() == "widget"
                && n.TryGetProperty("tag", out var tag))
            {
                found.Add(tag.GetString()!);
            }
        });
        return found;
    }

    /// <summary>The stat band's cells, as the pairs a reader sees: the figure and the line beneath it.</summary>
    private static List<string> Stats(JsonElement page)
    {
        var found = new List<string>();
        Walk(Node(page), n =>
        {
            if (!n.TryGetProperty("type", out var t) || t.GetString() != "stack")
            {
                return;
            }

            string? figure = null;
            string? label = null;
            foreach (var child in n.GetProperty("children").EnumerateArray())
            {
                var kind = child.GetProperty("type").GetString();
                if (kind == "heading")
                {
                    figure = child.GetProperty("text").GetString();
                }
                else if (kind == "richtext")
                {
                    label = child.GetProperty("html").GetString();
                }
            }

            if (figure is not null)
            {
                found.Add($"{figure} | {label}");
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

    private static JsonElement Node(JsonElement page) => page.GetProperty("Node");

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
