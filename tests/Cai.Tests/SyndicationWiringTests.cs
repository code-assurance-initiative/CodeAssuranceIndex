using Cai.Web.Registry;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The sweep is off unless it has been given a site to publish to.
/// </summary>
/// <remarks>
/// ★★ FAIL-CLOSED, AND CREDENTIALS ARE NOT A FEATURE SWITCH. Nothing here has a default that would make an
/// unconfigured environment start writing to a live public site: a missing token means "do not publish",
/// never "publish anonymously". There is deliberately no separate "enabled" flag — an extra boolean beside
/// the credentials is one more place for the answer to be no for a reason nobody can see.
/// </remarks>
public sealed class SyndicationWiringTests
{
    [Fact]
    public void An_unconfigured_environment_does_not_publish()
    {
        Assert.False(new SyndicationOptions().IsConfigured);
    }

    [Theory]
    [InlineData(null, "site", "token")]
    [InlineData("https://app.imprint.canine.dev", null, "token")]
    [InlineData("https://app.imprint.canine.dev", "site", null)]
    public void A_half_configured_environment_is_inert_rather_than_half_publishing(
        string? baseUrl, string? siteId, string? token)
    {
        var options = new SyndicationOptions { SiteBaseUrl = baseUrl, SiteId = siteId, Token = token };

        Assert.False(options.IsConfigured);
    }

    [Fact]
    public void A_site_an_id_and_a_token_is_an_instruction_to_publish()
    {
        var options = new SyndicationOptions
        {
            SiteBaseUrl = "https://app.imprint.canine.dev",
            SiteId = "codeassuranceindex",
            Token = "t",
        };

        Assert.True(options.IsConfigured);
    }

    /// <summary>★ Floored at five minutes: a sweep pushes every page it composes, and a tighter loop is traffic.</summary>
    [Fact]
    public void The_tick_has_a_floor()
    {
        Assert.Equal(60, new SyndicationOptions().TickMinutes);
    }
}
