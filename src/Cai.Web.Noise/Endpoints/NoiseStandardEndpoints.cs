namespace Cai.Web.Noise;

/// <summary>
/// The noise-measurement standard's public contract: the verdict set an engine implements, and the
/// method's own rules.
/// </summary>
/// <remarks>
/// <para>★ WHY THIS LIVES IN CAI. A self-measured number is not a claim a buyer can use — the reader
/// cannot tell one vendor's rigour from another's assertion, and today no scanner in this market
/// publishes a measured error rate at all. A shared method makes numbers commensurable, which is the
/// only thing that lets anyone compare.</para>
/// <para><b>CAI specifies and verifies; it does not referee.</b> It publishes the method, the verdict
/// set and the holdout with its seed, and checks that a submitted run reproduces. It never runs anyone's
/// scanner and never owns a verdict. The standard is owned by a participant, and a referee that plays
/// for one team is worth nothing — so the design removes the conflict rather than promising to manage
/// it.</para>
/// <para><b>Anonymous.</b> A standard nobody can read without credentials is not a standard.</para>
/// </remarks>
public static class NoiseStandardEndpoints
{
    /// <summary>
    /// The method version. ★ Versioned because it WILL change, and a standard that changes silently is
    /// worse than none — a number published against v1 must stay readable when v2 exists.
    /// </summary>
    public const string MethodVersion = "noise-1.0-draft";

    /// <summary>
    /// ★ Combined exclusions above this VOID a run.
    /// </summary>
    /// <remarks>
    /// Items nobody could judge leave the rate, because scoring agreement on a question with no
    /// determinate answer measures coin flips. But they are NOT randomly distributed — they concentrate
    /// where the evidence is thin, which is where judging is worst — so unbounded exclusion flatters a
    /// result for reasons having nothing to do with the tool. A bounded, published exclusion is a
    /// diagnostic; an unbounded one is a laundry.
    /// </remarks>
    /// <summary>
    /// Kept as a forwarder so nothing reads a second copy of the ceiling.
    /// </summary>
    /// <remarks>
    /// ★★ It USED to be the definition, declared here and compared against nothing while being echoed to
    /// readers at <c>/method</c> as though it governed something. The one definition now lives beside the
    /// check that applies it — see <see cref="PublicationContract.MaxExclusionRate"/>.
    /// </remarks>
    public const double MaxExclusionRate = PublicationContract.MaxExclusionRate;

    /// <remarks>
    /// ★ Each route family maps itself from its own class, and this entry point calls them in the order the routes
    /// have always been registered — so the endpoint table reads the same as it did when every route was one inline
    /// lambda in this method.
    /// </remarks>
    public static void MapNoiseStandard(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        NoiseMethodEndpoints.Map(endpoints);
        NoiseRecordEndpoints.Map(endpoints);
        NoiseCorpusEndpoints.Map(endpoints);
        NoiseSubmissionEndpoints.Map(endpoints);
        NoiseCascadeEndpoints.Map(endpoints);
        NoiseCrowdEndpoints.Map(endpoints);
        NoiseFindingEndpoints.Map(endpoints);
        NoiseRegisterEndpoints.Map(endpoints);
        NoiseDisputeEndpoints.Map(endpoints);
        NoiseRejudgeEndpoints.Map(endpoints);
        NoisePublicationEndpoints.Map(endpoints);
        NoiseRecallEndpoints.Map(endpoints);
        NoiseCrowdResultsEndpoints.Map(endpoints);
    }

