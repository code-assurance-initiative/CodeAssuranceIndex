using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// §3 of the sheet: every language the reading can speak for.
/// </summary>
/// <remarks>
/// <para>★★ AFFECTED IS TAKEN OVER MEASURABLE, NEVER OVER SURVEYS — in every row, not only in the
/// headline. A language that resolves few of its dependencies draws a shorter bar because less was looked
/// at, not because less is there, which is why the measurable count sits in a column of its own.</para>
/// <para>★★ AND A SUBJECT WITH NO LANGUAGE IS NEVER A FIELD CALLED "UNKNOWN". A bucket of subjects whose
/// language nobody could read is a fact about the metadata, not a field of software engineering. It is
/// counted — the sheet says how much of the reading could be placed in a language at all — and never given
/// a row or a page.</para>
/// </remarks>
public sealed class CorpusLanguageCutTests
{
    /// <summary>★ The same one-function rule the field guides keep: no link without a page.</summary>
    [Fact]
    public void A_language_below_the_gate_gets_no_page_and_no_link()
    {
        var cuts = CorpusCuts.ByLanguage(Records(("go", 4), ("csharp", 5)), TakenAt);

        var go = cuts.Single(c => c.Key == "go");
        var csharp = cuts.Single(c => c.Key == "csharp");

        Assert.Null(go.Href);
        Assert.Equal("/state-of-the-corpus/language/csharp/", csharp.Href);
    }

    [Fact]
    public void Affected_is_taken_over_measurable_in_every_language_row()
    {
        // Six Go subjects: four resolvable, two of those affected. The row reads 2 of 4, never 2 of 6.
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("go", 2, Measured(affected: true)));
        records.AddRange(Subjects("go", 2, Measured(affected: false)));
        records.AddRange(Subjects("go", 2, new SecurityReading { VulnMeasurable = false }));

        var cut = Assert.Single(CorpusCuts.ByLanguage(records, TakenAt));

        Assert.Equal(6, cut.Codebases);
        Assert.Equal(4, cut.Reading.VulnMeasurable);
        Assert.Contains("2 of 4", cut.Reading.VulnAffectedShare!.Headline(), StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ A row at zero reads as "none affected", which is exactly the substitution §1 exists to prevent,
    /// one table further in. A language with nothing resolvable draws no bar and says why.
    /// </summary>
    [Fact]
    public void A_language_with_nothing_resolvable_draws_no_bar_and_says_why()
    {
        var records = Subjects("rust", 5, new SecurityReading { VulnMeasurable = false, VulnScanFailed = true });

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        Assert.Contains("nothing a scanner could resolve", json, StringComparison.Ordinal);
    }

    /// <summary>★ Largest first — the order a reader scans, and the only ordering here that is not a judgement.</summary>
    [Fact]
    public void The_rows_are_ordered_by_size_not_by_how_badly_they_come_out()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 9, Measured(affected: true)));
        records.AddRange(Subjects("go", 6, Measured(affected: false)));

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        var csharp = json.IndexOf("C#", StringComparison.Ordinal);
        var go = json.IndexOf("Go", StringComparison.Ordinal);
        Assert.True(csharp >= 0 && go > csharp, "the larger field is listed first");
    }

    [Fact]
    public void The_sheet_says_how_much_of_the_reading_could_be_placed_in_a_language_at_all()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 6, Measured(affected: false)));
        records.AddRange(Subjects(null, 4, Measured(affected: false)));

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        Assert.Contains("could be placed in a language at all", json, StringComparison.Ordinal);
        Assert.Contains("6 of 10", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_subject_with_no_language_is_never_published_as_a_field_called_Unknown()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Subjects("csharp", 6, Measured(affected: false)));
        records.AddRange(Subjects(null, 9, Measured(affected: false)));

        Assert.DoesNotContain(CorpusCuts.ByLanguage(records, TakenAt), c => c.Key.Length == 0);
        Assert.DoesNotContain("Unknown", Json(CorpusSheetBuilder.Build(records, TakenAt)), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static SecurityReading Measured(bool affected) => new()
    {
        VulnMeasurable = true,
        VulnAffected = affected,
        Findings = affected ? 1 : 0,
        DisclosureMeasured = true,
    };

    private static List<SurveyRecord> Records(params (string Language, int Count)[] fields) =>
        [.. fields.SelectMany(f => Subjects(f.Language, f.Count, Measured(affected: false)))];

    private static List<SurveyRecord> Subjects(string? language, int count, SecurityReading reading) =>
        [.. Enumerable.Range(0, count).Select(_ => Record(language, reading))];

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return PageText.Json(page);
    }

    private static SurveyRecord Record(string? language, SecurityReading reading)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_lang_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = $"acme/repo-{ordinal}",
                Host = "github.com",
                Languages = language is null ? null : new SubjectLanguages { Primary = language },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
            Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                ProductionLoc = 1000,
                SecurityReading = reading,
            },
        }]);
    }
}
