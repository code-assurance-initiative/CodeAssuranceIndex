using Cai.Pages;
using Xunit;

namespace Cai.Tests;

/// <summary>Decision 0007: the published listing names the products that measure through the Canine Code Analyzer, and Watchdog
/// stays listed as a retired producer, so what it produced is still shown as its own.</summary>
public sealed class ProducerListingRetiredTests
{
    private static readonly ProducerListing Published = ProducerListing.Published;

    [Theory]
    [InlineData("assure.canine.dev", "Assure")]
    [InlineData("assert.canine.dev", "Assert")]
    [InlineData("assay.canine.dev", "Assay")]
    [InlineData("cca.canine.dev", "Canine Code Analyzer")]
    public void The_products_are_listed_and_producing(string producer, string name)
    {
        var listed = Published.Find(producer);
        Assert.NotNull(listed);
        Assert.Equal(name, listed!.Name);
        Assert.Null(listed.Retired);
    }

    [Fact]
    public void Watchdog_stays_listed_as_retired_and_keeps_its_report_host()
    {
        var watchdog = Published.Find("watchdog.canine.dev");
        Assert.NotNull(watchdog);
        Assert.Equal("Watchdog", watchdog!.Name);
        Assert.Equal("2026-11-04", watchdog.Retired);
        // The reports it already published live on its own host; the system keeps that host so they are still shown.
        Assert.Equal("https://watchdog.canine.dev/", Assert.Single(watchdog.Systems).Homepage);
    }

    [Fact]
    public void A_retirement_day_that_is_not_a_date_is_refused()
    {
        const string json = """
            {"producers":[{"producer":"p.example","name":"P","homepage":"https://p.example/","mark":"doc","retired":"soon",
              "systems":[{"id":"p","name":"P","homepage":"https://p.example/","mark":"doc"}]}]}
            """;
        Assert.Throws<InvalidDataException>(() => ProducerListing.Parse(json));
    }
}
