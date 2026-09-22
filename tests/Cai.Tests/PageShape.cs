using System.Text.Json;

namespace Cai.Tests;

/// <summary>
/// Reads the SHAPE of a composed page — its sections, its islands and its stat cells — so two
/// implementations of one page can be compared on the things a reader sees rather than on bytes.
/// </summary>
/// <remarks>
/// ★ SHARED BY EVERY PRODUCER DIFF, so a reader taught to see one kind of difference sees it everywhere.
/// When the section reader looked only at anchors, an "About this page" section ported with the wrong
/// APPEARANCE — an unstyled block at the right address — passed every check.
/// </remarks>
internal static class PageShape
{
    internal static List<string> Sections(JsonElement page)
    {
        var found = new List<string>();
        Walk(Node(page), n =>
        {
            if (n.TryGetProperty("type", out var t) && t.GetString() == "section")
            {
                // ★ THE APPEARANCE TOO, NOT ONLY THE ANCHOR. An appearance is what the theme styles the
                //   section BY, so a section carrying the right anchor and the wrong appearance is an
                //   unstyled block at the right address — invisible to a test that reads only anchors, and
                //   exactly what a hand port gets wrong.
                var anchor = n.TryGetProperty("anchor", out var a) && a.ValueKind == JsonValueKind.String
                    ? a.GetString()!
                    : "-";
                var appearance = n.TryGetProperty("appearance", out var ap) && ap.ValueKind == JsonValueKind.String
                    ? ap.GetString()!
                    : "-";
                found.Add($"{appearance}/{anchor}");
            }
        });
        return found;
    }

    internal static List<string> Widgets(JsonElement page)
    {
        var found = new List<string>();
        Walk(Node(page), n =>
        {
            if (n.TryGetProperty("type", out var t) && t.GetString() == "widget"
                && n.TryGetProperty("tag", out var tag))
            {
                found.Add(tag.GetString()!);
            }
        });
        return found;
    }

    /// <summary>The stat band's cells, as the pairs a reader sees: the figure and the line beneath it.</summary>
    internal static List<string> Stats(JsonElement page)
    {
        var found = new List<string>();
        Walk(Node(page), n =>
        {
            if (!n.TryGetProperty("type", out var t) || t.GetString() != "stack")
            {
                return;
            }

            string? figure = null;
            string? label = null;
            foreach (var child in n.GetProperty("children").EnumerateArray())
            {
                var kind = child.GetProperty("type").GetString();
                if (kind == "heading")
                {
                    figure = child.GetProperty("text").GetString();
                }
                else if (kind == "richtext")
                {
                    label = child.GetProperty("html").GetString();
                }
            }

            if (figure is not null)
            {
                found.Add($"{figure} | {label}");
            }
        });
        return found;
    }

    private static void Walk(JsonElement node, Action<JsonElement> visit)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            visit(node);
            foreach (var property in node.EnumerateObject())
            {
                Walk(property.Value, visit);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                Walk(item, visit);
            }
        }
    }

    private static JsonElement Node(JsonElement page) => page.GetProperty("Node");
}
