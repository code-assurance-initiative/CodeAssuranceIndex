namespace Cai.Web.Noise;

/// <summary>
/// The published rules <c>/api/noise/method</c> is assembled from — one block per rule, in the order the method
/// lists them.
/// </summary>
internal static class NoiseMethodRules
{
    internal static object VersionHistory() => MethodVersions.History.Select(v => new
    {
        version = v.Version,
        announcedAt = v.AnnouncedAt,
        effectiveFromPeriod = v.EffectiveFromPeriod,
        rationale = v.Rationale,
    });

    internal static readonly string[] RequiredWithEveryRate =
    [
        "validPer100kLoc",
        "noisePer100kLoc",
        "recallEstimate",
        "recallMethod",
        "exclusionCount",
        "exclusionRate",
        "actionabilityRate",
        "claimClassBreakdown",
        "toolVersion",
        "holdoutSeed",
        "minimumDetectableDifference",
    ];

    internal static object PooledRecallRule() => new
    {
        reference = "leave-one-out: the union of what every OTHER participating tool reported and a "
                  + "human adjudicated as valid. A tool is never scored against its own findings.",
        minimumTools = PooledRecall.MinimumTools,
        belowMinimum = "refused, not computed. At two tools the leave-one-out reference is one other "
                     + "tool's findings, so the figure is pairwise agreement and a tool scores well by "
                     + "being SIMILAR rather than deep.",
        pseudoOracle = true,
        scope = PooledRecall.PooledScope,
        lineWindow = PooledRecall.DefaultLineWindow,
        caveat = PooledRecall.PooledCaveat,

        // ★★ WHAT THE UNION CANNOT MATCH, in the published method rather than only in a response (#21).
        // A participant needs to know before it runs that its repo-level findings will not enter the
        // union — and that the figure it gets back therefore covers only part of what it reported.
        coordinateGap = PooledRecall.CoordinateGap,
        endpoint = "/api/noise/pooled",
    };

    internal static object RejudgeRule() => new
    {
        sampleSize = Rejudge.DefaultSampleSize,
        tolerance = Rejudge.Tolerance,
        toleranceRationale = Rejudge.ToleranceRationale,
        fold = Rejudge.Fold,
        sampleRule =
            "The sample is drawn from the period's published holdout seed, so a third party can "
          + "re-derive it from values published before any result existed. A second pass may only "
          + "answer findings in that sample, and every sampled finding it leaves unanswered blocks the "
          + "tolerance — otherwise re-judging until the sample agrees is a rate over the agreeing part.",
        endpoint = "/api/noise/rejudge/{period}",
    };

    internal static object[] VerificationChecks() =>
    [
        new
        {
            check = "holdout-membership",
            asks = "is every finding on a repository this period's draw published?",
        },
        new
        {
            check = "pinned-sha",
            asks = "does each finding cite the revision the holdout pinned? A different revision is "
                 + "different code.",
        },
        new
        {
            check = "run-ordering",
            asks = "did the run START after the draw was published? A result produced before its own "
                 + "holdout answers something else.",
        },
        new
        {
            check = "claim-class",
            asks = "does each finding declare one of the published claim classes?",
        },
        new
        {
            check = "recency-declaration",
            asks = "for each drawn repository, has the tool been developed against it?",
        },
        new
        {
            check = "configuration-declaration",
            asks = "which ruleset ran, and is it what customers get?",
        },
        new
        {
            // ★★ The one added by #7a, and the reason the list exists at all.
            check = "finding-count",
            asks = "does the number of findings submitted equal the number the run reports it "
                 + "produced? Dropping findings between the run and the submission is the simplest "
                 + "route to a flattering rate, and coverage cannot show it — a repository with one "
                 + "surviving finding is covered.",
        },
        new
        {
            // ★★ Added by #7b, and the only check here that points at the standard rather than at a
            // vendor: a rate produced by a process that disagrees with itself is not a measurement.
            check = "rejudge",
            asks = "does an independent second pass over a seed-drawn sample of the period's judged "
                 + "findings reach the same noise/not-noise answers, within the published tolerance?",
        },
        new
        {
            check = "coverage",
            asks = "which drawn repositories does the run not reach? Reported rather than refused, "
                 + "and a partial run is marked partial.",
        },
    ];

    internal static object ClaimClassRules() => Enum.GetValues<ClaimClass>().Select(c => new
    {
        claimClass = ClaimSpecificity.Wire(c),
        describes = ClaimSpecificity.Describes(c),
        admitsANoiseRate = c != ClaimClass.Statistical,
    });

    internal static object ComplianceMarkRule() => new
    {
        label = ComplianceMark.Label,
        free = ComplianceMark.Free,
        changeRule = ComplianceMark.ChangeRule,
        wordingRule = ComplianceMark.WordingRule,
        appealRoute = ComplianceMark.AppealRoute,
        conditions = ComplianceMark.Conditions.Select(c => new { c.Name, c.Reads }),
        published = "/api/noise/mark/{period}",
        isNotAQualityBadge = "Three of the four conditions are about PROCESS. A tool with a poor noise "
                           + "rate that ran the published draw, submitted in time and published in full "
                           + "earns the mark — it states that the measurement happened properly, and the "
                           + "rate states how it went.",
    };

