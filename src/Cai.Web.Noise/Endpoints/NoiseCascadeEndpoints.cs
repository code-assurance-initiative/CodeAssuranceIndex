using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The judging cascade: votes in, outcome out — and, when a period and finding are named, the judgement on the
/// record.
/// </summary>
internal static class NoiseCascadeEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── The cascade ───────────────────────────────────────────────────────────────────────────
        //
        // ★ Published as an endpoint so every participant resolves a disagreement THE SAME WAY. Two
        // vendors applying different escalation rules would produce numbers that look comparable and are
        // not — which is the failure a shared method exists to prevent. It is pure: votes in, outcome
        // out, no model and no state.
        endpoints.MapPost("/api/noise/cascade/resolve", Resolve)
        .AllowAnonymous()
        .WithName("NoiseCascadeResolve");
    }

    private static IResult Resolve(CascadeRequest request, INoiseStore store, TimeProvider clock)
    {
        if (request?.Round1 is null || request.Round1.Count != 2)
        {
            return Results.BadRequest(new
            {
                error = "a round is exactly two independent judges — one is not a cascade, and three "
                      + "invites a majority that hides a genuine split.",
            });
        }

        var round1 = request.Round1.Select(ToVote).ToList();
        var round2 = (request.Round2 ?? []).Select(ToVote).ToList();

        if (round1.Any(v => v is null) || round2.Any(v => v is null))
        {
            return Results.BadRequest(new
            {
                error = "every vote must be one of the six published verdicts",
                verdicts = Enum.GetValues<NoiseVerdict>().Select(v => v.Wire()),
            });
        }

        var outcome = JudgingCascade.Resolve([.. round1!], [.. round2!]);

        var now = clock.GetUtcNow();
        RecordCost(request, store, now);

        // ── The verdict record ────────────────────────────────────────────────────────────────
        //
        // ★★ 01 PROMISES THIS AND NOTHING STORED IT. "Every judge prompt, every model and version, every
        // raw verdict with its reasoning, and every human adjudication. Published in full." The cascade
        // resolved votes in memory and returned an answer, so the one claim a sceptic tests first was the
        // one with nothing behind it. A judged finding now leaves a record, and the record publishes at
        // /api/noise/record/{period}.
        //
        // ★ Recording is refused rather than half-done: a verdict without its model version or its
        // reasoning is not a record a reader can argue with, and storing it would let the endpoint report
        // "recorded" for something unusable.
        var recorded = false;
        List<string> unrecordable = [];
        if (request.Period is { Length: > 0 } period && request.FindingId is { Length: > 0 } findingId)
        {
            recorded = TryRecord(request, period, findingId, outcome, store, unrecordable, now);
        }

        return Results.Ok(new
        {
            // ★ Says plainly whether this judgement is ON THE RECORD. A caller that meant to record and
            // silently did not would believe the standard was keeping its promise on its behalf.
            recorded,
            unrecordable,

            methodVersion = MethodVersion,
            state = outcome.State.ToString(),
            verdict = outcome.Verdict?.Wire(),
            settledAtRound = outcome.SettledAtRound,

            // ★ Published separately: the judges can agree a finding is valid and split on whether
            // anyone could act on it. Picking one view would put a figure nobody agreed on into the
            // actionability axis.
            actionabilityContested = outcome.ActionabilityContested,
            actionable = outcome.Actionable,

            reason = outcome.Reason,
        });
    }

    private static JudgeVote? ToVote(CascadeVote v) =>
        NoiseVerdicts.ParseOrNull(v.Verdict) is { } parsed
            ? new JudgeVote(v.Judge ?? "unnamed", parsed)
            : null;

    private static void RecordCost(CascadeRequest request, INoiseStore store, DateTimeOffset now)
    {
        // ★★ THE COST LEDGER (#25). Attributed from the STORED FINDING — the cascade judges one tool's
        // findings, so the tool is looked up rather than taken from the caller: a cost the caller attributes
        // is one it can attribute to somebody else, and on a per-participant figure that is the whole game.
        // An unknown finding's cost is recorded with no tool rather than dropped; a total smaller than what
        // was spent would understate every participant computed from it.
        if (request.Period is { Length: > 0 } costPeriod && request.FindingId is { Length: > 0 } costFinding)
        {
            // ★ NO TOOL ON THE ROW. Attribution is a JOIN at read time, because a finding two tools
            // reported cost the standard once and was spent on both — see CostTally.JudgementsSolelyYours.
            var costAt = now;

            foreach (var vote in request.Round1!.Concat(request.Round2 ?? []))
            {
                store.RecordCost(new CostEntry(
                    costPeriod, null, "judging", costFinding,
                    vote.ModelSeconds, vote.InputTokens, vote.OutputTokens, costAt));
            }
        }
    }

    private static bool TryRecord(
        CascadeRequest request, string period, string findingId, CascadeOutcome outcome,
        INoiseStore store, List<string> unrecordable, DateTimeOffset now)
    {
        var all = request.Round1!.Select(v => (Round: 1, Vote: v))
            .Concat((request.Round2 ?? []).Select(v => (Round: 2, Vote: v)))
            .ToList();

        unrecordable.AddRange(MissingProvenance(all));

        // ★★ THE PANEL'S SHAPE (#10), checked once over both rounds. The cascade recorded whatever it was
        // handed: four votes from one model under four judge names would have been stored as a judgement,
        // and the record would have shown four agreeing judges where there was one opinion counted four
        // times. See JudgePanel — and note it constrains RECORDING, never the arithmetic below.
        unrecordable.AddRange(JudgePanel.Problems(
            [.. all.Select(x => new JudgePanel.Declaration(
                x.Vote.Judge ?? "unnamed", x.Vote.Model, x.Vote.ModelFamily, x.Vote.Temperature))]));

        if (unrecordable.Count != 0)
        {
            return false;
        }

        foreach (var (round, vote) in all)
        {
            if (vote.Prompt is { Length: > 0 } text)
            {
                store.RegisterPrompt(vote.PromptId!, text, now);
            }

            store.RecordVerdict(new VerdictRecord(
                period, findingId, round,
                vote.Judge ?? "unnamed", vote.Model!, vote.ModelVersion!, vote.PromptId!,
                NoiseVerdicts.ParseOrNull(vote.Verdict)!.Value.Wire(),
                vote.Reasoning!, now,

                // ★ Both travel with the raw verdict, like the model version already does: a
                // requirement whose inputs are not published cannot be checked by the reader it
                // exists for. Non-null here because JudgePanel.Problems refused otherwise.
                vote.ModelFamily!, vote.Temperature!.Value));
        }

        store.RecordResolution(new ResolutionRecord(
            period, findingId, outcome.State.ToString(), outcome.Verdict?.Wire(),
            outcome.SettledAtRound, outcome.ActionabilityContested, outcome.Actionable,
            outcome.Reason, now));
        return true;
    }

    private static IEnumerable<string> MissingProvenance(List<(int Round, CascadeVote Vote)> all)
    {
        foreach (var (round, vote) in all)
        {
            if (string.IsNullOrWhiteSpace(vote.Model)
                || string.IsNullOrWhiteSpace(vote.ModelVersion)
                || string.IsNullOrWhiteSpace(vote.PromptId)
                || string.IsNullOrWhiteSpace(vote.Reasoning))
            {
                yield return
                    $"round {round} judge '{vote.Judge}' — a recorded verdict needs model, "
                  + "modelVersion, promptId and reasoning. A verdict a reader cannot argue with is "
                  + "not open judging.";
            }
        }
    }
}
