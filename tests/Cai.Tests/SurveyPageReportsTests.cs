using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A survey page links the public reports written about its measurement, each under the mark of the system
/// that wrote it, in the standard's words (decision 0003).
/// </summary>
/// <remarks>
/// <para>★★ THE CARDS ARE THE STANDARD'S, THE LINKS ARE THE PRODUCER'S. A producer supplies a signed URL and
/// the id of a listed system; the wording, the shape and the mark come from this repository. So a producer
/// can point a reader at its report and nowhere else.</para>
/// <para>★★ SEVERAL SYSTEMS, ONE PAGE. One engine measures and several products built on it may each write
/// a report; a second engine may measure the same code. Each producer's newest delivery speaks for it.</para>
/// </remarks>
public sealed class SurveyPageReportsTests
{
    private const string Watchdog = "watchdog.canine.dev";
    private const string WatchdogReport = "https://watchdog.canine.dev/api/oss/Acme/checkout-api/report?run=9d3c";

    private static readonly ProducerListing Listing = ProducerListing.Parse(
        """
        {
          "producers": [
            {
              "producer": "watchdog.canine.dev",
              "name": "Watchdog",
              "homepage": "https://watchdog.canine.dev/",
              "mark": "watchdog",
              "systems": [
                { "id": "watchdog", "name": "Watchdog", "homepage": "https://watchdog.canine.dev/", "mark": "watchdog" }
              ]
            },
            {
              "producer": "cca.example.org",
              "name": "Example Analyzer",
              "homepage": "https://cca.example.org/",
              "mark": "doc",
              "systems": [
                { "id": "assert", "name": "Assert", "homepage": "https://assert.example.org/", "mark": "doc" },
                { "id": "assure", "name": "Assure", "homepage": "https://assure.example.org/", "mark": "doc" }
              ]
            }
          ]
        }
        """);

    [Fact]
    public void A_listed_producers_report_is_linked_under_its_systems_mark()
    {
        var cards = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z", Report(WatchdogReport, "watchdog"))));

        var report = Assert.Single(cards, c => c.Href == WatchdogReport);
        Assert.Equal("The full report by Watchdog", report.Label);
        Assert.Equal("watchdog", report.Icon);
        Assert.Contains("1 October 2026", report.Note, StringComparison.Ordinal);

        var measured = Assert.Single(cards, c => c.Label == "Measured by Watchdog");
        Assert.Equal("https://watchdog.canine.dev/", measured.Href);
        Assert.Equal("watchdog", measured.Icon);
    }

    /// <summary>★ The order is an argument: the code, what was found in it, who found it, then the standard's own routes.</summary>
    [Fact]
    public void The_report_comes_after_the_source_and_before_the_standards_own_routes()
    {
        var labels = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z", Report(WatchdogReport, "watchdog"))))
            .Select(c => c.Label).ToList();

        Assert.Equal(
            ["The project's source repository", "The full report by Watchdog", "Measured by Watchdog",
             "How this project compares", "Verify this score yourself"],
            labels);
    }

    /// <summary>★★ No report, no cards, and no word about a missing one: not publishing is a legitimate choice.</summary>
    [Fact]
    public void A_delivery_without_reports_publishes_the_page_it_always_did()
    {
        var cards = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z")));

        Assert.Equal(
            ["The project's source repository", "How this project compares", "Verify this score yourself"],
            cards.Select(c => c.Label).ToList());
    }

    /// <summary>★★ An unlisted producer cannot put a link on the standard's page, whatever it signs.</summary>
    [Fact]
    public void An_unlisted_producers_report_is_not_linked()
    {
        var cards = Cards(Record(Delivery(
            "stranger.example.net", "2026-10-01T10:00:00Z", Report("https://stranger.example.net/r/1", "stranger"))));

        Assert.DoesNotContain(cards, c => c.Href == "https://stranger.example.net/r/1");
        Assert.DoesNotContain(cards, c => c.Label.StartsWith("Measured by", StringComparison.Ordinal));
    }

