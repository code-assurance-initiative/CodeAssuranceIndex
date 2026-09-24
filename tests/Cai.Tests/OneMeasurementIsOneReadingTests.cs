using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Two deliveries about the same measurement are one reading, not two.
/// </summary>
/// <remarks>
/// <para>★★ A DELIVERY IS IMMUTABLE, SO SAYING MORE ABOUT A MEASUREMENT MEANS FILING AGAIN. The producer
/// filed 16,000 deliveries under MINOR 1.0, a schema with no language, no origin and no security reading —
/// facts it held all along in its own bundles and never put in the package. The only way those facts can
/// reach a page is a second delivery about the SAME scan: same commit, same instant, more said about it.
/// Nothing can be edited or withdrawn, and that is the design.</para>
///
/// <para>★★ WITHOUT THIS FOLD THAT SECOND FILING IS A LIE ABOUT THE TRAJECTORY. The portrait counts
/// deliveries as measurements over time and plots one point each, so a corpus-wide backfill would double
/// every repository's history overnight — "2 measurements" for one scan, two points at one date, a trend
/// drawn through a duplicate. The page would be reporting the filing, not the measuring.</para>
///
/// <para>★ THE KEY IS THE MEASUREMENT, NOT THE FILING: subject.commit AND measurement.scannedAt. Two
/// readings of the same commit taken on different days are genuinely two measurements and stay two. A
/// delivery missing either is never folded — absence is not proof of sameness.</para>
/// </remarks>
public sealed class OneMeasurementIsOneReadingTests
{
    [Fact]
    public void A_scan_filed_twice_is_one_measurement()
    {
        var record = SurveyRecord.From([
            Delivery("cd_old", "3f9a1c2", scannedAt: "2026-09-01T10:00:00Z", issuedAt: "2026-09-01T10:05:00Z", cai: 72.4),
            Delivery("cd_backfill", "3f9a1c2", scannedAt: "2026-09-01T10:00:00Z", issuedAt: "2026-09-24T19:00:00Z", cai: 72.4),
        ]);

        Assert.Single(record.Deliveries);
    }

    /// <summary>★★ And the one that survives is the one that says more — the later filing.</summary>
    [Fact]
    public void The_later_filing_wins_because_it_is_the_one_that_says_more()
    {
        var record = SurveyRecord.From([
            Delivery("cd_old", "3f9a1c2", "2026-09-01T10:00:00Z", "2026-09-01T10:05:00Z", 72.4),
            Delivery("cd_backfill", "3f9a1c2", "2026-09-01T10:00:00Z", "2026-09-24T19:00:00Z", 72.4,
                languages: new SubjectLanguages { Primary = "csharp" }),
        ]);

        Assert.Equal("cd_backfill", record.Latest.DeliveryId);
        Assert.Equal("csharp", record.Latest.Subject.Languages?.Primary);
    }

    /// <summary>★★ Two real measurements of one commit, taken on different days, stay two.</summary>
    [Fact]
    public void The_same_commit_measured_twice_is_two_readings()
    {
        var record = SurveyRecord.From([
            Delivery("cd_june", "3f9a1c2", "2026-06-01T10:00:00Z", "2026-06-01T10:05:00Z", 61.0),
            Delivery("cd_sept", "3f9a1c2", "2026-09-01T10:00:00Z", "2026-09-01T10:05:00Z", 72.4),
        ]);

        Assert.Equal(2, record.Deliveries.Count);
    }

    /// <summary>★ Different commits are always different measurements.</summary>
    [Fact]
    public void Two_commits_are_two_readings()
    {
        var record = SurveyRecord.From([
            Delivery("cd_a", "aaaaaaa", "2026-09-01T10:00:00Z", "2026-09-01T10:05:00Z", 61.0),
            Delivery("cd_b", "bbbbbbb", "2026-09-01T10:00:00Z", "2026-09-01T10:06:00Z", 72.4),
        ]);

        Assert.Equal(2, record.Deliveries.Count);
    }

    /// <summary>
    /// ★★ ABSENCE IS NOT PROOF OF SAMENESS. A delivery with no commit or no scan instant cannot be shown
    /// to describe the same measurement as another, so it is never folded away — losing a real reading is
    /// worse than carrying a duplicate, and only one of the two is recoverable.
    /// </summary>
    [Fact]
    public void A_delivery_that_cannot_prove_sameness_is_kept()
    {
        var record = SurveyRecord.From([
            Delivery("cd_1", commit: null, scannedAt: null, issuedAt: "2026-09-01T10:05:00Z", cai: 61.0),
            Delivery("cd_2", commit: null, scannedAt: null, issuedAt: "2026-09-02T10:05:00Z", cai: 61.0),
        ]);

        Assert.Equal(2, record.Deliveries.Count);
    }

    private static DeliveryPayload Delivery(
        string id, string? commit, string? scannedAt, string issuedAt, double cai,
        SubjectLanguages? languages = null) => new()
    {
        DeliveryId = id,
        IssuedAt = issuedAt,
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject
        {
            Repository = "acme/widgets",
            Host = "github.com",
            Commit = commit,
            Languages = languages,
        },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
        Measurement = scannedAt is null ? null : new DeliveryMeasurement { ScannedAt = scannedAt, ProductionLoc = 9000 },
        Verdict = new DeliveryVerdict { Cai = cai, Band = "Strong" },
        Evidence = new EvidenceBundle { RubricVersion = "rubric-2026.08.15", ProductionLoc = 9000 },
    };
}
