using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The crowd layer's intake: the queue, the one-item hand-out, answers, honeypots and who the raters are.
/// </summary>
internal static class NoiseCrowdEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── The crowd layer ───────────────────────────────────────────────────────────────────────
        //
        // ★★ The only check in the method that comes from OUTSIDE the model family. Four judges agreeing
        // shows they are consistent; it does not show they are right, and adding judges never converts
        // one into the other. So a sample of what they AGREED on goes to people too — not only the
        // contested tail, which is the efficient choice and exactly where the independence is wasted.
        endpoints.MapPost("/api/noise/crowd/queue", Queue)
        .AllowAnonymous()
        .WithName("NoiseCrowdQueue");

        // ★ ONE ITEM. The nine-second median in the pilot came from a 500-item list to get through;
        // there is no slog to race when the ask is a single question.
        endpoints.MapGet("/api/noise/crowd/next", Next)
        .AllowAnonymous()
        .WithName("NoiseCrowdNext");

        endpoints.MapPost("/api/noise/crowd/answers", Answer)
        .AllowAnonymous()
        .WithName("NoiseCrowdAnswer");

        // ★★ Calibration against findings that were settled OUTSIDE the rating process. The obvious
        // construction — score raters against what the crowd agreed — measures conformity and calls it
        // accuracy: highest for repeating the majority, lowest for catching what everyone missed. So a
        // honeypot's truth must be a fact about the world that would hold if nobody had rated anything.
        endpoints.MapPost("/api/noise/crowd/honeypots", Honeypots)
        .AllowAnonymous()
        .WithName("NoiseCrowdHoneypots");

        // ★★ Who answered. Agreement statistics are blind to shared bias: ten raters who all work in one
        // language, or all work for the vendor, agree at a rate that reads as reliability and is nothing
        // of the kind. κ measures whether raters agree, never whether what they agree on is true.
        endpoints.MapPost("/api/noise/crowd/raters", Raters)
        .AllowAnonymous()
        .WithName("NoiseCrowdRaters");
    }

    private static IResult Queue(CrowdQueueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Period) || request.Candidates is not { Count: > 0 })
        {
            return Results.BadRequest(new { error = "a period and at least one candidate are required" });
        }

        if (ParseCandidates(request.Candidates, out var candidates) is { } unplaceable)
        {
            return unplaceable;
        }

        if (RepeatedFindings(candidates) is { } repeatedRefusal)
        {
            return repeatedRefusal;
        }

        var queue = CrowdQueue.Build(
            candidates, request.Seed ?? request.Period, Math.Max(0, request.SpotCheck));
        CrowdQueues.Register(request.Period, queue);

        // ★ COUNTS, never names. The operator needs to know a sample was drawn; publishing which
        // findings are in it lets a participant recognise them, and a spot-check you can identify is
        // a spot-check you can prepare for.
        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            period = request.Period,
            queued = queue.Count,
            contested = queue.Count(i => i.Reason == CrowdReason.Contested),
            spotCheck = queue.Count(i => i.Reason == CrowdReason.SpotCheck),
        });
    }

    private static IResult? ParseCandidates(
        IReadOnlyList<CrowdCandidateRequest> requested, out List<CrowdCandidate> candidates)
    {
        candidates = [];
        foreach (var c in requested)
        {
            if (CrowdQueues.ParseState(c.State) is not { } state)
            {
                return Results.BadRequest(new
                {
                    error = $"unrecognised cascade state '{c.State}' — a candidate CAI cannot place is "
                          + "rejected, never dropped, because a sample that silently shrank looks "
                          + "exactly like one drawn correctly.",
                    states = new[] { "accepted", "needs-round-2", "needs-human" },
                });
            }

            candidates.Add(new CrowdCandidate(c.FindingId ?? "", state, c.OwnerId ?? ""));
        }

        return null;
    }

    private static IResult? RepeatedFindings(List<CrowdCandidate> candidates)
    {
        // ★★ ONE ROW PER FINDING, REFUSED HERE RATHER THAN FATAL LATER. A queue naming the same finding
        // twice was accepted and then threw "an item with the same key has already been added" inside the
        // slice the PUBLICATION endpoint renders — so a malformed crowd round took down publishing, several
        // calls later, with a 500 that named neither the queue nor the finding. Found by the end-to-end run;
        // no unit test on either side could see it, because each side was correct on its own.
        //
        // ★ Refused rather than de-duplicated: two rows for one finding means the caller's set is not what
        // it thinks it is — under the standard's own id rule two repo-level findings of one dimension in one
        // repository ARE one finding — and silently collapsing them would hide that from the sender.
        var repeated = candidates
            .GroupBy(c => c.FindingId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        if (repeated.Count == 0)
        {
            return null;
        }

        return Results.BadRequest(new
        {
            error = "these findings appear more than once in the queue: "
                  + string.Join(", ", repeated.Take(10))
                  + (repeated.Count > 10 ? $" (+{repeated.Count - 10} more)" : "")
                  + ". A finding id is DERIVED from its coordinates, so two rows with one id are two "
                  + "reports of the same finding — which the crowd must be handed once. Group them "
                  + "before registering the round.",
            duplicates = repeated.Count,
        });
    }

    private static IResult Next(string period, string raterId, INoiseFindingStore store, TimeProvider clock)
    {
        if (CrowdQueues.Find(period) is not { } round)
        {
            return Results.NotFound(new { error = $"no crowd queue is registered for period '{period}'" });
        }

        // ★★ THE ONE PATH (see CrowdOffering). The dosing, the load-aware choice, the estate exclusion and
        // the hand-out lease only work together, and the public page hands out items through this same call.
        if (CrowdOffering.Next(round, raterId, store, clock.GetUtcNow()) is not { } offer)
        {
            return Results.NoContent();
        }

        // ★★ THE FINDING AND NOTHING ELSE. Told four judges already agreed, a reasonable person
        // reads "probably fine" and rubber-stamps — and the spot-check exists precisely to catch the
        // case where all four were wrong together. A reason on the wire would destroy the only
        // evidence it was built to gather, and nothing downstream would ever show that it had.
        return Results.Ok(new
        {
            findingId = offer.FindingId,

            // ★★ THE EVIDENCE TRAVELS WITH THE ITEM (#23). Without it a rater is being asked whether a hex
            // string should have fired. It carries no tool: a rater told which vendor produced a finding is
            // being asked a different question, and on a standard its owner competes in that is the single
            // most corrupting thing this payload could leak.
            evidence = offer.Evidence is null ? null : new
            {
                repoId = offer.Evidence.RepoId,
                pinnedSha = offer.Evidence.PinnedSha,
                filePath = offer.Evidence.FilePath,
                line = offer.Evidence.Line,
                ruleId = offer.Evidence.RuleId,
                title = offer.Evidence.Title,
                claimClass = offer.Evidence.ClaimClass,
                sourceUrl = FindingEvidence.SourceUrl(
                    offer.Evidence.RepoId, offer.Evidence.PinnedSha,
                    offer.Evidence.FilePath, offer.Evidence.Line),
            },
            evidenceProblem = offer.EvidenceProblem,

            // ★★ THE QUESTIONS TRAVEL WITH THE ITEM (#13). Without them every client invents its own wording,
            // and "would you fix this?" against "is this worth fixing?" are different questions whose answers
            // are not comparable. Still nothing about what the judges said — that disguise is what the
            // spot-check depends on.
            questions = new
            {
                wouldFix = BehaviouralQuestions.WouldFix,
                wantInReport = BehaviouralQuestions.WantInReport,
            },
        });
    }

    private static IResult Answer(CrowdAnswerRequest request, INoiseCostStore store, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(request?.Period) || CrowdQueues.Find(request.Period) is not { } round)
        {
            return Results.NotFound(new { error = "no crowd queue is registered for that period" });
        }

        if (NoiseVerdicts.ParseOrNull(request.Verdict) is not { } verdict)
        {
            return Results.BadRequest(new
            {
                error = "an answer must be one of the six published verdicts",
                verdicts = Enum.GetValues<NoiseVerdict>().Select(v => v.Wire()),
            });
        }

        // ★★ THE ONE PATH (see CrowdOffering.Record) — the hand-out check included, so the public page
        // cannot record an answer this endpoint would have refused.
        if (CrowdOffering.Record(
                round, request.RaterId, request.FindingId, verdict,
                request.WouldFix, request.WantInReport,
                NoiseVerdicts.ParseOrNull(request.MachineVerdict)) is { } refusal)
        {
            return Results.Conflict(new { error = refusal, findingId = request.FindingId });
        }

        // ★ THE OTHER HALF OF THE MARGINAL COST (#25): a rated item. Counted as an item and never as money —
        // nobody has priced a rater's minute, and a currency figure invented here would be quoted as the cost
        // of the crowd.
        store.RecordCost(new CostEntry(
            request.Period!, null, "crowd",
            request.FindingId ?? "", null, null, null, clock.GetUtcNow()));

        return Results.Ok(new { recorded = true, findingId = request.FindingId });
    }

    private static IResult Honeypots(HoneypotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Period) || CrowdQueues.Find(request.Period) is not { } round)
        {
            return Results.NotFound(new { error = "no crowd queue is registered for that period" });
        }

        if (request.Honeypots is not { Count: > 0 })
        {
            return Results.BadRequest(new { error = "at least one honeypot is required" });
        }

        List<Honeypot> planted = [];
        foreach (var h in request.Honeypots)
        {
            if (ParseHoneypot(h, round, out var honeypot) is { } refusal)
            {
                return refusal;
            }

            planted.Add(honeypot!);
        }

        foreach (var honeypot in planted)
        {
            round.Honeypots[honeypot.FindingId] = honeypot;
        }

        return Results.Ok(new { period = request.Period, planted = planted.Count });
    }

    private static IResult? ParseHoneypot(HoneypotEntry h, CrowdRound round, out Honeypot? honeypot)
    {
        honeypot = null;
        if (RaterCalibration.ParseSource(h.Source) is not { } source)
        {
            return Results.BadRequest(new
            {
                error = $"'{h.Source}' is not an earned source. A honeypot's answer must be settled "
                      + "outside the rating process — scoring raters against what the crowd agreed "
                      + "measures conformity, not accuracy, and rewards repeating the majority.",
                sources = new[] { "upstream-fix-merged", "vendor-withdrew", "advisory-retracted" },
            });
        }

        if (NoiseVerdicts.ParseOrNull(h.Truth) is not { } truth)
        {
            return Results.BadRequest(new
            {
                error = "a honeypot's truth must be one of the six published verdicts",
                verdicts = Enum.GetValues<NoiseVerdict>().Select(v => v.Wire()),
            });
        }

        var candidate = new Honeypot(h.FindingId ?? "", truth, source, h.Evidence);
        if (!RaterCalibration.IsWellFormed(candidate))
        {
            return Results.BadRequest(new
            {
                error = "evidence must be a link a third party can open — \"we checked\" is the "
                      + "same claim the honeypot exists to be independent of.",
                findingId = h.FindingId,
            });
        }

        // ★ Planted into the EXISTING queue. A honeypot that is not already a question somebody
        // could be asked is a separate exam, and a separate exam is one a rater can recognise.
        if (!round.Queue.Any(i => string.Equals(i.FindingId, candidate.FindingId, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Conflict(new
            {
                error = "that finding is not in this period's queue",
                findingId = candidate.FindingId,
            });
        }

        honeypot = candidate;
        return null;
    }

    private static IResult Raters(RaterDeclarationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Period) || CrowdQueues.Find(request.Period) is not { } round)
        {
            return Results.NotFound(new { error = "no crowd queue is registered for that period" });
        }

        if (string.IsNullOrWhiteSpace(request.RaterId) || string.IsNullOrWhiteSpace(request.PrimaryLanguage))
        {
            return Results.BadRequest(new { error = "a raterId and a primaryLanguage are required" });
        }

        if (ParseAffiliation(request.Affiliation) is not { } affiliation)
        {
            return Results.BadRequest(new
            {
                error = "an affiliation is required, and 'unknown' is not one of them — a vendor "
                      + "rating its own tool's findings is the conflict this standard exists to "
                      + "remove, and it cannot be declared by omission.",
                affiliations = new[]
                {
                    "independent", "vendor-employed", "vendor-contracted", "compensated-in-product",
                },
            });
        }

        round.Strata[request.RaterId] = new RaterStratum(request.RaterId, request.PrimaryLanguage, affiliation);

        return Results.Ok(new { period = request.Period, raterId = request.RaterId });
    }

    /// <summary>
    /// Parse an affiliation, or null when it is not one of the three.
    /// </summary>
    /// <remarks>
    /// ★ There is deliberately no "unknown" to declare. Undeclared is a state the store can be IN — a
    /// rater who never said — but not a claim anyone may make, or the conflict of interest becomes
    /// something a vendor can assert its way out of.
    /// </remarks>
    private static RaterAffiliation? ParseAffiliation(string? affiliation) =>
        affiliation?.Trim().ToLowerInvariant() switch
        {
            "independent" => RaterAffiliation.Independent,
            "vendor-employed" => RaterAffiliation.VendorEmployed,
            "vendor-contracted" => RaterAffiliation.VendorContracted,

            // ★★ A rater earning the vendor's product by answering. Compensated in kind rather than in
            // cash — which changes the accounting and not the incentive.
            "compensated-in-product" => RaterAffiliation.CompensatedInProduct,
            _ => null,
        };
}
