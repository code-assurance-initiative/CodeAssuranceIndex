namespace Cai.Web.Noise;

/// <summary>
/// The compliance mark and the intent register — the two published facts about who took part and how.
/// </summary>
internal static class NoiseRegisterEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── The compliance mark ───────────────────────────────────────────────────────────────────
        //
        // ★★ A MARK THAT CAN BE PULLED IS POWER OVER A COMPETITOR'S MARKETING, and pulling one is far more
        // newsworthy than granting one. So it is decided here by arithmetic over facts CAI already holds — no
        // judgement anywhere in the path — and a withheld mark names the condition and the fact it read.
        endpoints.MapGet("/api/noise/mark/{period}", GetMark)
        .AllowAnonymous()
        .WithName("NoiseComplianceMark");

        // ── The intent register ───────────────────────────────────────────────────────────────────
        //
        // ★★ THE NO-WITHDRAWAL RULE HAD NOWHERE TO SEND ANYBODY. Its refusal already said "register intent before
        // the next draw instead", and there was no endpoint to do it at. Worse, the rule without a before is only
        // half a rule: a vendor can simply never submit the periods that went badly, and the published set quietly
        // becomes "the results people were happy with".
        endpoints.MapPost("/api/noise/intent", RegisterIntent)
        .AllowAnonymous()
        .WithName("NoiseIntentRegister");

        endpoints.MapGet("/api/noise/intent/{period}", GetIntent)
        .AllowAnonymous()
        .WithName("NoiseIntentPeriod");
    }

    private static IResult GetMark(string period, INoiseStore store)
    {
        var marks = MarksFor(store, period);

        return Results.Ok(new
        {
            period,
            label = ComplianceMark.Label,
            free = ComplianceMark.Free,
            changeRule = ComplianceMark.ChangeRule,
            wordingRule = ComplianceMark.WordingRule,
            appealRoute = ComplianceMark.AppealRoute,
            conditions = ComplianceMark.Conditions.Select(c => new { c.Name, c.Reads }),

            deadline = NoiseCorpus.Draws.TryGetValue(period, out var d) ? d.SubmissionsCloseAt : null,

            marks = marks.Select(m => new
            {
                tool = m.Tool,
                granted = m.Granted,
                statement = m.Statement,
                failing = m.Failing.Select(f => new { condition = f.Condition, why = f.Why }),
            }),

            note = marks.Count == 0
                ? "no tool has submitted for this period, so there is no mark to state either way."
                : null,
        });
    }

    private static IResult RegisterIntent(NoiseStandardEndpoints.IntentRequest request, INoiseSubmissionStore store)
    {
        if (string.IsNullOrWhiteSpace(request?.Period) || string.IsNullOrWhiteSpace(request.Tool))
        {
            return Results.BadRequest(new
            {
                error = "registering intent names the period and the tool. Both publish.",
            });
        }

        // ★★ CLOSED ONCE THE HOLDOUT IS DRAWN. Intent registered after seeing the draw is not intent — it is
        // a decision made with the sample in hand, which is the one thing the ordering exists to prevent. The
        // draw date travels with the refusal, so it can be checked against the published draw rather than
        // taken on this endpoint's word.
        if (NoiseCorpus.Draws.TryGetValue(request.Period, out var draw))
        {
            return Results.Conflict(new
            {
                request.Period,
                request.Tool,
                error = $"the holdout for {request.Period} has already been drawn, so intent for it can no "
                      + "longer be registered: a decision made with the sample in hand is not intent. "
                      + "Register for a period whose draw has not been published.",
                drawnAt = draw.DrawnAt,
            });
        }

        var record = store.RegisterIntent(request.Period, request.Tool, DateTimeOffset.UtcNow);

        return Results.Ok(new
        {
            record.Period,
            record.Tool,
            record.RegisteredAt,
            note = "This registration publishes. A tool that registers and then does not submit is named in "
                 + "the register — which is the point: the no-withdrawal rule cannot catch a run that was "
                 + "never submitted, and this can.",
        });
    }

    private static IResult GetIntent(string period, INoiseSubmissionStore store)
    {
        var registered = store.ListIntent(period);
        var submitted = store.ListSubmissions(period)
            .Select(r => r.Tool)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ★★ THE FIGURE THE REGISTER EXISTS FOR. A vendor who registered and then published nothing is the
        // case the no-withdrawal rule cannot catch on its own — they never submitted, so there is nothing to
        // withdraw. Naming them is the entire enforcement mechanism, and it costs a set lookup.
        var missing = registered.Where(r => !submitted.Contains(r.Tool)).Select(r => r.Tool).ToList();

        return Results.Ok(new
        {
            period,
            drawn = NoiseCorpus.Draws.TryGetValue(period, out var draw) ? draw.DrawnAt : (DateTimeOffset?)null,
            open = !NoiseCorpus.Draws.ContainsKey(period),

            registered = registered.Select(r => new
            {
                tool = r.Tool,
                registeredAt = r.RegisteredAt,
                submitted = submitted.Contains(r.Tool),
            }),

            registeredAndDidNotSubmit = missing,

            note = IntentNote(registered.Count, missing),
        });
    }

    private static string IntentNote(int registered, List<string> missing) =>
        registered == 0
            ? "nobody has registered intent for this period yet. That is an absence, not a statement "
            + "about anybody."
            : missing.Count == 0
                ? "everybody who registered intent for this period has submitted."
                : $"{missing.Count} tool(s) registered and did not submit: "
                + string.Join(", ", missing)
                + ". The no-withdrawal rule cannot see a run that was never submitted; this can.";

    /// <summary>
    /// Every tool's mark for a period, decided from what the store already holds.
    /// </summary>
    /// <remarks>
    /// ★★ NOTHING IS ASKED OF ANYBODY. The four conditions read the receipt (accepted, and its coverage), the
    /// deadline against the receipt's timestamp, and whether a publication exists — all facts CAI recorded when
    /// they happened. A mark that needed an input would be a mark somebody could argue for.
    /// </remarks>
    private static IReadOnlyList<MarkState> MarksFor(INoiseStore store, string period)
    {
        var deadline = NoiseCorpus.Draws.TryGetValue(period, out var draw) ? draw.SubmissionsCloseAt : null;
        var published = store.LatestPublication(period) is not null;

        return
        [
            .. store.ListSubmissions(period)
                .GroupBy(r => r.Tool, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g =>
                {
                    // ★ The BEST receipt this tool has for the period: a refused run followed by an accepted one
                    // is a tool that got it right, and the register keeps both either way.
                    var best = g.OrderByDescending(r => r.Accepted).ThenBy(r => r.ReceivedAt).First();

                    return ComplianceMark.Evaluate(g.Key, period, new MarkInputs(
                        // ★ Coverage is the holdout check the receipt already reports: an accepted run with no
                        // uncovered repositories ran the published draw.
                        RanAgainstTheHoldout: best.Accepted && best.CoveredRepositories > 0,
                        SubmittedBeforeTheDeadline: deadline is not { } closes || best.ReceivedAt <= closes,
                        RunReproduces: best.Accepted,
                        NumbersPublishedInFull: published));
                }),
        ];
    }
}
