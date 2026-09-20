using Cai.Scoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Cai.Web.Badges;

/// <summary>
/// <c>GET /api/badge/{provider}/{owner}/{repo}.svg</c> — the CAI badge, issued by the standard on behalf of a surveyor.
/// </summary>
/// <remarks>
/// <para>WHY THE STANDARD SERVES THIS AT ALL. A badge asserts a score and a score has an issuer, so the reflex is to
/// let the issuer serve the image. That gets presentation wrong the moment there is more than one implementation:
/// "a CAI badge" would look like whatever each vendor drew, and the three rules on /badge would be a request rather
/// than a property. The measurement stays the issuer's; how CAI is PRESENTED is the standard's.</para>
/// <para>THE NUMBER IS RE-FOLDED, NOT REPEATED. The issuer's published evidence bundle is fetched and folded through
/// this repository's own reference scorer under the rubric the bundle names. So the badge states a number the
/// standard computed, which anybody can reproduce from the same public bundle — rather than a figure taken on trust
/// from the party being measured. It also means a badge cannot disagree with the scorer: they are the same code.</para>
/// <para>THE HOST IS PART OF THE ADDRESS. <c>acme/widgets</c> on GitHub and <c>acme/widgets</c> on GitLab are
/// different repositories, so an owner/name pair is a display name, not an identity, and a badge URL built from
/// one alone cannot say which project it is about. The provider segment is first because that is the order the
/// standard's own survey pages already use (<c>/surveys/github/{owner}/{repo}/</c>), and it is REQUIRED rather
/// than optional: a badge that silently picked a host would be wrong in exactly the cases nobody checks.</para>
/// <para>ONLY PUBLISHED REPOSITORIES. If the issuer does not publish evidence for the repository, this 404s. It
/// deliberately does not fall back to a zero or an "unknown" badge: a badge reading 0% for a repository nobody
/// measured is not a missing answer, it is a wrong one, and it renders in the reader's README as a real verdict.</para>
/// </remarks>
internal static class BadgeEndpoints
{
    // Long enough that a popular README costs the issuer almost nothing, short enough that a fresh survey shows up the
    // same day. The rendered bytes are cached, so a hit costs no fetch, no parse and no fold.
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(30);

    public static void MapBadgeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/badge/{provider}/{owner}/{repo}.svg", [AllowAnonymous] async (
            string provider,
            string owner,
            string repo,
            string? issuer,
            HttpContext http,
            IssuerCatalog issuers,
            RubricCatalogStore rubrics,
            IHttpClientFactory clients,
            IMemoryCache cache,
            ILoggerFactory logs,
            CancellationToken ct) =>
        {
            ApiAccess.EnsureAllowed(http);

            var chosen = issuer is { Length: > 0 }
                ? (issuers.TryGet(issuer, out var found) ? found : null)
                : issuers.Default;
            if (chosen is null)
            {
                return Results.BadRequest(new
                {
                    error = "name an issuer with ?issuer=",
                    issuers = issuers.Ids,
                });
            }

            if (!GitHost.IsKnown(provider))
            {
                return Results.BadRequest(new { error = $"unknown git host '{provider}'", hosts = GitHost.Known });
            }

            var key = $"badge:{chosen.Id}:{provider}:{owner}:{repo}";
            if (cache.TryGetValue(key, out string? cached) && cached is not null)
            {
                return Svg(cached);
            }

            string bundleJson;
            try
            {
                var client = clients.CreateClient("watchdog");
                // Only ever the issuer's published-corpus route, composed here from an allowlisted origin and the
                // route values — never a URL the caller supplied.
                // The host travels to the issuer too. Without it the issuer resolves the pair its own way, and a
                // badge that named a host in its URL would be free to show another one's score.
                var url = $"{chosen.BaseUrl.TrimEnd('/')}/api/public/oss/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/evidence"
                        + $"?provider={Uri.EscapeDataString(GitHost.Canonical(provider))}";
                using var res = await client.GetAsync(url, ct);
                if (!res.IsSuccessStatusCode)
                {
                    return Results.NotFound(new { error = "no published survey for this repository" });
                }

                bundleJson = await res.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // The issuer is unreachable. 503 rather than a badge: an image that silently reports a stale or
                // invented number is worse than a broken image, because only one of the two is obviously broken.
                logs.CreateLogger("Cai.Web.Badges").LogWarning(ex, "Issuer {Issuer} unreachable while rendering a badge.", chosen.Id);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            EvidenceBundle bundle;
            try
            {
                bundle = EvidenceBundle.Parse(bundleJson);
            }
            catch (Exception ex)
            {
                logs.CreateLogger("Cai.Web.Badges").LogWarning(ex, "Issuer {Issuer} published an unparseable bundle for {Owner}/{Repo}.", chosen.Id, owner, repo);
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            // The rubric must be one THIS archive publishes, or the number cannot be reproduced from anything the
            // standard stands behind. An issuer naming a rubric we do not carry gets no badge.
            var catalog = rubrics.Get(bundle.RubricVersion);
            if (catalog is null)
            {
                return Results.NotFound(new { error = $"rubric '{bundle.RubricVersion}' is not in the published archive" });
            }

            var score = CaiScorer.Score(bundle, catalog);
            var svg = BadgeSvg.Render(owner, repo, score.Headline, score.Band, score.RubricVersion, chosen.Name);

            cache.Set(key, svg, CacheFor);
            return Svg(svg);
        });
    }

    private static IResult Svg(string svg) =>
        Results.Text(svg, "image/svg+xml", System.Text.Encoding.UTF8);
}
