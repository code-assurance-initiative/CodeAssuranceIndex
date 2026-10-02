namespace Cai.Web.Noise;

/// <summary>One judge's raw verdict on one finding, as it will be published.</summary>
/// <param name="Period">The measurement period.</param>
/// <param name="FindingId">Which finding — the join a reader follows to argue with a verdict.</param>
/// <param name="Round">1 for the first pair, 2 for the blind pair.</param>
/// <param name="Judge">The judge slot, e.g. <c>judge-a</c>.</param>
/// <param name="Model">The model that answered.</param>
/// <param name="ModelVersion">Its pinned version. ★ Without it the run cannot be re-derived at all.</param>
/// <param name="PromptId">The prompt used, whose full text is published beside the record.</param>
/// <param name="Verdict">The verdict, in the published vocabulary.</param>
/// <param name="Reasoning">Why. ★★ A verdict a reader cannot argue with is not open judging.</param>
/// <param name="ModelFamily">
/// ★★ The training tradition, not the product name. 02 §2: "a blind spot lives in the weights; no rephrasing
/// removes it — a single-family ensemble cannot see a single-family blind spot." Four different models from one
/// vendor is still one family, and the check that they agreed says nothing about all four being wrong the same way.
/// </param>
/// <param name="Temperature">
/// ★★ Must be 0. 01 §3 promises "anyone may re-run the judges and get the same answers"; a verdict produced at 0.7
/// cannot be re-run to the same answer, so the promise is false for it — and without this field a reader could not
/// tell which verdicts it was false for.
/// </param>
internal sealed record VerdictRecord(
    string Period, string FindingId, int Round,
    string Judge, string Model, string ModelVersion, string PromptId,
    string Verdict, string Reasoning, DateTimeOffset RecordedAt,
    string ModelFamily = "", double Temperature = 0);

/// <summary>How one finding's cascade settled.</summary>
internal sealed record ResolutionRecord(
    string Period, string FindingId, string State, string? Verdict, int? SettledAtRound,
    bool ActionabilityContested, bool? Actionable, string Reason, DateTimeOffset RecordedAt);

/// <summary>One verdict from the independent second pass over a period's re-judge sample.</summary>
/// <remarks>
/// ★ Carries the same provenance a first-pass verdict does. A reproducibility claim whose second pass cannot be
/// read is worth what the first pass's would be without its reasoning: nothing a reader can argue with.
/// </remarks>
internal sealed record RejudgeRecord(
    string Period, string FindingId, string Verdict,
    string Model, string ModelVersion, string PromptId, string Reasoning, DateTimeOffset RecordedAt);

/// <summary>A contested verdict, and how the contest was answered.</summary>
/// <param name="Reason">
/// ★★ Required. "I disagree" is not contestation — 01 §5 is about arguing against published reasoning, and the
/// reason is the half that makes the dispute answerable rather than a vote.
/// </param>
/// <param name="Outcome">
/// <c>upheld</c> or <c>overturned</c>, or null while it is open. ★ An OPEN dispute is the state a reader most
/// needs: it is the one where the standard has been challenged and has not answered.
/// </param>
/// <param name="ResolutionReasoning">
/// Why it was answered that way. ★ Required in BOTH directions: an outcome without reasoning is "the standard
/// says so", the exact argument 01 §5 says CAI does not get to make.
/// </param>
internal sealed record DisputeRecord(
    string DisputeId, string Period, string FindingId, string RaisedBy, string Reason, DateTimeOffset RaisedAt,
    string? Outcome, string? ResolutionReasoning, DateTimeOffset? ResolvedAt);

/// <summary>A vendor's declared intent to submit for a period, registered before that period was drawn.</summary>
/// <remarks>
/// ★★ THE "BEFORE" THE NO-WITHDRAWAL RULE WORKS FROM. Without it the rule is half a rule: a vendor can simply
/// never submit the periods that went badly, and the published set quietly becomes "the results people were happy
/// with". The register's value is its TIMESTAMP, so a second registration keeps the first.
/// </remarks>
internal sealed record IntentRecord(string Period, string Tool, DateTimeOffset RegisteredAt);

/// <summary>A judge prompt, stored once and published in full.</summary>
internal sealed record PromptRecord(string PromptId, string Text, DateTimeOffset FirstSeenAt);

/// <summary>
/// Durable storage for the two things the standard promises to keep: who submitted, and how it was judged.
/// </summary>
/// <remarks>
/// ★ SPLIT BY ROLE, not by convenience. One store implements all of it — it is one database file — but a caller
/// that only reads publications should not be able to record a verdict, and a reader of a handler should not have
/// to check which of twenty-eight members it reaches for. The role interfaces below are the dependency an
/// endpoint or a page declares; this composite exists for the handful of readers that genuinely span the record.
/// </remarks>
internal interface INoiseStore
    : INoiseSubmissionStore, INoiseJudgingStore, INoisePublicationStore,
      INoiseDisputeStore, INoiseFindingStore, INoiseCostStore;

