using Microsoft.Extensions.Configuration;

namespace Cai.Web.Badges;

/// <summary>One surveyor the standard will render a badge for.</summary>
/// <param name="Id">The short key used in <c>?issuer=</c>.</param>
/// <param name="Name">How the issuer is named on the badge's accessible label.</param>
/// <param name="BaseUrl">Origin of the issuer's PUBLIC API. Only the published-corpus routes are ever called.</param>
internal sealed record Issuer(string Id, string Name, string BaseUrl);

/// <summary>
/// The issuers the badge endpoint will fetch evidence from, read from configuration (<c>Badges:Issuers</c>).
/// </summary>
/// <remarks>
/// <para>AN ALLOWLIST, NOT A PARAMETER. The obvious shape for "render a badge from an issuer" is to take the issuer's
/// URL in the query string, and it must not be built that way: this endpoint would then fetch any URL an anonymous
/// caller names, from inside the standard's own network, which is a server-side request forgery primitive wearing a
/// badge. Issuers are configured, and anything else is a 400.</para>
/// <para>It is a LIST because there will be more than one. There is exactly one issuer today, and writing that
/// assumption into the code as a hardcoded host is how a standard quietly becomes one vendor's API — the thing the
/// /badge policy explicitly says it is not. A second issuer is a config entry, not a code change.</para>
/// </remarks>
internal sealed class IssuerCatalog
{
    private readonly IReadOnlyDictionary<string, Issuer> _byId;

    public IssuerCatalog(IConfiguration config) : this(config, Cai.Pages.ProducerListing.Published)
    {
    }

    /// <summary>Configured issuers (<c>Badges:Issuers</c>) when an operator sets them; otherwise the badge issuers the
    /// producer listing declares (decision 0012: CAI names no producer in code, only as data in the listing). An issuer's
    /// listed aliases resolve to it unless something configures that id itself; decision 0007's retired id stays an alias
    /// this way, declared by the listing rather than written into the standard's code.</summary>
    internal IssuerCatalog(IConfiguration config, Cai.Pages.ProducerListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        var configured = config.GetSection("Badges:Issuers").Get<List<Issuer>>() ?? [];
        var aliases = new List<(string Alias, string Of)>();
        if (configured.Count == 0)
        {
            foreach (var p in listing.Producers.Where(p => p.Badges is not null && p.Retired is null))
            {
                configured.Add(new Issuer(p.Badges!.Id, p.Producer, p.Badges.BaseUrl));
                aliases.AddRange(p.Badges.Aliases.Select(a => (a, p.Badges.Id)));
            }
        }

        var byId = configured
            .Where(i => !string.IsNullOrWhiteSpace(i.Id) && Uri.TryCreate(i.BaseUrl, UriKind.Absolute, out var u)
                        && u.Scheme == Uri.UriSchemeHttps)
            .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var (alias, of) in aliases)
        {
            if (!byId.ContainsKey(alias) && byId.TryGetValue(of, out var issuer))
            {
                byId[alias] = issuer;
            }
        }

        _byId = byId;
    }

    /// <summary>The issuer used when the caller names none — meaningful only while there is exactly one (an alias is the
    /// same issuer, not a second one).</summary>
    public Issuer? Default => _byId.Values.Distinct().Count() == 1 ? _byId.Values.First() : null;

    public bool TryGet(string id, out Issuer issuer) => _byId.TryGetValue(id, out issuer!);

    public IReadOnlyCollection<string> Ids => _byId.Keys.ToList();
}
