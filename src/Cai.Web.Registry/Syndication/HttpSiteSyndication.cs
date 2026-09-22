using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cai.Pages;
using Microsoft.Extensions.Options;

namespace Cai.Web.Registry;

/// <summary>
/// Talks to the site's authoring API over HTTP: <c>PUT/DELETE/GET
/// /api/authoring/sites/{siteId}/syndicated</c>, authenticated with the configured bearer token.
/// </summary>
/// <remarks>
/// ★ DELIBERATELY THIN. It owns the wire format and nothing else — no retry policy, no deciding which pages
/// should exist. A failed push throws and the sweep decides what that means; swallowing it here would let
/// the site drift from the deliveries while every log line said it was fine.
/// </remarks>
public sealed class HttpSiteSyndication : ISiteSyndication
{
    /// <summary>The named HTTP client this publisher uses.</summary>
    public const string ClientName = "cai-site-syndication";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SyndicationOptions _options;

    /// <summary>Create the client over a configured <see cref="HttpClient"/>.</summary>
    public HttpSiteSyndication(HttpClient http, IOptions<SyndicationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        _http = http;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.SiteBaseUrl))
        {
            _http.BaseAddress = new Uri(_options.SiteBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        }
    }

    private string Base => $"api/authoring/sites/{_options.SiteId}/syndicated";

    /// <inheritdoc />
    public async Task<bool> PublishAsync(SurveyPage page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var body = new PushBody(page.Title, page.Title, page.MetaDescription, page.Node);
        using var response = await _http
            .PutAsJsonAsync($"{Base}/{page.Path}", body, Json, cancellationToken)
            .ConfigureAwait(false);
        await ThrowIfFailedAsync(response, "publish", page.Path, cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync<PushResult>(Json, cancellationToken)
            .ConfigureAwait(false);
        return result?.Changed ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> WithdrawAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.DeleteAsync($"{Base}/{path}", cancellationToken).ConfigureAwait(false);

        // A page that is already gone is the state we wanted; only a real failure is worth raising.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await ThrowIfFailedAsync(response, "withdraw", path, cancellationToken).ConfigureAwait(false);
        var result = await response.Content
            .ReadFromJsonAsync<RemoveResult>(Json, cancellationToken)
            .ConfigureAwait(false);
        return result?.Removed ?? false;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>?> ListAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(Base, cancellationToken).ConfigureAwait(false);
        await ThrowIfFailedAsync(response, "list", string.Empty, cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync<ListResult>(Json, cancellationToken)
            .ConfigureAwait(false);
        return [.. (result?.Pages ?? []).Select(p => p.Path).Where(p => !string.IsNullOrWhiteSpace(p))];
    }

    // The site answers a rejection with a sentence explaining it ({"error": "..."}), and that sentence is the
    // whole value of the failure — an exception saying only "400" would send someone reading this code
    // instead of the body.
    private static async Task ThrowIfFailedAsync(
        HttpResponseMessage response, string verb, string path, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var detail = body.Length > 400 ? body[..400] : body;
        var where = string.IsNullOrEmpty(path) ? string.Empty : $" '{path}'";
        throw new HttpRequestException(
            $"Site {verb}{where} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {detail}");
    }

    private sealed record PushBody(
        string Title,
        string MetaTitle,
        string MetaDescription,
        [property: JsonPropertyName("node")] IReadOnlyDictionary<string, object?> Node);

    private sealed record PushResult(bool Changed);

    private sealed record RemoveResult(bool Removed);

    private sealed record ListResult(IReadOnlyList<ListedPage>? Pages);

    private sealed record ListedPage(string Path);
}
