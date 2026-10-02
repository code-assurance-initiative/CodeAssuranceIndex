using System.Text.Json;
using System.Text.Json.Nodes;
using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// Publishing a period's result against the method's contract, and serving what was published.
/// </summary>
internal static class NoisePublicationEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/noise/publication", Publish)
        .AllowAnonymous()
        .WithName("NoisePublication");

        // ★★ THE READ SIDE OF #23-4. Watchdog fetches this at request time; it is the only copy.
        endpoints.MapGet("/api/noise/published/{period}", GetPublished)
        .AllowAnonymous()
        .WithName("NoisePublishedResult");

        // ★★ AND THE SAME THING WITHOUT A PERIOD. A transcluding surface does not know which period is
        // current: given only the keyed route it would have to derive one — "this month", "last month if this
        // 404s" — which is the same class of guess the standard exists to remove, and it would quietly show a
        // stale period the month a cycle slips. The LATEST is a fact CAI holds, so CAI answers it.
        endpoints.MapGet("/api/noise/published", GetLatestPublished)
        .AllowAnonymous()
        .WithName("NoiseLatestPublishedResult");
    }

    private static IResult GetPublished(string period, INoisePublicationStore store) =>
        ServePublished(store, period);

    private static IResult GetLatestPublished(INoisePublicationStore store) =>
        // ★ PublishedPeriods() is ordered by period descending, so the answer comes from the period the
        // result measures and never from insertion order — a correction to an older period must not
        // become "the current number".
        ServePublished(store, store.PublishedPeriods().FirstOrDefault());

    private static IResult Publish(PublicationRequest request, INoiseStore store, TimeProvider clock)
    {
        if (request is null)
        {
            return Results.BadRequest(new { error = "a run is required" });
        }

        // ★ The census is checked FIRST. Whether the numbers can be trusted at all comes before
        // whether the publication is complete — told both at once, an operator fixes the easier one.
        var census = PublicationSurface.CheckCensus(
            request.Reported, request.Adjudicated, request.Excluded, request.Unrated);

        if (!census.Balances)
        {
            return CensusRefusal(census);
        }

        // ★★ THE WHOLE CONTRACT, IN ONE PASS. /api/noise/method has always published ten fields as
        // required with every rate; nothing checked them, and the exclusion ceiling it echoed was
        // compared against nothing at all. Every breach comes back together — told one at a time, a
        // submitter fixes six things over six round-trips and learns nothing about the shape of it.
        if (ParseClaims(request.ClaimClasses, out var claims) is { } claimRefusal)
        {
            return claimRefusal;
        }

        // ★ The vendor's own declaration of which holdout repositories it has developed against. It
        // cannot be derived from the draw — "has this tool seen this code?" is a property of the tool —
        // so it is declared, and published, which is what makes it costly to get wrong.
        if (ParseRecency(request.RecencyStrata, out var recency) is { } recencyRefusal)
        {
            return recencyRefusal;
        }

        // ★★ BOTH AVERAGES, from per-cluster tallies. 02 §5: "so no repository can dominate unseen".
        var clusterTallies = (request.ClusterTallies ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t.ClusterId))
            .Select(t => new ClusterTally(t.ClusterId!, t.Judged ?? 0, t.Noise ?? 0, t.ClaimClass))
            .ToList();
        var clusterAverages = ClusterAverages.Compute(clusterTallies);

        var rolling = RollingFor(request, store);
        var rejudgeOutcome = RejudgeOutcomeFor(request.Period ?? "", store);

        if (TallyMismatch(request, clusterTallies) is { } mismatch)
        {
            return mismatch;
        }

        if (ContractRefusal(request, claims, rejudgeOutcome) is { } breached)
        {
            return breached;
        }

        if (ParseAnchor(request, out var anchor) is { } anchorRefusal)
        {
            return anchorRefusal;
        }

        var payload = NoisePublicationPayload.Build(new PublicationInputs(
            request, claims, recency, clusterTallies, clusterAverages, rolling, rejudgeOutcome, anchor));

        // ★★ STORED, so there is something to transclude. #23-4 has CAI own the published result and
        // watchdog.canine.dev render it at request time rather than keeping a copy — two copies drifting
        // is this codebase's track record, and on this number a caching bug and a suppression are the
        // same event seen from outside. A POST that computed and forgot left nothing to render, so the
        // Watchdog surface could only have restated a figure kennel computed itself: the rejected option.
        //
        // ★ APPEND-ONLY, and only on the accepted path — a refused result must not be fetchable as
        // though it had passed the contract.
        store.RecordPublication(
            request.Period!, JsonSerializer.Serialize(payload), clock.GetUtcNow());

        return Results.Ok(payload);
    }

    private static IResult CensusRefusal(CensusCheck census) => Results.BadRequest(new
    {
        error = "the census does not balance: reported must equal adjudicated + excluded + "
              + "unrated. A funnel that does not add up has a step nobody is reporting, and "
              + "the missing findings are exactly the ones a reader would want to see.",
        reported = census.Reported,
        adjudicated = census.Adjudicated,
        excluded = census.Excluded,
        unrated = census.Unrated,
        shortfall = census.Shortfall,
    });

    private static IResult? ParseClaims(IReadOnlyList<ClaimClassEntry>? entries, out List<ClaimClassTally> claims)
    {
        claims = [];
        foreach (var entry in entries ?? [])
        {
            if (ClaimSpecificity.ParseOrNull(entry.ClaimClass) is not { } parsed)
            {
                return Results.BadRequest(new
                {
                    error = $"unrecognised claim class '{entry.ClaimClass}'",
                    classes = Enum.GetValues<ClaimClass>().Select(ClaimSpecificity.Wire),
                });
            }

            claims.Add(new ClaimClassTally(parsed, entry.Judged, entry.Noise));
        }

        return null;
    }

    private static IResult? ParseRecency(IReadOnlyList<RecencyEntry>? entries, out List<RecencyTally> recency)
    {
        recency = [];
        foreach (var entry in entries ?? [])
        {
            if (Noise.RecencyStrata.ParseOrNull(entry.Stratum) is not { } parsed)
            {
                return Results.BadRequest(new
                {
                    error = $"unrecognised recency stratum '{entry.Stratum}'",
                    strata = Enum.GetValues<RecencyStratum>().Select(Noise.RecencyStrata.Wire),
                });
            }

            recency.Add(new RecencyTally(parsed, entry.Judged, entry.Noise));
        }

        return null;
    }

    private static RollingSummary RollingFor(PublicationRequest request, INoiseStore store)
    {
        // ★★ THE ROLLING TWELVE-MONTH FIGURE, pooled from the append-only publication store — plus THIS
        // period, which is not stored yet. Leaving the current one out would publish a "rolling figure
        // beside a rate" that excludes that very rate: visibly wrong on the first period and subtly wrong
        // for ever after. 02 §5 lists it as required with every rate, and it existed nowhere.
        var judgedNow = request.ValidAndActionable + request.ValidNotActionable + request.Noise;
        return RollingFigure.Compute(
            [
                .. store.PublishedTallies().Where(t =>
                    !string.Equals(t.Period, request.Period, StringComparison.Ordinal)),
                new PeriodTally(request.Period ?? "", judgedNow, request.Noise),
            ],
            throughPeriod: request.Period ?? "");
    }

    private static RejudgeOutcome? RejudgeOutcomeFor(string period, INoiseStore store)
    {
        // ★★ READ FROM THE STORE, never from the request. See PublicationRequest.RejudgeUnavailable.
        var rejudgeSample = Rejudge.SelectSample(
            NoiseStandardShared.RejudgeSeed(period), period,
            NoiseStandardShared.JudgedFindings(store, period));
        var recordedRejudge = store.ListRejudge(period);
        return rejudgeSample.Count == 0 || recordedRejudge.Count == 0
            ? null
            : Rejudge.Compare(
                rejudgeSample,
                NoiseStandardShared.Settled(store, period),
                recordedRejudge.ToDictionary(
                    r => r.FindingId, r => r.Verdict, StringComparer.OrdinalIgnoreCase));
    }

    private static IResult? TallyMismatch(PublicationRequest request, List<ClusterTally> clusterTallies)
    {
        // ★★ TWO ROUTES TO ONE NUMBER IS HOW THEY DRIFT. The headline rate comes from the census counts and
        // the micro average from the tallies; if they disagree, one of them is wrong and publishing both
        // would let the reader pick. Checked here rather than in the contract because it is a relation
        // between two parts of THIS request, not a requirement the method states about a result.
        var judgedFromCensus = request.ValidAndActionable + request.ValidNotActionable + request.Noise;
        var talliedJudged = clusterTallies.Sum(t => t.Judged);
        var talliedNoise = clusterTallies.Sum(t => t.Noise);
        if (clusterTallies.Count == 0
            || (talliedJudged == judgedFromCensus && talliedNoise == request.Noise))
        {
            return null;
        }

        return Results.BadRequest(new
        {
            error = "this result does not meet the contract /api/noise/method publishes.",
            breaches = new[]
            {
                new
                {
                    field = "clusterTallies",
                    error = $"the per-cluster tallies add up to {talliedJudged} judged and "
                          + $"{talliedNoise} noise, but the census says {judgedFromCensus} and "
                          + $"{request.Noise}. One of the two is wrong: the pooled rate would then be "
                          + "computable two ways with two answers, and a reader shown both could pick.",
                },
            },
            methodVersion = MethodVersion,
        });
    }

    private static IResult? ContractRefusal(
        PublicationRequest request, List<ClaimClassTally> claims, RejudgeOutcome? rejudgeOutcome)
    {
        var breaches = PublicationContract.Check(
            request.LocCovered,
            request.RecallEstimate, request.RecallMethod, request.RecallNote,
            claims,
            request.ToolVersion, request.HoldoutSeed, request.ModelSet,
            request.GitMiningVerified,
            request.Adjudicated, request.Excluded,
            hasFixRateObservations: request.FixRateObservations is { Count: > 0 },
            fixRateUnavailable: request.FixRateUnavailable,
            fixRateWindowDays: request.FixRateWindowDays,
            configuration: request.Configuration,
            period: request.Period,
            rejudge: rejudgeOutcome,
            rejudgeUnavailable: request.RejudgeUnavailable);

        if (breaches.Count == 0)
        {
            return null;
        }

        return Results.BadRequest(new
        {
            error = "this result does not meet the contract /api/noise/method publishes. Every "
                  + "requirement below exists because a rate without it invites a comparison that "
                  + "cannot be made fairly.",
            breaches = breaches.Select(b => new { field = b.Field, error = b.Error }),
            methodVersion = MethodVersion,
        });
    }

    private static IResult? ParseAnchor(PublicationRequest request, out FixRateSummary? anchor)
    {
        anchor = null;
        var hasObservations = request.FixRateObservations is { Count: > 0 };
        if (!hasObservations)
        {
            return null;
        }

        List<FixObservation> observations = [];
        foreach (var o in request.FixRateObservations!)
        {
            if (NoiseStandardShared.ParseOutcome(o.Outcome) is not { } outcome)
            {
                return Results.BadRequest(new
                {
                    error = $"unrecognised fix-rate outcome '{o.Outcome}'",
                    outcomes = new[] { "cited-location-changed", "unchanged", "file-deleted", "not-observable" },
                });
            }

            observations.Add(new FixObservation(
                o.FindingId ?? "", o.RepoId ?? "", outcome, NoiseVerdicts.ParseOrNull(o.CrowdVerdict)));
        }

        anchor = FixRateAnchor.Compute(observations, request.FixRateWindowDays!.Value);
        return null;
    }

    /// <summary>
    /// Serve one period's published result, or a stated 404.
    /// </summary>
    /// <remarks>
    /// ★ ONE implementation behind both routes. Two would be two chances for the keyed and the latest answer to
    /// carry different fields, and a consumer that got a thinner body from one of them would render a rate
    /// without its interval — the one thing #23-4 forbids outright.
    /// </remarks>
    private static IResult ServePublished(INoisePublicationStore store, string? period)
    {
        var stored = string.IsNullOrWhiteSpace(period) ? null : store.LatestPublication(period);

        if (stored is not { } published)
        {
            // ★★ NEVER a zero-filled body. "We measured that period and found nothing" and "nothing has been
            // published for it" are different claims, and the first one is false.
            return Results.NotFound(new
            {
                period,
                error = period is { Length: > 0 }
                    ? $"no result has been published for period '{period}'."
                    : "no result has been published yet.",
                published = store.PublishedPeriods(),
            });
        }

        var node = JsonNode.Parse(published.PayloadJson)!.AsObject();
        node["publishedAt"] = JsonValue.Create(published.PublishedAt);

        // ★★ A CORRECTION IS VISIBLE AS ONE. On the single figure where §01 says that being seen to suppress
        // ends the standard, a store that overwrote would make the second publication of a period
        // indistinguishable from the first.
        node["supersededCount"] = JsonValue.Create(published.History.Count - 1);
        node["history"] = new JsonArray(
            published.History.Select(h => (JsonNode?)JsonValue.Create(h)).ToArray());

        // ★ What else exists, so a reader who arrived without a period can walk back through the history.
        node["publishedPeriods"] = new JsonArray(
            store.PublishedPeriods().Select(p => (JsonNode?)JsonValue.Create(p)).ToArray());

        return Results.Json(node);
    }
}
