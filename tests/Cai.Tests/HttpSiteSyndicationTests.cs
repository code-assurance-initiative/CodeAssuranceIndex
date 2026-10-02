using System.Net;
using System.Text;
using System.Text.Json;
using Cai.Pages;
using Cai.Web.Registry;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The wire format the publisher speaks to the site's authoring API — the one part of syndication no other
/// test reaches, because every sweep test runs against an in-memory site.
/// </summary>
/// <remarks>
/// ★★ THE CLIENT IS DELIBERATELY THIN, SO WHAT IT GETS WRONG IS THE CONTRACT: the URL a page lands at, the
/// token that authorises it, and whether a refusal surfaces with the site's own explanation or as a bare status.
/// Those are pinned here against a recording transport, not a live site.
/// </remarks>
public sealed class HttpSiteSyndicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly SurveyPage Page = new(
        "surveys/github/acme/api",
        "acme/api",
        "acme/api scored 72.4 on the Code Assurance Index.",
        new Dictionary<string, object?> { ["type"] = "section", ["children"] = Array.Empty<object>() });

    [Fact]
    public async Task Publish_PUTs_the_page_at_its_path_under_the_site_with_the_bearer_token()
    {
        var site = new RecordingSite(HttpStatusCode.OK, """{"changed":true}""");

        var changed = await Syndication(site).PublishAsync(Page, Ct);

        Assert.True(changed);
        var request = Assert.Single(site.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal(
            "https://site.example/api/authoring/sites/site-1/syndicated/surveys/github/acme/api",
            request.Uri);
        Assert.Equal("Bearer secret-token", request.Authorization);

        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Equal("acme/api", body.GetProperty("title").GetString());
        Assert.Equal("acme/api", body.GetProperty("metaTitle").GetString());
        Assert.Equal(Page.MetaDescription, body.GetProperty("metaDescription").GetString());
        Assert.Equal("section", body.GetProperty("node").GetProperty("type").GetString());
    }

    [Fact]
    public async Task An_unchanged_page_says_so()
    {
        var site = new RecordingSite(HttpStatusCode.OK, """{"changed":false}""");

        Assert.False(await Syndication(site).PublishAsync(Page, Ct));
    }

    /// <summary>
    /// ★★ A REFUSAL CARRIES THE SITE'S OWN SENTENCE. An exception saying only "400" sends whoever reads the log
    /// into this code instead of into the explanation the site already gave.
    /// </summary>
    [Fact]
    public async Task A_refused_publish_throws_with_the_status_the_path_and_the_sites_explanation()
    {
        var site = new RecordingSite(HttpStatusCode.BadRequest, """{"error":"node type 'section' is not allowed here"}""");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Syndication(site).PublishAsync(Page, Ct));

        Assert.Contains("publish 'surveys/github/acme/api'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("400", ex.Message, StringComparison.Ordinal);
        Assert.Contains("is not allowed here", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_long_refusal_is_cut_to_four_hundred_characters()
    {
        var site = new RecordingSite(HttpStatusCode.InternalServerError, new string('x', 1000));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Syndication(site).PublishAsync(Page, Ct));

        Assert.Contains(new string('x', 400), ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 401), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Withdraw_DELETEs_the_path_and_reports_whether_anything_was_removed()
    {
        var site = new RecordingSite(HttpStatusCode.OK, """{"removed":true}""");

        Assert.True(await Syndication(site).WithdrawAsync("surveys/github/acme/api", Ct));

        var request = Assert.Single(site.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.EndsWith("/api/authoring/sites/site-1/syndicated/surveys/github/acme/api", request.Uri, StringComparison.Ordinal);
    }

    /// <summary>★ A page that is already gone is the state the withdrawal wanted, not a failure.</summary>
    [Fact]
    public async Task Withdrawing_a_page_the_site_does_not_have_is_not_an_error()
    {
        var site = new RecordingSite(HttpStatusCode.NotFound, """{"error":"no such page"}""");

        Assert.False(await Syndication(site).WithdrawAsync("surveys/gone", Ct));
    }

    [Fact]
    public async Task A_failed_withdrawal_throws()
    {
        var site = new RecordingSite(HttpStatusCode.Forbidden, """{"error":"token cannot delete"}""");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => Syndication(site).WithdrawAsync("surveys/github/acme/api", Ct));
        Assert.Contains("withdraw", ex.Message, StringComparison.Ordinal);
        Assert.Contains("token cannot delete", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_GETs_the_syndicated_set_and_returns_its_paths_skipping_blanks()
    {
        var site = new RecordingSite(HttpStatusCode.OK,
            """{"pages":[{"path":"surveys/a"},{"path":""},{"path":"surveys/b"}]}""");

        var paths = await Syndication(site).ListAsync(Ct);

        Assert.Equal(["surveys/a", "surveys/b"], paths);
        var request = Assert.Single(site.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://site.example/api/authoring/sites/site-1/syndicated", request.Uri);
    }

    [Fact]
    public async Task A_site_listing_no_pages_is_an_empty_list()
    {
        var site = new RecordingSite(HttpStatusCode.OK, "{}");

        Assert.Empty((await Syndication(site).ListAsync(Ct))!);
    }

    [Fact]
    public async Task A_failed_listing_throws_without_naming_a_path()
    {
        var site = new RecordingSite(HttpStatusCode.ServiceUnavailable, "down");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Syndication(site).ListAsync(Ct));
        Assert.StartsWith("Site list failed: 503", ex.Message, StringComparison.Ordinal);
    }

    private static HttpSiteSyndication Syndication(RecordingSite site) =>
        new(new HttpClient(site), Options.Create(new SyndicationOptions
        {
            SiteBaseUrl = "https://site.example/", // ★ a trailing slash must not double up in the path
            SiteId = "site-1",
            Token = "secret-token",
        }));

    private sealed record Recorded(HttpMethod Method, string Uri, string? Authorization, string? Body);

    private sealed class RecordingSite(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public List<Recorded> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new Recorded(
                request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
