namespace Cai.Web.Registry;

/// <summary>
/// Config for publishing the standard's own pages to the site that serves them.
/// </summary>
/// <remarks>
/// <para>FAIL-CLOSED: the publisher is off unless it has a site and a token. Nothing here has a default
/// that would make an unconfigured environment start writing to a live public site — a missing token means
/// "do not publish", never "publish anonymously".</para>
/// <para>★ THERE IS NO SEPARATE "ENABLED" FLAG, DELIBERATELY. Credentials are not a feature switch: a
/// deployment that has been given a site and a token has been told to publish, and an extra boolean beside
/// them is one more place for the answer to be no for a reason nobody can see.</para>
/// </remarks>
public sealed class SyndicationOptions
{
    /// <summary>The config section (<c>Syndication</c>).</summary>
    public const string Section = "Syndication";

    /// <summary>The authoring API's base address, e.g. <c>https://app.imprint.canine.dev</c>.</summary>
    public string? SiteBaseUrl { get; set; }

    /// <summary>The id of the site the pages belong to.</summary>
    public string? SiteId { get; set; }

    /// <summary>The authoring bearer token. Absent means the publisher stays off.</summary>
    public string? Token { get; set; }

    /// <summary>The sweep interval, in minutes (floored at 5).</summary>
    public int TickMinutes { get; set; } = 60;

    /// <summary>
    /// Everything the publisher needs is present. Checked rather than assumed, so a half-configured
    /// environment is inert instead of half-publishing.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SiteBaseUrl)
        && !string.IsNullOrWhiteSpace(SiteId)
        && !string.IsNullOrWhiteSpace(Token);
}
