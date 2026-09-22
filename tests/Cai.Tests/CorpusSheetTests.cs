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
        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt));

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
        var json = Json(CorpusSheetBuilder.Build([Record(Nothing())], TakenAt));

        Assert.Contains("§1 Population", json, StringComparison.Ordinal);
        Assert.DoesNotContain("§2 Findings", json, StringComparison.Ordinal);
    }

    /// <summary>★ A heading over nothing is not a section. No reading at all states nothing at all.</summary>
    [Fact]
    public void A_corpus_no_producer_said_anything_about_publishes_neither_section()
    {
        var json = Json(CorpusSheetBuilder.Build([SilentRecord()], TakenAt));

        Assert.DoesNotContain("§1 Population", json, StringComparison.Ordinal);
        Assert.DoesNotContain("§2 Findings", json, StringComparison.Ordinal);
    }

    /// <summary>★★ No share borrows the band vocabulary — see the class remarks.</summary>
    [Fact]
    public void No_share_on_the_sheet_is_inked_with_an_assurance_band()
    {
        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt));

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
        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt));

        Assert.Contains("surveys whose dependencies a scanner could resolve", json, StringComparison.Ordinal);
        Assert.Contains("surveys where a disclosure policy was looked for", json, StringComparison.Ordinal);
        Assert.Contains("The four populations are not the same", json, StringComparison.Ordinal);
    }

    /// <summary>★ The sheet lives at the standard's own corpus address, not under the survey catalogue.</summary>
    [Fact]
    public void The_sheet_lives_at_the_corpus_address()
    {
        var page = CorpusSheetBuilder.Build(FullCorpus(), TakenAt);

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
        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt));

        Assert.Contains("unmeasured, not clean", json, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>★ A sheet with no recorded history draws no line and implies nothing.</summary>
    [Fact]
    public void A_sheet_with_too_few_recorded_readings_publishes_no_series()
    {
        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt));

        Assert.DoesNotContain("§5 Series", json, StringComparison.Ordinal);
        Assert.DoesNotContain("cai-trend", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ ONLY A MEDIAN IS DRAWN. The corpus's own growth is stated as figures and never plotted: the
    /// chart's axis is the band scale a CAI is defined on, and a count on it lands thousands of units above
    /// the box while a share lands inside it and looks right while being inked in a direction the corpus
    /// never claimed.
    /// </summary>
    [Fact]
    public void The_series_draws_the_median_and_states_nothing_else_as_a_line()
    {
        var history = new List<DatedReading>
        {
            new(new DateTimeOffset(2026, 8, 1, 2, 0, 0, TimeSpan.Zero), 580, 49.8),
            new(new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.Zero), 3545, 52.6),
        };

        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt, history));

        var flat = json.Replace("\\\"", "\"", StringComparison.Ordinal);

        Assert.Contains("§5 Series", flat, StringComparison.Ordinal);
        Assert.Contains("cai-trend", flat, StringComparison.Ordinal);

        // The line carries the medians, not the corpus size.
        Assert.Contains("49.8,52.6", flat, StringComparison.Ordinal);
        Assert.DoesNotContain("580,3545", flat, StringComparison.Ordinal);

        // ★ And the growth is STATED, as the island's own figures row: two pairs, each with its two ends.
        //   The second pair IS the first pair's population, which is why neither restates it in small type
        //   — "580 → 3,545 published measured codebases" is what "across 580" and "across 3,545" said.
        Assert.Contains("\"label\":\"Median\"", flat, StringComparison.Ordinal);
        Assert.Contains("\"label\":\"Published measured codebases\"", flat, StringComparison.Ordinal);
        Assert.Contains("\"value\":\"580\"", flat, StringComparison.Ordinal);
        Assert.Contains("\"value\":\"3,545\"", flat, StringComparison.Ordinal);
    }

    /// <summary>★ The line says it is sampled, because the island states that contract to a reader as fact.</summary>
    [Fact]
    public void The_series_says_it_is_sampled()
    {
        var history = Enumerable.Range(0, 30)
            .Select(i => new DatedReading(
                new DateTimeOffset(2026, 8, 1, 2, 0, 0, TimeSpan.Zero).AddDays(i), 3000 + i, 50 + (i * 0.1)))
            .ToList();

        var json = Json(CorpusSheetBuilder.Build(FullCorpus(), TakenAt, history));

        Assert.Contains("weekly", json, StringComparison.Ordinal);
        Assert.Contains("never recomputed from today's corpus", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return PageText.Json(page);
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

    private static List<SurveyRecord> FullCorpus() =>
    [
        .. new SecurityReading[]
        {
            new()
            {
                VulnMeasurable = true, VulnAffected = true, VulnHighOrCritical = true, VulnCritical = true,
                Findings = 12, FindingsCritical = 2, FindingsHigh = 4,
                DisclosureMeasured = true,
                SecretsHistoryMeasured = true, SecretsHistoryAffected = true, SecretsHistoryFindings = 3,
                SecretsCurrentMeasured = true,
                SupplyChainMeasurable = true,
            },
            new()
            {
                VulnMeasurable = true,
                DisclosureMeasured = true, DisclosurePolicy = true, DisclosureContact = true,
                SecretsHistoryMeasured = true,
                SecretsCurrentMeasured = true,
                SupplyChainMeasurable = true, Sbom = true, Signing = true,
            },
            new() { VulnScanFailed = true },
        }.Select(Record),
    ];

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
