using System.Text.Encodings.Web;
using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The corpus read as one document: what was measured, then what was found in it.
/// </summary>
/// <remarks>
/// <para>★★ §1 COMES BEFORE §2 BECAUSE EVERY SHARE IN §2 IS TAKEN OVER IT. A reader who meets "1,161 carry
/// a known-vulnerable component" and learns the denominator afterwards has already done the wrong
/// arithmetic, and the arithmetic they did is the corpus-wide claim the whole shape of these figures
/// exists to prevent.</para>
/// <para>★★ AND NOT ONE SHARE IS INKED WITH THE ASSURANCE BAND SCALE. That vocabulary is a judgement about
/// assurance, defined on a CAI score; borrowing it here would colour a share good or bad in a direction the
/// corpus never claimed, and on the vulnerability share it would have to be INVERTED to read the right way
/// round — so green would mean "high score" in one section and "low share" in another, on one page.</para>
/// </remarks>
public sealed class CorpusSheetTests
{
    [Fact]
    public void The_population_is_stated_before_the_findings()
    {
        var json = Json(CorpusSheetBuilder.Build(FullReading()));

        var population = json.IndexOf("§1 Population", StringComparison.Ordinal);
        var findings = json.IndexOf("§2 Findings", StringComparison.Ordinal);

        Assert.True(population >= 0, "the sheet states its population");
        Assert.True(findings > population, "the findings come after the population they are taken over");
    }

    /// <summary>
    /// ★★ A CORPUS NOBODY COULD SCAN STILL HAS A POPULATION, AND SAYING SO IS §1'S WHOLE JOB. "0 of 1
    /// surveys had a dependency graph a scanner could resolve" is a real measurement — of the instrument —
    /// and suppressing it would leave a reader with no §2 and no explanation of why. What must NOT appear is
    /// §2: there is no share to take, and a share of nothing is not zero.
    /// </summary>
    [Fact]
    public void A_corpus_nothing_could_be_measured_in_states_its_population_and_no_findings()
    {
        var json = Json(CorpusSheetBuilder.Build(Reading([Nothing()])));

        Assert.Contains("§1 Population", json, StringComparison.Ordinal);
        Assert.DoesNotContain("§2 Findings", json, StringComparison.Ordinal);
    }

    /// <summary>★ A heading over nothing is not a section. No reading at all states nothing at all.</summary>
    [Fact]
    public void A_corpus_no_producer_said_anything_about_publishes_neither_section()
    {
        var json = Json(CorpusSheetBuilder.Build(CorpusReading.From([SilentRecord()], TakenAt)));

        Assert.DoesNotContain("§1 Population", json, StringComparison.Ordinal);
        Assert.DoesNotContain("§2 Findings", json, StringComparison.Ordinal);
    }

    /// <summary>★★ No share borrows the band vocabulary — see the class remarks.</summary>
    [Fact]
    public void No_share_on_the_sheet_is_inked_with_an_assurance_band()
    {
        var json = Json(CorpusSheetBuilder.Build(FullReading()));

        foreach (var band in new[] { "exemplary", "strong", "adequate", "weak", "critical" })
        {
            Assert.DoesNotContain($"\"tone\":\"{band}\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"tone\\u0022:\\u0022{band}", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// ★ Every share states its own population, and the sheet says out loud that the populations differ —
    /// the one thing a row of four shares invites a reader to forget.
    /// </summary>
    [Fact]
    public void Every_share_carries_the_population_it_was_taken_over()
    {
        var json = Json(CorpusSheetBuilder.Build(FullReading()));

        Assert.Contains("surveys whose dependencies a scanner could resolve", json, StringComparison.Ordinal);
        Assert.Contains("surveys where a disclosure policy was looked for", json, StringComparison.Ordinal);
        Assert.Contains("The four populations are not the same", json, StringComparison.Ordinal);
    }

    /// <summary>★ The sheet lives at the standard's own corpus address, not under the survey catalogue.</summary>
    [Fact]
    public void The_sheet_lives_at_the_corpus_address()
    {
        var page = CorpusSheetBuilder.Build(FullReading());

        Assert.NotNull(page);
        Assert.Equal("state-of-the-corpus", page.Path);
    }

    /// <summary>
    /// ★★ A CODEBASE NOBODY COULD SCAN IS UNMEASURED, NOT CLEAN — and §1 says so in words, because that is
    /// the whole job of stating the population first.
    /// </summary>
    [Fact]
    public void The_population_says_that_an_unscanned_codebase_is_unmeasured_rather_than_clean()
    {
        var json = Json(CorpusSheetBuilder.Build(FullReading()));

        Assert.Contains("unmeasured, not clean", json, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    /// <summary>
    /// The page's node tree as text a test can read.
    /// </summary>
    /// <remarks>
    /// ★ RELAXED ESCAPING, deliberately. The default encoder writes "§" as \u00A7 and an inner quote as
    /// \u0022, so a test asserting on "§1 Population" finds nothing and passes or fails for a reason that
    /// has nothing to do with the page. The bytes the CMS receives are unaffected either way — JSON escaping
    /// is transparent to a parser — so this is a property of the assertion, not of the publication.
    /// </remarks>
    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return JsonSerializer.Serialize(
            page.Node, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    private static SecurityReading Nothing() => new();

    /// <summary>A delivery whose producer said nothing about security at all — a 1.0 package.</summary>
    private static SurveyRecord SilentRecord()
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_silent_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject { Repository = $"acme/silent-{ordinal}", Host = "github.com" },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
            Evidence = new EvidenceBundle { RubricVersion = "rubric-2026.08.15", ProductionLoc = 1000 },
        }]);
    }

    private static CorpusReading FullReading() => Reading(
        [
            new SecurityReading
            {
                VulnMeasurable = true, VulnAffected = true, VulnHighOrCritical = true, VulnCritical = true,
                Findings = 12, FindingsCritical = 2, FindingsHigh = 4,
                DisclosureMeasured = true,
                SecretsHistoryMeasured = true, SecretsHistoryAffected = true, SecretsHistoryFindings = 3,
                SecretsCurrentMeasured = true,
                SupplyChainMeasurable = true,
            },
            new SecurityReading
            {
                VulnMeasurable = true,
                DisclosureMeasured = true, DisclosurePolicy = true, DisclosureContact = true,
                SecretsHistoryMeasured = true,
                SecretsCurrentMeasured = true,
                SupplyChainMeasurable = true, Sbom = true, Signing = true,
            },
            new SecurityReading { VulnScanFailed = true },
        ]);

    private static CorpusReading Reading(IEnumerable<SecurityReading> readings) =>
        CorpusReading.From(readings.Select(Record), TakenAt);

    private static SurveyRecord Record(SecurityReading reading)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_sheet_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject { Repository = $"acme/repo-{ordinal}", Host = "github.com" },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                ProductionLoc = 1000,
                SecurityReading = reading,
            },
        }]);
    }
}
