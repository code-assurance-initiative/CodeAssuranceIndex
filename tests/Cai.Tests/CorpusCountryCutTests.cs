using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// §4 of the sheet: every country the reading can speak for.
/// </summary>
/// <remarks>
/// <para>★★ THE CAVEAT IS NOT OPTIONAL HERE, WHICH IS THE ONE WAY §4 IS NOT §3. A language is read off the
/// code by the pass that measured everything else — nobody declares it and no account can decline to. A
/// country is a claim an account writes about ITSELF in a free-text profile line, it places an OWNER rather
/// than a project or a legal entity, and most owners cannot be placed at all. A table of countries with no
/// caveat beside it is read as a map of open source, which is a claim this reading cannot make.</para>
/// <para>★★ AND THE TWO UNITS STAY APART. One prolific account can put forty codebases in a country with
/// three people in it, and a codebase count presented as a headcount is the commonest way a corpus map
/// flatters a small, busy account.</para>
/// </remarks>
public sealed class CorpusCountryCutTests
{
    [Fact]
    public void A_country_below_the_gate_gets_no_page_and_no_link()
    {
        var cuts = CorpusCuts.ByCountry(Corpus(("Denmark", 10, 10), ("Norway", 9, 9)), TakenAt);

        Assert.Equal("/state-of-the-corpus/country/denmark/", cuts.Single(c => c.Key == "Denmark").Href);
        Assert.Null(cuts.Single(c => c.Key == "Norway").Href);
    }

    /// <summary>★ A country nobody resolved is absent, never a row called "unknown".</summary>
    [Fact]
    public void A_codebase_whose_owner_could_not_be_placed_is_in_no_country_row()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Codebases("Denmark", 10, owners: 10));
        records.AddRange(Codebases(null, 6, owners: 6));

        var cuts = CorpusCuts.ByCountry(records, TakenAt);

        Assert.Equal(["Denmark"], cuts.Select(c => c.Key));
        Assert.DoesNotContain("unknown", Json(CorpusSheetBuilder.Build(records, TakenAt)), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>★★ Codebases and accounts are counted separately because neither converts into the other.</summary>
    [Fact]
    public void Codebases_and_owners_are_counted_separately()
    {
        // Forty codebases held by three accounts.
        var records = Codebases("Denmark", 40, owners: 3);

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        // ★ A COLUMN CELL CARRIES ONLY ITS VALUE — the column HEADING names the population, deliberately,
        //   because a population repeated in every cell is the wall that made these tables unreadable. So
        //   the place the two units are told apart IN WORDS is the section's own caveat, and that is what
        //   this test reads. The first draft asserted on the cell and was asserting against the design.
        Assert.Contains("Codebases and accounts are counted separately", json, StringComparison.Ordinal);
        Assert.Contains("40 measured codebases", json, StringComparison.Ordinal);
        Assert.Contains("3 accounts that own them", json, StringComparison.Ordinal);
        // And the two units head two separate columns. The headings live inside a widget prop, so they
        // arrive escaped one level deeper than the page's own text — flattened here rather than matched
        // against an escape sequence, which is a property of the serializer and not of the page.
        var flat = json.Replace("\\\"", "\"", StringComparison.Ordinal);
        Assert.Contains("[\"Codebases\",\"Owners\"", flat, StringComparison.Ordinal);
    }

    [Fact]
    public void The_section_says_how_little_of_the_corpus_could_be_placed_at_all()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Codebases("Denmark", 10, owners: 10));
        records.AddRange(Codebases(null, 30, owners: 30));

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        Assert.Contains("could be placed in a country at all", json, StringComparison.Ordinal);
        Assert.Contains("10 of 40", json, StringComparison.Ordinal);
        Assert.Contains("places an owner", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ Dropping the countries under the gate silently would publish a table saying "these are the
    /// countries in the corpus". The count of what was held back is what stops it saying that.
    /// </summary>
    [Fact]
    public void The_countries_held_back_by_the_gate_are_counted_rather_than_dropped_silently()
    {
        var records = new List<SurveyRecord>();
        records.AddRange(Codebases("Denmark", 10, owners: 10));
        records.AddRange(Codebases("Norway", 4, owners: 4));
        records.AddRange(Codebases("Sweden", 4, owners: 4));

        var json = Json(CorpusSheetBuilder.Build(records, TakenAt));

        Assert.Contains("have no page", json, StringComparison.Ordinal);
        Assert.Contains("2 of 3", json, StringComparison.Ordinal);
    }

    /// <summary>★ No section at all when no country clears the gate — a heading over nothing is not a section.</summary>
    [Fact]
    public void A_reading_with_no_country_above_the_gate_publishes_no_country_section()
    {
        var json = Json(CorpusSheetBuilder.Build(Codebases("Norway", 4, owners: 4), TakenAt));

        Assert.DoesNotContain("§4 By country", json, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static int _ordinal;

    private static List<SurveyRecord> Corpus(params (string Country, int Codebases, int Owners)[] countries) =>
        [.. countries.SelectMany(c => Codebases(c.Country, c.Codebases, c.Owners))];

    private static List<SurveyRecord> Codebases(string? country, int count, int owners) =>
        [.. Enumerable.Range(0, count).Select(i => Record(country, $"owner-{country ?? "none"}-{i % owners}"))];

    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return PageText.Json(page);
    }

    private static SurveyRecord Record(string? country, string owner)
    {
        var ordinal = Interlocked.Increment(ref _ordinal);
        return SurveyRecord.From([new DeliveryPayload
        {
            DeliveryId = $"cd_country_{ordinal}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = $"{owner}/repo-{ordinal}",
                Host = "github.com",
                Origin = country is null
                    ? new SubjectOrigin { Declared = false, UnresolvedReason = "blank" }
                    : new SubjectOrigin { Country = country, Declared = true, ResolvedBy = "countryName" },
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
    }
}
