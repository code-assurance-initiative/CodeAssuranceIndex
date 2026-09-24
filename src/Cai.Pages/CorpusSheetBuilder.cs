using System.Text.Json;
using Cai.Pages.Publishing;

namespace Cai.Pages;

/// <summary>
/// The corpus read as one document, on a stated instant: what was measured, then what was found in it.
/// </summary>
/// <remarks>
/// <para>★★ ONE SHEET FOR EVERY PRODUCER, exactly as the portrait and the field guide are. The corpus is
/// the union of every published delivery, whoever measured it — so the page about it is composed here,
/// from the standard's own fold, and a producer contributes measurements rather than sections.</para>
///
/// <para>★★ §1 COMES BEFORE §2 BECAUSE EVERY SHARE IN §2 IS TAKEN OVER IT. A reader who meets "1,161 carry
/// a known-vulnerable component" and learns the denominator afterwards has already done the wrong
/// arithmetic — and the arithmetic they did is the corpus-wide claim this whole shape exists to
/// prevent.</para>
///
/// <para>★★ AND NOT ONE SHARE HERE IS INKED WITH THE ASSURANCE BAND SCALE. That vocabulary is a judgement
/// about assurance and is defined on a CAI score; borrowing it would colour a share good or bad in a
/// direction the corpus never claimed, and on the vulnerability share it would have to be INVERTED to read
/// the right way round. The band scale is used exactly where it is defined: on scores.</para>
/// </remarks>
public static class CorpusSheetBuilder
{
    /// <summary>What the corpus count is a count of. ★ "Published", for the reason the index states.</summary>
    private static readonly Basis CodebasesBasis = Basis.Of(
        "published measured codebase",
        "published measured codebases");

    /// <summary>The first path segment every corpus page lives under.</summary>
    /// <remarks>
    /// ★★ DELIBERATELY OUTSIDE <see cref="SurveyPageBuilder.Root"/>. <c>surveys</c> is the catalogue of
    /// individual codebases; this is one reading of all of them at once, which is a different kind of
    /// document and belongs at a different address. The separation is also what keeps a survey sweep's
    /// reconciliation — which withdraws every path under its own root that it did not claim — from deleting
    /// these pages an hour after they appear.
    /// </remarks>
    public const string Root = "state-of-the-corpus";

    /// <summary>The address of the corpus sheet.</summary>
    public static string PathForIndex() => Root;

