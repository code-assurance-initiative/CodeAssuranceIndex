namespace Cai.Web.Noise;

/// <summary>
/// The helpers more than one Noise Standard route family renders through — so two routes that publish the
/// same fact publish it from one implementation.
/// </summary>
internal static class NoiseStandardShared
{
    /// <summary>The reservation, as a published rule — one object, used by the method, the corpus and each draw.</summary>
    internal static object ReservedSliceRule() => new
    {
        repositories = CorpusManifest.Load().Candidates.Count(c => c.Reserved),
        alwaysDrawn = true,
        why = "The recency strata compare 'never trained' against 'trained N cycles ago'. If every repository is "
            + "eventually developed against, the never-trained bucket empties as the standard matures — the "
            + "decay curve loses its endpoint, and the overfitting gap becomes uncomputable exactly when tools "
            + "have had time to overfit.",
        declaringItTrained = "refused. A submission that declares a reserved repository as anything but "
                           + "never-trained is rejected: that declaration IS the reservation being broken, and "
                           + "it is the only moment anybody outside the vendor can see it happen. A repository "
                           + "that has been developed against must LEAVE the reserved slice, which is a change "
                           + "to the signed corpus.",
        listedIn = "/api/noise/corpus and every /api/noise/holdout/{period}, per repository.",
    };

    /// <summary>The two answers a dispute can have.</summary>
    internal static class DisputeOutcomes
    {
        public const string Upheld = "upheld";
        public const string Overturned = "overturned";
    }

    /// <summary>The outcome, or null when it is not one of the two.</summary>
    internal static string? ParseDisputeOutcome(string? outcome) => outcome?.Trim().ToLowerInvariant() switch
    {
        DisputeOutcomes.Upheld => DisputeOutcomes.Upheld,
        DisputeOutcomes.Overturned => DisputeOutcomes.Overturned,
        _ => null,
    };

    /// <summary>One dispute as it publishes.</summary>
    internal static object RenderDispute(DisputeRecord d) => new
    {
        disputeId = d.DisputeId,
        period = d.Period,
        findingId = d.FindingId,
        raisedBy = d.RaisedBy,
        reason = d.Reason,
        raisedAt = d.RaisedAt,

        // ★ "open" is a state, not a missing field — see RenderDisputes.
        state = d.Outcome is null ? "open" : "answered",
        outcome = d.Outcome,
        resolutionReasoning = d.ResolutionReasoning,
        resolvedAt = d.ResolvedAt,
    };

    /// <summary>A period's disputes, with the counts a reader needs before the list.</summary>
    internal static object RenderDisputes(INoiseDisputeStore store, string period)
    {
        var disputes = store.ListDisputes(period);

        return new
        {
            raised = disputes.Count,
            open = disputes.Count(d => d.Outcome is null),
            upheld = disputes.Count(d => d.Outcome == DisputeOutcomes.Upheld),
            overturned = disputes.Count(d => d.Outcome == DisputeOutcomes.Overturned),

            // ★★ Two things a reader would otherwise assume, said once: the raw verdict survives an overturn,
            // and an overturn does not silently move a published rate.
            note = "The raw verdicts are append-only and nothing here removes one — an overturned verdict is "
                 + "still in this record, with the dispute beside it. An overturned verdict does not change a "
                 + "published rate by itself either: that takes a corrected publication, which the "
                 + "append-only publication record shows as a correction.",

            items = disputes.Select(RenderDispute),
        };
    }

