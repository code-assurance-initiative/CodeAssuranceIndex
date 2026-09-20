using System.Globalization;
using System.Net;
using System.Text;
using Cai.Scoring;

namespace Cai.Web.Badges;

/// <summary>
/// Renders the CAI badge. Pure and deterministic: same inputs → byte-identical SVG, so it caches and is testable.
/// </summary>
/// <remarks>
/// <para>The standard renders this, not the issuer, and that is the whole point of the endpoint. A badge is a claim
/// about a repository made under CAI's name; if every implementation drew its own, "a CAI badge" would mean whatever
/// each vendor's designer decided, and a reader could not tell a conforming badge from a decorative one. With one
/// renderer there is one shape, and the three rules on /badge are enforced by construction rather than by asking
/// issuers nicely.</para>
/// <para>THE REPOSITORY IS ON THE FACE OF IT. A badge whose image does not name its subject can be lifted from one
/// README into another and still look right — the strongest score in the corpus is one copy-paste from anybody's
/// front page. Naming owner/repo in the image makes that misuse visible at a glance, which is why it is here rather
/// than only in the link target.</para>
/// <para>Band colours are the published CAI palette (docs/brand/README.md), worst→best. They are the standard's own,
/// so the colour carries the same meaning on every issuer's badge.</para>
/// </remarks>
internal static class BadgeSvg
{
    // docs/brand/README.md: #c94f43 #d97f3e #c9a13b #55a06c #2e8f75, worst→best.
    private static string Colour(Band band) => band switch
    {
        Band.Critical => "#c94f43",
        Band.Poor => "#d97f3e",
        Band.Fair => "#c9a13b",
        Band.Healthy => "#55a06c",
        Band.Exemplary => "#2e8f75",
        _ => "#555",
    };

    // DejaVu Sans at 11px averages a shade over 6px per glyph. The badge is laid out from an ESTIMATE because an SVG
    // cannot measure its own text, and the estimate is deliberately generous: text that overflows its panel is a
    // broken badge, whereas a few px of slack is invisible.
    private const double CharWidth = 6.2;
    private static int PanelWidth(string text) => (int)Math.Ceiling(text.Length * CharWidth) + 16;

    /// <summary>The rendered badge. <paramref name="headline"/> is shown to whole numbers — the fold is exact and the
    /// rubric version names the rules, so a decimal on a 20px-tall image buys precision nobody can act on.</summary>
    public static string Render(string owner, string repo, double headline, Band band, string rubricVersion, string issuer)
    {
        var left = $"CAI {owner}/{repo}";
        var right = $"{Math.Round(headline, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)} · {band.Label()}";

        var lw = PanelWidth(left);
        var rw = PanelWidth(right);
        var total = lw + rw;

        // The accessible name carries what will not fit: the rubric version (rule 2) and who issued the survey. A
        // badge that states its rubric only in a page you have to open has not stated it to a screen reader.
        var aria = $"CAI {owner}/{repo}: {right}, under {rubricVersion}, issued by {issuer}";

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{total}\" height=\"20\" role=\"img\" aria-label=\"{E(aria)}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<title>{E(aria)}</title>");
        sb.Append("<linearGradient id=\"s\" x2=\"0\" y2=\"100%\"><stop offset=\"0\" stop-color=\"#bbb\" stop-opacity=\".1\"/><stop offset=\"1\" stop-opacity=\".1\"/></linearGradient>");
        sb.Append(CultureInfo.InvariantCulture, $"<rect rx=\"3\" width=\"{total}\" height=\"20\" fill=\"#555\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<rect rx=\"3\" x=\"{lw}\" width=\"{rw}\" height=\"20\" fill=\"{Colour(band)}\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<rect rx=\"3\" width=\"{total}\" height=\"20\" fill=\"url(#s)\"/>");
        sb.Append("<g fill=\"#fff\" text-anchor=\"middle\" font-family=\"DejaVu Sans,Verdana,Geneva,sans-serif\" font-size=\"11\">");
        sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{lw / 2}\" y=\"14\">{E(left)}</text>");
        sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{lw + (rw / 2)}\" y=\"14\">{E(right)}</text>");
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    // Owner and repo arrive from the route, so they reach the SVG as untrusted text. SVG is XML and an unescaped
    // '<' would end the <text> element and start whatever came next.
    private static string E(string s) => WebUtility.HtmlEncode(s);
}
