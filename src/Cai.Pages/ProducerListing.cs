using System.Text.Json;
using System.Text.Json.Serialization;
using Cai.Delivery;

namespace Cai.Pages;

/// <summary>A system a listed producer may name as the author of a report.</summary>
/// <param name="Id">What a delivery's <c>reports[].system</c> names.</param>
/// <param name="Name">How a card names it: "The full report by {Name}".</param>
/// <param name="Homepage">The system's home; a report URL must be on this host or a subdomain of it.</param>
/// <param name="Mark">The mark the site draws on the card.</param>
public sealed record ListedSystem(string Id, string Name, string Homepage, string Mark);

/// <summary>A producer the standard lists, and the systems it may name.</summary>
/// <param name="Producer">The authenticated producer name the registry stamps into every delivery.</param>
/// <param name="Name">How a card names it: "Measured by {Name}".</param>
/// <param name="Homepage">Where the "Measured by" card links.</param>
/// <param name="Mark">The mark the site draws on that card.</param>
/// <param name="Systems">The systems it may name as the author of a report.</param>
/// <param name="Retired">The day the producer was retired (decision 0007), or null while it produces. A retired producer's
/// deliveries stay exactly as signed and are still shown as its own.</param>
/// <summary>A producer's badge issuer, as data in the listing (decision 0012): the id a badge URL names (<c>?issuer=</c>),
/// the https origin the badge endpoint fetches the producer's public evidence from, and ids kept as aliases of it.</summary>
public sealed record ListedBadgeIssuer(string Id, string BaseUrl, IReadOnlyList<string> Aliases);

public sealed record ListedProducer(
    string Producer, string Name, string Homepage, string Mark, IReadOnlyList<ListedSystem> Systems, string? Retired = null,
    ListedBadgeIssuer? Badges = null);

/// <summary>
/// Who may put a report link on the standard's survey pages, and under which name and mark (decision 0003).
/// </summary>
/// <remarks>
/// <para>★★ THE LISTING IS THE STANDARD'S, NOT THE PRODUCER'S. A delivery carries a URL and a system id, both
/// signed; everything a reader sees beside them, the words, the destination of the producer card and the mark,
/// comes from <c>producers/producers.json</c> in this repository, changed only by pull request. A producer that
/// could choose its own name or mark on these pages could dress any link as anyone's.</para>
/// <para>★★ THE HOST RULE IS WHAT MAKES THE MARK TRUE. A card wearing a system's mark has to lead to that
/// system, so a report URL counts only on the system's own host or a subdomain of it, and only over https.
/// Matched on whole labels: <c>watchdog.canine.dev.evil.example.com</c> is not under <c>watchdog.canine.dev</c>.</para>
/// <para>★ A BROKEN LISTING FAILS AT LOAD. An entry missing a field would print an empty card on every page it
/// touched; refusing it is louder and happens in the test suite, before a deploy.</para>
/// </remarks>
public sealed class ProducerListing
{
    private const string ResourceName = "Cai.Pages.producers.json";

    private readonly Dictionary<string, ListedProducer> _byProducer;

    private ProducerListing(IReadOnlyList<ListedProducer> producers)
    {
        Producers = producers;
        _byProducer = producers.ToDictionary(p => p.Producer, StringComparer.Ordinal);
    }

    /// <summary>Every listed producer, in listing order.</summary>
    public IReadOnlyList<ListedProducer> Producers { get; }

    /// <summary>The listing this repository publishes (<c>producers/producers.json</c>, embedded).</summary>
    public static ProducerListing Published { get; } = LoadPublished();

    /// <summary>A listing with nobody on it: no page links any report.</summary>
    public static ProducerListing Empty { get; } = new([]);

    /// <summary>The listed producer with this authenticated name, or null.</summary>
    public ListedProducer? Find(string? producer) =>
        producer is not null && _byProducer.TryGetValue(producer, out var listed) ? listed : null;

    /// <summary>
    /// The listed system a report may be shown under, or null when the report must not be shown: the producer
    /// is not listed, the system is not one of its own, or the URL is not https on the system's host.
    /// </summary>
    public ListedSystem? Resolve(string? producer, DeliveryReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (Find(producer) is not { } listed
            || listed.Systems.FirstOrDefault(s => string.Equals(s.Id, report.System, StringComparison.Ordinal)) is not { } system)
        {
            return null;
        }

        return IsHttpsOn(report.Url, system.Homepage) ? system : null;
    }

