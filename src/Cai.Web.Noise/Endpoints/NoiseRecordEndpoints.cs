using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The verdict record, published in full: every raw verdict, how each finding settled, the prompts, the
/// disputes, the second pass and the submission register.
/// </summary>
internal static class NoiseRecordEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── The verdict record, published in full ─────────────────────────────────────────────────
        //
        // ★★ THE OPEN-JUDGING CLAIM, WITH SOMETHING BEHIND IT. 01-scope-and-governance: "every judge prompt,
        // every model and version, every raw verdict with its reasoning, and every human adjudication.
        // Published in full. A reader who disagrees with a verdict must be able to find it, read the
        // reasoning, and say so." Until this existed the cascade resolved in memory and kept nothing, so the
        // claim a sceptic tests first was the one with nothing to test.
        //
        // ★ Anonymous, like the method and the holdout. A judging record only readable by the people who
        // produced it is not published.
        endpoints.MapGet("/api/noise/record/{period}", GetRecord)
        .AllowAnonymous()
        .WithName("NoiseRecord");
    }

    private static IResult GetRecord(string period, INoiseStore store, HttpContext http)
    {
        // ★★ THE EMBARGO (#15). 03 commits to it as one of the four things that make the standard's conflict
        // of interest survivable, and this endpoint served everything to everyone immediately — early sight of
        // a rival's result being the single most valuable thing Watchdog's position could be worth. The lift
        // date is in the SIGNED manifest, and there is no caller name with a different answer.
        // ★★ THE EMBARGO APPLIES TO A DRAWN PERIOD. A period with no draw is not one the standard measures —
        // a submission against it is refused outright ("no holdout has been published for that period"), so no
        // participant's material can exist there to protect. The fail-closed rule is about a DRAWN period
        // whose entry has no date: that is the leak case, and it is embargoed.
        var drawn = NoiseCorpus.Draws.TryGetValue(period, out var periodDraw);
        var publishesAt = drawn ? periodDraw.PublishesAt : null;
        var caller = http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : null;

        var embargoed = drawn && Embargo.IsInForce(publishesAt, DateTimeOffset.UtcNow);
        var view = new RegisterView(embargoed, caller, publishesAt);

        var verdicts = store.ListVerdicts(period);
        var resolutions = store.ListResolutions(period);
        var prompts = store.ListPrompts(period);
        var submissions = store.ListSubmissions(period);

        return Results.Ok(new
        {
            period,
            methodVersion = MethodVersion,

            // ★ An empty record says so rather than looking like a clean one. "Nothing has been judged
            // for this period" and "everything was judged and agreed" are different facts.
            judged = resolutions.Count,
            rawVerdicts = verdicts.Count,
            note = resolutions.Count == 0
                ? "no judging has been recorded for this period yet — this is an absence, not a clean run."
                : null,

            // ★★ AND WHO DELIBERATELY FILED NONE, WITH THEIR REASON (#27). The record models a two-judge
            // cascade; a participant judging some other way has no per-judge rows to file, and a silent
            // absence reads exactly like a run whose judging was never done. Those are opposite claims, so
            // the declaration publishes here — where a reader looking for the verdicts will be standing.
            // ★★ EMBARGOED LIKE THE REGISTER IT NAMES. This carries a TOOL, and "who took part" is the
            // whole of what the embargo withholds — a second field naming the participant defeats it
            // exactly as thoroughly as the first would. Caught by the end-to-end run, which read the
            // register before the publish date and found a name in it.
            judgingUnavailable = JudgingUnavailable(submissions, view),

            // ★★ The prompts, in full and once each. The same prompt answers thousands of findings; a
            // record that repeats it per verdict is a record nobody downloads.
            prompts = prompts.Select(p => new { promptId = p.PromptId, text = p.Text, firstSeenAt = p.FirstSeenAt }),

            // ★★ DISPUTES, BESIDE THE VERDICTS THEY CONTEST. An open one is the state a reader most needs
            // to see: it is where the standard has been challenged and has not answered, and absent from the
            // record "no disputes" and "three we have not got round to" look identical.
            disputes = NoiseStandardShared.RenderDisputes(store, period),

            // ★★ THE SECOND PASS, RAW, beside the first. A reproducibility claim is worth exactly what its
            // evidence is: a reader who doubts "the judging reproduces" must be able to read both answers
            // and the reasoning behind each, and decide for themselves which one was wrong.
            rejudge = NoiseStandardShared.RenderRejudge(store, period),

            resolutions = RenderResolutions(resolutions),

            verdicts = RenderVerdicts(verdicts),

            // ★★ THE EMBARGO APPLIES HERE, and only here (#15). The register is the one part of this record
            // that is attributable to a PARTICIPANT: it names each tool, when it submitted and whether the
            // run was accepted. The judging beside it — verdicts, resolutions, disputes — is CAI's own
            // material about findings and carries no tool at all, so it cannot be read as anybody's result.
            // Before the lift a caller sees only its own entries, Watchdog included; there is no caller name
            // with a different answer.
            embargo = new
            {
                inForce = embargoed,
                publishesAt,
                readingAs = caller,
                note = embargoed
                    ? Embargo.Note(publishesAt)
                    : "This period has published: the register is open to everyone.",
            },

            // ★ The register belongs here too: who submitted, when, and whether it was accepted —
            // including the runs that were refused. A register of only the accepted ones is a register
            // that has been edited.
            submissions = RenderRegister(submissions, view, store),
        });
    }

    /// <summary>Who is reading the register, and whether the embargo is in force for them.</summary>
    private sealed record RegisterView(bool Embargoed, string? Caller, DateTimeOffset? PublishesAt)
    {
        public bool MayRead(string tool) =>
            !Embargoed || Embargo.MayRead(Caller, tool, PublishesAt, DateTimeOffset.UtcNow);
    }

    private static object JudgingUnavailable(IReadOnlyList<SubmissionReceipt> submissions, RegisterView view) =>
        submissions
            .Where(s => s.JudgingUnavailable is { Length: > 0 })
            .Where(s => view.MayRead(s.Tool))
            .GroupBy(s => s.Tool, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { tool = g.Key, reason = g.First().JudgingUnavailable });

    private static object RenderResolutions(IReadOnlyList<ResolutionRecord> resolutions) =>
        resolutions.Select(r => new
        {
            findingId = r.FindingId,
            state = r.State,
            verdict = r.Verdict,
            settledAtRound = r.SettledAtRound,
            actionabilityContested = r.ActionabilityContested,
            actionable = r.Actionable,
            reason = r.Reason,
            recordedAt = r.RecordedAt,
        });

    private static object RenderVerdicts(IReadOnlyList<VerdictRecord> verdicts) =>
        verdicts.Select(v => new
        {
            findingId = v.FindingId,
            round = v.Round,
            judge = v.Judge,
            model = v.Model,
            modelVersion = v.ModelVersion,

            // ★★ The panel's shape, per verdict — a requirement whose inputs are not published cannot be
            // checked by the reader it exists for.
            modelFamily = v.ModelFamily,
            temperature = v.Temperature,

            promptId = v.PromptId,
            verdict = v.Verdict,
            reasoning = v.Reasoning,
            recordedAt = v.RecordedAt,
        });

    private static object RenderRegister(
        IReadOnlyList<SubmissionReceipt> submissions, RegisterView view, INoiseStore store) =>
        submissions
            // ★★ FILTERED UNDER EMBARGO — see the `embargo` block above. Withheld rather than
            // anonymised: a register showing "three tools submitted, two accepted" with the names
            // removed still tells a rival what the field did before their own result published.
            .Where(r => view.MayRead(r.Tool))
            .Select(r => new
            {
                submissionId = r.SubmissionId,
                tool = r.Tool,
                toolVersion = r.ToolVersion,
                receivedAt = r.ReceivedAt,
                accepted = r.Accepted,
                problems = r.Problems,

                // ★★ The declaration publishes BESIDE the number. A configuration nobody can read is not
                // a disclosure — the whole value of the declaration is that a competitor or a buyer can
                // point at it.
                configuration = store.ConfigurationJson(r.SubmissionId) is { Length: > 0 } json
                    ? System.Text.Json.JsonDocument.Parse(json).RootElement
                    : (System.Text.Json.JsonElement?)null,
            });
}
