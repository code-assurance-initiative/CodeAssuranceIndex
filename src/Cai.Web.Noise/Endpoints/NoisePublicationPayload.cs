using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>Everything a publication's payload is built from, once the request has passed the contract.</summary>
internal sealed record PublicationInputs(
    PublicationRequest Request,
    List<ClaimClassTally> Claims,
    List<RecencyTally> Recency,
    List<ClusterTally> ClusterTallies,
    ClusterAverageSummary ClusterAverages,
    RollingSummary Rolling,
    RejudgeOutcome? RejudgeOutcome,
    FixRateSummary? Anchor);

/// <summary>
/// The published result for a period, as <c>/api/noise/publication</c> returns and stores it.
/// </summary>
internal static class NoisePublicationPayload
{
    internal static object Build(PublicationInputs x)
    {
        var request = x.Request;
        var summary = PublicationSurface.Summarise(
            request.Reported, request.Adjudicated, request.Excluded, request.Unrated,
            request.ValidAndActionable, request.ValidNotActionable, request.Noise,
            request.Clusters);

        var judged = request.ValidAndActionable + request.ValidNotActionable + request.Noise;
        var rate = judged > 0 ? (double?)request.Noise / judged : null;

        // ★★ THE INTERVAL, COMPUTED HERE AND CARRIED WITH THE RATE. #23-4: the number never appears
        // without its interval and its period — "if the surface cannot carry the qualifiers, it does not
        // carry the number". Nothing computed one before, so that constraint was unsatisfiable.
        var interval = PublicationSurface.WilsonIntervalOrNull(request.Noise, judged);

