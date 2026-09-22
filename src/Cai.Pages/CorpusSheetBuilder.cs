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
    public static SurveyPage Build(CorpusReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var children = new List<object?>
        {
            PageNodes.Section(
                PageNodes.Heading(1, "State of the corpus"),
                PageNodes.RichText(PageProse.Paragraph(
                    $"Every codebase measured against the Codebase Assurance Index, read together on "
                    + $"{PageProse.DayAndTime(reading.TakenAt)}. Each figure below carries the population it "
                    + "was taken over, because the populations are not the same."))),
            Population(reading),
            Findings(reading),
        };

        return new SurveyPage(
            PathForIndex(),
            "State of the corpus",
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