    /// <summary>★★ A producer may name only its own listed systems: no borrowing another vendor's mark.</summary>
    [Fact]
    public void A_report_naming_a_system_the_producer_is_not_listed_for_is_not_linked()
    {
        var cards = Cards(Record(Delivery(
            Watchdog, "2026-10-01T10:00:00Z", Report("https://assert.example.org/r/1", "assert"))));

        Assert.DoesNotContain(cards, c => c.Href == "https://assert.example.org/r/1");
    }

    /// <summary>★★ The URL must live where the system lives, so the mark on the card is true of the destination.</summary>
    [Theory]
    [InlineData("https://evil.example.com/api/oss/Acme/checkout-api/report")]
    [InlineData("https://watchdog.canine.dev.evil.example.com/report")]
    [InlineData("http://watchdog.canine.dev/api/oss/Acme/checkout-api/report")]
    public void A_report_hosted_away_from_its_system_is_not_linked(string url)
    {
        var cards = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z", Report(url, "watchdog"))));

        Assert.DoesNotContain(cards, c => c.Href == url);
        Assert.DoesNotContain(cards, c => c.Label.StartsWith("Measured by", StringComparison.Ordinal));
    }

    [Fact]
    public void A_subdomain_of_the_systems_home_is_its_own()
    {
        const string preview = "https://app.watchdog.canine.dev/api/oss/Acme/checkout-api/report";
        var cards = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z", Report(preview, "watchdog"))));

        Assert.Contains(cards, c => c.Href == preview);
    }

    /// <summary>★★ One engine, several products, one measurement: every report gets its own card and mark.</summary>
    [Fact]
    public void Every_system_that_wrote_a_report_gets_a_card_and_the_engine_gets_one()
    {
        var cards = Cards(Record(Delivery(
            "cca.example.org", "2026-10-01T10:00:00Z",
            Report("https://assert.example.org/r/1", "assert"),
            Report("https://assure.example.org/r/1", "assure"))));

        Assert.Contains(cards, c => c.Label == "The full report by Assert" && c.Href == "https://assert.example.org/r/1");
        Assert.Contains(cards, c => c.Label == "The full report by Assure" && c.Href == "https://assure.example.org/r/1");
        Assert.Single(cards, c => c.Label == "Measured by Example Analyzer");
    }

    /// <summary>
    /// ★★ TWO ENGINES ON ONE SUBJECT: each speaks through its OWN newest delivery. The page's number is the
    /// latest reading; a reader who wants the other engine's findings can still reach them.
    /// </summary>
    [Fact]
    public void Two_producers_each_link_the_report_of_their_own_newest_delivery()
    {
        var cards = Cards(Record(
            Delivery(Watchdog, "2026-09-01T10:00:00Z", Report(WatchdogReport + "-old", "watchdog")),
            Delivery(Watchdog, "2026-09-20T10:00:00Z", Report(WatchdogReport, "watchdog")),
            Delivery("cca.example.org", "2026-10-01T10:00:00Z", Report("https://assert.example.org/r/1", "assert"))));

        Assert.Contains(cards, c => c.Href == WatchdogReport);
        Assert.DoesNotContain(cards, c => c.Href == WatchdogReport + "-old");
        Assert.Contains(cards, c => c.Href == "https://assert.example.org/r/1");
        Assert.Single(cards, c => c.Label == "Measured by Watchdog");
        Assert.Single(cards, c => c.Label == "Measured by Example Analyzer");
    }

    /// <summary>★ A producer whose newest delivery withdrew its report has no report on the page.</summary>
    [Fact]
    public void A_producers_newest_delivery_speaks_for_it_even_when_it_carries_no_report()
    {
        var cards = Cards(Record(
            Delivery(Watchdog, "2026-09-01T10:00:00Z", Report(WatchdogReport, "watchdog")),
            Delivery(Watchdog, "2026-10-01T10:00:00Z")));

        Assert.DoesNotContain(cards, c => c.Href == WatchdogReport);
    }

