using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The verdict set and the method's own rules — the two documents every participant reads before it runs.
/// </summary>
internal static class NoiseMethodEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/noise/verdicts", GetVerdicts)
        .AllowAnonymous()
        .WithName("NoiseVerdicts");

        endpoints.MapGet("/api/noise/method", GetMethod)
        .AllowAnonymous()
        .WithName("NoiseMethod");
    }

    private static IResult GetVerdicts() => Results.Ok(new
    {
        methodVersion = MethodVersion,
        note = "The verdict set every participating engine implements. A human and a machine answer "
             + "from the SAME set — agreement between two different questions is not a measurement.",
        verdicts = Enum.GetValues<NoiseVerdict>().Select(v => new
        {
            value = v.Wire(),
            meaning = v.Meaning(),
            countsTowardRate = v.CountsTowardRate(),
            isNoise = v.IsNoise(),
            isActionable = v.IsActionable(),
            isProcessDefect = v.IsProcessDefect(),

            // ★ Published rather than assumed: an implementer who gets this backwards reports a
            // flattering number in good faith.
            excludesForHuman = v.ExcludesFor(NoiseRater.Human),
            excludesForMachine = v.ExcludesFor(NoiseRater.Machine),
            escalatesForHuman = v.EscalatesFor(NoiseRater.Human),
            escalatesForMachine = v.EscalatesFor(NoiseRater.Machine),
        }),
    });

    private static IResult GetMethod(string? period)
    {
        // ★★ Asked about a PERIOD, answer with the version that governed it — not the newest. A reader
        // re-deriving an old number has to be able to find the rules it was judged under, or "versioned"
        // means no more than "we will tell you what the rules are now".
        if (period is { Length: > 0 })
        {
            return MethodForPeriod(period);
        }

        return Results.Ok(new
        {
            version = MethodVersion,

            // ★★ THE WHOLE HISTORY, not just the current version. A reader judging whether a change was
            // self-serving needs when it was announced, which period it first applied to, and why — for every
            // version, because the interesting one is always the one before a number somebody disliked.
            versions = NoiseMethodRules.VersionHistory(),

            // ★★ The answer to "who can change this, and when". With no governance body in phase 1 the honest
            // answer was "Watchdog, unilaterally, at any time"; this is the constraint that replaces a meeting.
            versionTakesEffectFromNextHoldout = true,
            changeControlRule = MethodVersions.Rule,

            // ★★ The single most important sentence in the standard.
            noiseRateIsAQualityScore = false,
            requiresRecallCounterpart = true,
            precisionOnlyWarning =
                "A noise rate is a PRECISION measure. It says nothing about what a tool failed to find, "
              + "and the cheapest way to improve one is to report less — so a specification publishing "
              + "precision alone would reward under-detection across every tool that adopted it.",

            // ★★ And the counterpart is REACHABLE, not merely required in prose. A rule that names an
            // obligation without providing a way to meet it gets skipped by everyone and blamed on nobody.
            recallEndpoint = "/api/noise/pooled",

            // ★★ Required WITH EVERY PUBLICATION, not merely offered. A number nobody is obliged to fetch
            // does not get fetched: the noise rate has an audience and a marketing use, the fix rate has
            // neither, so left optional the published claim stays "our tool is quiet" instead of "our tool
            // is acted upon". Send the observations, or send a reason — the reason publishes too.
            requiresFixRateAnchor = true,
            fixRateEndpoint = "/api/noise/fixrate",
            recallMethodNote =
                "Recall has no ground truth on real repositories, so the reference is POOLED: the union of "
              + "what participating tools reported and a human adjudicated as valid. POST findings from two "
              + "or more tools to /api/noise/pooled. One tool alone gets no recall figure — its recall "
              + "against a union it alone defines is 100% by construction.",

            // The absolutes are what expose suppression; the ratio alone hides it. A tool at 42 valid /
            // 8 noise per 100k LoC has a WORSE ratio than one at 12 valid / 2 noise, and is plainly the
            // better instrument.
            requiredWithEveryRate = NoiseMethodRules.RequiredWithEveryRate,

            // ★★ ENFORCED, not merely published. This was a constant echoed here and compared against
            // nothing: exclusions above the ceiling now VOID a publication at /api/noise/publication.
            maxExclusionRate = PublicationContract.MaxExclusionRate,
            exclusionCeilingVoidsTheRun = true,

            // ★ Which recall methods the standard recognises. They do not measure the same thing, so an
            // estimate must name one — and "none" is a legitimate answer that publishes with its reason.
            recallMethods = PublicationContract.RecallMethods,

            // ★★ HOW POOLED RECALL IS COMPUTED, published because it is the most attackable line the method
            // could contain. Scoring a tool against a union INCLUDING its own findings gives the tool that
            // alone found everything a perfect 100 % against a reference it wrote — and with one participant
            // that reference is ours, so "depth" would have meant "agrees with Watchdog".
            pooledRecall = NoiseMethodRules.PooledRecallRule(),

            // ★★ Where the judging is published. 01 promises every prompt, model version and raw verdict with
            // its reasoning; this is the endpoint that keeps that promise, and naming it here is what makes it
            // findable by somebody who wants to disagree with a verdict.
            verdictRecordEndpoint = "/api/noise/record/{period}",

            // ★★ The pre-publication gate from 05: a run without readable git history cannot publish, because
            // its noise on the history-derived dimensions is an environment artefact rather than a capability
            // gap, and those are exactly the dimensions facing competitors who publish no error rate at all.
            requiresGitMiningVerified = true,

            // ★★ How the tool was CONFIGURED. Every other check constrains the run — the version, the shas,
            // the seed, the model set, the recency declaration — and none of them constrains which rules were
            // switched on, so the right version against the right shas with the noisiest rules off passes all
            // of them. Required, and it publishes.
            requiresConfigurationDeclaration = true,

            // ★★ THE RE-JUDGE, published with its numbers rather than described as "a sample". The sample size,
            // the ceiling, WHY the ceiling is there, and what agreement is measured on — a reader would
            // otherwise assume class-level agreement, which would manufacture instability out of vocabulary.
            rejudge = NoiseMethodRules.RejudgeRule(),

            // ★★ WHAT VERIFICATION ACTUALLY CHECKS, enumerated. "Verified" is the only thing CAI does and the
            // whole neutrality argument, and a reader had no way to find out what it covered — so a submission
            // that passed the easy checks and skipped the hard ones read exactly like one that passed them all.
            verificationChecks = NoiseMethodRules.VerificationChecks(),

            exclusionRule =
                "Items nobody could judge leave the rate. Their counts publish per dimension, and a run "
              + "whose combined exclusions exceed the ceiling is VOID — not a pass with a caveat and not "
              + "a verdict on the tool: the instrument was unfit to run, so it is fixed and run again.",

            // ★ A noise rate compares only tools making comparably falsifiable claims. "Line 42
            // dereferences null" can be a false positive; "this file is a hotspot" cannot be, in the
            // same sense — so a naive pooled rate penalises the more specific tool.
            // ★★ Was a bare string array — the vocabulary was PUBLISHED and never implemented, so a
            // submitter could read it and have nowhere to send it. Now each class carries what it asserts
            // and whether it admits a rate at all, and /publication refuses a result without the breakdown.
            claimClasses = NoiseMethodRules.ClaimClassRules(),
            claimClassRule =
                "Every dimension declares its class, and rates publish PER CLASS as well as pooled. A "
              + "tool that is 95% pointwise and one that is 80% statistical do not have comparable "
              + "pooled rates, and presenting them side by side is a category error.",

            reportingRule =
                "Publish both the pooled (micro) and cluster-weighted (macro) average, and the "
              + "leave-one-out range. Excluding an outlying repository is NOT permitted — dropping a "
              + "repo for having a high or low rate is selecting on the outcome.",

            // ★★ THE COMPLIANCE MARK (#4), published as conditions rather than as a badge. Mechanical or it is
            // an unreviewable veto held by a participant over its rivals.
            complianceMark = NoiseMethodRules.ComplianceMarkRule(),

            // ★★ THE INTENT REGISTER (#14). The no-withdrawal refusal told vendors to "register intent before the
            // next draw instead" and there was nowhere to do it — a rule whose remedy does not exist reads as an
            // excuse.
            intentRegister = NoiseMethodRules.IntentRegisterRule(),

            // ★★ THE TWO BEHAVIOURAL QUESTIONS (#13), verbatim. Two clients asking "would you fix this?" and
            // "is this worth fixing?" are asking different questions, and the answers stop being comparable.
            behaviouralQuestions = NoiseMethodRules.BehaviouralQuestionsRule(),

            // ★★ THE PANEL'S SHAPE (#10). The cascade recorded whatever it was handed, so "four judges agreed"
            // could have been one model counted four times — and 02 §2 is explicit that a single-family ensemble
            // cannot see a single-family blind spot.
            judgePanel = NoiseMethodRules.JudgePanelRule(),

            // ★★ THE PERMANENTLY PRISTINE SLICE. 02 §1: without an endpoint the decay curve measures nothing.
            reservedSlice = NoiseStandardShared.ReservedSliceRule(),

            // ★★ THE CONTESTATION ROUTE, published — a right nobody can find is not one. 01 §5 promises it and
            // nothing said where to go.
            contestation = NoiseMethodRules.ContestationRule(),

            // ★★ THE ROLLING FIGURE, published as a rule rather than as a habit. 02 §5 lists it as required
            // with every rate and nothing computed one.
            twelveMonth = NoiseMethodRules.TwelveMonthRule(),

            // ★★ AND NOW THE RULE HAS AN IMPLEMENTATION BEHIND IT. reportingRule above required both averages
            // while the publication carried only a COUNT of clusters, which cannot produce a cluster-weighted
            // anything — a rule published and unimplementable at the same time.
            clusterAverages = NoiseMethodRules.ClusterAveragesRule(),
        });
    }

    private static IResult MethodForPeriod(string period)
    {
        var inForce = MethodVersions.InForceFor(period);
        return Results.Ok(new
        {
            period,
            version = inForce?.Version,
            announcedAt = inForce?.AnnouncedAt,
            effectiveFromPeriod = inForce?.EffectiveFromPeriod,
            rationale = inForce?.Rationale,
            changeControlRule = MethodVersions.Rule,

            // ★ Null, never the earliest version: claiming a version governed a period that predates
            // it is the same retroactive application the rule forbids, pointing the other way.
            note = inForce is null
                ? $"no method version was in force for {period} — the first version takes effect from "
                  + MethodVersions.History[0].EffectiveFromPeriod + "."
                : null,
        });
    }
}
