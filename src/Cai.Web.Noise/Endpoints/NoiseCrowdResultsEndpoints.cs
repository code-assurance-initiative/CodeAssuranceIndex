using static Cai.Web.Noise.NoiseStandardEndpoints;

namespace Cai.Web.Noise;

/// <summary>
/// What the crowd layer found: rater calibration against honeypots, and the measured slices with who answered.
/// </summary>
internal static class NoiseCrowdResultsEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/noise/crowd/calibration/{period}", GetCalibration)
        .AllowAnonymous()
        .WithName("NoiseCrowdCalibration");

        endpoints.MapGet("/api/noise/crowd/results/{period}", GetResults)
        .AllowAnonymous()
        .WithName("NoiseCrowdResults");
    }

    private static IResult GetCalibration(string period)
    {
        if (CrowdQueues.Find(period) is not { } round)
        {
            return Results.NotFound(new { error = $"no crowd queue is registered for period '{period}'" });
        }

        var scores = RaterCalibration.Score([.. round.Answers], [.. round.Honeypots.Values]);

        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            period,
            minimumSample = RaterCalibration.MinimumSample,
            planted = round.Honeypots.Count,

            // ★ The COUNT travels with the figure. A reader who wants to discount four-of-five can; a
            // reader shown only "80%" cannot. Uncalibrated raters are listed too — leaving them out
            // would make the published list look like the whole crowd.
            raters = scores.Select(s => new
            {
                raterId = s.RaterId,
                answered = s.Answered,
                agreed = s.Agreed,
                accuracy = s.Accuracy,
                calibrated = s.Calibrated,
            }),

            // ★★ Stated on the surface itself, because the temptation is specific and strong: a poor
            // score never removes that rater's answers. Dropping them selects on the outcome and
            // leaves the subset that agreed — a cleaner number that means less.
            note = "scores are published, never applied. No answer is dropped for a poor score: "
                 + "excluding raters by the variable being measured is selection on the outcome.",
        });
    }

    private static IResult GetResults(string period)
    {
        if (CrowdQueues.Find(period) is not { } round)
        {
            return Results.NotFound(new { error = $"no crowd queue is registered for period '{period}'" });
        }

        var byFinding = round.Queue.ToDictionary(i => i.FindingId, i => i.Reason, StringComparer.OrdinalIgnoreCase);

        // ★★ Honeypot answers leave the measurement they calibrate. Their answer was known before it
        // was asked, so counting them would measure the mixture of honeypots that happened to be
        // planted rather than anything about the tool.
        var measured = RaterCalibration.ExcludeHoneypots([.. round.Answers], [.. round.Honeypots.Values]);

        // ★★ REPORTED SEPARATELY, never merged. The contested items are hard BY CONSTRUCTION and the
        // accepted ones are the pipeline's own claim; averaging them hides exactly the disagreement
        // rate on auto-accepted findings that the layer exists to measure. There is deliberately no
        // combined figure — one would be quoted.
        return Results.Ok(new
        {
            methodVersion = MethodVersion,
            period,
            contested = Slice(round, byFinding, measured, CrowdReason.Contested),
            spotCheck = Slice(round, byFinding, measured, CrowdReason.SpotCheck),

            // ★ Its own slice, so the calibration work is visible rather than invisible. A round that
            // spent a third of its questions on honeypots and one that spent none look identical from
            // the measured slices alone.
            honeypots = new
            {
                planted = round.Honeypots.Count,
                answered = round.Answers.Count(a => round.Honeypots.ContainsKey(a.FindingId)),
            },

            // ★★ WHAT THE RATERS WOULD DO, beside what they LABELLED it (#13). 02 §4 validates the spec
            // against practitioner behaviour rather than against opinions of the spec's vocabulary — and
            // where the two disagree is the most informative thing this layer produces.
            behaviour = Behaviour(measured),

            // ★★ Published beside the figures, never as a footnote elsewhere. A reader who cannot see
            // that four fifths of the answers came from one language, or from the vendor, is reading
            // an agreement rate as if it measured truth.
            composition = Composition(round, measured),
        });
    }

    private static object Behaviour(IReadOnlyList<CrowdAnswer> measured)
    {
        var asked = measured.Where(a => a.WouldFix is not null || a.WantInReport is not null).ToList();
        var fix = measured.Where(a => a.WouldFix is not null).ToList();
        var report = measured.Where(a => a.WantInReport is not null).ToList();

        return new
        {
            answered = asked.Count,

            // ★★ NOT ASKED is its own count, never folded into "no". A missing answer counted as "would
            // not fix" would manufacture evidence that practitioners ignore findings nobody asked them
            // about — and the more raters skipped it, the stronger that false signal would get.
            notAsked = measured.Count - asked.Count,

            wouldFix = fix.Count(a => a.WouldFix == true),
            wantInReport = report.Count(a => a.WantInReport == true),
            wouldFixRate = fix.Count > 0 ? (double?)fix.Count(a => a.WouldFix == true) / fix.Count : null,
            wantInReportRate = report.Count > 0
                ? (double?)report.Count(a => a.WantInReport == true) / report.Count
                : null,

            // ★★ WHERE THE LABEL AND THE BEHAVIOUR PART COMPANY. A finding called VALID that nobody would
            // fix is one the spec counts as a success and the practitioner would ignore; noise somebody
            // would fix anyway says the taxonomy is cutting in the wrong place. Merging the two figures
            // would destroy exactly this.
            validButWouldNotFix = measured.Count(a => !a.Verdict.IsNoise() && a.WouldFix == false),
            noiseButWouldFix = measured.Count(a => a.Verdict.IsNoise() && a.WouldFix == true),

            questions = new
            {
                wouldFix = BehaviouralQuestions.WouldFix,
                wantInReport = BehaviouralQuestions.WantInReport,
            },
            note = "Where a verdict and the behaviour disagree, the spec and the practitioner have parted "
                 + "company — that gap is what this layer is for. "
                 + BehaviouralQuestions.RelationToTheRate,
        };
    }

    private static object Composition(CrowdRound round, IReadOnlyList<CrowdAnswer> measured)
    {
        var c = CrowdStratification.Summarise([.. measured], [.. round.Strata.Values]);
        return new
        {
            answers = c.Answers,
            independent = c.Independent,
            vendorAffiliated = c.VendorAffiliated,

            // ★★ Counted apart. A cohort granted a paid tier for answering is compensated by the
            // vendor, and counting them as independent would let a vendor manufacture its own
            // independent bucket — the most valuable number on the page and the cheapest to fake.
            compensated = c.Compensated,

            // ★ Undeclared is its own bucket. Counting it as independence lets the most
            // interesting bias in the pool hide in a default.
            undeclared = c.Undeclared,
            largestLanguage = c.LargestLanguage,
            largestLanguageShare = c.LargestLanguageShare,
            dominated = c.Dominated,
            byLanguage = c.ByLanguage,
        };
    }

    private static object Slice(
        CrowdRound round, Dictionary<string, CrowdReason> byFinding, IReadOnlyList<CrowdAnswer> measured,
        CrowdReason reason)
    {
        var queued = round.Queue.Count(i =>
            i.Reason == reason && !round.Honeypots.ContainsKey(i.FindingId));
        var answers = measured
            .Where(a => byFinding.TryGetValue(a.FindingId, out var r) && r == reason)
            .ToList();

        return new
        {
            queued,
            answered = answers.Count,

            // ★ A contradiction is the whole point of the spot-check: four models agreed, and a
            // person outside the family says otherwise.
            contradicted = answers.Count(a =>
                a.MachineVerdict is { } m && a.Verdict.IsNoise() != m.IsNoise()),

            // ★ Answers with no machine verdict to compare against are counted HERE rather than
            // as agreement — otherwise omitting one field hides every disagreement.
            notComparable = answers.Count(a => a.MachineVerdict is null),
        };
    }
}