    /// <summary>
    /// A run being published.
    /// </summary>
    /// <param name="Clusters">
    /// ★★ Repositories, not findings. Required, because computing power from the finding count treats
    /// correlated findings as independent observations and understates the detectable difference.
    /// </param>
    /// <param name="PreviousRate">The last published rate, when there is one to compare against.</param>
    /// <param name="FixRateUnavailable">
    /// ★ Why the anchor is missing, when it is. A first cycle genuinely has no window yet, and refusing
    /// that outright would push everyone towards inventing observations — so the reason publishes, and the
    /// absence becomes one a reader can weigh instead of one they cannot see.
    /// </param>
    public sealed record PublicationRequest(
        int Reported, int Adjudicated, int Excluded, int Unrated,
        // ★★ The period the number measures. Required — see PublicationContract.
        string? Period,
        int ValidAndActionable, int ValidNotActionable, int Noise,
        int Clusters, double? PreviousRate,
        int? FixRateWindowDays,
        IReadOnlyList<FixObservationEntry>? FixRateObservations,
        string? FixRateUnavailable,
        // ★★ The fields /api/noise/method has always listed as requiredWithEveryRate, and which this record
        // had nowhere to put — so the contract was published and unenforceable. See PublicationContract.
        long? LocCovered = null,
        double? RecallEstimate = null,
        string? RecallMethod = null,
        string? RecallNote = null,
        IReadOnlyList<ClaimClassEntry>? ClaimClasses = null,
        IReadOnlyList<RecencyEntry>? RecencyStrata = null,
        RunConfiguration? Configuration = null,
        string? ToolVersion = null,
        string? HoldoutSeed = null,
        string? ModelSet = null,
        bool? GitMiningVerified = null,
        int? GapsFoundSinceLastPeriod = null,

        // ★★ PER-CLUSTER TALLIES, without which the macro average cannot exist. See ClusterTallyEntry.
        IReadOnlyList<ClusterTallyEntry>? ClusterTallies = null,

        // ★★ A stated reason no second pass was run. NOTE there is deliberately NO field for its OUTCOME: CAI
        // holds that, and a body able to declare its own reproducibility would be publishing the self-measured
        // number the standard exists to replace. Optional and LAST, because inserting a required parameter in
        // the middle of a positional record silently reorders every caller that passes them by position.
        string? RejudgeUnavailable = null);

    /// <summary>One claim class's share of the run, as it arrives on the wire.</summary>
    public sealed record ClaimClassEntry(string? ClaimClass, int Judged, int Noise);

    /// <summary>One recency stratum's share of the run, as it arrives on the wire.</summary>
    public sealed record RecencyEntry(string? Stratum, int Judged, int Noise);


    /// <summary>One tool's finding and its adjudication, as it arrives on the wire.</summary>
    /// <param name="HasFixPairOracle">
    /// ★ Whether a real before/after fix pair establishes this defect. Those findings leave the pool: a commit
    /// is evidence where a pool is consensus, and blending them publishes the blend under the stronger name.
    /// </param>
    /// <summary>One cluster's judged findings as they arrive on the wire.</summary>
    /// <remarks>
    /// ★★ A COUNT OF CLUSTERS CANNOT PRODUCE A CLUSTER-WEIGHTED AVERAGE. The publication carried
    /// <c>clusters: 14</c> and nothing else about them, which is enough for the clustering interval and
    /// structurally insufficient for 02 §5's second average — so the requirement was published in
    /// <c>reportingRule</c> and unimplementable at the same time.
    /// </remarks>
    public sealed record ClusterTallyEntry(
        string? ClusterId, int? Judged, int? Noise, string? ClaimClass = null);

    public sealed record PooledFindingEntry(
        string? Tool, string? RepoId, string? FilePath, int? Line, bool? Valid,
        bool? HasFixPairOracle = null);

    /// <summary>
    /// The pooled reference to build.
    /// </summary>
    /// <param name="SilentTools">
    /// ★ Tools that submitted a run and reported nothing. Named explicitly, or they vanish from the table
    /// and a tool that found nothing looks identical to one that never entered.
    /// </param>
    public sealed record PooledRecallRequest(
        int? LineWindow, IReadOnlyList<string>? SilentTools, IReadOnlyList<PooledFindingEntry>? Findings);

    /// <summary>What a rater declares about themselves, as it arrives on the wire.</summary>
    public sealed record RaterDeclarationRequest(
        string? Period, string? RaterId, string? PrimaryLanguage, string? Affiliation);

    /// <summary>One finding's fate in the repository, as the history reports it.</summary>
    public sealed record FixObservationEntry(
        string? FindingId, string? RepoId, string? Outcome, string? CrowdVerdict);

    /// <summary>A fix-rate calculation over a declared window.</summary>
    public sealed record FixRateRequest(int? WindowDays, IReadOnlyList<FixObservationEntry>? Observations);

    /// <summary>A finding the cascade has finished with, as it arrives on the wire.</summary>
    public sealed record CrowdCandidateRequest(string? FindingId, string? State, string? OwnerId);

    /// <summary>A period's crowd queue, as a participant registers it.</summary>
    public sealed record CrowdQueueRequest(
        string? Period, string? Seed, int SpotCheck, IReadOnlyList<CrowdCandidateRequest>? Candidates);