/// <summary>The submission register: who declared they would run, who did run, and what they declared they ran with.</summary>
internal interface INoiseSubmissionStore
{
    /// <summary>Record a receipt. Returns false when an accepted submission already claims (tool, period).</summary>
    /// <param name="configurationJson">
    /// The configuration declaration as submitted. ★ Stored VERBATIM rather than re-serialised from a parsed
    /// shape: it is a claim the vendor made, and the record's job is to publish what they said.
    /// </param>
    bool TryRecordSubmission(
        SubmissionReceipt receipt, DateTimeOffset? runStartedAt, string? configurationJson);

    /// <summary>A receipt by id, or null.</summary>
    SubmissionReceipt? FindSubmission(string submissionId);

    /// <summary>Whether an ACCEPTED submission already exists for this tool and period.</summary>
    bool AlreadySubmitted(string tool, string period);

    /// <summary>Every submission for a period, newest first — the register.</summary>
    IReadOnlyList<SubmissionReceipt> ListSubmissions(string period);

    /// <summary>The configuration a submission declared, as raw JSON, or null when it declared none.</summary>
    string? ConfigurationJson(string submissionId);

    /// <summary>
    /// Register intent, or keep the existing registration. Returns the record that now stands.
    /// </summary>
    /// <remarks>
    /// ★★ IDEMPOTENT, AND THE FIRST TIME STANDS. The registration's whole value is the moment it was made — "before
    /// the draw" is the claim it makes — so a re-post that moved the time forward would let a vendor register
    /// early, watch, and quietly refresh the record to a moment that suited them.
    /// </remarks>
    IntentRecord RegisterIntent(string period, string tool, DateTimeOffset now);

    /// <summary>Every intent registered for a period, oldest first.</summary>
    IReadOnlyList<IntentRecord> ListIntent(string period);
}

/// <summary>The verdict record: raw verdicts, how each finding settled, the prompts behind them, and the independent second pass.</summary>
internal interface INoiseJudgingStore
{
    /// <summary>Record one judge's raw verdict.</summary>
    void RecordVerdict(VerdictRecord verdict);

    /// <summary>Record how a finding settled, replacing any earlier resolution of the same finding.</summary>
    void RecordResolution(ResolutionRecord resolution);

    /// <summary>Register a prompt's full text under its id, if not already known.</summary>
    void RegisterPrompt(string promptId, string text, DateTimeOffset now);

    /// <summary>Every raw verdict for a period, in the order recorded.</summary>
    IReadOnlyList<VerdictRecord> ListVerdicts(string period);

    /// <summary>Every resolution for a period.</summary>
    IReadOnlyList<ResolutionRecord> ListResolutions(string period);

    /// <summary>The prompts referenced by a period's verdicts, in full.</summary>
    IReadOnlyList<PromptRecord> ListPrompts(string period);

    /// <summary>Record the independent second pass over a period's re-judge sample.</summary>
    /// <remarks>
    /// ★ Replaces any earlier pass for the same (period, findingId): a second pass IS the re-judge, and keeping
    /// several would let whoever ran them choose which counted — the steerable sample again, one level up.
    /// </remarks>
    void RecordRejudge(IReadOnlyList<RejudgeRecord> verdicts);

    /// <summary>Every re-judge verdict recorded for a period.</summary>
    IReadOnlyList<RejudgeRecord> ListRejudge(string period);

    /// <summary>Periods that have any judging recorded, newest first — what a record page can be asked for.</summary>
    /// <remarks>
    /// ★ Distinct from <see cref="INoisePublicationStore.PublishedPeriods"/>: a period can have judging without a published rate (the
    /// judging happens first), and a reader looking for the record wants the former.
    /// </remarks>
    IReadOnlyList<string> JudgedPeriods();
}

/// <summary>The publication record: what was published for a period, when, and every correction since.</summary>
internal interface INoisePublicationStore
{
    /// <summary>
    /// Store an accepted publication for a period. APPEND-ONLY: a correction is a second row.
    /// </summary>
    /// <param name="payloadJson">The published body verbatim — what was published, not a re-derivation.</param>
    void RecordPublication(string period, string payloadJson, DateTimeOffset publishedAt);

    /// <summary>The latest published payload for a period plus its history, or null when none exists.</summary>
    (string PayloadJson, DateTimeOffset PublishedAt, IReadOnlyList<DateTimeOffset> History)? LatestPublication(
        string period);

    /// <summary>Periods that have a published result, newest first — what a reader can ask for.</summary>
    IReadOnlyList<string> PublishedPeriods();

