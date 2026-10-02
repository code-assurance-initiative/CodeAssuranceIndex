using System.Text.Json;
using System.Text.Json.Nodes;
using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The submission register: a vendor's run against the published holdout, verified and kept.
/// </summary>
internal static class NoiseSubmissionEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── Submissions ───────────────────────────────────────────────────────────────────────────
        //
        // ★ CAI never runs anyone's scanner. A vendor runs their own tool against the published holdout,
        // on their own infrastructure, and submits findings — so CAI needs no credentials, no access and
        // no licence to anybody's product. What it does is VERIFY the run covered the holdout that was
        // published, at the shas that were published.
        endpoints.MapPost("/api/noise/submissions", Submit)
        .AllowAnonymous()
        .WithName("NoiseSubmit");

        endpoints.MapGet("/api/noise/submissions/{submissionId}", GetSubmission)
        .AllowAnonymous()
        .WithName("NoiseSubmission");
    }

    private static IResult Submit(NoiseSubmission submission, INoiseStore store)
    {
        if (submission is null || string.IsNullOrWhiteSpace(submission.Period))
        {
            return Results.BadRequest(new { error = "a submission names the period it answers" });
        }

        if (!NoiseCorpus.Draws.TryGetValue(submission.Period, out var draw))
        {
            return Results.NotFound(new
            {
                submission.Period,
                error = "no holdout has been published for that period",
            });
        }

        // ★★ NO WITHDRAWAL, and therefore no quiet re-run. Otherwise a vendor runs, dislikes the
        // result and submits again, and the published set silently becomes "the results people were
        // happy with" — which is the whole failure the rule exists to prevent.
        if (store.AlreadySubmitted(submission.Tool, submission.Period))
        {
            return AlreadySubmitted(submission);
        }

        var holdout = HoldoutSampler.Draw(draw.Seed, NoiseCorpus.Candidates, NoiseCorpus.Rules);
        // ★ The draw's own publication timestamp goes in, so the ordering check has something to compare
        // against rather than trusting the submitter's word about which came first.
        var receipt = NoiseSubmissions.Accept(
            submission, holdout, DateTimeOffset.UtcNow, draw.DrawnAt);

        // ★★ THE REGISTER IS THE DATABASE. A rejected run is stored too — it is evidence, and a run a
        // vendor would like to forget is exactly the kind the no-withdrawal rule exists to keep. Only an
        // ACCEPTED one claims the (tool, period) slot, and the claim is a UNIQUE index: losing that race
        // is the rule working, so it comes back as the same conflict a second attempt would get.
        // ★ Serialised here from the parsed declaration so the record publishes what the gate checked —
        // the two cannot disagree about what was declared.
        var configurationJson = submission.Configuration is null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(
                submission.Configuration,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var findingIds = RecordFindings(submission, store);

        if (!store.TryRecordSubmission(receipt, submission.RunStartedAt, configurationJson))
        {
            return AlreadySubmitted(submission);
        }

        // ★ The ids publish with the receipt: the submitter needs them to dispute a verdict on one of their
        // own findings, and they are DERIVED so the submitter can compute them independently and check.
        var body = JsonSerializer.SerializeToNode(
            Render(receipt), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body["findingIds"] = new JsonArray(
            [.. findingIds.Select(id => (JsonNode?)JsonValue.Create(id))]);

        return Results.Json(body);
    }

    /// <summary>The refusal a second submission for the same (tool, period) gets — before or after the race.</summary>
    private static IResult AlreadySubmitted(NoiseSubmission submission) => Results.Conflict(new
    {
        submission.Tool,
        submission.Period,
        error = "this tool has already submitted for this period, and a submission cannot be "
              + "withdrawn or replaced. Register intent for a period whose holdout has not been "
              + "drawn yet, at POST /api/noise/intent — it publishes, and it is what makes "
              + "not submitting visible.",
    });

    private static List<string> RecordFindings(NoiseSubmission submission, INoiseStore store)
    {
        // ★★ THE FINDINGS THEMSELVES ARE STORED (#23). Nothing kept them: the receipt held counts, and a
        // rater was handed an id and asked whether it should have fired — the one check that comes from
        // outside the model family was unanswerable by design. Keyed by the DERIVED id, so a second tool
        // reporting the same defect writes the same row and cross-vendor matching has something to match on.
        var findingIds = (submission.Findings ?? [])
            // ★★ THE TITLE IS PART OF THE KEY ONLY WHERE THERE IS NO COORDINATE (#21) — otherwise 160
            // repo-level findings of one rule in one repository key to a single id, which is what the
            // measurement found. See FindingKey.For.
            .Select(f => (
                Id: FindingKey.For(f.RepoId, f.PinnedSha, f.FilePath, f.Line, f.RuleId, f.Title),
                Finding: f))
            .ToList();

        store.RecordFindings(
        [
            .. findingIds.Select(x => new FindingRecord(
                x.Id, submission.Period, submission.Tool,
                x.Finding.RepoId, x.Finding.PinnedSha, x.Finding.FilePath, x.Finding.Line,
                x.Finding.RuleId, x.Finding.Title, x.Finding.ClaimClass)),
        ]);

        return [.. findingIds.Select(x => x.Id)];
    }

    private static IResult GetSubmission(string submissionId, INoiseSubmissionStore store)
    {
        var receipt = store.FindSubmission(submissionId);
        if (receipt is null)
        {
            return Results.NotFound(new { submissionId, error = "no such submission" });
        }

        // ★★ THE CONFIGURATION BELONGS ON THE RECEIPT, found by the embargo (#15). It was published only
        // through the period record's register — which is embargoed until the period publishes — so a
        // participant could not read back its OWN declaration at all until then. The receipt is fetched by an
        // id only the submitter holds, which makes it the right place for it.
        var node = JsonSerializer.SerializeToNode(
            Render(receipt), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        node["configuration"] = store.ConfigurationJson(receipt.SubmissionId) is { Length: > 0 } json
            ? JsonNode.Parse(json)
            : null;

        return Results.Json(node);
    }

    /// <summary>The receipt as it publishes — what was checked, and what it found.</summary>
    private static object Render(SubmissionReceipt r) => new
    {
        submissionId = r.SubmissionId,
        period = r.Period,
        tool = r.Tool,
        toolVersion = r.ToolVersion,
        receivedAt = r.ReceivedAt,
        methodVersion = MethodVersion,
        samplerVersion = NoiseCorpus.SamplerVersion,
        accepted = r.Accepted,

        // ★★ "Accepted" must not be readable as "complete". A run covering two of twelve repositories
        // is well-formed and is accepted — refusing it outright would push a vendor whose tool genuinely
        // lacks a language into not participating at all. But a receipt saying only accepted:true can be
        // quoted as a clean bill by somebody who scanned a sixth of the holdout, so completeness is its
        // own flag and partial coverage says so in words.
        // ★★ THE COUNTS, published even when they agree. A check whose inputs are not shown cannot be
        // re-derived by the reader it exists for, and this is the one check that says how much of the run
        // reached the standard at all.
        findingCount = new
        {
            reportedByRun = r.ReportedFindingCount,
            submitted = r.SubmittedFindingCount,
            agrees = r.ReportedFindingCount == r.SubmittedFindingCount,
        },

        complete = r.Accepted && r.Uncovered.Count == 0,
        // ★★ Why this participant files no per-judge verdicts, when it files none (#27). A reader looking for
        // the judging finds the reason rather than an empty list they must interpret.
        judgingUnavailable = r.JudgingUnavailable,

        completenessNote = r.Uncovered.Count == 0
            ? "full coverage: every drawn repository appears in this run."
            : $"PARTIAL coverage: {r.CoveredRepositories} of {r.HoldoutRepositories} drawn repositories "
              + "appear in this run. A rate computed over a subset is not comparable with one computed "
              + "over the whole holdout.",

        problems = r.Problems,

        // ★ Coverage publishes whether or not it is complete — "zero uncovered" is a stated fact rather
        // than the absence of a complaint, and partial coverage is the most obvious route to a
        // flattering number.
        coverage = new
        {
            holdoutRepositories = r.HoldoutRepositories,
            coveredRepositories = r.CoveredRepositories,
            uncovered = r.Uncovered,
        },
    };
}
