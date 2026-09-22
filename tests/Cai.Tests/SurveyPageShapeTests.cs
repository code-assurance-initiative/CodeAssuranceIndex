using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The survey portrait has to carry what the published page carries — the sections a reader recognises
/// it by, in the order the evidence earns them.
/// </summary>
/// <remarks>
/// <para>★★ THE SHAPE IS TAKEN FROM A CAPTURED RENDER, NOT FROM MEMORY.
/// <c>~/Hentet/kennel/cai-owns-its-pages/baseline/survey-portrait__surveys-github-ruben-rasmussen-auth__1280__light.png</c>
/// is the page this must reproduce, and `tests/visual/baseline-manifest.json` records the commit and
/// rubric version behind it.</para>
/// <para>★ THE ORDER IS AN ARGUMENT, NOT A LAYOUT. Measurement, then scale, then movement, then lenses,
/// then the invitation, then the provenance. An invitation placed before the evidence is an advert, and
/// this page's whole claim is that it is evidence first.</para>
/// </remarks>
public sealed class SurveyPageShapeTests
{
    [Fact]
    public void The_portrait_carries_every_section_the_published_page_carries()
    {
        var json = Json(Page());

        Assert.Contains("\"cai-band-scale\"", json, StringComparison.Ordinal);
        Assert.Contains("\"cai-trend\"", json, StringComparison.Ordinal);
        Assert.Contains("\"cai-lens-gauges\"", json, StringComparison.Ordinal);
        Assert.Contains("\"cai-link-cards\"", json, StringComparison.Ordinal);
        Assert.Contains("About this page", json, StringComparison.Ordinal);
        Assert.Contains("Survey your own repository", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ NOT ONE WIDGET MAY BE GIVEN SOMEWHERE TO FETCH FROM. A widget that CAN fetch will, and what it
    /// fetches is whichever card the gallery hands back — the hero — which is how one project's figures
    /// came to be printed under a thousand repositories' names. Every island here is handed its numbers.
    /// </summary>
    [Fact]
    public void No_island_on_the_page_is_given_an_api_base()
    {
        var json = Json(Page());

        Assert.DoesNotContain("api-base", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiBase", json, StringComparison.Ordinal);
    }

    /// <summary>The invitation comes after the evidence, and the provenance closes the page.</summary>
    [Fact]
    public void The_evidence_comes_before_the_invitation_and_the_provenance_comes_last()
    {
        var json = Json(Page());

        var scale = json.IndexOf("cai-band-scale", StringComparison.Ordinal);
        var lenses = json.IndexOf("cai-lens-gauges", StringComparison.Ordinal);
        var invite = json.IndexOf("Survey your own repository", StringComparison.Ordinal);
        var about = json.IndexOf("About this page", StringComparison.Ordinal);

        Assert.True(scale < lenses, "the scale is read before the lenses");
        Assert.True(lenses < invite, "an invitation before the evidence is an advert");
        Assert.True(invite < about, "the provenance closes the page");
    }

    /// <summary>
    /// ★ A LENS THE SURVEY DID NOT REACH IS NOT A ZERO. The page says so in its own words, because a
    /// reader who sees five lenses where another page shows six is owed the difference between "this
    /// codebase does not call for it" and "we could not read it".
    /// </summary>
    [Fact]
    public void A_lens_the_survey_did_not_reach_is_named_rather_than_scored_zero()
    {
        var json = Json(Page());

        Assert.DoesNotContain("|0|", json, StringComparison.Ordinal);
        Assert.Contains("stayed dark", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_single_reading_draws_no_trend_because_there_is_none()
    {
        // Two points make a line; one makes a dot somebody will read as a flat trajectory.
        var one = SurveyRecord.From([Reading("2026-07-01T10:32:04Z", 7.5)]);
        var json = Json(SurveyPageBuilder.Build(one));

        Assert.DoesNotContain("cai-trend", json, StringComparison.Ordinal);
        Assert.Contains("cai-band-scale", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static SurveyPage? Page() => SurveyPageBuilder.Build(SurveyRecord.From(
        [Reading("2026-05-02T09:00:00Z", 6.4), Reading("2026-07-01T10:32:04Z", 7.5)]));

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return JsonSerializer.Serialize(page.Node);
    }

    private static DeliveryPayload Reading(string issuedAt, double codeHealth) =>
        DeliveryTestHelp.Build(
            new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                QualityBar = "production",
                AnalyzableProjects = 3,
                ProductionLoc = 35954,
                Dimensions =
                [
                    new DimensionScore("D1", "code-quality", codeHealth, 0.95),
                    new DimensionScore("D5", "architecture", 7.1, 0.95),
                    new DimensionScore("D9", "testing", 7.0, 0.85),
                    new DimensionScore("D30", "security", 7.6, 0.90),
                ],
            },
            new DeliveryBuildRequest
            {
                DeliveryId = $"cd_{issuedAt}",
                IssuedAt = issuedAt,
                Subject = new DeliverySubject { Repository = "acme/checkout-api", Commit = "3f9a1c2", Host = "github.com" },
                Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor", ScannerVersion = "3.1.0" },
            });
}