    /// <summary>The sheet for one reading of the corpus.</summary>
    /// <param name="records">One record per measured codebase — the corpus itself.</param>
    /// <param name="takenAt">The instant the corpus was read. ★ Not the instant the page was built.</param>
    /// <param name="history">Every reading recorded before now, in any order. Empty draws no line.</param>
    /// <remarks>
    /// ★ IT TAKES THE CORPUS, NOT A READING AND ITS CUTS. The totals and the per-group rows are folded here,
    /// from one set of records, so a row cannot be drawn over a population the headline above it would not
    /// recognise — which is exactly what happens when a caller assembles the two separately.
    /// </remarks>
    public static SurveyPage Build(
        IReadOnlyList<SurveyRecord> records,
        DateTimeOffset takenAt,
        IReadOnlyList<DatedReading>? history = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        var reading = CorpusReading.From(records, takenAt);
        var languages = CorpusCuts.ByLanguage(records, takenAt);
        var countries = CorpusCuts.ByCountry(records, takenAt);

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, "The state of the corpus"),
                PageNodes.RichText(PageProse.Paragraph(
                    $"Every codebase measured against the Codebase Assurance Index, read together on "
                    + $"{PageProse.Day(reading.TakenAt)}. Each figure below carries the population it "
                    + "was taken over, because the populations are not the same."))),
            Masthead(reading),
            Population(reading),
            Findings(reading),
            Languages(reading, languages),
            Countries(reading, countries),
            Series(history ?? []),
            Elsewhere(records, takenAt),
            About(reading),
        };

        return new SurveyPage(
            PathForIndex(),
            "The state of the corpus",
            MetaDescription(reading),
            PageNodes.Section([.. children]));
    }

    // ── §1, the population, stated before any share ──────────────────────────────────────────────────────────

    /// <summary>
    /// How much of the reading a dependency scanner could resolve, drawn to scale — or nothing at all.
    /// </summary>
    /// <remarks>
    /// ★ NULL, NOT AN EMPTY SECTION. A heading over nothing is a blank where a reader expects a measurement.
    /// </remarks>
    private static Dictionary<string, object?>? Population(CorpusReading reading)
    {
        if (reading.ResolvedShare is not { } resolvable)
        {
            return null;
        }

        // ★★ THE COUNT ON ITS OWN TERMS. "2,014 surveys nobody could scan" is a fact about the corpus that
        // survives being quoted, and it is the part of the gap a share can never state.
        var unscanned = reading.VulnUnmeasured == 0
            ? null
            : Figure.Count(
                reading.VulnUnmeasured,
                Basis.Of(
                    "survey whose dependencies no scanner could resolve",
                    "surveys whose dependencies no scanner could resolve"),
                reading.TakenAt);

        // ★ The register is impersonal standards prose. This page is served under the standard's own name,
        //   and a first person here would claim the standard ran the scan — the defect belongs to whoever
        //   ran it, named as a role rather than as a voice.
        var tip =
            "Every share in §2 is taken over the resolved part, never over the corpus."
            + "\n\nThe rest ship no lockfile a scanner can read. They are **unmeasured, not clean**. Each "
            + "share leaves them out rather than counting them as passing, because nobody looked at them."
            + Beside(unscanned, "ship nothing a dependency scanner could resolve into a graph")
            + Beside(
                reading.ScanFailedShare,
                "is the part of that gap where a scanner ran and failed. That is a defect in the measuring, "
                + "and it belongs to whoever ran the scan. The codebase's own condition is untouched by it, "
                + "and the remainder ship no lockfile for a scanner to resolve");

        return PageNodes.ShareBars(
            "population",
            "wide",
            "§1 Population",
            null,
            null,
            "A survey with no dependency graph is absent from every share below rather than counted as clean.",
            null,
            null,
            null,
            [
                PageNodes.BarRow(
                    "Surveys with a dependency graph a scanner could resolve",
                    null,
                    tip,
                    resolvable,
                    "resolved",
                    "unmeasured",
                    null),
            ]);
    }

    // ── §2, the findings, each leading with its share ────────────────────────────────────────────────────────

    /// <summary>The four quantities, or nothing at all when the reading can state none of them.</summary>
    /// <remarks>
    /// ★★ EACH LEADS WITH ITS SHARE, and the share never travels alone: the line directly beneath it is
    /// "1,161 of 1,511 surveys whose dependencies a scanner could resolve", so the denominator and the basis
    /// are one line away and cannot be left behind by anyone copying the cell.
    /// <para>★ THE FOUR POPULATIONS ARE FOUR DIFFERENT POPULATIONS, which is the one thing a row of four
    /// cells invites a reader to forget. Each states its own beneath itself, and the footnote says they are
    /// not the same.</para>
    /// </remarks>
    private static Dictionary<string, object?>? Findings(CorpusReading reading)
    {
        return PageNodes.FigureBand(
            "findings",
            "lead",
            "§2 Findings",
            null,
            "Each is taken over its own population, named beneath it. The four populations are not the same.",
            reading.VulnAffectedShare is not { } vulnerable
                ? null
                : PageNodes.Reading(
                    vulnerable,
                    "carry a known-vulnerable component",
                    "A component with an advisory published against it, matched from the lockfile the "
                    + "codebase ships. Counted once per survey however many findings it carries."
                    + Beside(reading.VulnHighOrCriticalShare, "carry at least one High or Critical")
                    + Beside(reading.VulnCriticalShare, "carry at least one Critical")
                    + BesideTotal(
                        reading.FindingsTotal,
                        "findings were found",
                        "More than one scanner can contribute, so a component two of them see counts as one "
                        + "finding each, while the survey counts above never double-count.")),
            reading.DisclosureWithoutPolicyShare is not { } noPolicy
                ? null
                : PageNodes.Reading(
                    noPolicy,
                    "publish no disclosure policy",
                    "No SECURITY.md or security.txt **in the repository itself**. An organisation can "
                    + "publish one policy in its .github repository covering everything it owns, and a "
                    + "codebase covered that way is reportable while having no file of its own. This share "
                    + "counts only a policy published in the repository itself."
                    + Beside(
                        reading.DisclosureContactShare,
                        "of the policies that do exist name someone to contact")
                    + Beside(
                        Absent(
                            reading.SecurityReadings - reading.DisclosureMeasured,
                            Basis.Of(
                                "survey taken before a disclosure policy was ever looked for",
                                "surveys taken before a disclosure policy was ever looked for"),
                            reading.TakenAt),
                        "were never asked, and are excluded from the population above rather than counted "
                        + "as codebases without a policy")),
            reading.SecretsInHistoryShare is not { } secrets
                ? null
                : PageNodes.Reading(
                    secrets,
                    "committed a secret at some point",
                    "Found anywhere in the full git history, not only at the current commit. Deleting the "
                    + "file does not remove it from history; only rotating the credential does."
                    + BesideTotal(
                        reading.SecretsHistoryFindings == 0 || reading.SecretsHistoryMeasured == 0
                            ? null
                            : Figure.Scalar(
                                reading.SecretsHistoryFindings,
                                reading.SecretsHistoryMeasured,
                                CorpusReading.SecretsScannedBasis,
                                reading.TakenAt),
                        "secrets were found")),
            reading.SbomShare is not { } sbom
                ? null
                : PageNodes.Reading(
                    sbom,
                    "publish an SBOM",
                    "Taken over the codebases with a release pipeline there was something to judge. A "
                    + "codebase that publishes no releases is outside this population rather than counted "
                    + "as failing it."
                    + Beside(reading.NoneOfFourShare, "do none of the four supply-chain controls")));
    }

    // ── §3, every language the reading can speak for ─────────────────────────────────────────────────────────

    /// <summary>The language cut as a bar per row, or nothing at all when no language clears the gate.</summary>
    /// <remarks>
    /// <para>★★ A LANGUAGE WITH NOTHING RESOLVABLE DRAWS NO BAR AND SAYS SO. A row at zero reads as "none
    /// affected" — exactly the substitution §1 exists to prevent, one table further in.
    /// <see cref="PageNodes.BarRow"/> refuses a row that has neither a reading nor a sentence saying why
    /// there is none, so the alternative cannot be written.</para>
    ///
    /// <para>★★ AFFECTED IS TAKEN OVER MEASURABLE, NEVER OVER SURVEYS. A language that resolves few of its
    /// dependencies draws a shorter bar because less was looked at, not because less is there — which is why
    /// the measurable count sits in a column of its own, beside the bar rather than behind it.</para>
    ///
    /// <para>★★ AND THE MEDIAN COLUMN IS NOT INKED WITH A BAND, WHICH IS A DELIBERATE DIFFERENCE FROM THE
    /// PRODUCER'S VERSION OF THIS TABLE. A band word is read off a rubric's cutlines, and the codebases in
    /// one of these rows were scored under many different rubric versions whose cutlines a later one may
    /// have moved. Banding their MEDIAN would publish a word no single rubric backs — and
    /// <c>Cai.Scoring.Bands</c> deliberately offers no way to band a bare number for exactly that reason.
    /// The cell states the median and the population it was taken over, which is checkable.</para>
    /// </remarks>
    private static Dictionary<string, object?>? Languages(CorpusReading reading, IReadOnlyList<CorpusSlice> cuts)
    {
        if (cuts.Count == 0)
        {
            return null;
        }

        var here = Basis.Of(
            "survey here whose dependencies a scanner could resolve",
            "surveys here whose dependencies a scanner could resolve");
        var written = Basis.Of(
            "survey written mainly in this language",
            "surveys written mainly in this language");

        var rows = new List<object?>();
        foreach (var cut in cuts)
        {
            rows.Add(PageNodes.BarRow(
                cut.Name,
                cut.Href,
                null,
                cut.Reading.VulnAffectedShare is { } affected
                    ? Figure.Ratio(cut.Reading.VulnAffected, cut.Reading.VulnMeasurable, here, reading.TakenAt)
                    : null,
                "affected",
                "not affected",
                "nothing a scanner could resolve",
                PageNodes.Column(Figure.Count(cut.Codebases, written, reading.TakenAt)),
                PageNodes.Column(
                    cut.Codebases == 0
                        ? null
                        : Figure.Score(cut.MedianHeadline, cut.Codebases, written, reading.TakenAt)),
                PageNodes.Column(Figure.Count(cut.Reading.VulnMeasurable, here, reading.TakenAt)),
                PageNodes.Column(Figure.Count(
                    cut.Reading.DisclosureMeasured - cut.Reading.DisclosurePolicy,
                    CorpusReading.DisclosureAskedBasis,
                    reading.TakenAt))));
        }

        var coverage = CorpusCuts.Coverage(
            cuts, reading.Codebases, Basis.Of("survey in this reading", "surveys in this reading"), reading.TakenAt);

        return PageNodes.ShareBars(
            "languages",
            "table",
            "§3 By language",
            // ★ Impersonal standards prose. A first person on the standard's own domain would claim the
            //   standard did the indexing; the limit belongs to whichever implementation produced the reading.
            "A language is read off the code, so nothing here rests on anybody declaring anything."
            + "\n\n**Affected is taken over Measurable, never over Surveys.** A language that resolves few of "
            + "its dependencies shows a shorter bar. Less was looked at, which is not the same as less being "
            + "there."
            // ★ ... and the sentence about what was left out is only written when something was. See
            //   Figure.Remainder: at full coverage "The rest" named nobody.
            + (coverage is null
                ? string.Empty
                : "\n\n" + coverage.Headline() + " could be placed in a language at all."
                    + (coverage.Remainder is > 0
                        ? " The rest carry no primary language this reading could read, which is a limit of "
                            + "the indexing behind this reading and never a fact about the codebases themselves."
                        : string.Empty)),
            null,
            null,
            "Language",
            "Affected of measurable",
            ["Surveys", "Median", "Measurable", "No policy"],
            rows);
    }

    // ── §4, every country the reading can speak for ──────────────────────────────────────────────────────────

    /// <summary>The two units a country is counted in — codebases, and the accounts that hold them.</summary>
    private static readonly Basis CountryCodebases = Basis.Of(
        "codebase whose owner declared this country",
        "codebases whose owner declared this country");

    /// <summary>The other unit, kept apart from the first.</summary>
    private static readonly Basis CountryOwners = Basis.Of(
        "owner who declared this country",
        "owners who declared this country");

    /// <summary>The denominator every bar in §4 is drawn over — the same one §3's bars are drawn over.</summary>
    private static readonly Basis CountryResolvable = Basis.Of(
        "survey here whose dependencies a scanner could resolve",
        "surveys here whose dependencies a scanner could resolve");

    /// <summary>The country cut as a bar per row, or nothing at all when no country clears the gate.</summary>
    /// <remarks>
    /// <para>★★ THE SAME ISLAND AND THE SAME LAYOUT AS §3, WHICH IS THE POINT RATHER THAN A CONVENIENCE. Two
    /// cuts of one corpus drawn differently read as two different kinds of claim, and a reader who has learnt
    /// how to read the language table should not have to learn a second grammar one section down.</para>
    ///
    /// <para>★★ AND THE CAVEAT IS NOT OPTIONAL HERE, WHICH IS THE ONE WAY §4 IS NOT §3. A language is read
    /// off the code by the pass that measured everything else: nobody declares it and no account can decline
    /// to. A country is a claim an account writes about ITSELF in a free-text profile line, it places an
    /// OWNER rather than a project or a legal entity, and most owners cannot be placed at all. A table of
    /// countries with no caveat beside it is read as a map of open source, which is a claim this reading
    /// cannot make and has never made.</para>
    ///
    /// <para>★ A COUNTRY UNDER THE GATE IS NOT A ROW, AND THE NUMBER OF THEM IS STATED. Dropping them
    /// silently would publish a table that says these are the countries in the corpus; the count of what was
    /// held back is what stops it saying that.</para>
    /// </remarks>
    private static Dictionary<string, object?>? Countries(CorpusReading reading, IReadOnlyList<CorpusSlice> cuts)
    {
        var published = cuts.Where(c => c.Path is not null).ToList();
        if (published.Count == 0)
        {
            return null;
        }

        var rows = new List<object?>();
        foreach (var cut in published)
        {
            rows.Add(PageNodes.BarRow(
                cut.Name,
                cut.Href,
                null,
                cut.Reading.VulnAffectedShare is not null
                    ? Figure.Ratio(
                        cut.Reading.VulnAffected, cut.Reading.VulnMeasurable, CountryResolvable, reading.TakenAt)
                    : null,
                "affected",
                "not affected",
                "nothing a scanner could resolve",
                PageNodes.Column(Figure.Count(cut.Codebases, CountryCodebases, reading.TakenAt)),
                PageNodes.Column(Figure.Count(cut.Reading.Origins.OwnersConsidered, CountryOwners, reading.TakenAt)),
                // ★ NO BAND INK ON THE MEDIAN, for the same reason as §3: a band word is read off a rubric's
                //   cutlines, and these codebases were scored under many rubric versions.
                PageNodes.Column(
                    cut.Codebases == 0
                        ? null
                        : Figure.Score(cut.MedianHeadline, cut.Codebases, CountryCodebases, reading.TakenAt)),
                PageNodes.Column(Figure.Count(cut.Reading.VulnMeasurable, CountryResolvable, reading.TakenAt)),
                PageNodes.Column(Figure.Count(
                    cut.Reading.DisclosureMeasured - cut.Reading.DisclosurePolicy,
                    CorpusReading.DisclosureAskedBasis,
                    reading.TakenAt))));
        }

        return PageNodes.ShareBars(
            "countries",
            "table",
            "§4 By country",
            Census(reading, cuts, published.Count),
            null,
            null,
            "Country",
            "Affected of measurable",
            ["Codebases", "Owners", "Median", "Measurable", "No policy"],
            rows);
    }

    /// <summary>
    /// What a country table is a table OF — stated where a reader meets the rows.
    /// </summary>
    /// <remarks>
    /// ★★ THE TWO UNITS ARE STATED AS THIS READING'S OWN FIGURES. The sentence could argue that a codebase
    /// count is not a headcount by asserting "one account can hold forty codebases" — a concrete-sounding
    /// number nobody measured, on a page whose entire argument is that a figure without its population cannot
    /// be checked. Two figures this reading already holds make the same point and can be checked against the
    /// table above.
    /// </remarks>
    private static string Census(CorpusReading reading, IReadOnlyList<CorpusSlice> cuts, int published)
    {
        var origins = reading.Origins;
        var placedOwners = Share(
            origins.OwnersResolved,
            origins.OwnersConsidered,
            Basis.Of("owner behind the whole corpus", "owners behind the whole corpus"),
            reading.TakenAt);
        var reach = Share(
            origins.CodebasesResolved,
            origins.Codebases,
            Basis.Of("measured codebase", "measured codebases"),
            reading.TakenAt);
        var held = Share(
            cuts.Count - published,
            cuts.Count,
            Basis.Of("country the corpus could place at all", "countries the corpus could place at all"),
            reading.TakenAt);

        var codebases = Figure.Count(
            origins.Codebases, Basis.Of("measured codebase", "measured codebases"), reading.TakenAt);
        var owners = Figure.Count(
            origins.OwnersConsidered,
            Basis.Of("account that owns one of them", "accounts that own them"),
            reading.TakenAt);

        return "A country is normalised from what an account writes about itself in its own profile line, so "
            + "it places an owner. It never places a project, and never a legal entity."
            + (placedOwners is null
                ? string.Empty
                : "\n\n**" + placedOwners.Headline() + "** could be placed in a country at all. This is a "
                    + "reading of the codebases whose owners said where they are, and of nothing wider.")
            + (reach is null
                ? string.Empty
                : "\n\n**" + reach.Headline() + "** is how much of the corpus that reaches.")
            + (held is null
                ? string.Empty
                : "\n\n**" + held.Headline() + "** have no page, because they hold fewer than "
                    + CorpusCuts.MinimumCountryCodebases + " measured codebases. Below that, a national figure "
                    + "describes a few account holders and not a country. They are absent from the rows above "
                    + "rather than folded into them.")
            + "\n\nCodebases and accounts are counted separately because neither converts into the other: the "
            + codebases.Headline() + " in this reading sit behind " + owners.Headline() + ".";
    }

    /// <summary>A share for one of §4's three sentences, or null when the sentence should not be written.</summary>
    /// <remarks>
    /// ★★ NULL ON A ZERO NUMERATOR TOO, WHICH IS NOT WHAT <see cref="CorpusReading"/>'S SHARE DOES — and the
    /// difference is deliberate. Each of the three readings taken through here introduces a SENTENCE that
    /// only exists when it has something to say: how many owners could be placed, how much of the corpus the
    /// rows reach, how many countries were held back. "0 countries have no page, because they hold fewer
    /// than ten measured codebases" is a sentence about nobody. A measured share is the opposite case: "0.0%
    /// carry a known-vulnerable component (0 of 34)" is the most valuable thing §2 can say, so
    /// <see cref="CorpusReading"/> refuses a share only when its DENOMINATOR is empty. Do not reuse this
    /// helper for a figure where zero is a result.
    /// </remarks>
    private static Figure? Share(int numerator, int denominator, Basis basis, DateTimeOffset takenAt) =>
        denominator == 0 || numerator == 0 ? null : Figure.Ratio(numerator, denominator, basis, takenAt);

    // ── §5, the one quantity this axis is defined over ───────────────────────────────────────────────────────

    /// <summary>
    /// The median drawn, and both of its ends stated beneath it — or nothing at all when too few dated
    /// readings are held to draw a line.
    /// </summary>
    /// <remarks>
    /// <para>★★ ONLY A MEDIAN IS DRAWN, WHICH IS THE WHOLE POINT OF STATING THE OTHER. The site ships one
    /// chart and its axis is the fixed assurance band scale; a count of codebases plotted on it lands
    /// thousands of units above the box, and a share lands inside it and looks right while being inked good
    /// or bad in a direction the corpus never claimed. So the corpus's own growth is a figure and never a
    /// line, and the note says so where a reader will ask.</para>
    ///
    /// <para>★★ EVERY POINT IS A READING KEPT AS IT WAS TAKEN. A correction is a new reading on a new day,
    /// never an edit to an old one, so this line is read out of what was recorded at the time and is never
    /// recomputed from today's corpus — which the standard COULD do, and which would draw a line that never
    /// happened the first time a publication was withdrawn.</para>
    ///
    /// <para>★★ AND WHEN IT IS ABSENT, §6 KEEPS ITS NUMBER — the sheet reads §1, §2, §3, §4, §6. That gap
    /// is deliberate and must not be closed by renumbering: this page is "a citable observation of a single
    /// reading and is never edited", so a citation of "§4 By country" has to mean the same section on every
    /// reading ever published. Renumbering would make a section number depend on how many dated readings
    /// the store happened to hold that day. Rendered and looked at with a single dated reading
    /// (<c>CORPUS_E2E_WEEKS=1</c>): six islands rather than seven, no heading over a blank gap, and the
    /// published HTML contains no §5 at all.</para>
    ///
    /// <para>★ THE SECOND PAIR IS THE FIRST PAIR'S POPULATION, which is why neither restates it: "580 →
    /// 3,545 published measured codebases" IS what "across 580" and "across 3,545" say under the medians.
    /// Saying it twice is the redundancy; moving it behind a disclosure would be worse, because behind a
    /// disclosure a population is separated from its figure.</para>
    /// </remarks>
    private static Dictionary<string, object?>? Series(IReadOnlyList<DatedReading> history)
    {
        if (CorpusSeries.Medians(history) is not { } medians)
        {
            return null;
        }

        var weekly = medians.Weekly();
        var sizes = CorpusSeries.Sizes(history);

        // ★ THE SECOND PAIR IS THE FIRST PAIR'S POPULATION, so neither restates it: "6 → 10 published
        //   measured codebases" IS what "across 6" and "across 10" say under the medians. The label spells
        //   the basis, which is the condition for dropping the sub-lines at all.
        var statesItsOwn = sizes is null;
        var figures = new List<object?>
        {
            PageNodes.Movement(medians.First, medians.Last, "Median", statesItsOwn),
            sizes is null ? null : PageNodes.Movement(sizes.First, sizes.Last, "Published measured codebases", false),
        };

        var tip =
            "The median CAI of the whole measured corpus. Every point is one reading taken on the day it is "
            + "dated, kept as it was taken and never rewritten: a correction is a new reading on a new day, "
            + "never an edit to an old one. So this line is read out of what was recorded at the time and is "
            + "never recomputed from today's corpus."
            + "\n\nOnly a median is drawn. The chart's axis is the fixed band scale a CAI is defined on, "
            + "which fits a median and leaves a share or a count to be stated as its own dated figure instead."
            + "\n\nThe line is sampled to one point a week, and each point is the newest reading on or "
            + "before its own date. The corpus is read nightly and its median sits still for weeks, so a mark "
            + "per reading put a mark wherever the median happened to change. The spacing of the marks then "
            + "drew the data's volatility rather than the passage of time.";

        return PageNodes.Section(
            null,
            "trend",
            PageNodes.Widget(
                "cai-trend",
                ("kicker", "\u00a75 Series"),
                ("tip", tip),
                ("figures", JsonSerializer.Serialize(figures.Where(f => f is not null).ToList())),
                ("series", weekly.Points()),
                // ★ The dates are the SAMPLED series' own ends, so resampling cannot desynchronise them from
                //   the points they label.
                ("first-date", PageProse.Day(weekly.First.TakenAt)),
                ("last-date", PageProse.Day(weekly.Last.TakenAt)),
                // ★ The claim travels WITH the points: only Weekly sets it, so no call site can promise a
                //   sampling it did not do.
                ("sampled", weekly.Sampling)));
    }

    // ── the masthead, §6 and the provenance ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// The figures a reader should see first, over the instant the reading was taken.
    /// </summary>
    /// <remarks>
    /// ★★ THE INSTANT OF THE READING, NOT THE DAY AND NOT THE BUILD. The dateline is the label a reader
    /// cites this sheet by, and two readings a day under one day stamp cannot be told apart.
    /// </remarks>
    private static Dictionary<string, object?>? Masthead(CorpusReading reading)
    {
        var codebases = Figure.Count(reading.Codebases, CodebasesBasis, reading.TakenAt);

        return PageNodes.FigureBand(
            "masthead",
            "head",
            null,
            // ★ THE DAY, NOT THE MINUTE, where a reader meets it. This page is republished hourly, so a
            //   timestamp in the masthead changed all day and said nothing a reader could use — and the
            //   reading it describes is a day's reading. The exact instant is kept at the foot of the
            //   page, in the note that makes the figure citable, which is the only place it has work to do.
            PageProse.Day(reading.TakenAt),
            null,
            PageNodes.Reading(codebases, "Codebases"),
            reading.SecurityReadings == 0
                ? null
                : PageNodes.Reading(
                    Figure.Count(reading.SecurityReadings, CorpusReading.SurveyedBasis, reading.TakenAt),
                    "Read for security"));
    }

    /// <summary>
    /// §6. Where a reader goes from the sheet — and only to pages this build produced.
    /// </summary>
    /// <remarks>
    /// ★★ A CARD IS OFFERED BECAUSE A PAGE WAS BUILT, NOT BECAUSE ONE PROBABLY WAS. The cards are counted
    /// out of the same call that produces the pages, so the index and the set cannot disagree. They did
    /// once, in the producer: the cards were gated on a sub-reading merely EXISTING while the pages were
    /// gated on it having something to say, and at a zero population a reader was offered four links to
    /// pages nobody built — which is a 404, or worse, the page the last sweep left behind.
    /// </remarks>
    private static Dictionary<string, object?>? Elsewhere(IReadOnlyList<SurveyRecord> records, DateTimeOffset takenAt)
    {
        var built = CorpusGroupPages.Build(records, takenAt)
            .Concat(CorpusAdvisoryPages.Build(CorpusReading.From(records, takenAt), takenAt))
            .Select(p => p.Path)
            .ToHashSet(StringComparer.Ordinal);

        var cards = new List<object?>();
        foreach (var (path, label, note) in new[]
        {
            (CorpusGroupPages.PathForLanguageIndex(), "By language", "Every language with enough measured codebases to read as a group"),
            (CorpusGroupPages.PathForCountryIndex(), "By country", "Where the codebases' owners say they are, with the denominators that make it readable"),
            (CorpusAdvisoryPages.PathForAdvisoryIndex(), "Advisories seen", "Every advisory seen across the corpus — each count a floor"),
            (CorpusAdvisoryPages.PathForPackageIndex(), "Vulnerable packages", "Every package seen carrying an advisory — each count a floor"),
        })
        {
            if (built.Contains(path))
            {
                cards.Add(PageNodes.LinkCard("cai", label, note, $"/{path}/"));
            }
        }

        return PageNodes.LinkCards("elsewhere", "§6 Elsewhere in this reading", null, cards);
    }

    /// <summary>What this reading is, and the one thing a reader must know before quoting it.</summary>
    private static Dictionary<string, object?> About(CorpusReading reading) =>
        PageNodes.Section(
            PageNodes.Note,
            "about",
            PageNodes.RichText(PageProse.Paragraph(
                $"Taken over {PageProse.Count(reading.Codebases)} published measured codebases, measured "
                + $"{PageProse.DayAndTime(reading.TakenAt)}. Corpus readings are append-only: this one is a "
                + "citable observation of a single reading and is never edited. Every share on this page "
                + "states the counts it was derived from, so any of them can be checked against the reading "
                + "rather than taken on faith.")));

    // ── the sentences a figure is stated inside ──────────────────────────────────────────────────────────────

    /// <summary>A reading beside its figures, or nothing when it was not taken.</summary>
    /// <remarks>
    /// ★ Assumes the headline ends in a noun phrase the caller's clause can take as its subject, which is
    /// true of a share and of a count. It is NOT true of a scalar — see <see cref="BesideTotal"/>.
    /// </remarks>
    private static string Beside(Figure? figure, string rest) =>
        figure is null ? string.Empty : "\n\n" + figure.Headline() + " " + rest + ".";

    /// <summary>
    /// A TOTAL beside its figures — the count, what it counts, and what the count is not.
    /// </summary>
    /// <remarks>
    /// ★★ WHY THIS IS NOT <see cref="Beside"/>. A scalar's headline reads "63,266 across 1,888 surveys whose
    /// dependencies a scanner could resolve" — value first, population last — so a clause appended to it
    /// lands after the population and describes the wrong noun. What that produced was "…surveys whose
    /// dependencies a scanner could resolve were found across them": "across" twice, and no statement
    /// anywhere in it of what 63,266 counts. The noun belongs BETWEEN the number and its population, which
    /// is a position only something writing both can reach.
    /// </remarks>
    private static string BesideTotal(Figure? figure, string counting, string? rest = null) =>
        figure is null
            ? string.Empty
            : "\n\n" + figure.Total(counting) + "." + (rest is null ? string.Empty : " " + rest);

    /// <summary>A count that exists only as an absence — null when there is nothing to count.</summary>
    private static Figure? Absent(int count, Basis basis, DateTimeOffset takenAt) =>
        count <= 0 ? null : Figure.Count(count, basis, takenAt);

    private static string MetaDescription(CorpusReading reading) =>
        $"{PageProse.Count(reading.Codebases)} codebases measured against the Codebase Assurance Index, read "
        + $"together on {PageProse.Day(reading.TakenAt)} — what was measured, and what was found in it.";
}