    internal static object IntentRegisterRule() => new
    {
        endpoint = "/api/noise/intent",
        published = "/api/noise/intent/{period}",
        closesWhen = "the period's holdout is drawn. Intent registered after the draw is a decision made "
                   + "with the sample in hand, so it must be declared BEFORE — that ordering is the whole "
                   + "value of the register.",
        why = "The no-withdrawal rule cannot see a run that was never submitted: a vendor can simply skip "
            + "the periods that went badly, and the published set quietly becomes 'the results people "
            + "were happy with'. The register names who said they would take part, so not submitting is "
            + "visible.",
        idempotent = "registering twice keeps the FIRST timestamp — the moment it was made is the claim.",
    };

    internal static object BehaviouralQuestionsRule() => new
    {
        wouldFix = BehaviouralQuestions.WouldFix,
        wantInReport = BehaviouralQuestions.WantInReport,
        why = BehaviouralQuestions.Why,
        relationToTheRate = BehaviouralQuestions.RelationToTheRate,
        unansweredIsNotNo = "a missing behavioural answer is counted as NOT ASKED. Folding it into 'no' "
                          + "would manufacture evidence that practitioners would not act on findings "
                          + "nobody asked them about.",
    };

    internal static object JudgePanelRule() => new
    {
        distinctModels = JudgePanel.FullPanelDistinctModels,
        rule = "no model may appear twice in a panel, and the panel must span at least two families. A "
             + "round-one settle is therefore two distinct models from two traditions; a round-two panel "
             + "is four. Requiring four to RECORD would contradict the cascade — round two convenes only "
             + "when round one has split, so most findings could never be recorded at all.",
        familiesRequired = JudgePanel.RequiredFamilies,
        temperature = JudgePanel.RequiredTemperature,
        declaredPerVerdict = new[] { "model", "modelVersion", "modelFamily", "temperature" },
        why = JudgePanel.Why,
        undeclaredIsNotAPass = "an undeclared family does not count as a different family, and an "
                             + "undeclared temperature does not count as 0. Either default would let a "
                             + "panel pass by omitting the field.",
        appliesTo = "RECORDING a judgement. /api/noise/cascade/resolve still resolves votes as a "
                  + "calculation; what it refuses is to record a judgement from a panel that breaks the "
                  + "method.",
    };

    internal static object ContestationRule() => new
    {
        endpoint = "/api/noise/verdicts/{findingId}/dispute",
        resolveEndpoint = "/api/noise/disputes/{disputeId}/resolve",
        requires = "the period, and a REASON. An unexplained objection cannot be argued with in either "
                 + "direction, and the reason publishes with the dispute.",
        publishes = "either way. Upheld and overturned appear identically in the period's record, with "
                  + "the resolution's reasoning — a dispute that only appeared when the vendor won "
                  + "would be a complaints box.",
        rawVerdictIsKept = "always. The verdict register is append-only and nothing here deletes from "
                         + "it: a contestation mechanism that removed what it overturned would be a "
                         + "withdrawal mechanism, and the register would become 'the verdicts nobody "
                         + "objected to'.",
        effectOnAPublishedRate = "an overturned verdict does not silently change a published number. "
                               + "The rate is corrected by publishing the period again, which the "
                               + "append-only publication record shows as a correction.",
    };

    internal static object TwelveMonthRule() => new
    {
        windowMonths = RollingFigure.WindowMonths,
        pooledFrom = "the published results for the window ending at the period, from the append-only "
                   + "publication record, plus the period being published. A corrected period counts "
                   + "ONCE, at its latest value.",
        why = "A single period's interval is wide enough to hide most movements, and the minimum "
            + "detectable difference computed over repositories is wider still — so month-to-month "
            + "comparison is mostly noise about noise. The rolling figure is the only rate here whose "
            + "interval can support a claim about a trend.",
        shortWindowRule = "A window covering fewer than the full twelve periods is published as such: "
                        + "it is a real pooled rate and it is not yet a twelve-month figure. The two "
                        + "look identical, so `spansTheFullWindow` is what separates them.",
        interval = "wilson-95",
    };

    internal static object ClusterAveragesRule() => new
    {
        requires = "clusterTallies: one entry per repository with its judged and noise counts, "
                 + "optionally per claim class. A count of clusters is enough for the clustering "
                 + "interval and cannot produce a cluster-weighted average.",
        why = "The pooled rate is a count over a count, so a repository contributing half the findings "
            + "contributes half the rate and can dominate the number unseen. The remedy is NOT to drop "
            + "the outlier — excluding a repository for having an extreme rate is selecting on the "
            + "outcome — it is a second average that weights repositories equally, published beside "
            + "the first.",
        notableDivergence = ClusterAverages.NotableDivergence,
        divergenceMeans = "a run to READ twice, never a run to void: neither average is the wrong "
                        + "answer, and which one to quote depends on the question asked.",
        emptyClusters = "a repository the run reached and judged nothing in has NO rate and is "
                      + "excluded from the macro. Counting it as 0 % would improve the number for "
                      + "going unjudged.",
        talliesMustMatchTheCensus = true,
    };
}
