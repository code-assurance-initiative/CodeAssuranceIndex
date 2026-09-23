using System.Globalization;
using System.Text;

namespace Cai.Pages;

/// <summary>
/// The small text helpers a syndicated page is built from: escaping into the site's canonical inline HTML subset,
/// and the handful of number/date formats a reader sees.
/// </summary>
/// <remarks>
/// The site accepts only <c>&lt;p&gt;</c>, <c>&lt;ul&gt;/&lt;ol&gt;</c> with <c>&lt;li&gt;</c>, and the inline
/// <c>&lt;strong&gt; &lt;em&gt; &lt;a&gt; &lt;br&gt;</c> — and REJECTS anything else rather than sanitising it. So
/// everything that reaches a rich-text node is built here, from escaped fragments, and never by interpolating a
/// repository's own text (an owner or a project name is attacker-controlled as far as this code is concerned).
/// </remarks>
internal static class PageProse
{
    /// <summary>Plain text as it may appear inside the canonical subset: only <c>&amp;</c> and <c>&lt;</c> can end a
    /// text run, and <c>&gt;</c> is escaped too so a stray sequence can never read as markup.</summary>
    public static string Escape(string? text) => (text ?? string.Empty)
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>One paragraph of plain prose, or an empty string when there is nothing to say (so a caller can append
    /// unconditionally and a missing sentence simply leaves no paragraph behind).</summary>
    public static string Paragraph(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : $"<p>{Escape(text.Trim())}</p>";

    /// <summary>A bullet list of already-escaped item fragments; empty when there are no items.</summary>
    public static string List(IEnumerable<string> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                sb.Append("<li>").Append(item).Append("</li>");
            }
        }

        return sb.Length == 0 ? string.Empty : $"<ul>{sb}</ul>";
    }

    /// <summary>A labelled fact, as a list item fragment: <c><![CDATA[<strong>Label</strong> value]]></c>.</summary>
    public static string Fact(string label, string value) =>
        $"<strong>{Escape(label)}</strong> {Escape(value)}";

    /// <summary>A link, as an inline fragment. The href is emitted verbatim, so callers pass only URLs this system
    /// composed or a provider URL it already validated.</summary>
    public static string Link(string href, string text) =>
        $"<a href=\"{Escape(href)}\">{Escape(text)}</a>";

    /// <summary>
    /// The indefinite article a noun takes — <c>"an"</c> before advisory, <c>"a"</c> before package.
    /// </summary>
    /// <remarks>
    /// ★★ A SENTENCE PARAMETERISED BY A NOUN MUST TAKE ITS ARTICLE FROM THE NOUN. The advisory and package
    /// indexes share one preamble with the noun substituted in; the article was written for "advisory" and
    /// stayed literal, so the package index read "An package survives only in...". Nothing about the numbers
    /// was wrong, which is why every test of that page passed and a rendered screenshot found it.
    /// <para>
    /// This is a SPELLING rule, not a phonetic one: it would say "a hour" and "an unit". The nouns on these
    /// pages are a closed set the tests name, and a caller with a noun whose sound and spelling disagree
    /// should write the article rather than ask for one.
    /// </para>
    /// </remarks>
    public static string An(string noun) =>
        noun.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(noun[0])) ? "an" : "a";

    /// <summary>A score as a reader writes it — one decimal, never a float's full expansion.</summary>
    public static string Score(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>A count with thousands separators.</summary>
    public static string Count(long value) => value.ToString("#,0", CultureInfo.InvariantCulture);

    /// <summary>
    /// A large count at a glance: <c>173k</c>, <c>2.1M</c>. For a figure that has to fit a stat-band cell and be
    /// read at a glance rather than reconciled — the exact number is on the report for anyone who wants it.
    /// </summary>
    public static string Compact(long value) => value switch
    {
        >= 1_000_000_000 => (value / 1_000_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "B",
        >= 1_000_000 => (value / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "M",
        >= 10_000 => (value / 1_000d).ToString("0", CultureInfo.InvariantCulture) + "k",
        >= 1_000 => (value / 1_000d).ToString("0.#", CultureInfo.InvariantCulture) + "k",
        _ => Count(value),
    };

    /// <summary>A date as an unambiguous day — the same in every locale a reader might be in.</summary>
    public static string Day(DateTimeOffset at) => at.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// The instant a reading was taken, to the minute and in UTC: <c>15 September 2026, 00:33 UTC</c>.
    /// </summary>
    /// <param name="at">The instant of the reading — NOT the instant the page was built.</param>
    /// <returns>The day and the time, unambiguous in every locale.</returns>
    /// <remarks>
    /// ★★ THE DAY ALONE CANNOT TELL TWO READINGS APART, and on this site two readings a day is the normal
    /// case rather than the edge one. The surveys index published three different populations over one morning
    /// — 4,065, then 4,075, then 4,078 — each stamped with the same day and nothing else, while the corpus
    /// index beside it rebuilds once a night. A reader holding two such figures cannot order them and a
    /// citation of either cannot be resolved back to the reading it came from. The minute is what makes the
    /// figure citable. See <c>Figure.Provenance</c>, which states the
    /// same instant in the note at the foot of the page.
    /// </remarks>
    public static string DayAndTime(DateTimeOffset at) =>
        at.UtcDateTime.ToString("d MMMM yyyy, HH:mm 'UTC'", CultureInfo.InvariantCulture);

    /// <summary>The month a date falls in, named — for a heading over the days inside it.</summary>
    /// <param name="at">The instant.</param>
    /// <returns>The month and year, e.g. <c>September 2026</c>.</returns>
    public static string Month(DateTimeOffset at) =>
        at.UtcDateTime.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// The first sentence of <paramref name="text"/>, for a meta description — trimmed to
    /// <paramref name="maxLength"/> on a word boundary when the sentence itself is too long.
    /// </summary>
    public static string FirstSentence(string? text, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        var end = trimmed.IndexOf(". ", StringComparison.Ordinal);
        var sentence = end > 0 ? trimmed[..(end + 1)] : trimmed;
        if (sentence.Length <= maxLength)
        {
            return sentence;
        }

        var cut = sentence.LastIndexOf(' ', Math.Min(maxLength, sentence.Length - 1));
        return (cut > 0 ? sentence[..cut] : sentence[..maxLength]).TrimEnd(',', ';', ':', ' ') + "…";
    }

    /// <summary>
    /// A language identifier as it is written for a reader: <c>csharp</c> is C#, <c>fsharp</c> is F#, and everything
    /// else is simply capitalised. Unknown values pass through capitalised rather than being dropped — a language this
    /// list has not met yet is still a real language.
    /// </summary>
    public static string LanguageName(string? id) => (id ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "" => "Unknown",
        "csharp" => "C#",
        "fsharp" => "F#",
        "vbnet" => "VB.NET",
        "cpp" => "C++",
        // Counted-but-unmodelled census codes the engine publishes since backlog 01a0a098-6082.
        "objectivec" => "Objective-C",
        "ocaml" => "OCaml",
        "gdscript" => "GDScript",
        "vhdl" => "VHDL",
        "javascript" => "JavaScript",
        "typescript" => "TypeScript",
        "php" => "PHP",
        "sql" => "SQL",
        "html" => "HTML",
        "css" => "CSS",
        var other => char.ToUpperInvariant(other[0]) + other[1..],
    };
}