        return new
        {
            period = request.Period,

            // ★★ THE VERSION IN FORCE FOR THIS PERIOD, not the newest. A period judged under 1.0 publishes
            // as judged under 1.0 for ever, which is exactly what stops a later change reaching back and
            // reinterpreting a number somebody disliked.
            methodVersion = MethodVersions.InForceFor(request.Period)?.Version ?? MethodVersion,
            methodVersionRationale = MethodVersions.InForceFor(request.Period)?.Rationale,

            census = Census(summary.Census),

            // ★ Published as counts as well as a rate. The absolutes are what expose suppression; the
            // ratio alone hides it.
            validAndActionable = summary.ValidAndActionable,
            validNotActionable = summary.ValidNotActionable,
            noise = summary.Noise,
            noiseRate = rate,

            // ★★ NEVER the bare rate. A noise rate lives near the ends of the scale, which is where the
            // normal approximation fails outright — 0 of 200 becomes "0 % to 0 %", certainty from a sample
            // that proves nothing of the kind. Wilson stays inside [0,1] and stays honest there.
            noiseRateInterval = Interval(interval),

            // ★★ Over VALID findings only. Divided by everything reported it would mix precision in,
            // and a tool could improve its actionability by producing more noise.
            actionabilityRate = summary.ActionabilityRate,

            // ★★ THE ABSOLUTES, per 100k LoC. The ratio above hides suppression; these expose it. A tool
            // that stops reporting improves its rate and cannot improve these.
            locCovered = request.LocCovered,
            validPer100kLoc = Per100k(request.ValidAndActionable + request.ValidNotActionable, request.LocCovered),
            noisePer100kLoc = Per100k(request.Noise, request.LocCovered),

            // ★★ Per class, never only pooled. Two tools' pooled rates are not comparable unless their
            // output is comparably falsifiable, and the statistical class gets NO rate — "not measurable
            // under this method" is an honest cell; a blank that reads as clean is not.
            claimClasses = ClaimClasses(x.Claims, x.ClusterTallies),
            measurableShare = ClaimSpecificity.MeasurableShare(x.Claims),
            pooledRateComparable = !ClaimSpecificity.NothingFalsifiable(x.Claims),

            // ★★ The ceiling, APPLIED. It was published here and compared against nothing.
            exclusionRate = PublicationContract.ExclusionRate(request.Adjudicated, request.Excluded),
            maxExclusionRate = PublicationContract.MaxExclusionRate,

            // ★★ THE OVERFITTING NUMBER — the most interesting figure the standard produces, and one no
            // vendor would publish about itself unprompted. Every other number here can be improved by
            // building a better tool; this one can only be improved by building one that generalises.
            recency = RecencyBlock(x.Recency),

            // ★★ WHETHER THE JUDGING BEHIND THIS NUMBER REPRODUCES, published with the number. It is the
            // only figure here that says anything about the instrument rather than about the tool, and a
            // reader weighing a two-point move needs it more than they need any of the rest.
            rejudge = RejudgeBlock(x.RejudgeOutcome, request.RejudgeUnavailable),

            // ★★ THE ROLLING FIGURE, with its interval and its SPAN. A single period's interval is wide
            // enough to hide most movements, so this is the only rate here that can support a claim about a
            // trend — and a three-period pool quoted as the annual number is the natural failure, because
            // the two look identical and only `spansTheFullWindow` separates them.
            twelveMonth = TwelveMonth(x.Rolling),

            // ★★ THE SPOT-CHECK, and the contested tail BESIDE it rather than merged. The contested items
            // are hard by construction; the spot-check sample is the pipeline's own claim about itself, and
            // it is the only evidence that the judges agreeing made them right. A combined figure would hide
            // the disagreement rate on auto-accepted findings — and it is the one that would get quoted.
            spotCheck = CrowdSlice(request.Period, CrowdReason.SpotCheck),
            contestedTail = CrowdSlice(request.Period, CrowdReason.Contested),

            // ★ The recall counterpart, beside the precision figure rather than in a side endpoint.
            recall = new
            {
                estimate = request.RecallEstimate,
                method = request.RecallMethod,
                note = request.RecallNote,
            },

            // ★ 04 fix #1: the gap backlog IS a recall signal, and it costs a query. Published as a
            // standing figure so a falling noise rate beside a rising gap count is visible as what it is.
            gapsFoundSinceLastPeriod = request.GapsFoundSinceLastPeriod,

            // ★★ The configuration travels WITH the number. #23-1: deviations publish alongside it, and
            // the publication is the number a reader quotes.
            configuration = ConfigurationBlock(request.Configuration),

            provenance = new
            {
                toolVersion = request.ToolVersion,
                holdoutSeed = request.HoldoutSeed,
                modelSet = request.ModelSet,
                gitMiningVerified = request.GitMiningVerified,
            },

            clusters = summary.Clusters,

            // ★★ THE SAME RATE, TWICE. 02 §5 requires both so no repository can dominate the number
            // unseen — and the defence cannot be to drop the outlier, because excluding a repository for
            // having an extreme rate is selecting on the outcome.
            clusterAverages = ClusterAveragesBlock(x.ClusterAverages),

            intraClusterCorrelation = PublicationSurface.DefaultIntraClusterCorrelation,
            minimumDetectableDifference = summary.MinimumDetectableDifference,

            // ★★ And the threshold is APPLIED, not merely published: a move smaller than it is
            // neither an improvement nor a regression.
            distinguishableFromPrevious = request.PreviousRate is { } previous && rate is { } current
                && PublicationSurface.Distinguishable(current, previous, summary.MinimumDetectableDifference),

            // ★★ Beside the judged numbers, not in a side endpoint. It is the only figure here that
            // no amount of shared bias among raters can move.
            fixRate = FixRateBlock(x.Anchor, request.FixRateUnavailable),

            fixRateNote =
                "An anchor, not a complement — never one minus the noise rate. Valid findings go "
              + "unfixed for want of time, and worthless ones are 'fixed' by a refactor that touched "
              + "the line. Read side by side; a sharp disagreement means one of them is wrong.",

            mddNote =
                "Findings are not independent observations — they cluster by repository, so power is "
              + "computed from the repository count with a design effect, not from the finding count. "
              + "Treating 2,000 correlated findings as 2,000 observations is how a two-point move gets "
              + "published as progress when it is a statement about which repositories were drawn.",
        };
    }

    private static object Census(CensusCheck census) => new
    {
        reported = census.Reported,
        adjudicated = census.Adjudicated,
        excluded = census.Excluded,
        unrated = census.Unrated,
        balances = census.Balances,
    };

    private static object Interval((double Low, double High)? interval) =>
        interval is { } ci
            ? new { low = (double?)ci.Low, high = (double?)ci.High, method = "wilson-95" }
            : new { low = (double?)null, high = (double?)null, method = "wilson-95" };

