using System.Text.Json.Serialization;

namespace Cai.Scoring;

/// <summary>
/// DESCRIPTIVE, NON-SCORED security reading of ONE codebase — what the survey found when it looked for
/// known-vulnerable dependencies, a disclosure policy, committed secrets and supply-chain attestations.
/// </summary>
/// <remarks>
/// <para>★★ EVERY FIELD IS A YES/NO ABOUT ONE REPOSITORY, and the corpus aggregate is a count of
/// REPOSITORIES. That is what stops a repository with 985 vulnerabilities from being 985 votes: the
/// corpus's headline counts codebases affected, and only <see cref="Findings"/> and its severity siblings
/// count findings. Carried over from the producer's implementation together with the reason, because the
/// reason is the part that stops it being aggregated the other way.</para>
///
/// <para>★★ "NOT MEASURED" IS A THIRD ANSWER AND IT IS CARRIED EXPLICITLY. <c>finding_count: 0</c> is
/// written by three different paths that mean three different things: a clean scan, a scan that exited
/// non-zero with nothing parsed, and a scan whose output could not be read. Only the first is a
/// measurement. A reading that collapsed them would publish the other two as clean bills of health —
/// which is how a corpus comes to report 3,464 codebases measured when it measured 1,588. So
/// <see cref="VulnMeasurable"/>, <see cref="DisclosureMeasured"/>, <see cref="SecretsHistoryMeasured"/>,
/// <see cref="SecretsCurrentMeasured"/> and <see cref="SupplyChainMeasurable"/> each travel beside the
/// answer they qualify, and the standard publishes the denominator it was told.</para>
///
/// <para>★ IT IS NEVER FOLDED INTO THE CAI and can never move the headline — the same standing as
/// <c>rebuildCost</c>, <c>busFactor</c> and <c>topology</c>. It exists so the standard can publish what was
/// FOUND across the corpus beside what it SCORED, from the same signed artifacts.</para>
///
/// <para>★ WHO DECIDES WHAT COUNTS AS MEASURED IS THE PRODUCER. These are the producer's answers about its
/// own instrument, which is exactly why they are evidence and not verdict: a second producer whose scanner
/// resolves different ecosystems says so here, and the standard's denominators move with it rather than
/// silently absorbing it.</para>
/// </remarks>
public sealed record SecurityReading
{
    /// <summary>A scanner resolved this codebase's dependency graph and reported counts.</summary>
    [JsonPropertyName("vulnMeasurable")] public bool VulnMeasurable { get; init; }

    /// <summary>A dependency scanner RAN and fell over, or emitted output the parser could not read. Only
    /// meaningful when <see cref="VulnMeasurable"/> is false — a survey another scanner measured is measured.</summary>
    [JsonPropertyName("vulnScanFailed")] public bool VulnScanFailed { get; init; }

    /// <summary>At least one known-vulnerable component.</summary>
    [JsonPropertyName("vulnAffected")] public bool VulnAffected { get; init; }

    /// <summary>At least one High or Critical.</summary>
    [JsonPropertyName("vulnHighOrCritical")] public bool VulnHighOrCritical { get; init; }

    /// <summary>At least one Critical.</summary>
    [JsonPropertyName("vulnCritical")] public bool VulnCritical { get; init; }

    /// <summary>Vulnerability findings, summed across the measurable dimensions of this survey.</summary>
    [JsonPropertyName("findings")] public long Findings { get; init; }

    /// <summary>Critical findings.</summary>
    [JsonPropertyName("findingsCritical")] public long FindingsCritical { get; init; }

    /// <summary>High findings.</summary>
    [JsonPropertyName("findingsHigh")] public long FindingsHigh { get; init; }

    /// <summary>Medium findings.</summary>
    [JsonPropertyName("findingsMedium")] public long FindingsMedium { get; init; }

    /// <summary>Low findings.</summary>
    [JsonPropertyName("findingsLow")] public long FindingsLow { get; init; }

    /// <summary>A disclosure policy was actually looked for.</summary>
    /// <remarks>
    /// ★ NOT the same as "a policy applies". The producer's no-policy path is a NotApplicable result, and
    /// gating on applicability would throw away every MEASURED "this codebase publishes no way to report a
    /// vulnerability" as though the question had never been asked — which is the corpus's single most
    /// quotable figure. The question having been asked is the measurement; the answer is below.
    /// </remarks>
    [JsonPropertyName("disclosureMeasured")] public bool DisclosureMeasured { get; init; }

    /// <summary>A disclosure policy file was found.</summary>
    [JsonPropertyName("disclosurePolicy")] public bool DisclosurePolicy { get; init; }

    /// <summary>The policy names a reporting contact.</summary>
    [JsonPropertyName("disclosureContact")] public bool DisclosureContact { get; init; }

    /// <summary>The full-history secret scan ran.</summary>
    [JsonPropertyName("secretsHistoryMeasured")] public bool SecretsHistoryMeasured { get; init; }

