using System.Text.Json;
using Cai.Delivery;
using Cai.Scoring;
using Cai.Web.Registry;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A delivery can say where the producer's public reports for this measurement are read, and the registry's
/// own ingest gate accepts it saying so (decision 0003).
/// </summary>
/// <remarks>
/// <para>★★ A LIST, BECAUSE ONE MEASUREMENT CAN HAVE SEVERAL WRITE-UPS. One engine measures; several systems
/// built on it may each publish a report about the same scan, and a second engine measuring the same code
/// files its own deliveries. Each report names the listed system that wrote it, so a page can say whose
/// report a reader is opening and wear that system's mark.</para>
/// <para>★★ IN THE SIGNED PAYLOAD. The link is a claim about THIS measurement, and the same signature that
/// covers the score covers it, so a sharer cannot swap it for another address.</para>
/// <para>★ ABSENT, NEVER EMPTY. Not publishing a report is a legitimate choice; an empty list would put a
/// field in every package that says nothing.</para>
/// </remarks>
public sealed class ReportsRideInTheDeliveryTests
{
    private const string Report = "https://watchdog.canine.dev/api/oss/Acme/checkout-api/report?run=9d3c";

    [Fact]
    public void A_delivery_can_carry_the_reports_written_about_it()
    {
        var json = JsonSerializer.Serialize(WithReports());
        using var round = JsonDocument.Parse(json);
        var report = Assert.Single(round.RootElement.GetProperty("reports").EnumerateArray());

        Assert.Equal(Report, report.GetProperty("url").GetString());
        Assert.Equal("watchdog", report.GetProperty("system").GetString());
    }

    /// <summary>★★ The one that would be dead on the wire: the gate has to know the field.</summary>
    [Fact]
    public void A_delivery_that_carries_reports_passes_the_registrys_own_ingest_gate()
    {
        Assert.Empty(DeliveryPackageSchema.Validate(Document(WithReports())));
    }

    /// <summary>★ A report a reader cannot open safely is not a report the standard links.</summary>
    [Theory]
    [InlineData("http://watchdog.canine.dev/api/oss/Acme/checkout-api/report")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/api/oss/Acme/checkout-api/report")]
    public void A_report_that_is_not_https_is_refused_at_ingest(string url)
    {
        var payload = Bare() with { Reports = [new DeliveryReport { Url = url, System = "watchdog" }] };

        Assert.NotEmpty(DeliveryPackageSchema.Validate(Document(payload)));
    }

    /// <summary>★ A report that does not say which system wrote it cannot be attributed, so it is refused.</summary>
    [Fact]
    public void A_report_without_a_system_is_refused_at_ingest()
    {
        var payload = Bare() with { Reports = [new DeliveryReport { Url = Report, System = "" }] };

        Assert.NotEmpty(DeliveryPackageSchema.Validate(Document(payload)));
    }

    /// <summary>
    /// ★★ A PACKAGE WITHOUT REPORTS IS BYTE-IDENTICAL TO ONE MINTED BEFORE THE FIELD EXISTED, so every
    /// signature already issued keeps verifying.
    /// </summary>
    [Fact]
    public void A_payload_without_reports_canonicalises_exactly_as_it_did_before()
    {
        var canonical = System.Text.Encoding.UTF8.GetString(CanonicalJson.Canonicalize(Bare()));

        Assert.DoesNotContain("reports", canonical, StringComparison.Ordinal);
    }

    /// <summary>★★ And the builder, the only path a real delivery takes, carries them.</summary>
    [Fact]
    public void The_builder_carries_the_reports_into_the_payload_it_folds()
    {
        var payload = Build([new DeliveryReport { Url = Report, System = "watchdog" }]);

        var report = Assert.Single(payload.Reports!);
        Assert.Equal(Report, report.Url);
        Assert.Equal("watchdog", report.System);
    }

    /// <summary>★ An empty list from a producer is "no reports", and the package says nothing.</summary>
    [Fact]
    public void The_builder_turns_no_reports_into_no_field()
    {
        Assert.Null(Build([]).Reports);
    }

    [Fact]
    public void The_format_version_moved_to_one_point_three()
    {
        Assert.Equal("1.3", DeliverySchema.Current);
        Assert.Equal(1, DeliverySchema.MajorOf(DeliverySchema.Current));
    }

    // ---------------------------------------------------------------- fixtures

    private static DeliveryPayload Build(IReadOnlyList<DeliveryReport> reports) => DeliveryBuilder.Build(
        new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 900,
            Dimensions = [new DimensionScore("D1", "code-quality", 7.0, 0.95)],
        },
        ResolvedRubric.FromCatalog(new RubricCatalog { RubricVersion = "rubric-2026.08.15" }),
        new DeliveryBuildRequest
        {
            DeliveryId = "cd_build",
            IssuedAt = "2026-10-22T20:00:00Z",
            Subject = new DeliverySubject { Repository = "Acme/checkout-api", Host = "github.com" },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Reports = reports,
        });

    /// <summary>Genuinely signed, so the gate can only fail on the SHAPE.</summary>
    private static JsonElement Document(DeliveryPayload payload)
    {
        var pair = DeliveryKeyPair.Generate("cai-ed25519-test");
        using var signer = new DeliverySigner(pair);
        return JsonSerializer.Deserialize<JsonElement>(signer.SignPackage(payload).ToJson());
    }

    private static DeliveryPayload WithReports() =>
        Bare() with { Reports = [new DeliveryReport { Url = Report, System = "watchdog" }] };

    private static DeliveryPayload Bare() => new()
    {
        DeliveryId = "cd_bare_001",
        IssuedAt = "2026-10-22T10:00:00Z",
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject { Repository = "Acme/checkout-api", Host = "github.com", Commit = "3f9a1c2" },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
        Verdict = new DeliveryVerdict { Cai = 72.4, Band = "Strong" },
        Evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 1500,
            Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
        },
    };
}
