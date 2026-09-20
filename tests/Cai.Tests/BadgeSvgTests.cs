using Cai.Scoring;
using Cai.Web.Badges;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The badge renderer. These are unit tests over pure output — the endpoint's fetch/fold path is exercised separately.
/// </summary>
public sealed class BadgeSvgTests
{
    private static string Render(string owner = "acme", string repo = "widgets", double headline = 86.8,
        Band band = Band.Healthy, string rubric = "rubric-2026.09.15", string issuer = "watchdog.canine.dev") =>
        BadgeSvg.Render(owner, repo, headline, band, rubric, issuer);

    [Fact]
    public void The_badge_names_the_repository_it_is_about()
    {
        // THE MISUSE THIS PREVENTS: a badge that shows only a score can be copied out of the strongest repository's
        // README into a weaker one and still look correct. Naming the subject in the image makes that visible.
        Assert.Contains("CAI acme/widgets", Render());
    }

    [Fact]
    public void The_band_word_is_the_published_vocabulary_not_the_enum_token()
    {
        // Band.Healthy is a POSITIONAL rank token; the published display word is "Strong". A badge showing "Healthy"
        // would be using an internal key as public vocabulary.
        var svg = Render(band: Band.Healthy);
        Assert.Contains("Strong", svg);
        Assert.DoesNotContain("Healthy</text>", svg);
    }

    [Fact]
    public void The_rubric_version_and_issuer_reach_the_accessible_label()
    {
        // Rule 2 on /badge: a badge must state the rubric version. It does not fit on the face, so it must at least
        // be in the accessible name — stating it only in a page the reader has to open is not stating it.
        var svg = Render(rubric: "rubric-2026.09.15", issuer: "watchdog.canine.dev");
        Assert.Contains("rubric-2026.09.15", svg);
        Assert.Contains("watchdog.canine.dev", svg);
    }

    [Theory]
    [InlineData(Band.Critical, "#c94f43")]
    [InlineData(Band.Poor, "#d97f3e")]
    [InlineData(Band.Fair, "#c9a13b")]
    [InlineData(Band.Healthy, "#55a06c")]
    [InlineData(Band.Exemplary, "#2e8f75")]
    public void Each_band_uses_the_published_CAI_palette(Band band, string colour)
    {
        // docs/brand/README.md carries these five, worst→best. They are the standard's, so the colour means the same
        // thing on every issuer's badge — which is only true while one renderer draws them all.
        Assert.Contains(colour, Render(band: band));
    }

    [Fact]
    public void A_hostile_repository_name_cannot_break_out_of_the_svg()
    {
        // owner/repo arrive from the route, so they are untrusted text in an XML document.
        var svg = Render(repo: "</text><script>alert(1)</script>");
        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);
    }

    [Fact]
    public void Rendering_is_deterministic()
    {
        Assert.Equal(Render(), Render());
    }

    [Fact]
    public void The_panel_is_wide_enough_for_its_text()
    {
        // An SVG cannot measure its own text, so the width is estimated. A long name overflowing its panel is the
        // failure this guards: the declared width must grow with the label.
        var shortB = Render(owner: "a", repo: "b");
        var longB = Render(owner: "a-very-long-organisation-name", repo: "and-a-long-repository-name");
        Assert.True(Width(longB) > Width(shortB) + 200, $"width did not track the label: {Width(shortB)} vs {Width(longB)}");
    }

    private static int Width(string svg) =>
        int.Parse(System.Text.RegularExpressions.Regex.Match(svg, @"width=""(\d+)""").Groups[1].Value);
}

/// <summary>
/// The badge's ADDRESS. owner/name is a display pair, not an identity — the same pair can name different
/// repositories on different hosts — so the host is part of the URL.
/// </summary>
public sealed class BadgeAddressTests(RegistryUnconfiguredFixture fx) : IClassFixture<RegistryUnconfiguredFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_unknown_git_host_is_refused_rather_than_guessed()
    {
        // A badge that quietly picked a host would be wrong in exactly the cases nobody checks.
        using var client = fx.Client(token: null);
        var res = await client.GetAsync("/api/badge/nosuchhost/acme/widgets.svg", Ct);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task The_old_hostless_address_is_gone()
    {
        // /api/badge/{owner}/{repo}.svg no longer routes: it could not say which project it meant. What matters
        // is that it does not serve a badge — the exact status is the API's default-deny fallback for an unmatched
        // /api path (401), not something this endpoint chooses.
        using var client = fx.Client(token: null);
        var res = await client.GetAsync("/api/badge/acme/widgets.svg", Ct);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
        Assert.NotEqual("image/svg+xml", res.Content.Headers.ContentType?.MediaType);
    }
}
