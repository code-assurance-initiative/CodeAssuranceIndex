using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// One submitted finding as a rater sees it, and what a period's participants cost to judge.
/// </summary>
internal static class NoiseFindingEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ★★ What has to travel with a rate for the rate to mean anything: the funnel it was computed
        // over, the actionability split, and the difference this sample could actually detect.
        // ★★ THE EVIDENCE A RATER IS SHOWN. Deliberately carries NO tool: a rater told which vendor produced a
        // finding is being asked a different question, and on a standard its owner competes in, "this one is
        // Watchdog's" is the most corrupting thing this endpoint could leak.
        endpoints.MapGet("/api/noise/findings/{findingId}", GetFinding)
        .AllowAnonymous()
        .WithName("NoiseFinding");

        // ★★ WHAT ONE MORE PARTICIPANT COSTS, counted while it happens (#25). #23-3 deliberately asserts no
        // figure because none has been measured, and says to measure it during the first full period. An estimate
        // reconstructed afterwards is the kind of number this standard exists not to publish.
        endpoints.MapGet("/api/noise/cost/{period}", GetCost)
        .AllowAnonymous()
        .WithName("NoiseParticipantCost");
    }

    private static IResult GetFinding(string findingId, INoiseFindingStore store)
    {
        if (store.FindFinding(findingId) is not { } f)
        {
            return Results.NotFound(new
            {
                findingId,
                error = "no finding with that id has been submitted. Ids are derived from the finding's "
                      + "coordinates — see /api/noise/method — so a wrong one is usually a wrong coordinate "
                      + "rather than a missing finding.",
            });
        }

        return Results.Ok(new
        {
            findingId = f.FindingId,
            repoId = f.RepoId,
            pinnedSha = f.PinnedSha,
            filePath = f.FilePath,
            line = f.Line,
            ruleId = f.RuleId,
            title = f.Title,
            claimClass = f.ClaimClass,

            // ★★ A link to the PINNED revision, never a branch: HEAD would show a rater code that may have
            // changed since the run, and they would judge the finding against a file that no longer matches.
            sourceUrl = FindingEvidence.SourceUrl(f.RepoId, f.PinnedSha, f.FilePath, f.Line),
        });
    }

    private static IResult GetCost(string period, INoiseCostStore store)
    {
        var tallies = store.CostFor(period);
        var attributed = tallies.Where(t => t.Tool is { Length: > 0 }).ToList();
        var unattributed = tallies.FirstOrDefault(t => t.Tool is null);

        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            period,

            participants = attributed.Select(t => new
            {
                tool = t.Tool,
                judgements = t.Judgements,

                // ★★ Published beside the seconds, because a mean over the judgements that reported a time
                // is not the mean anybody will quote. Null was not read as zero when it was stored, and it
                // is not read as zero here either.
                judgementsWithNoTimeReported = t.JudgementsWithNoTimeReported,
                modelSeconds = t.ModelSeconds,
                inputTokens = t.InputTokens,
                outputTokens = t.OutputTokens,

                // ★★ THE MARGINAL FIGURE, and the one #23-3 asks for: judgements on findings no other
                // participant reported. The rest were spent on findings that would have been judged anyway.
                judgementsSolelyYours = t.JudgementsSolelyYours,
                modelSecondsSolelyYours = t.ModelSecondsSolelyYours,

                // ★ Items, never money.
                crowdItemsRated = t.CrowdItemsRated,
            }),

            // ★★ A cost nobody can attribute must not vanish into a smaller total — that would understate
            // every participant computed from it. Its own row, so the gap is visible.
            unattributed = RenderUnattributed(unattributed),

            // ★★ STATED, because a marginal cost that quietly omitted these would be read as the cost of
            // participation — and #23-3 asserts no figure precisely because a partial one is worse than none.
            // ★★ THEY DO NOT SUM, and saying so is the difference between a figure and a misreading. A
            // finding two tools reported was judged once and appears in both their tallies, because the cost
            // WAS spent on both — so adding the participants up double-counts the shared work. The
            // `solelyYours` figures are the ones that add up to a marginal cost.
            perParticipantFiguresOverlap =
                "a finding two participants both reported was judged once and is counted for each of them: "
              + "the judgement was spent on both. So these rows do not sum to the period's spend. The "
              + "'solelyYours' figures are the marginal ones — what would not have been spent had that "
              + "participant not taken part.",

            notCounted = "human time (ours and the raters'), our own engine's compute for the run being "
                       + "measured, corpus hosting, and the operator's attention. No price is attached to "
                       + "any of this: nobody has priced a rater's minute or a judge's second, and a currency "
                       + "figure here would be quoted as the cost of the standard.",
        });
    }

    private static object RenderUnattributed(CostTally? unattributed) => new
    {
        judgements = unattributed?.Judgements ?? 0,
        judgementsWithNoTimeReported = unattributed?.JudgementsWithNoTimeReported ?? 0,
        modelSeconds = unattributed?.ModelSeconds ?? 0,
        inputTokens = unattributed?.InputTokens ?? 0,
        outputTokens = unattributed?.OutputTokens ?? 0,
        crowdItemsRated = unattributed?.CrowdItemsRated ?? 0,
        note = "judgements against a finding no submission recorded. Real cost, attributable to "
             + "nobody — counted here rather than absorbed into a total that would then be smaller "
             + "than what was spent.",
    };
}
