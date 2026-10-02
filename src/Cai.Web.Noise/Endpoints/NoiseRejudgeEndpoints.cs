using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The re-judge: an independent second pass over a seed-drawn sample of a period's judged findings.
/// </summary>
internal static class NoiseRejudgeEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ★★ THE CHECK THAT POINTS AT US. Every other verification asks whether a vendor's run answered the
        // holdout it claims; this one asks whether the standard's own judging REPRODUCES. A rate produced by a
        // process that disagrees with itself is not a measurement however carefully the corpus was drawn, and
        // CAI owns the judging — so it is the check a critic asks for first, and it was absent.
        endpoints.MapGet("/api/noise/rejudge/{period}", GetRejudge)
        .AllowAnonymous()
        .WithName("NoiseRejudgeStatus");

        endpoints.MapPost("/api/noise/rejudge/{period}", RecordRejudge)
        .AllowAnonymous()
        .WithName("NoiseRejudgeRecord");
    }

    private static IResult GetRejudge(string period, INoiseJudgingStore store)
    {
        var judged = NoiseStandardShared.JudgedFindings(store, period);
        var seed = NoiseStandardShared.RejudgeSeed(period);
        var sample = Rejudge.SelectSample(seed, period, judged);
        var second = store.ListRejudge(period);

        if (sample.Count == 0)
        {
            return Results.Ok(new
            {
                period,
                sample = Array.Empty<string>(),
                sampleSeed = seed,
                sampleSize = Rejudge.DefaultSampleSize,
                tolerance = Rejudge.Tolerance,
                rejudged = false,
                note = "nothing has been judged for this period, so there is no sample to re-judge.",
            });
        }

        var outcome = second.Count == 0
            ? null
            : Rejudge.Compare(
                sample,
                NoiseStandardShared.Settled(store, period),
                second.ToDictionary(r => r.FindingId, r => r.Verdict, StringComparer.OrdinalIgnoreCase));

        return RejudgeStatus(period, sample, seed, judged.Count, outcome);
    }

    private static IResult RejudgeStatus(
        string period, IReadOnlyList<string> sample, string seed, int judgedInPeriod, RejudgeOutcome? outcome) =>
        Results.Ok(new
        {
            period,

            // ★★ PUBLISHED BEFORE ANYBODY RE-JUDGES IT, and with the seed beside it: a third party
            // re-derives the same sample from the full judged set, so it cannot have been steered toward
            // the findings that happen to agree.
            sample,
            sampleSeed = seed,
            sampleSize = Rejudge.DefaultSampleSize,
            judgedInPeriod,

            tolerance = Rejudge.Tolerance,
            toleranceRationale = Rejudge.ToleranceRationale,

            rejudged = outcome is not null,
            compared = outcome?.Compared,
            disagreements = outcome?.Disagreements,
            disagreementRate = outcome?.DisagreementRate,
            withinTolerance = outcome?.WithinTolerance ?? false,
            unjudged = outcome?.Unjudged ?? [],
            excluded = outcome?.Excluded ?? [],
            unusable = outcome?.Unusable ?? [],
            note = outcome is null
                ? "no second pass has been recorded for this period, so the judging has not been shown to "
                + "reproduce. A rate published on it is unverified in the only sense CAI can verify."
                : null,
        });

    private static IResult RecordRejudge(string period, RejudgeRequest request, INoiseJudgingStore store)
    {
        if (request?.Verdicts is not { Count: > 0 })
        {
            return Results.BadRequest(new { error = "at least one re-judged verdict is required" });
        }

        var seed = NoiseStandardShared.RejudgeSeed(period);
        var sample = Rejudge.SelectSample(seed, period, NoiseStandardShared.JudgedFindings(store, period));
        if (sample.Count == 0)
        {
            return Results.BadRequest(new
            {
                period,
                error = "nothing has been judged for this period, so there is no sample to re-judge.",
            });
        }

        if (StraysOutsideTheSample(period, request.Verdicts, sample) is { } strays)
        {
            return strays;
        }

        // ★ The same provenance a first-pass verdict needs. A verdict a reader cannot argue with is not
        // open judging, and here it is also the unauditable half of a reproducibility claim.
        var unrecordable = Unrecordable(request.Verdicts);
        if (unrecordable.Count > 0)
        {
            return Results.BadRequest(new { period, unrecordable });
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var v in request.Verdicts.Where(v => !string.IsNullOrWhiteSpace(v.Prompt)))
        {
            store.RegisterPrompt(v.PromptId!, v.Prompt!, now);
        }

        store.RecordRejudge([.. request.Verdicts.Select(v => new RejudgeRecord(
            period, v.FindingId!, v.Verdict!, v.Model!, v.ModelVersion!, v.PromptId!, v.Reasoning!, now))]);

        var outcome = Rejudge.Compare(
            sample,
            NoiseStandardShared.Settled(store, period),
            store.ListRejudge(period)
                .ToDictionary(r => r.FindingId, r => r.Verdict, StringComparer.OrdinalIgnoreCase));

        return Results.Ok(new
        {
            period,
            sampleSize = outcome.SampleSize,
            compared = outcome.Compared,
            disagreements = outcome.Disagreements,
            disagreementRate = outcome.DisagreementRate,
            tolerance = Rejudge.Tolerance,
            withinTolerance = outcome.WithinTolerance,
            unjudged = outcome.Unjudged,
            excluded = outcome.Excluded,
            unusable = outcome.Unusable,
            fold = Rejudge.Fold,
        });
    }

    private static IResult? StraysOutsideTheSample(
        string period, IReadOnlyList<RejudgeVote> verdicts, IReadOnlyList<string> sample)
    {
        // ★★ ONLY THE SAMPLE. Without this the second pass re-judges whatever it likes and reports
        // agreement over its own choice — the steerable sample the seed exists to prevent, arriving
        // through the back door.
        var sampled = sample.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var strays = verdicts
            .Select(v => v.FindingId ?? "")
            .Where(id => !sampled.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (strays.Count == 0)
        {
            return null;
        }

        return Results.BadRequest(new
        {
            period,
            error = "these findings are not in this period's re-judge sample: "
                  + string.Join(", ", strays)
                  + ". The sample is drawn from the period's seed; re-judging a set of your own "
                  + "choosing and reporting agreement over it measures the chooser.",
            sample,
        });
    }

    private static List<string> Unrecordable(IReadOnlyList<RejudgeVote> verdicts) =>
        verdicts
            .Where(v => string.IsNullOrWhiteSpace(v.Verdict)
                     || string.IsNullOrWhiteSpace(v.Model)
                     || string.IsNullOrWhiteSpace(v.ModelVersion)
                     || string.IsNullOrWhiteSpace(v.PromptId)
                     || string.IsNullOrWhiteSpace(v.Reasoning))
            .Select(v => $"{v.FindingId ?? "(no id)"}: a re-judged verdict needs a verdict, model, "
                       + "modelVersion, promptId and reasoning.")
            .ToList();
}
