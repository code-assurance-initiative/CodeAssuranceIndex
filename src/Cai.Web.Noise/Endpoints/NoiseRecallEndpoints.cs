using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The counterweights to the noise rate: pooled recall across tools, and the fix-rate anchor.
/// </summary>
internal static class NoiseRecallEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ★★ THE COUNTERWEIGHT TO THE NOISE RATE. Precision alone rewards under-firing: a tool reporting
        // one finding it is certain about scores a perfect 0%, and a tool reporting everything worth
        // knowing scores worse. There is no ground truth on real repositories, so the reference is the
        // union of what participating tools reported and a human adjudicated valid — which is the
        // standard's strongest reason to exist, because no single vendor can build it alone.
        endpoints.MapPost("/api/noise/pooled", Pooled)
        .AllowAnonymous()
        .WithName("NoisePooledRecall");

        // ★★ The anchor that needs nobody's opinion. Every other number here rests on a judgement; this
        // one rests on commits, and no amount of shared bias among raters can move it.
        endpoints.MapPost("/api/noise/fixrate", FixRate)
        .AllowAnonymous()
        .WithName("NoiseFixRate");
    }

    private static IResult Pooled(PooledRecallRequest request)
    {
        if (request?.Findings is not { Count: > 0 })
        {
            return Results.BadRequest(new { error = "at least one finding is required" });
        }

        var summary = PooledRecall.Compute(
            ToPooledFindings(request.Findings),
            request.LineWindow ?? PooledRecall.DefaultLineWindow,
            request.SilentTools ?? []);

        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            participatingTools = summary.ParticipatingTools,
            unionSize = summary.UnionSize,

            // ★ The matching window travels with the figures it produced — an undeclared tolerance is
            // a knob whoever computes the number can quietly turn.
            lineWindow = summary.LineWindow,

            tools = RenderTools(summary),

            // ★★ Whether the pool is large enough for the figure to mean what its name says, and the floor
            // itself. Below three tools each leave-one-out reference is essentially one other tool's
            // findings, so "recall" is pairwise agreement — published as refused rather than as a number.
            pooledRecallAvailable = summary.PooledRecallAvailable,
            minimumTools = PooledRecall.MinimumTools,

            // ★★ Said out loud: this is a PSEUDO-oracle. The difference between "our recall is 62 %" and
            // "62 % of what this pool found" is the whole reading.
            pseudoOracle = summary.PseudoOracle,
            scope = summary.Scope,
            excludedWithFixPairOracle = summary.ExcludedWithFixPairOracle,

            // ★★ THE COORDINATE GAP, PUBLISHED (#21). 26 % of a real corpus carries no file, or a file with
            // no line — weighted towards exactly the repo-level dimensions this axis exists to cover. A union
            // that quietly lost them would report a recall figure about the code-level findings every tool
            // already agrees on, and nothing in the response would say so.
            unmatchableWithoutCoordinate = summary.UnmatchableWithoutCoordinate,
            coordinateGap = summary.CoordinateGapNote,

            // ★★ In the response, not in a document nobody reads.
            caveat = summary.Caveat,
        });
    }

    private static List<PooledFinding> ToPooledFindings(IReadOnlyList<PooledFindingEntry> findings) =>
        findings
            .Select(f => new PooledFinding(
                f.Tool ?? "", f.RepoId ?? "", f.FilePath ?? "", f.Line ?? 0, f.Valid ?? false,
                f.HasFixPairOracle ?? false))
            .ToList();

    private static object RenderTools(PooledRecallSummary summary) => summary.Tools.Select(t => new
    {
        tool = t.Tool,
        reported = t.Reported,
        valid = t.Valid,
        matchedUnion = t.MatchedUnion,
        uniqueContribution = t.UniqueContribution,
        precision = t.Precision,
        pooledRecall = t.PooledRecall,

        // ★★ THIS TOOL'S OWN DENOMINATOR. Every tool is scored against a different reference —
        // its own findings removed — so a single shared union size would be the wrong denominator
        // for everybody, and the figure could not be checked by the reader it is published for.
        leaveOneOutReferenceSize = t.LeaveOneOutReferenceSize,
        pooledRecallUnavailable = t.PooledRecallUnavailable,

        // ★★ PER TOOL (#21). A tool whose output is mostly repo-level is being measured on a
        // fraction of what it reported, and its recall means much less than the same figure from a
        // tool whose findings all carry a line. A single total hides that difference.
        withoutCoordinate = t.WithoutCoordinate,
    });

    private static IResult FixRate(FixRateRequest request)
    {
        if (request?.Observations is not { Count: > 0 })
        {
            return Results.BadRequest(new { error = "at least one observation is required" });
        }

        if (request.WindowDays is not > 0)
        {
            return Results.BadRequest(new
            {
                error = "a window in days is required — \"60% of findings get fixed\" without a period "
                      + "is unfalsifiable, because over a long enough window nearly all code changes "
                      + "and the number converges on the churn rate.",
            });
        }

        List<FixObservation> observations = [];
        foreach (var o in request.Observations)
        {
            if (NoiseStandardShared.ParseOutcome(o.Outcome) is not { } outcome)
            {
                return Results.BadRequest(new
                {
                    error = $"unrecognised outcome '{o.Outcome}'",
                    outcomes = new[] { "cited-location-changed", "unchanged", "file-deleted", "not-observable" },
                });
            }

            observations.Add(new FixObservation(
                o.FindingId ?? "", o.RepoId ?? "", outcome, NoiseVerdicts.ParseOrNull(o.CrowdVerdict)));
        }

        var summary = FixRateAnchor.Compute(observations, request.WindowDays.Value);

        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            windowDays = summary.WindowDays,
            observed = summary.Observed,
            fixedFindings = summary.Fixed,
            rate = summary.Rate,
            minimumObservations = FixRateAnchor.MinimumObservations,

            // ★ Both exclusions are published. A deleted file is not a fix — counting it hands a
            // flattering rate to any repository mid-refactor — and a vanished repository is
            // unobservable rather than unfixed.
            excludedFileDeleted = summary.ExcludedFileDeleted,
            unobservable = summary.Unobservable,

            // ★★ The contradiction is the point: a finding the crowd called noise that the maintainer
            // then fixed is evidence the crowd was wrong, from a source independent of every rater.
            // Each is a candidate honeypot — an upstream fix is exactly the earned source they need.
            calledNoiseThenFixed = summary.CalledNoiseThenFixed,

            note = "an anchor, not a complement. The fix rate is not one minus the noise rate: valid "
                 + "findings go unfixed for want of time, and worthless ones are 'fixed' by a refactor "
                 + "that touched the line. Read them side by side; a sharp disagreement means one is wrong.",
        });
    }
}
