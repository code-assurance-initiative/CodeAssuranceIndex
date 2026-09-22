using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The facts about a SUBJECT that a page needs and the standard must therefore receive: when the code was
/// measured, and what it is written in.
/// </summary>
/// <remarks>
/// <para>★★ "MEASURED AT" IS NOT "SIGNED AT", and a page that conflates them tells a reader the wrong
/// date. The standard signs when a delivery is pushed; the producer measured whenever it scanned, which
/// may be days earlier. `measurement.scannedAt` already carries it — the plan's phase-0 mapping listed
/// this as a gap needing a schema change, and it was wrong: the field was there the whole time. Found by
/// reading the payload instead of the plan.</para>
/// <para>★ THE LANGUAGE IS A REAL GAP. Nothing in a 1.0 delivery says what a subject is written in, and
/// the language field guides are grouped by exactly that. It is added additively at MINOR 1.1, so 1.0
/// packages keep verifying and simply group under no language.</para>
/// </remarks>
public sealed class SubjectFactsTests
{
    [Fact]
    public void The_page_dates_the_measurement_not_the_signature()
    {
        // Scanned in May, signed in July. A reader asking "how old is this?" means the scan.
        var page = SurveyPageBuilder.Build(SurveyRecord.From([Reading(
            issuedAt: "2026-07-01T10:32:04Z", scannedAt: "2026-05-02T09:00:00Z")]));

        var json = Json(page);
        Assert.Contains("2 May 2026", json, StringComparison.Ordinal);
        Assert.DoesNotContain("1 July 2026", json, StringComparison.Ordinal);
    }

    [Fact]
    public void With_no_scan_time_the_page_falls_back_to_the_signature_and_says_nothing_it_cannot_support()
    {
        var page = SurveyPageBuilder.Build(SurveyRecord.From([Reading(
            issuedAt: "2026-07-01T10:32:04Z", scannedAt: null)]));

        Assert.Contains("1 July 2026", Json(page), StringComparison.Ordinal);
    }

    /// <summary>★ Additive at MINOR 1.1: a 1.0 delivery still parses, and simply has no language.</summary>
    [Fact]
    public void A_subject_may_name_the_language_it_is_written_in()
    {
        var subject = new DeliverySubject
        {
            Repository = "acme/checkout-api",
            Host = "github.com",
            Languages = new SubjectLanguages { Primary = "csharp", Secondary = ["typescript"] },
        };

        var round = JsonSerializer.Deserialize<DeliverySubject>(JsonSerializer.Serialize(subject))!;

        Assert.Equal("csharp", round.Languages?.Primary);
        Assert.Equal(["typescript"], round.Languages?.Secondary);
    }

    [Fact]
    public void A_delivery_that_names_no_language_is_still_a_valid_delivery()
    {
        var round = JsonSerializer.Deserialize<DeliverySubject>(
            """{"repository":"acme/checkout-api","host":"github.com"}""")!;

        Assert.Null(round.Languages);
        Assert.Equal("acme/checkout-api", round.Repository);
    }

    // ---------------------------------------------------------------- fixtures

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return JsonSerializer.Serialize(page.Node);
    }

    private static DeliveryPayload Reading(string issuedAt, string? scannedAt) =>
        DeliveryTestHelp.Build(
            new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                ProductionLoc = 1500,
                Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
            },
            new DeliveryBuildRequest
            {
                DeliveryId = "cd_facts_001",
                IssuedAt = issuedAt,
                Subject = new DeliverySubject { Repository = "acme/checkout-api", Commit = "3f9a1c2", Host = "github.com" },
                Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
                Measurement = scannedAt is null ? null : new DeliveryMeasurement { ScannedAt = scannedAt },
            });
}
