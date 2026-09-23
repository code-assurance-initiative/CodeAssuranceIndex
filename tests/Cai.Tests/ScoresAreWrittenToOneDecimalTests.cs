using Cai.Delivery;
using Cai.Pages;
using Cai.Pages.Publishing;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A CAI score is published to one decimal, on every surface that publishes one.
/// </summary>
/// <remarks>
/// <para>★★ THE SAME MEDIAN WAS WRITTEN TWO WAYS ON TWO PAGES. A group page states "median CAI across
/// them" through <see cref="PageProse.Score"/>, which is always one decimal; the sheet's §3 and §4 rows
/// state the same number through <see cref="Figure.Scalar"/>, whose format is "one decimal AT MOST"
/// because the same factory also carries totals. So a language whose median lands on a whole number read
/// <c>66</c> in the table and <c>66.0</c> on the page it links to, in a column beside <c>64.4</c> and
/// <c>49.3</c>. Nothing was wrong with the number — only with how many of its digits a reader was shown,
/// which is exactly the class of defect no assertion on a figure can see.</para>
///
/// <para>★ Found at 390px, reading the corpus sheet's reflowed §3 — where the medians stack under their
/// own labels and an odd one out is obvious in a way it is not in a wide table.</para>
///
/// <para>★★ A TOTAL IS NOT A SCORE and must not be dragged along: "63,266 findings" does not become
/// "63,266.0". The two live in different factories for that reason, and the last test here holds it.</para>
/// </remarks>
public sealed class ScoresAreWrittenToOneDecimalTests
{
    /// <summary>★★ The sheet's §3 row, which is where the odd spelling was seen.</summary>
    [Fact]
    public void A_language_median_that_lands_on_a_whole_number_keeps_its_decimal()
    {
        var json = PageText.Json(CorpusSheetBuilder.Build(Corpus(), TakenAt));

        // ★ A table cell, as the island receives it. The row props are an escaped JSON string inside the
        //   node tree, so the quotes a cell is written with are backslashed here — assert on what is
        //   actually published rather than on the shape the value has in C#.
        Assert.Contains("\\\"70.0\\\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\\"70\\\"", json, StringComparison.Ordinal);
    }

    /// <summary>★ And the country fold, which builds its row the same way.</summary>
    [Fact]
    public void A_country_median_that_lands_on_a_whole_number_keeps_its_decimal()
    {
        var json = PageText.Json(CorpusSheetBuilder.Build(Corpus(), TakenAt));
        var page = PageText.Json(CorpusGroupPages.Build(Corpus(), TakenAt)
            .Single(p => p.Path == "state-of-the-corpus/country/denmark"));

        // The two surfaces state one median. They must spell it the same way.
        Assert.Contains("70.0", page, StringComparison.Ordinal);
        Assert.Contains("\\\"70.0\\\"", json, StringComparison.Ordinal);
    }

    /// <summary>★ The corpus-wide medians on the surveys index, stated beside per-language rows that use PageProse.Score.</summary>
    [Fact]
    public void The_corpus_medians_on_the_index_keep_their_decimal()
    {
        var json = PageText.Json(SurveyIndexBuilder.Build(Corpus(), TakenAt));

        // ★ Printed rather than asserted-around: the index states the corpus median twice, one vote per
        //   language and one per codebase, and a bare Contains("70.0") is satisfied by the rows beneath
        //   them — which already use PageProse.Score and were never the defect.
        System.Console.WriteLine(json);
        Assert.Contains("70.0 across", json, StringComparison.Ordinal);
        Assert.DoesNotContain("70 across", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ AND THE SAME DEFECT ON THE SCORE ITSELF, on the page a reader reaches first. A portrait's
    /// headline, the band-scale marker beside it and the "Where N sits on the scale" heading all went
    /// through a private "0.#" formatter, so a codebase scoring exactly 54 published <c>54</c> where
    /// every other surface — its own lens gauges two sections below, the field guide it belongs to,
    /// the index that lists it — says <c>54.0</c>.
    /// <para>★ Invisible until a whole-numbered score was rendered: the seed gave every subject a
    /// random score with a decimal. Found by teaching the harness to publish a subject measured ONCE,
    /// which is the most common portrait in production and had never been drawn.</para>
    /// </summary>
    [Fact]
    public void A_portrait_of_a_whole_numbered_score_keeps_its_decimal()
    {
        var json = PageText.Json(SurveyPageBuilder.Build(SurveyRecord.From([Scored(54)])));

        Assert.Contains("54.0", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Where 54 sits", json, StringComparison.Ordinal);
        Assert.DoesNotContain("scored 54 on", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\\"54\\\"", json, StringComparison.Ordinal);
    }

    /// <summary>★ The other half: a score that already has a decimal is untouched.</summary>
    [Fact]
    public void A_portrait_of_a_fractional_score_is_unchanged()
    {
        var json = PageText.Json(SurveyPageBuilder.Build(SurveyRecord.From([Scored(78.2)])));

        Assert.Contains("Where 78.2 sits", json, StringComparison.Ordinal);
        Assert.DoesNotContain("78.20", json, StringComparison.Ordinal);
    }

    /// <summary>★★ The other half: a TOTAL keeps its own spelling and gains no decimal.</summary>
    [Fact]
    public void A_total_is_not_dragged_along_with_the_scores()
    {
        var total = Figure.Scalar(63266, 1888, Basis.Of("survey", "surveys"), TakenAt);

        Assert.StartsWith("63,266 across", total.Headline(), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    /// <summary>One delivery at a given score — the shape of a repository's first survey.</summary>
    private static DeliveryPayload Scored(double cai) => new()
    {
        DeliveryId = $"cd_score_page_{Interlocked.Increment(ref _ordinal)}",
        IssuedAt = "2026-09-01T10:00:00Z",
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject { Repository = "acme/once", Host = "github.com", Commit = "3f9a1c2" },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
        Measurement = new DeliveryMeasurement { ScannedAt = "2026-09-01T10:00:00Z", ProductionLoc = 9000 },
        Verdict = new DeliveryVerdict { Cai = cai, Band = "Adequate" },
        Evidence = new EvidenceBundle { RubricVersion = "rubric-2026.08.15", ProductionLoc = 9000 },
    };

    /// <summary>Twelve codebases, one language, one country, every score 70 — so every median is 70 exactly.</summary>
    private static List<SurveyRecord> Corpus() =>
    [
        .. Enumerable.Range(0, 12).Select(_ =>
        {
            var ordinal = Interlocked.Increment(ref _ordinal);
            return SurveyRecord.From([new DeliveryPayload
            {
                DeliveryId = $"cd_score_{ordinal}",
                IssuedAt = "2026-09-01T10:00:00Z",
                RubricVersion = "rubric-2026.08.15",
                Subject = new DeliverySubject
                {
                    Repository = $"acme/score-{ordinal}",
                    Host = "github.com",
                    Languages = new SubjectLanguages { Primary = "csharp" },
                    Origin = new SubjectOrigin { Country = "Denmark", Declared = true, ResolvedBy = "countryName" },
                },
                Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
                Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
                Evidence = new EvidenceBundle
                {
                    RubricVersion = "rubric-2026.08.15",
                    ProductionLoc = 1000,
                    SecurityReading = new SecurityReading { VulnMeasurable = true, DisclosureMeasured = true },
                },
            }]);
        }),
    ];
}
