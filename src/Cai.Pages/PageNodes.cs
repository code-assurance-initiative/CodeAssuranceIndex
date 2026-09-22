using System.Text.Json;
using System.Text.Json.Serialization;
using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// The authoring node specs a syndicated page is assembled from — the site's own layout vocabulary, not markup.
/// </summary>
/// <remarks>
/// ★ THIS SLICE IS THE SHARED VOCABULARY AND NOTHING ELSE. It is how this system writes a page for the site, and
/// it belongs to no one publisher: the survey registry and the corpus-wide reading both author with it. It lived
/// in <c>Registry</c> while there was only one author, and the day a second one arrived the two slices became a
/// dependency knot — each naming the other, neither readable, testable or movable without the other. Extracting
/// the vocabulary they share cut the knot without either of them learning the other's name.
/// <para>
/// The producer sends CONTENT and lets the site supply form: a section's <c>appearance</c> is a name the marketing
/// theme styles (<c>.ip-ap-stat-band</c>, <c>.ip-ap-table-list</c>, …), so a published page picks up every future
/// theme change for free and can never drift into being a second renderer.
/// </para>
/// <para>
/// Two of those appearances carry this design. <see cref="StatBand"/> renders a grid of stacks as the theme's
/// strongest numeric treatment — big mono figures over small muted labels, walled cells edge to edge. And
/// <see cref="TableList"/> renders a flat <c>&lt;ul&gt;</c> as a REAL table, one <c>&lt;li&gt;</c> per cell,
/// because the canonical rich-text subset has no <c>&lt;table&gt;</c>. That is what lets one rich-text node carry
/// two hundred projects: a page may hold at most 500 elements, so a structured card per repository would blow the
/// cap on the first big language.
/// </para>
/// </remarks>
internal static class PageNodes
{
    /// <summary>A plain section — the default framing for prose.</summary>
    public static Dictionary<string, object?> Section(params object?[] children) =>
        new(StringComparer.Ordinal) { ["type"] = "section", ["children"] = children.Where(c => c is not null).ToList() };

    /// <summary>A section with an optional named appearance the theme styles, and an optional anchor.</summary>
    /// <remarks>
    /// Both are optional because a section may want a deep link without wanting a treatment: an island brings its
    /// own, and pinning one of the theme's appearances on top of it would frame the widget twice.
    /// </remarks>
    public static Dictionary<string, object?> Section(string? appearance, string? anchor, params object?[] children)
    {
        var node = Section(children);
        if (appearance is not null)
        {
            node["appearance"] = appearance;
        }

        if (anchor is not null)
        {
            node["anchor"] = anchor;
        }

        return node;
    }

    /// <summary>
    /// A grid of stacks — the shape both the stat band and the panel appearances lay out.
    /// </summary>
    /// <remarks>
    /// ★ NOT FOR A STAT BAND. Use <see cref="Band"/>: how many cells fit a row is the SITE's decision and it
    /// moves with the viewport, so a page builder that hands a raw grid a list it happened to assemble publishes
    /// a filled grey slab at some widths. Both publishers did exactly that, and both are now held to
    /// <see cref="Band"/> by <c>PublishedBandFillsItsRowsTests</c>.
    /// </remarks>
    public static Dictionary<string, object?> Grid(int minItemPx, params object?[] children) =>
        new(StringComparer.Ordinal)
        {
            ["type"] = "grid",
            ["minItemPx"] = minItemPx,
            ["children"] = children.Where(c => c is not null).ToList(),
        };

    /// <summary>A vertical stack — one cell of a grid.</summary>
    public static Dictionary<string, object?> Stack(params object?[] children) =>
        new(StringComparer.Ordinal) { ["type"] = "stack", ["children"] = children.Where(c => c is not null).ToList() };

    /// <summary>A heading. Level 2 inside a stat band is the big figure; elsewhere it is a heading.</summary>
    public static Dictionary<string, object?> Heading(int level, string text) =>
        new(StringComparer.Ordinal) { ["type"] = "heading", ["level"] = level, ["text"] = text };

