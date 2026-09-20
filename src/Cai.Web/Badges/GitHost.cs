namespace Cai.Web.Badges;

/// <summary>
/// The git hosts a badge URL may name.
/// </summary>
/// <remarks>
/// An allowlist rather than free text, for the same reason the issuer is one: the value is forwarded to the issuer,
/// and a segment the standard does not recognise should fail here rather than travel onward and be interpreted by
/// somebody else. The spellings are the ones the standard's survey pages already use in their own URLs.
/// </remarks>
internal static class GitHost
{
    private static readonly Dictionary<string, string> Canonicalised = new(StringComparer.OrdinalIgnoreCase)
    {
        ["github"] = "GitHub",
        ["gitlab"] = "GitLab",
        ["bitbucket"] = "Bitbucket",
        ["azuredevops"] = "AzureDevOps",
    };

    public static IReadOnlyCollection<string> Known => Canonicalised.Keys.ToList();

    public static bool IsKnown(string? host) => host is not null && Canonicalised.ContainsKey(host);

    /// <summary>The spelling the issuer's API expects, from the lower-case one a URL carries.</summary>
    public static string Canonical(string host) => Canonicalised[host];
}
