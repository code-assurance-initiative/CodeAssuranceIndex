using System.Text.Encodings.Web;
using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The advisory and package pages — the part of the corpus that is a FLOOR and has to say so.
/// </summary>
/// <remarks>
/// <para>★★ A COUNT OF SURVEYS CARRYING AN ADVISORY IS A LOWER BOUND, NEVER A TOTAL. An advisory survives
/// only in a survey's readable list of affected locations, and that list stops without the count above it
/// changing — so what a survey was SEEN to carry is what fitted. The cut bites hardest on the codebases
/// carrying the most vulnerabilities, so the error is not noise: it is systematic and always in one
/// direction.</para>
/// <para>★★ WHICH IS WHY EXACTLY ONE SHARE ON THESE PAGES IS EXACT — the one taken over the surveys
/// nothing was withheld from. Its numerator and denominator are counted over the same thing. A share of
/// the whole corpus cannot be, and no wording fixes a percentage built out of two different kinds of
/// number.</para>
/// </remarks>
public sealed class CorpusAdvisoryPagesTests
{
    [Fact]
    public void An_advisory_seen_in_fewer_than_three_surveys_gets_no_page()
    {
        var pages = CorpusAdvisoryPages.Build(Reading(Carrying("CVE-2026-0001", 2)), TakenAt);

        Assert.DoesNotContain(pages, p => p.Path.Contains("/advisory/", StringComparison.Ordinal));
    }

    [Fact]
    public void An_advisory_seen_in_three_gets_one()
    {
        var pages = CorpusAdvisoryPages.Build(Reading(Carrying("CVE-2026-0001", 3)), TakenAt);

        Assert.Contains(pages, p => p.Path == "state-of-the-corpus/advisory/cve-2026-0001");
    }

    /// <summary>★★ The figure itself says "at least" and names the blind spot — the hedge is the type's job.</summary>
    [Fact]
    public void The_reach_is_a_floor_that_names_its_own_blind_spot()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Carrying("CVE-2026-0001", 3));
        records.AddRange(Truncated("CVE-2026-0001", 2));

        var page = Page(CorpusAdvisoryPages.Build(Reading(records), TakenAt), "advisory/cve-2026-0001");

        var json = Json(page);
        Assert.Contains("at least 5", json, StringComparison.Ordinal);
        Assert.Contains("could not be checked", json, StringComparison.Ordinal);
    }

    /// <summary>★ The one share that can be read as a statement about the corpus.</summary>
    [Fact]
    public void The_exact_share_is_taken_over_the_surveys_nothing_was_withheld_from()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Carrying("CVE-2026-0001", 3));
        records.AddRange(Clean(1));
        records.AddRange(Truncated("CVE-2026-0001", 2));

        var json = Json(Page(CorpusAdvisoryPages.Build(Reading(records), TakenAt), "advisory/cve-2026-0001"));

        Assert.Contains("3 of 4", json, StringComparison.Ordinal);
        Assert.Contains("surveys whose advisory list was complete", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_package_page_names_the_advisories_raised_against_it()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Carrying("CVE-2026-0001", 3, package: "left-pad"));
        records.AddRange(Carrying("CVE-2026-0002", 3, package: "left-pad"));

        var json = Json(Page(CorpusAdvisoryPages.Build(Reading(records), TakenAt), "package/left-pad"));

        Assert.Contains("CVE-2026-0001", json, StringComparison.Ordinal);
        Assert.Contains("CVE-2026-0002", json, StringComparison.Ordinal);
    }

    /// <summary>★★ The same one-function rule: every link on an index is a page that was built.</summary>
    [Fact]
    public void The_indexes_link_exactly_the_pages_that_were_built()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Carrying("CVE-2026-0001", 4, package: "left-pad"));
        records.AddRange(Carrying("CVE-2026-0002", 2, package: "right-pad"));

        var pages = CorpusAdvisoryPages.Build(Reading(records), TakenAt);
        var built = pages.Select(p => $"/{p.Path}/").ToHashSet(StringComparer.Ordinal);
        var index = pages.Single(p => p.Path == "state-of-the-corpus/advisories");

        foreach (var href in Hrefs(Json(index)).Where(h => h.Contains("/advisory/", StringComparison.Ordinal)))
        {
            Assert.Contains(href, built);
        }

        Assert.DoesNotContain("CVE-2026-0002", Json(index), StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ The advisories held back are COUNTED. Dropping them silently publishes a table that reads as a
    /// census of the corpus while being a census of the part big enough to print.
    /// </summary>
    [Fact]
    public void The_advisories_held_back_by_the_gate_are_counted()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Carrying("CVE-2026-0001", 4));
        records.AddRange(Carrying("CVE-2026-0002", 2));
        records.AddRange(Carrying("CVE-2026-0003", 1));

        var pages = CorpusAdvisoryPages.Build(Reading(records), TakenAt);
        var json = Json(pages.Single(p => p.Path == "state-of-the-corpus/advisories"));

        Assert.Contains("2 of 3", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static SurveyPage Page(IReadOnlyList<SurveyPage> pages, string suffix) =>
        pages.Single(p => p.Path.EndsWith(suffix, StringComparison.Ordinal));

    private static CorpusReading Reading(IEnumerable<SurveyRecord> records) =>
        CorpusReading.From(records, TakenAt);

    private static List<SurveyRecord> Carrying(string advisory, int count, string package = "acme.widgets") =>
        [.. Enumerable.Range(0, count).Select(_ => Record(new SecurityReading
        {
            VulnMeasurable = true,
            VulnAffected = true,
            Findings = 1,
            AdvisoriesRead = true,
            AdvisoryListComplete = true,
            Advisories = [new VisibleAdvisory { AdvisoryId = advisory, Package = package, PackageVersion = "1.0.0" }],
        }))];

    private static List<SurveyRecord> Truncated(string advisory, int count, string package = "acme.widgets") =>
        [.. Enumerable.Range(0, count).Select(_ => Record(new SecurityReading
        {
            VulnMeasurable = true,
            VulnAffected = true,
            Findings = 60,
            AdvisoriesRead = true,
            AdvisoryListComplete = false,
            Advisories = [new VisibleAdvisory { AdvisoryId = advisory, Package = package, PackageVersion = "1.0.0" }],
        }))];

    private static List<SurveyRecord> Clean(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Record(new SecurityReading
        {
            VulnMeasurable = true,
            AdvisoriesRead = true,
            AdvisoryListComplete = true,
        }))];

    private static string Json(SurveyPage page) =>
        JsonSerializer.Serialize(
            page.Node, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    private static IEnumerable<string> Hrefs(string json)
    {
        var flat = json.Replace("\\\"", "\"", StringComparison.Ordinal);
        var seen = new List<string>();
        var index = 0;
        while ((index = flat.IndexOf("href", index, StringComparison.Ordinal)) >= 0)
        {
            var colon = flat.IndexOf(':', index);
            var open = colon < 0 ? -1 : flat.IndexOf('"', colon);
            var close = open < 0 ? -1 : flat.IndexOf('"', open + 1);
            if (close > open)
            {
                seen.Add(flat[(open + 1)..close]);
            }

            index += 4;
        }

        return seen;
    }

    private static SurveyRecord Record(SecurityReading reading)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_adv_{ordinal}",
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