    /// <summary>Parse a listing document, refusing an entry that is missing what a card needs.</summary>
    /// <exception cref="InvalidDataException">The document is malformed or an entry is incomplete.</exception>
    public static ProducerListing Parse(string json)
    {
        var document = JsonSerializer.Deserialize<ListingDocument>(json)
                       ?? throw new InvalidDataException("The producer listing is empty.");
        var producers = new List<ListedProducer>();
        foreach (var entry in document.Producers ?? [])
        {
            var systems = (entry.Systems ?? []).Select(s => new ListedSystem(
                Required(s.Id, "system id"),
                Required(s.Name, $"name of system '{s.Id}'"),
                Https(s.Homepage, $"homepage of system '{s.Id}'"),
                Required(s.Mark, $"mark of system '{s.Id}'"))).ToList();
            producers.Add(new ListedProducer(
                Required(entry.Producer, "producer"),
                Required(entry.Name, $"name of producer '{entry.Producer}'"),
                Https(entry.Homepage, $"homepage of producer '{entry.Producer}'"),
                Required(entry.Mark, $"mark of producer '{entry.Producer}'"),
                systems,
                Day(entry.Retired, $"retirement day of producer '{entry.Producer}'"),
                entry.Badges is { } b
                    ? new ListedBadgeIssuer(Required(b.Id, $"badge issuer id of producer '{entry.Producer}'"),
                        Https(b.BaseUrl, $"badge issuer base URL of producer '{entry.Producer}'"),
                        [.. (b.Aliases ?? []).Where(a => !string.IsNullOrWhiteSpace(a))])
                    : null));
        }

        if (producers.GroupBy(p => p.Producer, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1) is { } twice)
        {
            throw new InvalidDataException($"The producer '{twice.Key}' is listed twice.");
        }

        return new ProducerListing(producers);
    }

    private static ProducerListing LoadPublished()
    {
        using var stream = typeof(ProducerListing).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"The embedded producer listing '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Whether <paramref name="url"/> is https on <paramref name="homepage"/>'s host or a subdomain of it.</summary>
    private static bool IsHttpsOn(string? url, string homepage)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var target) || target.Scheme != Uri.UriSchemeHttps
            || !Uri.TryCreate(homepage, UriKind.Absolute, out var home))
        {
            return false;
        }

        var host = target.IdnHost.ToLowerInvariant();
        var own = home.IdnHost.ToLowerInvariant();
        return host == own || host.EndsWith("." + own, StringComparison.Ordinal);
    }

    private static string Required(string? value, string what) =>
        value is { Length: > 0 } present && !string.IsNullOrWhiteSpace(present)
            ? present
            : throw new InvalidDataException($"The producer listing is missing the {what}.");

    private static string? Day(string? value, string what) =>
        value is null ? null
        : DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
              System.Globalization.DateTimeStyles.None, out _) ? value
        : throw new InvalidDataException($"The {what} must be a yyyy-MM-dd date.");

    private static string Https(string? value, string what) =>
        Uri.TryCreate(Required(value, what), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? value!
            : throw new InvalidDataException($"The {what} must be an absolute https URL.");

    private sealed class ListingDocument
    {
        [JsonPropertyName("producers")] public List<ListingProducer>? Producers { get; init; }
    }

    private sealed class ListingProducer
    {
        [JsonPropertyName("producer")] public string? Producer { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("homepage")] public string? Homepage { get; init; }
        [JsonPropertyName("mark")] public string? Mark { get; init; }
        [JsonPropertyName("systems")] public List<ListingSystem>? Systems { get; init; }
        [JsonPropertyName("retired")] public string? Retired { get; init; }
        [JsonPropertyName("badges")] public ListingBadges? Badges { get; init; }
    }

    private sealed class ListingBadges
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("baseUrl")] public string? BaseUrl { get; init; }
        [JsonPropertyName("aliases")] public List<string>? Aliases { get; init; }
    }

    private sealed class ListingSystem
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("homepage")] public string? Homepage { get; init; }
        [JsonPropertyName("mark")] public string? Mark { get; init; }
    }
}
