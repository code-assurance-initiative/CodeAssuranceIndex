using Cai.Web.Badges;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cai.Tests;

/// <summary>Decision 0007: the default badge issuer is the Canine Code Analyzer, and the retired id <c>watchdog</c> stays an alias
/// of it for good, so every badge URL already in a README keeps drawing.</summary>
public sealed class IssuerCatalogTests
{
    private static IssuerCatalog Catalog(Dictionary<string, string?>? settings = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build());

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