    private static object ClaimClasses(List<ClaimClassTally> claims, List<ClusterTally> clusterTallies) =>
        claims.Select(c => new
        {
            claimClass = ClaimSpecificity.Wire(c.Class),
            describes = ClaimSpecificity.Describes(c.Class),
            judged = c.Judged,
            noise = c.Noise,
            noiseRate = c.NoiseRate,

            // ★★ THE MACRO PER CLASS. A pooled rate across claim classes is a category error the
            // method already refuses; a pooled macro across them is the same error one level up, so
            // the pointwise average must be readable without the structural findings moving it.
            macroRate = ClusterAverages
                .ComputeFor(clusterTallies, ClaimSpecificity.Wire(c.Class)).MacroRate,

            measurable = c.Measurable,
            notMeasurableReason = c.Measurable
                ? null
                : "a statistical claim has no false-positive state, so a rate over it would measure "
                + "the raters' opinions rather than the tool.",
        });

    private static object RecencyBlock(List<RecencyTally> recency) => new
    {
        declared = recency.Count > 0,
        hasPristineSlice = Noise.RecencyStrata.HasPristineSlice(recency),
        strata = recency.Select(t => new
        {
            stratum = Noise.RecencyStrata.Wire(t.Stratum),
            means = Noise.RecencyStrata.Means(t.Stratum),
            judged = t.Judged,
            noise = t.Noise,
            noiseRate = t.NoiseRate,
        }),
        overfittingGapPoints = Noise.RecencyStrata.OverfittingGap(recency),
        gapIsNotable = Noise.RecencyStrata.GapIsNotable(Noise.RecencyStrata.OverfittingGap(recency)),
        // ★★ HOW BIG THE PRISTINE SLICE IS. The overfitting gap is computed against the
        // never-trained stratum, which IS this slice — so its size is what tells a reader whether the
        // gap rests on three repositories or thirty.
        reservedRepositories = CorpusManifest.Load().Candidates.Count(c => c.Reserved),
        reservedNote = "The reserved repositories are never used for development by any participant, "
                     + "and are in every draw. Declaring one as trained is refused — without a "
                     + "never-trained endpoint the decay curve measures nothing.",

        // ★ A missing gap and a gap of zero are OPPOSITE claims and look identical when one of
        // them is a blank, so the absence is stated rather than left as null.
        note = recency.Count == 0
            ? "no recency strata declared: this run says nothing about whether its rate describes "
            + "the instrument or the vendor's familiarity with the sample."
            : Noise.RecencyStrata.HasPristineSlice(recency)
                ? null
                : "no pristine slice in this holdout, so the overfitting gap cannot be computed. "
                + "Without a never-trained endpoint the decay curve measures nothing, and "
                + "'one cycle of cooling off is enough' stays an assertion.",
    };

    private static object RejudgeBlock(RejudgeOutcome? rejudgeOutcome, string? rejudgeUnavailable) =>
        rejudgeOutcome is { } ro
            ? new
            {
                declared = true,
                unavailableReason = (string?)null,
                sampleSize = (int?)ro.SampleSize,
                compared = (int?)ro.Compared,
                disagreements = (int?)ro.Disagreements,
                disagreementRate = ro.DisagreementRate,
                tolerance = (double?)Rejudge.Tolerance,
                withinTolerance = ro.WithinTolerance,
                fold = Rejudge.Fold,
            }
            : new
            {
                declared = false,
                unavailableReason = rejudgeUnavailable,
                sampleSize = (int?)null,
                compared = (int?)null,
                disagreements = (int?)null,
                disagreementRate = (double?)null,
                tolerance = (double?)Rejudge.Tolerance,
                withinTolerance = false,
                fold = Rejudge.Fold,
            };

    private static object TwelveMonth(RollingSummary rolling) => new
    {
        windowMonths = RollingFigure.WindowMonths,
        periods = rolling.Periods,
        spansTheFullWindow = rolling.SpansTheFullWindow,
        firstPeriod = rolling.FirstPeriod,
        lastPeriod = rolling.LastPeriod,
        judged = rolling.Judged,
        noise = rolling.Noise,
        rate = rolling.Rate,
        intervalLow = rolling.IntervalLow,
        intervalHigh = rolling.IntervalHigh,
        intervalMethod = "wilson-95",
        note = rolling.Note,
    };

    private static object? ConfigurationBlock(RunConfiguration? configuration) =>
        configuration is { } cfg ? new
        {
            rulesetId = cfg.RulesetId,
            isProductDefault = cfg.IsProductDefault,
            divergenceExplanation = cfg.DivergenceExplanation,
            rulesDisabled = cfg.RulesDisabled ?? [],
            thresholdsAltered = (cfg.ThresholdsAltered ?? []).Select(t => new
            {
                ruleId = t.RuleId, shipped = t.Shipped, used = t.Used,
            }),
        } : null;

