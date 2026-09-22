using System.Text.Encodings.Web;
using System.Text.Json;
using Cai.Pages;

namespace Cai.Tests;

/// <summary>
/// Reads a composed page the way a test needs to see it.
/// </summary>
/// <remarks>
/// <para>★★ ONE READER, BECAUSE THE PAGE STATES ITS LINKS TWO WAYS. An index states them as real anchors
/// in rich text (<c>href="…"</c>); a sheet states them as island props, which are JSON inside JSON
/// (<c>"href":"…"</c>, escaped one level deeper). A test that knew only one of them finds no links, and
/// then passes on an empty list — which is exactly how a link-integrity test comes to assert nothing. Both
/// forms happened in this suite, in that order, and the second one passed.</para>
/// <para>★ Relaxed escaping, deliberately: the default encoder writes "§" as § and an inner quote as
/// ", so an assertion on "§1 Population" finds nothing for a reason that has nothing to do with the
/// page. The bytes the CMS receives are unaffected — JSON escaping is transparent to a parser.</para>
/// </remarks>
internal static class PageText
{
    private static readonly JsonSerializerOptions Readable =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>The page's node tree as text.</summary>
    internal static string Json(SurveyPage page) => JsonSerializer.Serialize(page.Node, Readable);

    /// <summary>Every href the page carries, in whichever of the two forms it carries it.</summary>
    internal static IEnumerable<string> Hrefs(string json)
    {
        // Flatten one level of escaping so an island prop's own JSON reads like the page's.
        var flat = json.Replace("\\\"", "\"", StringComparison.Ordinal);
        var found = new List<string>();
        var index = 0;

        while ((index = flat.IndexOf("href", index, StringComparison.Ordinal)) >= 0)
        {
            var cursor = index + "href".Length;

            // A JSON key closes its own quote first; an HTML attribute does not.
            if (cursor < flat.Length && flat[cursor] == '"')
            {
                cursor++;
            }

            while (cursor < flat.Length && (flat[cursor] is ':' or '=' or ' '))
            {
                cursor++;
            }

            if (cursor < flat.Length && flat[cursor] == '"')
            {
                var close = flat.IndexOf('"', cursor + 1);
                if (close > cursor)
                {
                    found.Add(flat[(cursor + 1)..close]);
                }
            }

            index += "href".Length;
        }

        return found;
    }
}