    /// <summary>Rich text, in the canonical inline subset. Null when there is nothing to say, so callers can pass
    /// it straight into a children list and have it disappear.</summary>
    public static Dictionary<string, object?>? RichText(string? html) =>
        string.IsNullOrWhiteSpace(html)
            ? null
            : new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "richtext", ["html"] = html };

    /// <summary>
    /// A section's EYEBROW in served HTML: the small mono accent label a section is introduced by, for a
    /// section that is not an island and therefore cannot take the islands' rail.
    /// </summary>
    /// <param name="text">The label — "Every language", "September 2026", "About this reading".</param>
    /// <returns>The rich-text node.</returns>
    /// <remarks>
    /// <para>★★ THIS IS THE SITE'S OWN EYEBROW AND NOT MARKUP WE INVENTED, WHICH IS THE WHOLE REASON IT IS
    /// WRITABLE AT ALL. The canonical inline grammar admits <c>p, ul, ol, li, br, strong, em, a[href]</c> and
    /// REFUSES anything else rather than rewriting it — no <c>span</c>, no <c>class</c>. What the renderer
    /// does have is a shape rule: a rich-text value that is EXACTLY one paragraph whose entire content is one
    /// <c>&lt;strong&gt;</c> is stamped as a kicker, and the marketing theme sets it as the mono, uppercase,
    /// accent-coloured eyebrow. So the label has to be its OWN node — folded into the paragraph beside it, it
    /// is bold text in a sentence and nothing more.</para>
    ///
    /// <para>★★ AND IT IS THE ANSWER TO THE ONE THING THE RAIL CANNOT REACH. The four islands take a
    /// <c>kicker</c> and lay the section out as a 112px label beside a full-width content column; an ordinary
    /// section cannot, because the rail lives inside a shadow root. But the addressable tables under this root
    /// MUST stay served HTML — a crawler, a reader with no script and every check that reads the markup stop
    /// at the shadow boundary — so they cannot become islands to gain a label. Before this, they had a display
    /// <c>&lt;h2&gt;</c> or no label at all, and both are wrong: the first shouts over a page of quiet
    /// eyebrows, the second leaves a table of two hundred rows with nothing saying what it is.</para>
    /// </remarks>
    public static Dictionary<string, object?> Eyebrow(string text) =>
        RichText($"<p><strong>{PageProse.Escape(text)}</strong></p>")!;

    /// <summary>
    /// One stat-band cell, from the figure it states: the reading in the display slot, the label completing
    /// the sentence it starts, and the population beneath both.
    /// </summary>
    /// <param name="figure">The reading. Split HERE, so a call site cannot hand over half of it.</param>
    /// <param name="label">
    /// What the value is a value of, in the page's own words — "carry a known-vulnerable component". It
    /// continues the sentence the display slot begins; it does not repeat the population, which the figure
    /// states for itself. May be blank, and then the population stands alone.
    /// </param>
    /// <returns>The cell.</returns>
    /// <remarks>
    /// <para>★★ THE DISPLAY SLOT IS AN <c>&lt;h2&gt;</c>, AND IT USED TO BE HANDED THE WHOLE SENTENCE. Every
    /// stat on every published corpus page was built as <c>Stat(figure.Headline(), label)</c>, so
    /// "76.8% of surveys whose dependencies a scanner could resolve (1,161 of 1,511)" was set in heading type —
    /// four lines of it, in every cell, on every page. Right in every particular, and the pages read as text
    /// and then more text.</para>
    ///
    /// <para>★★ SO WHY THE OVERLOAD TAKES A FIGURE RATHER THAN THE CALLER SPLITTING IT. The readable shape
    /// needs the value on its own, and a bare "76.8%" is precisely the share that outlives its denominator —
    /// the defect <see cref="Figure"/> exists to make unwritable. <see cref="Figure.Split"/> therefore hands
    /// back BOTH halves as one value and nothing hands back one, and this is the single place that puts each
    /// half in its slot. A page builder passes the figure; which half goes where is not its decision, exactly
    /// as how many cells fit a row is not its decision. <c>StatCellComesFromAFigureTests</c> refuses a page
    /// builder that hands this method text, the same way a raw <see cref="Grid"/> is refused.</para>
    ///
    /// <para>★ The label goes FIRST, then the population: the value and the label are the claim
    /// ("76.8%" · "carry a known-vulnerable component"), and the population is what qualifies it. A
    /// <c>&lt;br&gt;</c> rather than a second paragraph, so the theme styles the whole block as one muted
    /// label — which is what it is.</para>
    /// </remarks>
    public static Dictionary<string, object?> Stat(Figure figure, string label)
    {
        ArgumentNullException.ThrowIfNull(figure);

        var split = figure.Split();
        var population = PageProse.Escape(split.Support);

        return Stack(
            Heading(2, split.Lead),
            RichText(string.IsNullOrWhiteSpace(label)
                ? $"<p>{population}</p>"
                : $"<p>{PageProse.Escape(label)}<br>{population}</p>"));
    }

    /// <summary>
    /// One stat-band cell for something that is NOT a reading over a population: a score and the band it falls
    /// in, a date, a name.
    /// </summary>
    /// <param name="figure">The text for the display slot. Short — the slot is a heading.</param>
    /// <param name="label">The line beneath it.</param>
    /// <returns>The cell.</returns>
    /// <remarks>
    /// ★ IF WHAT YOU HAVE IS A MEASUREMENT, THIS IS THE WRONG OVERLOAD — build a <see cref="Figure"/> and use
    /// the one above, which states the population for you. Passing <c>figure.Headline()</c> here is how every
    /// corpus page came to set a whole sentence in heading type, and passing a formatted count is how a number
    /// reaches a reader with no population anywhere near it. The registry still calls this: it predates
    /// <see cref="Figure"/> and states its counts and scores through <c>PageProse</c>, migrating it is its own
    /// item, and until then <c>StatCellComesFromAFigureTests</c> holds that backlog to a ceiling that may only
    /// fall.
    /// </remarks>
    public static Dictionary<string, object?> Stat(string figure, string label) =>
        Stack(Heading(2, figure), RichText($"<p>{PageProse.Escape(label)}</p>"));

    /// <summary>
    /// A stat band, laid out in rows the site can fill — or nothing at all when no cell survived.
    /// </summary>
    /// <param name="anchor">The section's anchor.</param>
    /// <param name="cells">The cells, of which any may be null. Any number of them.</param>
    /// <returns>The section, or null when every cell was null.</returns>
    /// <remarks>
    /// <para>★★ A BAND THAT DOES NOT FIT ITS ROW IS NOT BLANK SPACE — IT IS A FILLED GREY SLAB. The stat band
    /// paints its cell walls by showing a hairline BACKGROUND through a 1px gap between opaque cells, so a track
    /// with no cell in it is drawn, not skipped. A band of five in a four-column row published one cell and three
    /// quarters of a row of grey; a browser found it in both themes on the country pages and on supply-chain, and
    /// the e2e driver reported 0 problems on that same run, because a filled empty cell is not something it knows
    /// to check.</para>
    ///
    /// <para>★ WHY THE ROW WIDTHS ARE EXACTLY {4, 2, 1}, MEASURED RATHER THAN ASSUMED. A band's grid is
    /// <c>repeat(auto-fit, minmax(min(240px, 100%), 1fr))</c>, so its column count is whatever fits and moves with
    /// the viewport — 4, 3, 2 and 1 all occur on these pages between a phone and a wide desktop.
    /// <c>auto-fit</c> collapses only the tracks that are empty in EVERY row, which is why a band of three looked
    /// correct at 1280px and wrapped 2+1 at 700px: the bad state was there all along and the width it shows at was
    /// not the width anybody screenshotted. No single <c>minItemPx</c> can fix that — for any minimum, some width
    /// divides it into a column count that does not divide the cell count. What can be relied on is the site's own
    /// stylesheet, which pins two counts against precisely this failure: a band of two is always a pair, and a
    /// band of four is always 2×2 or 4-across, at every width. Those two pins plus the trivial single are the only
    /// counts that fill every row at every width, so a band of any other count becomes several rows drawn from
    /// them, largest first, in the order the figures were given. Five is a row of four and a row of one — not a
    /// filler cell, which would be a slab that had been given a border.</para>
    ///
    /// <para>★★ IT LIVES HERE, IN THE VOCABULARY, BECAUSE IT WAS NEVER ONE PUBLISHER'S PROBLEM. The corpus and the
    /// registry each wrote the same three lines — a <c>StatBand</c> section around a raw <see cref="Grid"/> — and
    /// each shipped the same defect, the registry's across roughly 2,398 survey pages, every language field guide
    /// and the index. A call site cannot be responsible for handing over a number that happens to divide: the
    /// divisor is the site's, it is not a constant, and the counts move with the data (a median that cannot be
    /// taken, a share with no denominator, a card that states its size but not its language). So the band takes
    /// any count and lays itself out, and a raw grid inside either publisher is refused by
    /// <c>PublishedBandFillsItsRowsTests</c>.</para>
    ///
    /// <para>★ AND IT REFUSES TO BE EMPTY, which is the same rule the figures already obey one level down: a share
    /// whose denominator is zero is published as nothing, so the frame around it must ask its content whether it
    /// exists. A band whose every cell came back null returns null here and disappears from its parent's children,
    /// rather than emitting a walled strip with nothing inside it.</para>
    ///
    /// <para>★ Handed to the site's owner, and not depended on here: a <c>:has(&gt; :nth-child(3):last-child)</c>
    /// rule beside the existing two would make three a pinned count as well, and a three-across band is a nicer
    /// shape than 2+1. The producer cannot assume a stylesheet it does not own, so this lays out against the rules
    /// the site has today.</para>
    /// </remarks>
    public static Dictionary<string, object?>? Band(string anchor, params object?[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var present = cells.Where(c => c is not null).ToArray();
        if (present.Length == 0)
        {
            return null;
        }

        var rows = new List<object?>();
        var taken = 0;
        foreach (var width in Rows(present.Length))
        {
            rows.Add(Grid(CellMinPx, present[taken..(taken + width)]));
            taken += width;
        }

        return Section(StatBand, anchor, [.. rows]);
    }

    /// <summary>
    /// How a count of cells is broken into rows: as many wide rows as it will fill, then the remainder.
    /// </summary>
    /// <param name="cells">How many cells there are; at least one.</param>
    /// <returns>The width of each row, in order.</returns>
    /// <remarks>
    /// Greedy, and it terminates on any count because 1 is one of the widths. The order of the cells is never
    /// disturbed: a reader is given the figures in the order the page meant them, and only the point at which
    /// the row breaks is this function's decision.
    /// </remarks>
    private static IEnumerable<int> Rows(int cells)
    {
        foreach (var width in RowWidths)
        {
            while (cells >= width)
            {
                yield return width;
                cells -= width;
            }
        }
    }

    /// <summary>
    /// The cell counts the site lays out full at EVERY width — a lone cell, its pinned pair and its pinned
    /// four. Largest first, because the rows are filled greedily from this list.
    /// </summary>
    private static readonly int[] RowWidths = [4, 2, 1];

    /// <summary>
    /// The width a band's cell is laid out against, in pixels; the site derives its track count from it.
    /// </summary>
    /// <remarks>
    /// ★ ONE VALUE, AND FOR THE COUNTS THIS BAND EMITS IT CANNOT SHOW — WHICH IS WHY IT IS NOT A PARAMETER. The
    /// corpus wrote 240 into its bands and the registry wrote 170 into all three of its, a disagreement neither of
    /// them had a reason for. It cannot show, because the only counts <see cref="Band"/> ever emits are the two the
    /// site PINS (2 and 4, whose <c>:has()</c> rules out-specify this track sizing entirely) and the single, whose
    /// one track fills the row whatever the minimum. The claim is narrow on purpose: at the counts the publishers
    /// USED to emit — three and five — the minimum did decide the column count, and therefore which widths showed
    /// the slab. A per-call-site number that reads as a layout lever while having no effect at the only counts it
    /// can be passed is how the layout came to be the call site's business in the first place, so the band states
    /// it once and says why it is inert.
    /// </remarks>
    private const int CellMinPx = 240;

    /// <summary>
    /// An island: one of the site's own widgets, with its props. Null-valued props are dropped, so a caller can
    /// pass an optional value straight through and have the widget fall back to its own default.
    /// </summary>
    /// <remarks>
    /// This is what lets a survey page carry the same score card, band scale, composition bar and C4 heat-map the
    /// marketing site renders, instead of describing them in prose. The widget is rendered by the SITE from live
    /// data — the producer never draws a chart, so a survey page cannot drift from the product the way a picture
    /// baked at publish time would. Tags are validated against the site's widget manifest on arrival: an unknown
    /// tag is refused rather than rendered as an empty custom element.
    /// </remarks>
    public static Dictionary<string, object?> Widget(string tag, params (string Key, string? Value)[] props) =>
        new(StringComparer.Ordinal)
        {
            ["type"] = "widget",
            ["tag"] = tag,
            ["props"] = props
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .ToDictionary(p => p.Key, p => p.Value!, StringComparer.Ordinal),
        };

    /// <summary>
    /// A call to action. The href must be absolute — the site refuses anything that is not https, http or mailto,
    /// so a site-relative path would be rejected at the door rather than rendered as a dead button.
    /// </summary>
    public static Dictionary<string, object?> Button(string label, string href) =>
        new(StringComparer.Ordinal)
        {
            ["type"] = "button",
            ["label"] = label,
            ["href"] = href,
        };

    // ── figures handed to an island ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One figure as an island reads it: the value, the words completing it, the population beneath, and the
    /// explanation a reader can open if they want it.
    /// </summary>
    /// <param name="figure">The reading. Split HERE, so a call site cannot hand over half of it.</param>
    /// <param name="label">What the value is a value of, in the page's own words.</param>
    /// <param name="tip">The explanation behind the (i), or null for no disclosure at all.</param>
    /// <param name="tone">A band key for the value's ink, or null for the page's own.</param>
    /// <returns>The item, for an island's JSON.</returns>
    /// <remarks>
    /// <para>★★ AN ISLAND IS NOT AN EXEMPTION FROM THE FIGURE RULE, AND IT IS THE EASIEST PLACE TO BECOME ONE.
    /// A widget prop is a string in an attribute: nothing about it stops a page builder writing
    /// <c>"76.8%"</c> into one, and nothing about the SHADOW ROOT it renders into is visible to a checker
    /// reading the page's markup. So the split happens here for exactly the reason it happens in
    /// <see cref="Stat(Figure, string)"/> — a page builder passes the whole figure, and which half goes where
    /// is not its decision. <c>StatCellComesFromAFigureTests</c> refuses a page builder that hands this method
    /// text, the same way it refuses one that hands a stat cell text.</para>
    ///
    /// <para>★ THE VALUE IS NOT A KEY THE ISLAND MAY RE-DERIVE FROM. Everything a reader sees arrives already
    /// rendered; the island is given no numerator, no denominator and no rounding to do. A widget that could
    /// compute a percentage would be a second renderer of this system's figures, and two renderers of one
    /// reading is the disagreement this whole type exists to prevent.</para>
    /// </remarks>
    public static Dictionary<string, object?> Reading(
        Figure figure, string label, string? tip = null, string? tone = null) =>
        Item(figure.Split(), label, tip, tone);

    /// <summary>
    /// The same, with the TALLY in the display slot: "1,161" over the whole reading, rather than "76.8%" over
    /// half of it.
    /// </summary>
    /// <param name="figure">The reading.</param>
    /// <param name="label">What the value is a value of.</param>
    /// <param name="tip">The explanation behind the (i), or null.</param>
    /// <param name="tone">A band key for the value's ink, or null.</param>
    /// <returns>The item.</returns>
    /// <remarks>
    /// ★ FOR A PAGE THAT RECORDS RATHER THAN ARGUES. See <see cref="FigureLead"/>: a percentage in the largest
    /// type on a page arrives already arguing, and the count it came from is something a reader can go and
    /// check. The support line is the whole headline either way, so nothing is given up for it.
    /// </remarks>
    public static Dictionary<string, object?> Tally(
        Figure figure, string label, string? tip = null, string? tone = null) =>
        Item(figure.Split(FigureLead.Tally), label, tip, tone);

    /// <summary>
    /// One item of an island's band that is NOT a reading over a population: a rubric span, a date, a name.
    /// </summary>
    /// <param name="value">The text for the display slot. Short — the slot is a display slot.</param>
    /// <param name="label">The line beneath it.</param>
    /// <param name="tip">The explanation behind the (i), or null.</param>
    /// <returns>The item.</returns>
    /// <remarks>
    /// ★★ IF WHAT YOU HAVE IS A MEASUREMENT, THIS IS THE WRONG MEMBER — build a <see cref="Figure"/> and use
    /// <see cref="Reading"/> or <see cref="Tally"/>, which state the population for you. This exists for the
    /// one thing on the corpus sheet that genuinely is not a reading: the RANGE OF RUBRIC VERSIONS the
    /// surveys were scored under, which is a pair of identifiers and has no population, no denominator and no
    /// arithmetic. <c>StatCellComesFromAFigureTests</c> holds the corpus to a ceiling on these, so a second
    /// one has to be argued for rather than typed.
    /// </remarks>
    public static Dictionary<string, object?> Marker(string value, string label, string? tip = null)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["lead"] = value,
            ["label"] = label,
        };
        Unless(item, "tip", tip);
        return item;
    }

    /// <summary>
    /// A movement as the one reading it is: two dated ends of the same quantity, each with the population it
    /// was taken over, for the pair a chart carries beside or beneath its caption.
    /// </summary>
    /// <param name="from">The oldest reading. Split HERE, so a call site cannot hand over half of it.</param>
    /// <param name="to">The latest reading, split the same way.</param>
    /// <param name="label">The eyebrow naming what moved — "Median", "Codebases".</param>
    /// <returns>The pair, for a trend island's JSON.</returns>
    /// <remarks>
    /// <para>★★ THIS IS WHAT MAKES A CHART LEGAL ON THESE PAGES, AND IT IS ONE FIGURE RATHER THAN TWO. A
    /// chart's points carry no population and no date, because a chart is a picture of a shape — so the
    /// movement is STATED and the line is the evidence under it. It was stated as two separate stat cells,
    /// and two movements drawn that way is four numbers in four boxes: the exact shape the owner read on the
    /// old sheet and said left their eye nowhere to rest. A movement is ONE reading with two ends, so it goes
    /// to the island as one pair and the island draws "45.2 → 49.5". THE JOIN IS A LAYOUT DECISION AND IS NOT
    /// MADE HERE — which is also what keeps this honest, because joining two values into a string is exactly
    /// where a population would have to be dropped. Both halves of both ends are split by this method, so a
    /// call site never sees a bare value and cannot publish one.</para>
    ///
    /// <para>★★ THE DAY IS ADDED ONLY WHEN THE TWO ENDS DO NOT OTHERWISE DIFFER FROM EACH OTHER — and the
    /// rule is computed from the pair rather than chosen by the caller, deliberately. A median's ends were
    /// taken over different populations ("across 580 measured codebases" against "across 3,542"), so a reader
    /// needs to know which reading is which and when each was read, and the day completes them. A COUNT's
    /// population is its own value, so both ends name one basis — "measured codebases" — which is one fact
    /// about one population that the island states ONCE. Dating that would split a single quiet line in two
    /// and put a day on a phrase that is a basis rather than a reading; worse, a call site free to decide
    /// could emit two subs differing only by a day, which reads as two populations where there is one.</para>
    ///
    /// <para>★ SO AN UNDATED PAIR IS A CLAIM ABOUT ITS OWN ENDS, NOT A DROPPED DATE. The days such a pair is
    /// read over are the section's — the island is given <c>first-date</c> and <c>last-date</c> either way,
    /// and on the corpus sheet the dated pair beside it states them in words. A page putting two movements
    /// here that were NOT read on the same days would be stating something this shape cannot say, which is
    /// why <c>CorpusSheetTests</c> pins that invariant rather than trusting it.</para>
    /// </remarks>
    public static Dictionary<string, object?> Movement(Figure from, Figure to, string label) =>
        Movement(from, to, label, statesItsOwnPopulation: true);

    /// <summary>
    /// The same movement, with the caller saying whether this pair states the population it was taken over
    /// under its own two ends — or whether another pair on the same island states it.
    /// </summary>
    /// <param name="from">The oldest reading. Split HERE, so a call site cannot hand over half of it.</param>
    /// <param name="to">The latest reading, split the same way.</param>
    /// <param name="label">The eyebrow naming what moved.</param>
    /// <param name="statesItsOwnPopulation">
    /// <c>true</c> for the ordinary shape: each end carries the population it was taken over, dated when that
    /// is what tells the two ends apart. <c>false</c> only when the population is stated by ANOTHER pair on
    /// the same island — see the remarks, and note that nothing in this method can check that, which is why
    /// nothing but <c>CorpusTrends.Movements</c> is allowed to pass it.
    /// </param>
    /// <returns>The pair.</returns>
    /// <remarks>
    /// <para>★★ THIS IS THE ONE PLACE IN THE PUBLISHER WHERE A FIGURE REACHES A PAGE WITHOUT ITS POPULATION
    /// BESIDE IT, AND IT LOOKS EXACTLY LIKE THE DEFECT EVERYTHING HERE IS BUILT AGAINST. It is not one, and
    /// the distinction is worth keeping rather than deleting. The corpus sheet's §5 states two movements: a
    /// median, and the COUNT OF CODEBASES THE MEDIAN WAS TAKEN OVER. The second pair is, number for number,
    /// the populations of the first — "580 → 3,545" is what "across 580 measured codebases" and "across 3,545
    /// measured codebases" said. Printing both is the same fact twice, in small type, under a pair that
    /// already says it: the owner's verdict on reading it live was that most of it was redundant with what
    /// came right after.</para>
    ///
    /// <para>★★ THE RULE IS SATISFIED BY THE PAGE, NOT BY EVERY LINE IN ISOLATION — and that is the whole of
    /// the correction. The first proposal was to move the two sub lines behind the section's (i), which WOULD
    /// have been the defect: a figure separated from its population, with the population one interaction away
    /// and absent from the served markup. Stating it in the pair directly beneath is not separation. What the
    /// omission costs is that the population's own noun has to live somewhere, so it moves into the second
    /// pair's LABEL — "Measured codebases" rather than "Codebases" — and the caller that decides all this
    /// checks that the label spells the basis before it passes <c>false</c>.</para>
    ///
    /// <para>★ SO THE FLAG IS NAMED FOR WHAT IT ASSERTS, NOT FOR A LAYOUT. A call site that reads
    /// <c>statesItsOwnPopulation: false</c> is claiming something about the rest of the island, and there is
    /// exactly one caller in a position to know it. <c>StatCellComesFromAFigureTests</c> governs the call as
    /// it governs every other display slot.</para>
    /// </remarks>
    public static Dictionary<string, object?> Movement(
        Figure from, Figure to, string label, bool statesItsOwnPopulation)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var first = from.Split();
        var last = to.Split();
        var dated = !string.Equals(first.Support, last.Support, StringComparison.Ordinal);

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["label"] = label,
            ["from"] = End(first, from, dated, statesItsOwnPopulation),
            ["to"] = End(last, to, dated, statesItsOwnPopulation),
        };
    }

    /// <summary>One end of a movement: the reading, and the population it was taken over beneath it.</summary>
    /// <param name="split">The end, already split — the lead is never assembled here.</param>
    /// <param name="figure">The reading, for the day it was read.</param>
    /// <param name="dated">Whether the day is what tells this end from the other.</param>
    /// <param name="stated">Whether this pair carries the population under its own ends at all.</param>
    /// <returns>The end.</returns>
    /// <remarks>
    /// ★ THE KEY IS ABSENT RATHER THAN EMPTY when the pair does not state its own population. The island reads
    /// a missing <c>sub</c> and an empty one identically, and an absent key is the one a later reader of this
    /// JSON cannot mistake for a population that came out blank.
    /// </remarks>
    private static Dictionary<string, object?> End(
        FigureSplit split, Figure figure, bool dated, bool stated)
    {
        var end = new Dictionary<string, object?>(StringComparer.Ordinal) { ["value"] = split.Lead };
        if (stated)
        {
            end["sub"] = dated ? $"{split.Support}, {PageProse.Day(figure.TakenAt)}" : split.Support;
        }

        return end;
    }

    /// <summary>Both halves of a split in their slots, with the disclosure and the ink beside them.</summary>
    private static Dictionary<string, object?> Item(
        FigureSplit split, string label, string? tip, string? tone)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["lead"] = split.Lead,
            ["label"] = label,
            ["support"] = split.Support,
        };
        Unless(item, "tip", tip);
        Unless(item, "tone", tone);
        return item;
    }

    /// <summary>Null for a value that is not there, so the island falls back to its own default.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Add a key only when it has a value — an ABSENT key, not a null one.
    /// </summary>
    /// <remarks>
    /// ★ A DICTIONARY VALUE IS NOT A PROPERTY, and <c>JsonIgnoreCondition.WhenWritingNull</c> only reaches
    /// properties: a null in a <c>Dictionary&lt;string, object?&gt;</c> is serialised as <c>null</c> whatever
    /// the options say. So the island would receive <c>"tone": null</c> and have to test for it, and a widget
    /// that tests for null is a widget one refactor away from rendering the word. What is not there is not
    /// written.
    /// </remarks>
    private static void Unless(Dictionary<string, object?> item, string key, string? value)
    {
        if (Blank(value) is { } present)
        {
            item[key] = present;
        }
    }

    /// <summary>
    /// A band of figures rendered by the site's own island — or nothing at all, when no figure survived.
    /// </summary>
    /// <param name="anchor">The section's anchor.</param>
    /// <param name="layout">"head" for the sheet's masthead, "lead" for a row of leading quantities.</param>
    /// <param name="kicker">The small line above, or null.</param>
    /// <param name="dateline">The masthead's date, or null.</param>
    /// <param name="footnote">The small line beneath, or null.</param>
    /// <param name="figures">The items, of which any may be null.</param>
    /// <returns>The section, or null.</returns>
    /// <remarks>
    /// ★ THE SAME REFUSAL AS <see cref="Band"/>, and for the same reason: a frame around content that does not
    /// exist is a heading over a blank gap. A masthead whose every reading came back null is not a masthead
    /// with empty cells — it is a reading that cannot state anything, and it is absent.
    /// </remarks>
    public static Dictionary<string, object?>? FigureBand(
        string anchor,
        string layout,
        string? kicker,
        string? dateline,
        string? footnote,
        params object?[] figures)
    {
        ArgumentNullException.ThrowIfNull(figures);
        var present = figures.Where(f => f is not null).ToList();

        return present.Count == 0
            ? null
            : Section(
                null,
                anchor,
                Widget(
                    "cai-figure-band",
                    ("layout", layout),
                    ("kicker", kicker),
                    ("dateline", dateline),
                    ("figures", JsonSerializer.Serialize(present, IslandJson)),
                    ("footnote", footnote)));
    }

    /// <summary>
    /// One card in a section of link cards: where it goes, what is behind it, and how much of it there is.
    /// </summary>
    /// <param name="icon">The site's own mark for the destination.</param>
    /// <param name="label">What the destination is.</param>
    /// <param name="note">A sentence about it.</param>
    /// <param name="href">The address, site-relative.</param>
    /// <param name="figure">How much is behind the card, or null when the card states no quantity.</param>
    /// <param name="tip">The explanation behind the (i), or null.</param>
    /// <param name="go">The words on the card's foot — "Every advisory".</param>
    /// <returns>The card, for the island's JSON.</returns>
    /// <remarks>
    /// ★ A CARD SAYING ONLY "ADVISORIES" ASKS A READER TO CLICK TO FIND OUT WHETHER THERE IS ANYTHING BEHIND
    /// IT. The figure is what makes a section of cards scannable — and it is split here, from a whole
    /// <see cref="Figure"/>, for exactly the reason a stat cell is: the display value and the population it
    /// was taken over are one value, and a call site is not handed half of it.
    /// </remarks>
    public static object LinkCard(
        string icon,
        string label,
        string note,
        string href,
        Figure? figure = null,
        string? tip = null,
        string? go = null)
    {
        var card = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["icon"] = icon,
            ["label"] = label,
            ["note"] = note,
            ["href"] = href,
        };
        Unless(card, "tip", tip);
        Unless(card, "go", go);

        if (figure is not null)
        {
            var split = figure.Split();
            card["figure"] = split.Lead;
            // ★ The population travels with the value, in the card's own note, so a figure lifted off a card
            // still says what it counts. A card is the shape a number is quoted from.
            //
            // ★ A COMMA, NOT AN EM DASH. Every card in a section joined its population to its note the same
            // way, so one composition put five em dashes on the sheet at once and they read as a house tic
            // rather than as five separate choices. The dash also has work to do elsewhere in this family,
            // where it marks a real aside; spending it on a join it does not need is what made it invisible
            // where it counts. The join is apposition and a comma is what apposition takes.
            card["note"] = $"{split.Support}, {note}";
        }

        return card;
    }

    /// <summary>
    /// A section of link cards in the islands' rail — or nothing at all, when there is nowhere to go.
    /// </summary>
    /// <param name="anchor">The section's anchor.</param>
    /// <param name="kicker">The eyebrow naming the section.</param>
    /// <param name="tip">The explanation behind an (i) beside the kicker, or null.</param>
    /// <param name="links">The cards, from <see cref="LinkCard"/>.</param>
    /// <returns>The section, or null when there are no cards.</returns>
    /// <remarks>
    /// ★ RECONCILED WITH <c>CorpusPageBuilder.Listed</c>, WHICH BUILDS THE SAME NODE AND SHOULD FOLD INTO
    /// THIS. That private helper is the corpus sheet's §-of-cards and was written before a second page family
    /// needed one; it is byte-for-byte this call with its own kicker. It is not merged here only because that
    /// file was being edited by somebody else the day this landed, and a merge of two hands into one method is
    /// how a section quietly loses a prop. The duplication is one call, and it is named here so the next
    /// person to open either file finds the other.
    /// </remarks>
    public static Dictionary<string, object?>? LinkCards(
        string anchor, string kicker, string? tip, IReadOnlyList<object?> links)
    {
        ArgumentNullException.ThrowIfNull(links);
        var present = links.Where(l => l is not null).ToList();

        return present.Count == 0
            ? null
            : Section(
                null,
                anchor,
                Widget(
                    "cai-link-cards",
                    ("kicker", kicker),
                    ("tip", tip),
                    ("links", JsonSerializer.Serialize(present, IslandJson))));
    }

    // ── a population split into named parts, drawn to scale ──────────────────────────────────────────────────────

    /// <summary>
    /// One row of a bar board: what it is called, what it states, and the reading its bar is drawn from.
    /// </summary>
    /// <param name="label">The row's name.</param>
    /// <param name="href">Where the name links, or null for a row that is not a link.</param>
    /// <param name="tip">The explanation behind the (i). The reading's own headline is appended to it.</param>
    /// <param name="drawn">
    /// The share the bar draws, or null when this row's population was never measured. A
    /// <see cref="FigureKind.Ratio"/> and nothing else: a bar is a part of a whole, and a count, a median and
    /// a floor are none of them a part of anything.
    /// </param>
    /// <param name="inked">What the drawn part IS — "resolved", "affected".</param>
    /// <param name="rest">What the remainder is — "unmeasured", "not affected".</param>
    /// <param name="unmeasured">Why there is no bar. Required exactly when <paramref name="drawn"/> is null.</param>
    /// <param name="cells">The row's other columns, from <see cref="Column"/>.</param>
    /// <returns>The row.</returns>
    /// <remarks>
    /// <para>★★ A ROW WHOSE POPULATION WAS NOT MEASURED DRAWS NO BAR AND SAYS SO — and this signature is what
    /// makes the alternative unwritable. A language with nothing a scanner could resolve has no share to take;
    /// drawing it at zero reads as "none affected", which is the same error as counting an unscanned project as
    /// a clean one, one row further down the same table. So a row arrives with a reading or with a sentence
    /// saying why there is none, and a row with neither throws rather than publishing an empty track.</para>
    ///
    /// <para>★★ THE BAR, THE COUNTS AND THE (i) ALL COME FROM ONE RATIO, so a row cannot draw a bar that
    /// disagrees with the numbers beside it. The parts are built as counts over the ratio's own basis and
    /// rendered by <see cref="Figure"/>, and the ratio's <see cref="Figure.Headline"/> is appended to the
    /// explanation — so the share is stated in full, with both counts and the phrase naming the population,
    /// wherever the bar is drawn. Nothing here prints a percentage: the bar's LENGTH is the share, the text
    /// beside it is a quantity, and the percentage is one hover away with its denominator attached.</para>
    /// </remarks>
    public static Dictionary<string, object?> BarRow(
        string label,
        string? href,
        string? tip,
        Figure? drawn,
        string inked,
        string rest,
        string? unmeasured,
        params (string Text, string? Tone)[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (drawn is null && string.IsNullOrWhiteSpace(unmeasured))
        {
            throw new ArgumentException(
                "a row with no reading must say why there is none: a bar left undrawn and unexplained is a "
                + "blank where a reader expects a measurement, and a bar drawn at zero says nobody is affected "
                + "when what is true is that nobody looked",
                nameof(unmeasured));
        }

        if (drawn is { Kind: not FigureKind.Ratio })
        {
            throw new ArgumentException(
                "a bar draws a part of a whole, so it is drawn from a Ratio and from nothing else",
                nameof(drawn));
        }

        var row = new Dictionary<string, object?>(StringComparer.Ordinal) { ["label"] = label };
        Unless(row, "href", href);
        if (cells.Length > 0)
        {
            row["cells"] = cells.Select(c => c.Text).ToArray();
            row["cellTones"] = cells.Select(c => c.Tone ?? string.Empty).ToArray();
        }

        if (drawn is null)
        {
            row["unmeasured"] = unmeasured;
            Unless(row, "tip", tip);
            return row;
        }

        var numerator = drawn.Numerator!.Value;
        var part = Figure.Count(numerator, drawn.Basis, drawn.TakenAt);
        var remainder = Figure.Count(drawn.Population - numerator, drawn.Basis, drawn.TakenAt);

        row["parts"] =
            new object[]
            {
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["weight"] = numerator,
                    ["count"] = part.Split().Lead,
                    ["label"] = inked,
                    ["tone"] = "accent",
                },
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["weight"] = drawn.Population - numerator,
                    ["count"] = remainder.Split().Lead,
                    ["label"] = rest,
                    ["tone"] = "track",
                },
            };
        row["note"] = drawn.Split(FigureLead.Tally).Lead + " of " + Figure
            .Count(drawn.Population, drawn.Basis, drawn.TakenAt).Split().Lead;
        row["tip"] = string.IsNullOrWhiteSpace(tip)
            ? drawn.Headline()
            : tip + "\n\n" + drawn.Headline();
        return row;
    }

    /// <summary>
    /// One cell of a bar board's row: a figure's value under a column heading that names it.
    /// </summary>
    /// <param name="figure">The reading, or null for a cell this row cannot state.</param>
    /// <param name="tone">A band key for the cell's ink, or null.</param>
    /// <param name="note">A word beside the value — the band's own, when the ink is a band's.</param>
    /// <returns>The cell.</returns>
    /// <remarks>
    /// <para>★★ A CELL IS WRITTEN FROM A FIGURE OR IT IS WRITTEN EMPTY, which is the rule the CSV mirror
    /// already obeys and for the same reason: a reading that does not exist has no figure, so it cannot
    /// acquire a zero by a caller being helpful, and a reading that IS zero has one, so it cannot be blanked.
    /// The column heading is what names the population — which is exactly the sentence a percentage outlives,
    /// so no cell here is ever a share.</para>
    ///
    /// <para>★ AND A TONE ALWAYS ARRIVES WITH ITS WORD. The band scale is a judgement about assurance, defined
    /// on a CAI score and on nothing else; inking a number with it and not saying which band it is leaves a
    /// reader to decode a colour. <paramref name="note"/> is not optional decoration — it is what makes the
    /// ink a label rather than a mood.</para>
    /// </remarks>
    public static (string Text, string? Tone) Column(Figure? figure, string? tone = null, string? note = null)
    {
        if (tone is { Length: > 0 } && string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException(
                "a band-inked cell states which band it is: a colour with no word beside it asks a reader to "
                + "decode a hue, and the same hue means something else on a page that is not about scores",
                nameof(note));
        }

        // ★ THE WORD IS PART OF THE CELL'S TEXT, not a field beside it. A band name that lived in its own
        // slot could be dropped by a later layout while the ink stayed, which is precisely the state this
        // guard exists to make unreachable — "49.7" inked amber and nothing saying "Adequate".
        var text = figure is null ? string.Empty : figure.Split().Lead;
        return (
            string.IsNullOrWhiteSpace(note) || text.Length == 0 ? text : $"{text} {note.Trim()}",
            Blank(tone));
    }

    /// <summary>
    /// A board of populations split into named parts — or nothing at all, when no row survived.
    /// </summary>
    /// <param name="anchor">The section's anchor.</param>
    /// <param name="layout">"wide" for one row edge to edge, "table" for a row per entry.</param>
    /// <param name="kicker">The small line above, or null.</param>
    /// <param name="tip">
    /// The explanation behind an (i) BESIDE THE KICKER, or null for a board whose eyebrow carries none.
    /// ★★ THIS IS WHERE A WALL OF TEXT GOES INSTEAD OF ABOVE THE ROWS. A board's explanation used to be its
    /// <paramref name="lede"/> and rendered as running prose between the eyebrow and the table — which is the
    /// shape the owner named when they asked that the explanatory text live behind the same (i) the product
    /// already ships, so that nothing is lost and nothing is a wall. A lede is still right for ONE short
    /// sentence; a paragraph belongs here.
    /// </param>
    /// <param name="heading">The board's heading, or null.</param>
    /// <param name="lede">
    /// The paragraph under the heading, or null. ★ THE BOARD HAS NO FOOTNOTE, DELIBERATELY: the sentence that
    /// says what a row's population is has to be read BEFORE the rows, not after them, and the island declares
    /// no such prop — an undeclared widget prop is dropped at render without a word, so a footnote written
    /// here would simply not appear.
    /// </param>
    /// <param name="labelHeading">The heading over the label column, or null.</param>
    /// <param name="barHeading">The heading over the bar column, or null.</param>
    /// <param name="columns">The column headings for each row's cells, or null when the rows carry none.</param>
    /// <param name="rows">The rows, from <see cref="BarRow"/>.</param>
    /// <returns>The section, or null when there are no rows.</returns>
    public static Dictionary<string, object?>? ShareBars(
        string anchor,
        string layout,
        string? kicker,
        string? tip,
        string? heading,
        string? lede,
        string? labelHeading,
        string? barHeading,
        IReadOnlyList<string>? columns,
        IReadOnlyList<object?> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var present = rows.Where(r => r is not null).ToList();

        return present.Count == 0
            ? null
            : Section(
                null,
                anchor,
                Widget(
                    "cai-share-bars",
                    ("layout", layout),
                    ("kicker", kicker),
                    ("tip", tip),
                    ("heading", heading),
                    ("lede", lede),
                    ("label-heading", labelHeading),
                    ("bar-heading", barHeading),
                    ("columns", columns is { Count: > 0 } ? JsonSerializer.Serialize(columns, IslandJson) : null),
                    ("rows", JsonSerializer.Serialize(present, IslandJson))));
    }

    /// <summary>
    /// How an island's props are serialised: nulls dropped, so an absent value reaches the widget as an absent
    /// key and the widget falls back to its own default rather than to an explicit null it has to test for.
    /// </summary>
    private static readonly JsonSerializerOptions IslandJson =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    // ── the appearance names the theme styles ────────────────────────────────────────────────────────────────────

    /// <summary>Big mono figures in walled cells — the numbers a reader should see first.</summary>
    public const string StatBand = "StatBand";

    /// <summary>A flat list rendered as a real table; the anchor suffix picks the column count.</summary>
    public const string TableList = "TableList";

    /// <summary>A quieter, set-apart block — for the provenance a reader is owed but did not come for.</summary>
    public const string Note = "Note";

    /// <summary>Anchor suffixes the theme reads for a table's column count.</summary>
    public const string TwoColumns = "-cols2";

    /// <summary>Three columns is the table default; named so a call site says which it means.</summary>
    public const string ThreeColumns = "";
}
