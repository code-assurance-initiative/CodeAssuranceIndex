using System.Text;

namespace Cai.Pages;

/// <summary>
/// Where a measured subject's page lives on the standard's own site.
/// </summary>
/// <remarks>
/// <para>★★ THE ADDRESS NAMES THE SUBJECT, NEVER THE PRODUCER. Two producers measuring one repository
/// publish to the SAME page; whoever measured it is content on that page, not part of where it lives. An
/// address that carried the producer would fork the corpus the first time a second one arrived, and every
/// link ever shared to the first would quietly become a link to one of two.</para>
/// <para>★ A NAMESPACE IS A PATH, NOT A NAME. GitLab subgroups, Gitea orgs and self-hosted forges all
/// nest, and folding <c>zairakai/php-packages</c> into one segment deleted the slash outright: the address
/// became <c>zairakaiphp-packages</c>, which no reader can parse, nothing can read back to the source, and
/// which collides <c>a/bc</c> with <c>ab/c</c>. Each level gets its own segment. Carried over verbatim from
/// the producer's implementation, including the reason.</para>
/// </remarks>
internal static class SurveyAddress
{
    /// <summary>The page for a subject, or null when it cannot be addressed.</summary>
    public static string? For(string? host, string? repository)
    {
        if (Segment(HostLabel(host)) is not { } forge)
        {
            return null;
        }

        var parts = (repository ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var segments = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (Segment(part) is not { } slug)
            {
                return null;
            }

            segments.Add(slug);
        }

        return $"{SurveyPageBuilder.Root}/{forge}/{string.Join('/', segments)}";
    }

    /// <summary>
    /// The label before the public suffix: <c>github.com</c> → <c>github</c>.
    /// </summary>
    /// <remarks>
    /// Good enough on purpose: what an address needs is a stable, readable name per forge, not a
    /// registrable-domain parser.
    /// </remarks>
    private static string? HostLabel(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return labels.Length >= 2 ? labels[^2] : labels.FirstOrDefault();
    }

    private static string? Segment(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if ((ch is '-' or '.' or '_' or ' ') && sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? null : slug;
    }
}