    private static object ClusterAveragesBlock(ClusterAverageSummary clusterAverages) => new
    {
        micro = clusterAverages.MicroRate,
        macro = clusterAverages.MacroRate,
        leaveOneOutLow = clusterAverages.LeaveOneOutLow,
        leaveOneOutHigh = clusterAverages.LeaveOneOutHigh,

        // ★ Named: "the range is wide" without saying which repository did it is a fact nobody
        // can act on.
        mostInfluentialCluster = clusterAverages.MostInfluentialCluster,

        clustersWithARate = clusterAverages.ClustersWithARate,
        clustersWithNothingJudged = clusterAverages.ClustersWithNothingJudged,

        // ★★ Flagged rather than left as arithmetic: publishing two numbers and expecting the
        // reader to subtract them is how the second one gets ignored.
        averagesDiverge = clusterAverages.AveragesDiverge,
        notableDivergence = ClusterAverages.NotableDivergence,
        note = clusterAverages.Note,
    };

    private static object FixRateBlock(FixRateSummary? anchor, string? fixRateUnavailable) =>
        anchor is null
            ? new
            {
                declared = false,
                unavailableReason = fixRateUnavailable,
                windowDays = (int?)null,
                observed = (int?)null,
                fixedFindings = (int?)null,
                rate = (double?)null,
                excludedFileDeleted = (int?)null,
                unobservable = (int?)null,
                calledNoiseThenFixed = Array.Empty<string>(),
            }
            : new
            {
                declared = true,
                unavailableReason = (string?)null,
                windowDays = (int?)anchor.WindowDays,
                observed = (int?)anchor.Observed,
                fixedFindings = (int?)anchor.Fixed,
                rate = anchor.Rate,
                excludedFileDeleted = (int?)anchor.ExcludedFileDeleted,
                unobservable = (int?)anchor.Unobservable,

                // ★★ Promoted with it: a finding the crowd called noise that the maintainer then
                // fixed is evidence the crowd was wrong, from a source independent of every rater.
                // In a side endpoint it is a curiosity; here it is a check on the rate above it.
                calledNoiseThenFixed = anchor.CalledNoiseThenFixed.ToArray(),
            };

    /// <summary>A count per 100k LoC, or null without a denominator.</summary>
    private static double? Per100k(int count, long? loc) =>
        loc is > 0 ? count * 100_000d / loc.Value : null;

    /// <summary>
    /// One crowd slice for a period, as the publication carries it.
    /// </summary>
    /// <remarks>
    /// ★★ THE SAME ARITHMETIC AS <c>/api/noise/crowd/results/{period}</c>, and deliberately the same shape: a
    /// spot-check figure that disagreed with the crowd endpoint's would leave a reader unable to tell which was
    /// the standard's answer. Honeypots leave the count here too — their answer was known before it was asked.
    /// </remarks>
    private static object CrowdSlice(string? period, CrowdReason reason)
    {
        if (period is not { Length: > 0 } || CrowdQueues.Find(period) is not { } round)
        {
            // ★ An absence, never a blank. "No spot-check was run" and "the spot-check found no
            // contradictions" are opposite claims and look identical when one of them is a missing field.
            return new
            {
                run = false,
                queued = (int?)null,
                answered = (int?)null,
                contradicted = (int?)null,
                notComparable = (int?)null,
                note = "no crowd round is registered for this period, so this check was not run. That is an "
                     + "absence and not a clean result.",
            };
        }

        var byFinding = round.Queue.ToDictionary(
            i => i.FindingId, i => i.Reason, StringComparer.OrdinalIgnoreCase);
        var measured = RaterCalibration.ExcludeHoneypots([.. round.Answers], [.. round.Honeypots.Values]);
        var answers = measured
            .Where(a => byFinding.TryGetValue(a.FindingId, out var r) && r == reason)
            .ToList();

        return new
        {
            run = true,
            queued = (int?)round.Queue.Count(i =>
                i.Reason == reason && !round.Honeypots.ContainsKey(i.FindingId)),
            answered = (int?)answers.Count,

            // ★ A contradiction is the whole point of the spot-check: the judges agreed, and a person outside
            // the model family says otherwise.
            contradicted = (int?)answers.Count(a =>
                a.MachineVerdict is { } m && a.Verdict.IsNoise() != m.IsNoise()),

            // ★ Answers with nothing to compare against are counted HERE and never as agreement, or omitting
            // one field would hide every disagreement.
            notComparable = (int?)answers.Count(a => a.MachineVerdict is null),
            note = (string?)null,
        };
    }
}