    /// <summary>
    /// ★★ NEUTRALITY HOLDS WITH REPORTS. Two listed producers sending one report each publish the same cards,
    /// differing only in the names, the links and the marks, which are the listing's.
    /// </summary>
    [Fact]
    public void Two_listed_producers_get_the_same_cards_but_for_their_own_names()
    {
        var a = Cards(Record(Delivery(Watchdog, "2026-10-01T10:00:00Z", Report(WatchdogReport, "watchdog"))));
        var b = Cards(Record(Delivery(
            "cca.example.org", "2026-10-01T10:00:00Z", Report("https://assert.example.org/r/1", "assert"))));

        Assert.Equal(a.Count, b.Count);
        Assert.Equal(
            a.Select(c => c.Note.Replace("Watchdog", "X", StringComparison.Ordinal)),
            b.Select(c => c.Note.Replace("Assert", "X", StringComparison.Ordinal)
                                .Replace("Example Analyzer", "X", StringComparison.Ordinal)));
    }

    /// <summary>★ The listing this repository publishes parses, and every destination in it is https.</summary>
    [Fact]
    public void The_published_listing_parses_and_names_only_https_destinations()
    {
        var listing = ProducerListing.Published;

        Assert.NotEmpty(listing.Producers);
        foreach (var producer in listing.Producers)
        {
            Assert.StartsWith("https://", producer.Homepage, StringComparison.Ordinal);
            Assert.NotEmpty(producer.Systems);
            Assert.All(producer.Systems, s => Assert.StartsWith("https://", s.Homepage, StringComparison.Ordinal));
        }

        Assert.Equal("Watchdog", listing.Find(Watchdog)?.Name);
    }

    /// <summary>★ A listing missing what a card needs fails loudly, at load, rather than printing an empty card.</summary>
    [Fact]
    public void A_listing_entry_without_a_name_is_refused()
    {
        Assert.ThrowsAny<Exception>(() => ProducerListing.Parse(
            """{ "producers": [ { "producer": "x", "homepage": "https://x/", "mark": "doc", "systems": [] } ] }"""));
    }

    // ---------------------------------------------------------------- fixtures

    private sealed record Card(string Icon, string Label, string Note, string Href);

    private static List<Card> Cards(SurveyRecord record)
    {
        var page = SurveyPageBuilder.Build(record, Listing);
        Assert.NotNull(page);

        var links = FindLinks(JsonSerializer.SerializeToElement(page.Node));
        Assert.NotNull(links);
        return JsonSerializer.Deserialize<JsonElement>(links!).EnumerateArray()
            .Select(l => new Card(
                l.GetProperty("icon").GetString() ?? "",
                l.GetProperty("label").GetString() ?? "",
                l.GetProperty("note").GetString() ?? "",
                l.GetProperty("href").GetString() ?? ""))
            .ToList();
    }

    /// <summary>The <c>links</c> prop of the page's <c>cai-link-cards</c> widget, wherever the tree put it.</summary>
    private static string? FindLinks(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                if (node.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.String)
                {
                    return links.GetString();
                }

                foreach (var property in node.EnumerateObject())
                {
                    if (FindLinks(property.Value) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    if (FindLinks(item) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    private static DeliveryReport Report(string url, string system) => new() { Url = url, System = system };

    private static SurveyRecord Record(params DeliveryPayload[] deliveries) => SurveyRecord.From(deliveries);

    private static DeliveryPayload Delivery(string producer, string at, params DeliveryReport[] reports) => new()
    {
        DeliveryId = $"cd_{producer}_{at}",
        IssuedAt = at,
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject { Repository = "Acme/checkout-api", Host = "github.com", Commit = at },
        Producer = new DeliveryProducer { Name = producer, Scanner = "surveyor", ScannerVersion = "1.0" },
        Measurement = new DeliveryMeasurement { ProductionLoc = 1500, ScannedAt = at },
        Verdict = new DeliveryVerdict { Cai = 72.4, Band = "Strong" },
        Evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 1500,
            Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
        },
        Reports = reports.Length > 0 ? reports : null,
    };
}