    /// <summary>A declared intent to submit for a period.</summary>
    public sealed record IntentRequest(string? Period, string? Tool);

    /// <summary>A verdict being contested.</summary>
    public sealed record DisputeRequest(string? Period, string? RaisedBy, string? Reason);

    /// <summary>How a dispute was answered.</summary>
    public sealed record DisputeResolutionRequest(string? Outcome, string? Reasoning);

    /// <summary>One verdict from the independent second pass.</summary>
    public sealed record RejudgeVote(
        string? FindingId, string? Verdict,
        string? Model = null, string? ModelVersion = null,
        string? PromptId = null, string? Prompt = null, string? Reasoning = null);

    /// <summary>A second pass over a period's re-judge sample.</summary>
    public sealed record RejudgeRequest(IReadOnlyList<RejudgeVote>? Verdicts);

    /// <summary>A honeypot as it arrives on the wire.</summary>
    public sealed record HoneypotEntry(string? FindingId, string? Truth, string? Source, string? Evidence);

    /// <summary>Honeypots to plant into a period's queue.</summary>
    public sealed record HoneypotRequest(string? Period, IReadOnlyList<HoneypotEntry>? Honeypots);

    /// <summary>One person's answer to one finding.</summary>
    /// <param name="WouldFix">
    /// ★★ Nullable, and a missing one is NOT ASKED rather than "no" — folding it into "no" would manufacture
    /// evidence that practitioners would not act on findings nobody asked them about.
    /// </param>
    public sealed record CrowdAnswerRequest(
        string? Period, string? RaterId, string? FindingId, string? Verdict, string? MachineVerdict,
        bool? WouldFix = null, bool? WantInReport = null);

    /// <summary>One judge's vote as it arrives on the wire.</summary>
    /// <param name="Judge">The judge slot, e.g. <c>judge-a</c>.</param>
    /// <param name="Verdict">One of the six published verdicts.</param>
    /// <param name="Model">Which model answered. Required to RECORD a verdict.</param>
    /// <param name="ModelVersion">Its pinned version — without it the run cannot be re-derived.</param>
    /// <param name="PromptId">The prompt used; its full text is published beside the record.</param>
    /// <param name="Prompt">The prompt's text, registered once under its id on first use.</param>
    /// <param name="Reasoning">
    /// ★★ WHY. A verdict a reader cannot argue with is not open judging — 01 promises "a reader who disagrees
    /// with a verdict must be able to find it, read the reasoning, and say so".
    /// </param>
    public sealed record CascadeVote(
        string? Judge, string? Verdict,
        string? Model = null, string? ModelVersion = null,
        string? PromptId = null, string? Prompt = null, string? Reasoning = null,

        // ★★ The panel's shape (#10). The FAMILY is the training tradition, not the product name — four models
        // from one vendor is one family — and the TEMPERATURE must be 0 or the verdict cannot be re-run to the
        // same answer. Both nullable on the wire so a missing one is REFUSED rather than defaulted: an undeclared
        // family that counted as "some other family", or an undeclared temperature that counted as 0, would let
        // every panel pass by omitting the field.
        string? ModelFamily = null, double? Temperature = null,

        // ★★ WHAT THE JUDGEMENT COST (#25). #23-3 asserts no cost figure because none has been measured and says
        // to measure it during the first full period — which only happens if the counter exists before the period
        // opens. Nullable, and null is NOT zero: a judge that did not report its time is counted as a judgement
        // with no time, because a mean over the half that reported is not the mean anybody will quote.
        double? ModelSeconds = null, int? InputTokens = null, int? OutputTokens = null);

    /// <summary>
    /// The votes to resolve. Round two is absent until round one has actually split — sending both at
    /// once would mean the second pair had been convened before there was anything to convene them for.
    /// </summary>
    /// <param name="Period">
    /// ★★ Supplying this AND <paramref name="FindingId"/> makes the call a RECORDED judgement rather than a
    /// calculation. Without them the endpoint stays a pure resolver, which is what the cascade's own unit
    /// tests use — but a real judging run must record, or the standard's open-judging promise has nothing
    /// behind it.
    /// </param>
    /// <param name="FindingId">Which finding was judged — the join a reader follows to argue with a verdict.</param>
    public sealed record CascadeRequest(
        IReadOnlyList<CascadeVote>? Round1, IReadOnlyList<CascadeVote>? Round2,
        string? Period = null, string? FindingId = null);
}
