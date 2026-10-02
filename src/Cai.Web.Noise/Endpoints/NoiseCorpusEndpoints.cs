using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// The holdout and the pool it is drawn from — published with the seed, the rules and the signed manifest.
/// </summary>
internal static class NoiseCorpusEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        // ── The holdout ───────────────────────────────────────────────────────────────────────────
        //
        // ★★ Published WITH ITS SEED AND RULES, so a third party re-derives the draw and confirms it was
        // not chosen to flatter anybody. A holdout published without them is an assertion, and the whole
        // standard rests on it being a fact.
        endpoints.MapGet("/api/noise/holdout/{period}", GetHoldout)
        .AllowAnonymous()
        .WithName("NoiseHoldout");

        // ★ The pool publishes too. A third party re-deriving a draw needs the seed AND the candidates
        // it was drawn from — publishing only the seed proves nothing, because the pool could have been
        // chosen after the fact.
        endpoints.MapGet("/api/noise/corpus", GetCorpus)
        .AllowAnonymous()
        .WithName("NoiseCorpus");
    }

    private static IResult GetHoldout(string period)
    {
        // ★★ FAIL CLOSED. An unverifiable corpus serves NO draws rather than serving them unsigned: a
        // holdout endpoint that quietly degrades to "here is the pool, unsigned" is worse than one that
        // stops, because the degradation is invisible in the thing it hands back — and a draw from a pool
        // nobody can check is not a draw. 503, because the fault is ours and it is fixable.
        if (NoiseStandardShared.CorpusUnverifiable() is { } unverifiable)
        {
            return unverifiable;
        }

        if (!NoiseCorpus.Draws.TryGetValue(period, out var draw))
        {
            // ★ 404, never an empty draw. An empty holdout reads as "we measured nothing there",
            // which is a different and false claim from "no draw has been published for that period".
            return Results.NotFound(new
            {
                period,
                error = "no holdout has been published for that period",
                published = NoiseCorpus.Draws.Keys.OrderBy(k => k, StringComparer.Ordinal),
            });
        }

        var repos = HoldoutSampler.Draw(draw.Seed, NoiseCorpus.Candidates, NoiseCorpus.Rules);

        return Results.Ok(new
        {
            period,
            methodVersion = MethodVersion,
            samplerVersion = NoiseCorpus.SamplerVersion,

            // ★★ WHICH MANIFEST THIS DRAW CAME FROM, and the signature over it. 01 §2 asks for the draw
            // "timestamped AND signed"; the timestamp was here and the signature was our word.
            manifest = NoiseStandardShared.ManifestIdentity(),

            // Everything needed to re-run the draw, and nothing that could have steered it.
            seed = draw.Seed,
            drawnAt = draw.DrawnAt,
            rules = new
            {
                targetProductionLocPerLanguage = NoiseCorpus.Rules.TargetProductionLocPerLanguage,
                maxRepositoryLoc = NoiseCorpus.Rules.MaxRepositoryLoc,
                minRepositoriesPerLanguage = NoiseCorpus.Rules.MinRepositoriesPerLanguage,
                minRepositoriesPerSlice = NoiseCorpus.Rules.MinRepositoriesPerSlice,
            },
            reproduce =
                "Rank each candidate by SHA-256(seed + NUL + repoId), ascending, tie-broken by "
              + "repoId; per language take in that order until BOTH the LoC target and the "
              + "repository floor are met. Candidates above maxRepositoryLoc are excluded first.",

            languages = repos.GroupBy(r => r.Language, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new
                {
                    language = g.Key,
                    repositories = g.Count(),
                    productionLoc = g.Sum(r => r.ProductionLoc),
                }),

            repositories = repos.Select(r => new
            {
                repoId = r.RepoId,
                language = r.Language,
                pinnedSha = r.PinnedSha,
                productionLoc = r.ProductionLoc,
                licence = r.Licence,

                // ★★ A vendor cannot honour a reservation it cannot see. Published with the draw, and
                // declaring one of these as trained is refused at submission.
                reserved = r.Reserved,
            }),

            // ★ The reservation as a rule, beside the repositories it applies to.
            reservedSlice = NoiseStandardShared.ReservedSliceRule(),
        });
    }

    private static IResult GetCorpus()
    {
        // ★★ Fail closed here too — an unverifiable pool must not be served as though it were the published
        // one, and this is the endpoint a third party fetches to check a draw against.
        if (NoiseStandardShared.CorpusUnverifiable() is { } unverifiable)
        {
            return unverifiable;
        }

        return Results.Ok(new
        {
            samplerVersion = NoiseCorpus.SamplerVersion,

            // ★★ The manifest identity and how to check it — beside the pool, not in a document elsewhere.
            manifest = NoiseStandardShared.ManifestIdentity(),
            howToVerify = CorpusManifest.VerificationInstructions,

            note = "Public repositories only. Everything a human rater is shown must already be public.",
            count = NoiseCorpus.Candidates.Count,
            repositories = NoiseCorpus.Candidates
                .OrderBy(c => c.RepoId, StringComparer.Ordinal)
                .Select(c => new
                {
                    repoId = c.RepoId,
                    language = c.Language,
                    productionLoc = c.ProductionLoc,
                    licence = c.Licence,
                    pinnedSha = c.PinnedSha,
                    reserved = c.Reserved,
                }),
            reservedSlice = NoiseStandardShared.ReservedSliceRule(),
        });
    }
}