    /// <summary>
    /// Every published period's judged and noise counts, oldest first, for the rolling figure.
    /// </summary>
    /// <remarks>
    /// ★★ READ FROM THE STORED PAYLOADS, so the rolling figure is pooled from what was actually PUBLISHED rather
    /// than from a running total kept beside it. A counter would drift from the publications the moment a
    /// correction landed, and this is the one figure whose whole value is that it aggregates the record.
    /// <para>★ A corrected period appears twice, later row last — <see cref="RollingFigure"/> takes the latest.</para>
    /// </remarks>
    IReadOnlyList<PeriodTally> PublishedTallies();
}

/// <summary>The dispute register: verdicts that were contested, and how the contest was answered.</summary>
internal interface INoiseDisputeStore
{
    /// <summary>Record a raised dispute.</summary>
    void RaiseDispute(DisputeRecord dispute);

    /// <summary>One dispute by id, or null.</summary>
    DisputeRecord? FindDispute(string disputeId);

    /// <summary>
    /// Answer an open dispute. Returns false when it is already answered.
    /// </summary>
    /// <remarks>
    /// ★★ ONCE. Otherwise the outcome is whatever was written last, and "publishes either way" becomes
    /// "publishes whichever way we ended up preferring". Enforced in SQL by the <c>outcome IS NULL</c> predicate
    /// rather than by a read-then-write above it.
    /// </remarks>
    bool ResolveDispute(string disputeId, string outcome, string reasoning, DateTimeOffset resolvedAt);

    /// <summary>Every dispute for a period, oldest first.</summary>
    IReadOnlyList<DisputeRecord> ListDisputes(string period);
}

/// <summary>The findings a submission reported, stored so a rater — or a reader — has something to look at.</summary>
internal interface INoiseFindingStore
{
    /// <summary>
    /// Store what a submission reported, so a rater has something to look at.
    /// </summary>
    /// <remarks>
    /// ★★ THE CROWD CANNOT JUDGE WHAT IT CANNOT SEE. Nothing stored the findings: the receipt kept counts and a
    /// rater was handed an id. Keyed by the DERIVED id, so a second tool reporting the same defect writes the same
    /// row rather than a duplicate — which is what makes cross-vendor matching possible at all.
    /// </remarks>
    void RecordFindings(IReadOnlyList<FindingRecord> findings);

    /// <summary>One finding by its derived id, or null.</summary>
    FindingRecord? FindFinding(string findingId);
}

/// <summary>The cost ledger: what judging and crowd rating cost, attributed to the participant it was spent on.</summary>
internal interface INoiseCostStore
{
    /// <summary>
    /// Record what one judgement or one rated item cost.
    /// </summary>
    /// <remarks>
    /// ★★ #23-3 ASSERTS NO COST FIGURE BECAUSE NONE HAS BEEN MEASURED, and says to measure it during the first
    /// full period — which only happens if the counters are in place before the period opens. Afterwards it is
    /// an estimate reconstructed from memory, which is the kind of number this standard exists not to publish.
    /// </remarks>
    void RecordCost(CostEntry entry);

    /// <summary>Per-participant tallies for a period, with the unattributed row last.</summary>
    IReadOnlyList<CostTally> CostFor(string period);
}

/// <summary>What one judgement or one rated item cost, attributed to the participant it was spent on.</summary>
/// <param name="Tool">
/// The participant whose finding it was spent on, or null when the finding is unknown. ★★ LOOKED UP from the
/// stored finding, never taken from the caller: a cost the caller attributes is one it can attribute elsewhere.
/// </param>
/// <param name="Kind">"judging" or "crowd".</param>
/// <param name="ModelSeconds">Model time, or null when the judge did not report it — which is not zero.</param>
internal sealed record CostEntry(
    string Period, string? Tool, string Kind, string FindingId,
    double? ModelSeconds, int? InputTokens, int? OutputTokens, DateTimeOffset At);

/// <summary>
/// One participant's tally for a period. ★ Tool is null on the unattributed row.
/// </summary>
/// <param name="JudgementsSolelyYours">
/// ★★ THE MARGINAL FIGURE, and the one #23-3 actually asks for: judgements on findings NO OTHER participant
/// reported. The rest were spent on findings that would have been judged anyway, so counting them as this
/// participant's marginal cost would answer a different question — and per-participant totals therefore overlap
/// by design and do not sum to the period's spend.
/// </param>
internal sealed record CostTally(
    string? Tool, int Judgements, int JudgementsWithNoTimeReported,
    double ModelSeconds, int InputTokens, int OutputTokens, int CrowdItemsRated,
    int JudgementsSolelyYours = 0, double ModelSecondsSolelyYours = 0);
