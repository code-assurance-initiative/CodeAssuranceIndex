using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Every share the corpus publishes, and the population it is taken over.
/// </summary>
/// <remarks>
/// <para>★★ AFFECTED IS TAKEN OVER MEASURABLE, NEVER OVER THE CORPUS. A codebase nobody could scan is
/// unmeasured, not clean, and dividing by the whole corpus counts it as passing. The shares are
/// <see cref="Cai.Pages.Publishing.Figure"/>s so the denominator cannot be left behind by anyone quoting
/// one.</para>
/// <para>★★ AND THE POPULATIONS ARE DIFFERENT POPULATIONS — that is the one thing a row of four shares
/// invites a reader to forget. Each states its own.</para>
/// </remarks>
public sealed class CorpusShareTests
{
    [Fact]
    public void Affected_is_taken_over_what_was_measured_never_over_the_corpus()
    {
        // Ten codebases; four had a dependency graph a scanner could resolve; two of those are affected.
        var reading = Reading(
            [
                Measured(affected: true), Measured(affected: true), Measured(affected: false), Measured(affected: false),
                Unmeasurable(), Unmeasurable(), Unmeasurable(), Unmeasurable(), Unmeasurable(), Unmeasurable(),
            ]);

        var share = reading.VulnAffectedShare;

        Assert.NotNull(share);
        Assert.Equal(0.5, share.Value, 3);
        Assert.Equal(4, share.Population);
        Assert.Contains("2 of 4", share.Headline(), StringComparison.Ordinal);
        Assert.Contains("a scanner could resolve", share.Headline(), StringComparison.Ordinal);
    }

    /// <summary>★ The share of nothing is not zero, and a bar drawn at zero says nobody is affected.</summary>
    [Fact]
    public void A_share_with_nothing_in_its_denominator_is_absent_not_zero()
    {
        var reading = Reading([Unmeasurable(), Unmeasurable()]);

        Assert.Null(reading.VulnAffectedShare);
        Assert.Null(reading.DisclosureWithoutPolicyShare);
        Assert.Null(reading.SecretsInHistoryShare);
        Assert.Null(reading.SbomShare);
    }

    [Fact]
    public void The_findings_shares_each_name_their_own_population()
    {
        var reading = Reading(
            [
                Measured(affected: true) with
                {
                    DisclosureMeasured = true,
                    SecretsHistoryMeasured = true,
                    SecretsHistoryAffected = true,
                    SupplyChainMeasurable = true,
                },
                Measured(affected: false) with
                {
                    DisclosureMeasured = true,
                    DisclosurePolicy = true,
                    DisclosureContact = true,
                    SecretsHistoryMeasured = true,
                    SupplyChainMeasurable = true,
                    Sbom = true,
                },
            ]);

        var populations = new[]
        {
            reading.VulnAffectedShare!.Basis.Many,
            reading.DisclosureWithoutPolicyShare!.Basis.Many,
            reading.SecretsInHistoryShare!.Basis.Many,
            reading.SbomShare!.Basis.Many,
        };

        Assert.Equal(populations.Length, populations.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// ★ The contact share is taken over the policies that EXIST, not over the codebases that were asked —
    /// "of the policies that do exist, how many name someone to contact" is the only reading of it that is
    /// true.
    /// </summary>
    [Fact]
    public void The_contact_share_is_taken_over_the_policies_that_exist()
    {
        var reading = Reading(
            [
                Measured(affected: false) with { DisclosureMeasured = true, DisclosurePolicy = true, DisclosureContact = true },
                Measured(affected: false) with { DisclosureMeasured = true, DisclosurePolicy = true },
                Measured(affected: false) with { DisclosureMeasured = true },
            ]);

        var contact = reading.DisclosureContactShare;

        Assert.NotNull(contact);
        Assert.Equal(2, contact.Population);
        Assert.Equal(0.5, contact.Value, 3);
    }

    /// <summary>
    /// ★ The findings TOTAL is a count of findings and says so — it is the one figure here that is not a
    /// count of codebases, and it names the population it was summed across.
    /// </summary>
    [Fact]
    public void The_findings_total_says_what_it_was_summed_across()
    {
        var reading = Reading([Measured(affected: true) with { Findings = 63 }, Measured(affected: false)]);

        var total = reading.FindingsTotal;

        Assert.NotNull(total);
        Assert.Equal(63, total.Value);
        Assert.Equal(2, total.Population);
        Assert.Contains("findings were found", total.Total("findings were found"), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static SecurityReading Measured(bool affected) => new()
    {
        VulnMeasurable = true,
        VulnAffected = affected,
        Findings = affected ? 1 : 0,
    };

    private static SecurityReading Unmeasurable() => new() { VulnMeasurable = false };

    private static CorpusReading Reading(IEnumerable<SecurityReading> readings) =>
        CorpusReading.From(readings.Select(Record), TakenAt);

    private static SurveyRecord Record(SecurityReading reading)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_share_{ordinal}",
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
