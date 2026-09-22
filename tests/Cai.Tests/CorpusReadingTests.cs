using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The corpus read as one measurement: what was FOUND across every codebase, folded from the same signed
/// deliveries the scores come from.
/// </summary>
/// <remarks>
/// <para>★★ IT COUNTS CODEBASES. A repository carrying 985 known-vulnerable components is ONE affected
/// codebase, not 985 votes — only the finding totals count findings. Every headline on the corpus pages is
/// a count of codebases for exactly this reason, and a fold that got it the other way round would publish
/// a corpus shaped like its worst repository.</para>
/// <para>★★ "NOT MEASURED" IS NEVER FOLDED INTO "CLEAN". A codebase whose dependency graph nobody could
/// resolve is not a codebase with no vulnerabilities; it is one that was not asked. It leaves the
/// denominator, and the reading says how many did.</para>
/// </remarks>
public sealed class CorpusReadingTests
{
    [Fact]
    public void A_codebase_with_many_vulnerabilities_is_one_affected_codebase_not_many_votes()
    {
        var reading = CorpusReading.From(
            [
                Record(new SecurityReading { VulnMeasurable = true, VulnAffected = true, Findings = 985 }),
                Record(new SecurityReading { VulnMeasurable = true }),
            ],
            TakenAt);

        Assert.Equal(2, reading.VulnMeasurable);
        Assert.Equal(1, reading.VulnAffected);
        Assert.Equal(985, reading.Findings);
    }

    [Fact]
    public void A_codebase_nobody_could_measure_is_counted_as_unmeasured_never_as_clean()
    {
        var reading = CorpusReading.From(
            [
                Record(new SecurityReading { VulnMeasurable = true, VulnAffected = true, Findings = 2 }),
                Record(new SecurityReading { VulnMeasurable = false, VulnScanFailed = true }),
            ],
            TakenAt);

        Assert.Equal(1, reading.VulnMeasurable);
        Assert.Equal(1, reading.VulnUnmeasured);
        Assert.Equal(1, reading.VulnScanFailed);
        // The share a page may publish is over what was measured, and that is the only denominator offered.
        Assert.Equal(1, reading.VulnAffected);
    }

    /// <summary>★ A 1.0 delivery says nothing about security. Silence is not a clean bill of health.</summary>
    [Fact]
    public void A_delivery_that_carries_no_reading_contributes_to_no_denominator()
    {
        var reading = CorpusReading.From(
            [Record(new SecurityReading { VulnMeasurable = true }), Record(null)],
            TakenAt);

        Assert.Equal(2, reading.Codebases);
        Assert.Equal(1, reading.SecurityReadings);
        Assert.Equal(1, reading.VulnMeasurable);
        Assert.Equal(0, reading.VulnUnmeasured);
    }

    [Fact]
    public void An_advisory_seen_twice_in_one_codebase_counts_that_codebase_once()
    {
        var reading = CorpusReading.From(
            [
                Record(WithAdvisories(
                    Advisory("GHSA-aaaa-bbbb-cccc", "Acme.Widgets", "1.0.0"),
                    Advisory("GHSA-aaaa-bbbb-cccc", "Acme.Widgets", "1.1.0"))),
            ],
            TakenAt);

        var stat = reading.Advisories.ByAdvisory["GHSA-aaaa-bbbb-cccc"];
        Assert.Equal(1, stat.Surveys);
        Assert.Equal(1, reading.Advisories.ByPackage["Acme.Widgets"].Surveys);
    }

    /// <summary>
    /// ★★ A share of the whole corpus cannot be published: its numerator is a floor (the rendered advisory
    /// list is capped, and a dimension whose findings carry no file path never reaches it) and its
    /// denominator is not. A share over the codebases whose list was COMPLETE is exact and checkable.
    /// </summary>
    [Fact]
    public void The_exact_share_is_taken_only_over_the_codebases_whose_list_was_complete()
    {
        var reading = CorpusReading.From(
            [
                Record(WithAdvisories(Advisory("CVE-2026-0001", "left-pad", "1.3.0"))),
                Record(WithAdvisories(truncated: true, Advisory("CVE-2026-0001", "left-pad", "1.3.0"))),
            ],
            TakenAt);

        Assert.Equal(2, reading.Advisories.Surveys);
        Assert.Equal(1, reading.Advisories.SurveysComplete);
        Assert.Equal(1, reading.Advisories.SurveysTruncated);

        var stat = reading.Advisories.ByAdvisory["CVE-2026-0001"];
        Assert.Equal(2, stat.Surveys);
        Assert.Equal(1, stat.SurveysAmongComplete);
    }

    /// <summary>
    /// ★ Collapsed case-insensitively where they are COUNTED, and the id is still published verbatim: a GHSA
    /// id is canonically lower case and a CVE id upper, and a reader pastes what the page shows into the
    /// advisory database.
    /// </summary>
    [Fact]
    public void Two_spellings_of_one_advisory_id_are_one_advisory()
    {
        var reading = CorpusReading.From(
            [
                Record(WithAdvisories(Advisory("GHSA-aaaa-bbbb-cccc", "left-pad", "1.3.0"))),
                Record(WithAdvisories(Advisory("ghsa-aaaa-bbbb-cccc", "left-pad", "1.3.0"))),
            ],
            TakenAt);

        var only = Assert.Single(reading.Advisories.ByAdvisory);
        Assert.Equal(2, only.Value.Surveys);
    }

    /// <summary>★ A survey nobody indexed for advisories is an absence in our own records, never a fact.</summary>
    [Fact]
    public void A_survey_never_read_for_advisories_is_outside_the_advisory_population()
    {
        var reading = CorpusReading.From(
            [
                Record(WithAdvisories(Advisory("CVE-2026-0001", "left-pad", "1.3.0"))),
                Record(new SecurityReading { VulnMeasurable = true, AdvisoriesRead = false }),
            ],
            TakenAt);

        Assert.Equal(1, reading.Advisories.Surveys);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static SecurityReading WithAdvisories(params VisibleAdvisory[] advisories) =>
        WithAdvisories(false, advisories);

    private static SecurityReading WithAdvisories(bool truncated, params VisibleAdvisory[] advisories) => new()
    {
        VulnMeasurable = true,
        VulnAffected = advisories.Length > 0,
        Findings = advisories.Length,
        AdvisoriesRead = true,
        AdvisoryListComplete = !truncated,
        Advisories = advisories,
    };

    private static VisibleAdvisory Advisory(string id, string package, string version) =>
        new() { AdvisoryId = id, Package = package, PackageVersion = version };

    private static SurveyRecord Record(SecurityReading? reading)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_corpus_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject { Repository = $"acme/repo-{ordinal}", Host = "github.com" },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z" },
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
