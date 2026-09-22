using System.Text.Json;
using Cai.Delivery;
using Cai.Scoring;
using Cai.Web.Registry;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A delivery has to CARRY the facts the standard's pages publish, and the registry's own ingest gate has
/// to accept it carrying them.
/// </summary>
/// <remarks>
/// <para>★★ THE SCHEMA IS THE GATE, NOT THE RECORD TYPE. <c>schemas/cai-delivery-1.0.schema.json</c> is
/// embedded in the registry binary and every inbound package is validated against it before any
/// cryptography runs — and <c>subject</c> declares <c>additionalProperties: false</c>. So a field added to
/// <see cref="DeliverySubject"/> alone is not a field a producer can send: it is rejected 400 at ingest,
/// and the only symptom is a producer whose deliveries stop arriving. A computed value is not done until
/// something accepts it.</para>
/// <para>★ WHICH IS WHY THE READING GOES UNDER <c>evidence</c>. That object is
/// <c>additionalProperties: true</c> by design — it is the producer's open record — and a descriptive,
/// non-scored block belongs beside <c>rebuildCost</c>, <c>busFactor</c> and <c>topology</c> rather than in
/// the verdict, which is the arithmetic.</para>
/// </remarks>
public sealed class DeliveryCarriesThePageFactsTests
{
    /// <summary>★★ The one that was dead on the wire: the gate refuses a language it never heard of.</summary>
    [Fact]
    public void A_delivery_that_names_its_language_passes_the_registrys_own_ingest_gate()
    {
        var violations = DeliveryPackageSchema.Validate(Document(WithPageFacts()));

        Assert.Empty(violations);
    }

    /// <summary>
    /// The corpus reading is a fact about ONE repository, folded by the standard into a count of
    /// repositories. Every field of it has to survive the wire.
    /// </summary>
    [Fact]
    public void The_security_reading_round_trips_through_the_wire_form()
    {
        var round = DeliveryPackage.Parse(new DeliveryPackage { Payload = WithPageFacts() }.ToJson());

        var reading = round.Payload.Evidence.SecurityReading;
        Assert.NotNull(reading);
        Assert.True(reading.VulnMeasurable);
        Assert.True(reading.VulnAffected);
        Assert.Equal(3, reading.Findings);
        Assert.Equal(1, reading.FindingsCritical);
        Assert.True(reading.DisclosureMeasured);
        Assert.False(reading.DisclosurePolicy);
        Assert.True(reading.Sbom);
        Assert.Equal("GHSA-xxxx-yyyy-zzzz", Assert.Single(reading.Advisories).AdvisoryId);
        Assert.Equal("csharp", round.Payload.Subject.Languages?.Primary);
    }

    /// <summary>
    /// ★★ A PACKAGE THAT CARRIES NONE OF THIS IS BYTE-IDENTICAL TO ONE MINTED BEFORE IT EXISTED. That is
    /// what "additive" has to mean when the shape is signed: an absent optional field never enters the
    /// canonical form, so every signature ever issued keeps verifying.
    /// </summary>
    [Fact]
    public void A_payload_carrying_none_of_it_canonicalises_exactly_as_it_did_before()
    {
        var canonical = System.Text.Encoding.UTF8.GetString(CanonicalJson.Canonicalize(Bare()));

        Assert.DoesNotContain("languages", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("securityReading", canonical, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ THE MINOR SAYS SO. A reader of the wire cannot tell 1.0 from "1.0 with fields 1.0 never defined",
    /// and a verifier that meets an unknown field under a version that does not admit it has no way to know
    /// whether to ignore it. A higher MINOR is forward-compatible; the MAJOR is what verification refuses.
    /// </summary>
    [Fact]
    public void The_additions_announce_themselves_as_a_higher_minor()
    {
        Assert.Equal("1.1", DeliverySchema.Current);
        Assert.Equal(1, DeliverySchema.MajorOf(DeliverySchema.Current));
    }

    // ---------------------------------------------------------------- fixtures

    /// <summary>
    /// The package as the registry receives it — genuinely signed, so the only thing this test can fail on
    /// is the SHAPE. An unsigned stand-in fails the gate on its empty signature block and would report the
    /// schema as broken whatever the schema said.
    /// </summary>
    private static JsonElement Document(DeliveryPayload payload)
    {
        var pair = DeliveryKeyPair.Generate("cai-ed25519-test");
        using var signer = new DeliverySigner(pair);
        return JsonSerializer.Deserialize<JsonElement>(signer.SignPackage(payload).ToJson());
    }

    private static DeliveryPayload Bare() => new()
    {
        DeliveryId = "cd_bare_001",
        IssuedAt = "2026-09-01T10:00:00Z",
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject { Repository = "acme/checkout-api", Host = "github.com", Commit = "3f9a1c2" },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
        Verdict = new DeliveryVerdict { Cai = 72.4, Band = "Strong" },
        Evidence = new EvidenceBundle
        {
            RubricVersion = "rubric-2026.08.15",
            ProductionLoc = 1500,
            Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
        },
    };

    private static DeliveryPayload WithPageFacts()
    {
        var bare = Bare();
        return bare with
        {
            Subject = bare.Subject with
            {
                Languages = new SubjectLanguages { Primary = "csharp", Secondary = ["typescript"] },
            },
            Evidence = bare.Evidence with
            {
                SecurityReading = new SecurityReading
                {
                    VulnMeasurable = true,
                    VulnAffected = true,
                    VulnHighOrCritical = true,
                    VulnCritical = true,
                    Findings = 3,
                    FindingsCritical = 1,
                    FindingsHigh = 1,
                    FindingsMedium = 1,
                    DisclosureMeasured = true,
                    SecretsHistoryMeasured = true,
                    SupplyChainMeasurable = true,
                    Sbom = true,
                    AdvisoriesRead = true,
                    AdvisoryListComplete = true,
                    Advisories = [new VisibleAdvisory
                    {
                        AdvisoryId = "GHSA-xxxx-yyyy-zzzz",
                        Package = "Acme.Widgets",
                        PackageVersion = "2.1.0",
                        Inherited = true,
                    }],
                },
            },
        };
    }
}
