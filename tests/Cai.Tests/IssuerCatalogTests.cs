using Cai.Web.Badges;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cai.Tests;

/// <summary>Decision 0012: the standard's code names no issuer; the badge issuers are the producer listing's (data), or an
/// operator's configuration. Decision 0007's promise still holds, declared by the listing: the retired id <c>watchdog</c>
/// is an alias of the cca issuer, so every badge URL already in a README keeps drawing.</summary>
public sealed class IssuerCatalogTests
{
    private static IssuerCatalog Catalog(Dictionary<string, string?>? settings = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build());

    private static Cai.Pages.ProducerListing Listing(string producers) =>
        Cai.Pages.ProducerListing.Parse($"{{\"producers\": [{producers}]}}");

    private const string Plain = "{\"producer\": \"p.example\", \"name\": \"P\", \"homepage\": \"https://p.example/\", \"mark\": \"html\", \"systems\": []}";

    [Fact]
    public void The_code_names_no_issuer_a_listing_without_badges_serves_none()
    {
        var catalog = new IssuerCatalog(new ConfigurationBuilder().Build(), Listing(Plain));
        Assert.Null(catalog.Default);
        Assert.False(catalog.TryGet("cca", out _));
        Assert.False(catalog.TryGet("watchdog", out _));
    }

    [Fact]
    public void A_listed_badge_issuer_and_its_aliases_are_served()
    {
        var listed = "{\"producer\": \"q.example\", \"name\": \"Q\", \"homepage\": \"https://q.example/\", \"mark\": \"html\", \"systems\": [], "
                     + "\"badges\": {\"id\": \"q\", \"baseUrl\": \"https://api.q.example\", \"aliases\": [\"old-q\"]}}";
        var catalog = new IssuerCatalog(new ConfigurationBuilder().Build(), Listing(Plain + ", " + listed));
        Assert.True(catalog.TryGet("q", out var q));
        Assert.True(catalog.TryGet("old-q", out var old));
        Assert.Same(q, old);
        Assert.Equal("https://api.q.example", q.BaseUrl);
    }

    [Fact]
    public void With_no_configuration_the_default_issuer_is_cca()
    {
        var catalog = Catalog();
        Assert.NotNull(catalog.Default);
        Assert.Equal("cca", catalog.Default!.Id);
        Assert.Equal("https://cca.canine.dev", catalog.Default.BaseUrl);
    }

    [Fact]
    public void The_retired_watchdog_id_resolves_to_the_same_issuer_and_is_not_a_second_one()
    {
        var catalog = Catalog();
        Assert.True(catalog.TryGet("watchdog", out var alias));
        Assert.True(catalog.TryGet("cca", out var cca));
        Assert.Same(cca, alias);
        Assert.NotNull(catalog.Default);   // one issuer under two ids is still one issuer
    }

    [Fact]
    public void An_operator_who_configures_watchdog_themselves_keeps_their_own_entry()
    {
        var catalog = Catalog(new Dictionary<string, string?>
        {
            ["Badges:Issuers:0:Id"] = "cca", ["Badges:Issuers:0:Name"] = "cca.canine.dev", ["Badges:Issuers:0:BaseUrl"] = "https://cca.canine.dev",
            ["Badges:Issuers:1:Id"] = "watchdog", ["Badges:Issuers:1:Name"] = "other", ["Badges:Issuers:1:BaseUrl"] = "https://other.example",
        });
        Assert.True(catalog.TryGet("watchdog", out var watchdog));
        Assert.Equal("https://other.example", watchdog.BaseUrl);
        Assert.Null(catalog.Default);   // two real issuers: the caller must name one
    }
}
