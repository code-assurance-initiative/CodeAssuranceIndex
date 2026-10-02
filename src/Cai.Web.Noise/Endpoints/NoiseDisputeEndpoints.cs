using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// Contestation: raising a dispute against a recorded verdict, and answering it — in public, either way.
/// </summary>
internal static class NoiseDisputeEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── Contestation ──────────────────────────────────────────────────────────────────────────
        //
        // ★★ 01 §5: "a vendor who thinks a verdict is wrong can contest it in public, against published
        // reasoning. 'The standard says so' is not an argument CAI gets to make." There was no way to say so —
        // which makes the standard the last word on its own judgements, the position it exists to avoid.
        endpoints.MapPost("/api/noise/verdicts/{findingId}/dispute", RaiseDispute)
        .AllowAnonymous()
        .WithName("NoiseVerdictDispute");

        endpoints.MapPost("/api/noise/disputes/{disputeId}/resolve", ResolveDispute)
        .AllowAnonymous()
        .WithName("NoiseDisputeResolve");
    }

    private static IResult RaiseDispute(string findingId, DisputeRequest request, INoiseStore store, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(request?.Period))
        {
            return Results.BadRequest(new { error = "a dispute names the period the verdict was recorded in" });
        }

        // ★★ THE REASON IS THE POINT. "I disagree" is not contestation: 01 §5 is about arguing against
        // published reasoning, and the reason is the half that makes the dispute answerable rather than a vote.
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new
            {
                error = "a dispute requires a reason. It publishes with the dispute, and it is what makes "
                      + "the contest answerable rather than a vote — an unexplained objection cannot be "
                      + "argued with in either direction.",
            });
        }

        // ★ A dispute about a judgement nobody made would fill the register with noise of its own, and
        // "twelve disputes this period" would stop meaning anything.
        var judged = store.ListResolutions(request.Period)
            .Any(r => string.Equals(r.FindingId, findingId, StringComparison.OrdinalIgnoreCase));
        if (!judged)
        {
            return Results.NotFound(new
            {
                findingId,
                request.Period,
                error = "no verdict has been recorded for that finding in that period, so there is nothing "
                      + "to contest yet.",
            });
        }

        var dispute = new DisputeRecord(
            DisputeId: Guid.CreateVersion7().ToString("n"),
            Period: request.Period,
            FindingId: findingId,
            RaisedBy: request.RaisedBy ?? "unnamed",
            Reason: request.Reason,
            RaisedAt: clock.GetUtcNow(),
            Outcome: null,
            ResolutionReasoning: null,
            ResolvedAt: null);

        store.RaiseDispute(dispute);

        return Results.Ok(NoiseStandardShared.RenderDispute(dispute));
    }

    private static IResult ResolveDispute(string disputeId, DisputeResolutionRequest request, INoiseDisputeStore store, TimeProvider clock)
    {
        var outcome = NoiseStandardShared.ParseDisputeOutcome(request?.Outcome);
        if (outcome is null)
        {
            return Results.BadRequest(new
            {
                error = "a dispute is answered as upheld or overturned",
                outcomes = new[] { NoiseStandardShared.DisputeOutcomes.Upheld, NoiseStandardShared.DisputeOutcomes.Overturned },
            });
        }

        // ★★ REQUIRED IN BOTH DIRECTIONS. An outcome with no reasoning is "the standard says so", which is
        // exactly the argument 01 §5 says CAI does not get to make — and upholding needs a reason as much as
        // overturning does, because the upheld ones are what show this is not a complaints box.
        if (string.IsNullOrWhiteSpace(request!.Reasoning))
        {
            return Results.BadRequest(new
            {
                error = "a resolution requires its reasoning, whichever way it goes. An outcome without one "
                      + "is 'the standard says so', which is not an argument CAI gets to make.",
            });
        }

        if (store.FindDispute(disputeId) is null)
        {
            return Results.NotFound(new { disputeId, error = "no such dispute" });
        }

        if (!store.ResolveDispute(disputeId, outcome, request.Reasoning, clock.GetUtcNow()))
        {
            // ★ Already answered. Otherwise the outcome is whatever was written last, and "publishes either
            // way" becomes "publishes whichever way we ended up preferring".
            return Results.Conflict(new
            {
                disputeId,
                error = "this dispute has already been answered, and an answer is not replaced. Raise a new "
                      + "dispute if there is something new to say.",
                resolved = NoiseStandardShared.RenderDispute(store.FindDispute(disputeId)!),
            });
        }

        return Results.Ok(NoiseStandardShared.RenderDispute(store.FindDispute(disputeId)!));
    }
}