    /// <summary>
    /// A 503 when the shipped corpus does not verify, or null when it does.
    /// </summary>
    /// <remarks>
    /// ★★ FAIL CLOSED, AND SAY WHY. The alternative is an endpoint that serves the pool with the signature field
    /// missing or false — a degradation nobody reading the response would notice, on the one artefact whose whole
    /// job is to be checkable. 503 rather than 500: the corpus is fixable and the fault is ours.
    /// </remarks>
    internal static IResult? CorpusUnverifiable()
    {
        var manifest = CorpusManifest.Load();

        // ★ The DECISION is CorpusManifest.RefusalReason — a pure function, so it has a test. Shipping a broken
        // manifest is the only other way to reach this branch, and that breaks every other test in the suite.
        return CorpusManifest.RefusalReason(manifest) is not { } reason
            ? null
            : Results.Json(
                new
                {
                    error = reason,
                    detail = manifest.Problem,
                    manifestVersion = manifest.Version,
                    keyId = manifest.KeyId,
                    howToVerify = CorpusManifest.VerificationInstructions,
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>Which manifest, signed by which key — published with every draw and with the pool.</summary>
    internal static object ManifestIdentity()
    {
        var manifest = CorpusManifest.Load();

        return new
        {
            version = manifest.Version,
            keyId = manifest.KeyId,
            algorithm = CorpusManifest.Algorithm,
            signature = manifest.Signature,

            // ★★ THE CUSTODY CLAIM, published with the signature. Who holds the key IS what a signature is worth,
            // so a reader gets it in words rather than a key id to interpret — and it says plainly what this one
            // does NOT prove.
            keyCustody = manifest.KeyCustody,

            files = new
            {
                manifest = CorpusManifest.ManifestFileName,
                signature = CorpusManifest.SignatureFileName,
                publicKey = CorpusManifest.PublicKeyFileName,
            },
        };
    }

    /// <summary>A period's re-judge as it publishes: the sample, the outcome and the raw second pass.</summary>
    internal static object RenderRejudge(INoiseJudgingStore store, string period)
    {
        var judged = JudgedFindings(store, period);
        var seed = RejudgeSeed(period);
        var sample = Rejudge.SelectSample(seed, period, judged);
        var second = store.ListRejudge(period);

        var outcome = second.Count == 0 || sample.Count == 0
            ? null
            : Rejudge.Compare(
                sample,
                Settled(store, period),
                second.ToDictionary(r => r.FindingId, r => r.Verdict, StringComparer.OrdinalIgnoreCase));

        return new
        {
            sample,
            sampleSeed = seed,
            tolerance = Rejudge.Tolerance,
            fold = Rejudge.Fold,
            compared = outcome?.Compared,
            disagreements = outcome?.Disagreements,
            disagreementRate = outcome?.DisagreementRate,
            withinTolerance = outcome?.WithinTolerance ?? false,
            unjudged = outcome?.Unjudged ?? [],
            excluded = outcome?.Excluded ?? [],
            unusable = outcome?.Unusable ?? [],

            // ★ Raw, with provenance. Named `verdicts` to mirror the first pass's shape.
            verdicts = second.Select(r => new
            {
                findingId = r.FindingId,
                verdict = r.Verdict,
                model = r.Model,
                modelVersion = r.ModelVersion,
                promptId = r.PromptId,
                reasoning = r.Reasoning,
                recordedAt = r.RecordedAt,
            }),

            note = second.Count == 0
                ? "no second pass has been recorded, so the judging has not been shown to reproduce."
                : null,
        };
    }

    /// <summary>The findings a period has a settled verdict for — the population the sample is drawn from.</summary>
    /// <remarks>
    /// ★ Only SETTLED ones. A finding still in the cascade has no first-pass answer to disagree with, so
    /// sampling it would produce an unjudged entry that blocks the tolerance through no fault of the re-judge.
    /// </remarks>
    internal static IReadOnlyList<string> JudgedFindings(INoiseJudgingStore store, string period) =>
        [.. store.ListResolutions(period)
            .Where(r => !string.IsNullOrWhiteSpace(r.Verdict))
            .Select(r => r.FindingId)
            .Distinct(StringComparer.Ordinal)];

    /// <summary>Each finding's settled verdict, for comparison against a second pass.</summary>
    internal static IReadOnlyDictionary<string, string> Settled(INoiseJudgingStore store, string period) =>
        store.ListResolutions(period)
            .Where(r => !string.IsNullOrWhiteSpace(r.Verdict))
            .GroupBy(r => r.FindingId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.RecordedAt).First().Verdict!,
                StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The seed the re-judge sample is drawn from.
    /// </summary>
    /// <remarks>
    /// ★★ THE PERIOD'S OWN PUBLISHED HOLDOUT SEED, so the sample is reproducible from a value that was
    /// published before any result existed. A period with no published draw falls back to its own identifier —
    /// which in production cannot happen, because judging without a draw is judging findings from nowhere; it
    /// is the honest answer for a dev or test period rather than a throw that would hide the case.
    /// </remarks>
    internal static string RejudgeSeed(string period) =>
        NoiseCorpus.Draws.TryGetValue(period ?? "", out var draw) ? draw.Seed : period ?? "";

    internal static FixOutcome? ParseOutcome(string? outcome) => outcome?.Trim().ToLowerInvariant() switch
    {
        "cited-location-changed" => FixOutcome.CitedLocationChanged,
        "unchanged" => FixOutcome.Unchanged,
        "file-deleted" => FixOutcome.FileDeleted,
        "not-observable" => FixOutcome.NotObservable,
        _ => null,
    };
}