    /// <summary>It found at least one secret ever committed.</summary>
    [JsonPropertyName("secretsHistoryAffected")] public bool SecretsHistoryAffected { get; init; }

    /// <summary>How many historical secret findings it reported.</summary>
    [JsonPropertyName("secretsHistoryFindings")] public long SecretsHistoryFindings { get; init; }

    /// <summary>The current-files secret scan ran.</summary>
    [JsonPropertyName("secretsCurrentMeasured")] public bool SecretsCurrentMeasured { get; init; }

    /// <summary>It found at least one secret still in the working tree.</summary>
    [JsonPropertyName("secretsCurrentAffected")] public bool SecretsCurrentAffected { get; init; }

    /// <summary>There is a release pipeline the four supply-chain controls could be judged against.</summary>
    [JsonPropertyName("supplyChainMeasurable")] public bool SupplyChainMeasurable { get; init; }

    /// <summary>Publishes an SBOM.</summary>
    [JsonPropertyName("sbom")] public bool Sbom { get; init; }

    /// <summary>Publishes build provenance.</summary>
    [JsonPropertyName("provenance")] public bool Provenance { get; init; }

    /// <summary>Signs its releases.</summary>
    [JsonPropertyName("signing")] public bool Signing { get; init; }

    /// <summary>Pins its CI actions to a commit.</summary>
    [JsonPropertyName("pinnedActions")] public bool PinnedActions { get; init; }

    /// <summary>Every advisory this survey could be SEEN to name — a FLOOR, not a list.</summary>
    /// <remarks>
    /// ★ See <see cref="AdvisoryListComplete"/>. The list is what was visible; the counts above are what was
    /// measured, and they are allowed to disagree.
    /// </remarks>
    [JsonPropertyName("advisories")] public IReadOnlyList<VisibleAdvisory> Advisories { get; init; } = [];

    /// <summary>At least one vulnerability brief on this survey was read for advisories at all. False means
    /// the producer's own indexing has not reached it, which is never a fact about the codebase.</summary>
    [JsonPropertyName("advisoriesRead")] public bool AdvisoriesRead { get; init; }

    /// <summary>
    /// Every finding the counts above describe also appeared as a row that could be read.
    /// </summary>
    /// <remarks>
    /// ★★ THE COUNT IS THE TRUTH AND THE LIST IS WHAT WAS SEEN. They differ for real reasons — a rendered
    /// list capped at a row limit, a dimension whose findings carry no file path and so never reach the list
    /// at all — and either way a survey may carry advisories nobody can see. A corpus figure that did not
    /// know which surveys those were could only ever be published as though complete. Defaults to true so a
    /// producer that says nothing is taken at its word; a producer that knows better says so.
    /// </remarks>
    [JsonPropertyName("advisoryListComplete")] public bool AdvisoryListComplete { get; init; } = true;

    /// <summary>Nobody could look at this codebase's dependencies.</summary>
    [JsonIgnore] public bool VulnUnmeasured => !VulnMeasurable;

    /// <summary>A disclosure policy was never looked for — a fact about the survey, never about the codebase.</summary>
    [JsonIgnore] public bool DisclosureUnmeasured => !DisclosureMeasured;

    /// <summary>Does none of the four. Only meaningful when <see cref="SupplyChainMeasurable"/> is true.</summary>
    [JsonIgnore]
    public bool NoneOfFour => SupplyChainMeasurable && !Sbom && !Provenance && !Signing && !PinnedActions;
}

/// <summary>
/// One advisory seen against one package version in one survey.
/// </summary>
/// <param name="AdvisoryId">The advisory as the ecosystem names it — <c>GHSA-…</c>, <c>CVE-…</c>,
/// <c>RUSTSEC-…</c>, <c>GO-…</c> — VERBATIM. ★ Not case-folded: a GHSA id is canonically lower case and a
/// CVE id upper, so a reader who pastes one from a page into the advisory database has to get back the
/// string the ecosystem uses. Two spellings of one id are collapsed case-insensitively at the point they
/// are counted, which is the only place the distinction would do harm.</param>
/// <param name="Package">The package the advisory is against, verbatim — an npm scope
/// (<c>@humanfs/node</c>) and a Go module path (<c>google.golang.org/grpc</c>) are part of the name and are
/// kept.</param>
/// <param name="PackageVersion">The resolved version the scan found.</param>
/// <param name="Inherited">The package was pulled in by something else rather than declared by the
/// codebase.</param>
public sealed record VisibleAdvisory
{
    /// <summary>The advisory id, verbatim.</summary>
    [JsonPropertyName("advisoryId")] public string AdvisoryId { get; init; } = "";

    /// <summary>The package it is against, verbatim.</summary>
    [JsonPropertyName("package")] public string Package { get; init; } = "";

    /// <summary>The resolved version the scan found.</summary>
    [JsonPropertyName("packageVersion")] public string PackageVersion { get; init; } = "";

    /// <summary>Pulled in transitively rather than declared by this codebase.</summary>
    [JsonPropertyName("inherited")] public bool Inherited { get; init; }
}
