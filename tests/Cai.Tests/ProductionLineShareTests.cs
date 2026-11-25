using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Decision 0024 §5.4: an engine publishes a repository's CAI only where the languages it reads hold at least 95 % of
/// the repository's production lines, and states the share in the bundle's <c>productionLineShare</c>. The reference
/// scorer refuses a bundle without it, or under the minimum, when the named rubric declares
/// <c>minProductionLineShare</c>. A catalog that does not declare it folds as before, so every rubric published before
/// the field keeps verifying to its published number (ADR-0004).
/// </summary>
public sealed class ProductionLineShareTests
{
    private const string Version = "rubric-test";

    private static EvidenceBundle Bundle(double? share) => new()
    {
        RubricVersion = Version,
        QualityBar = "production",
        AnalyzableProjects = 3,
        ProductionLoc = 4000,
        ProductionLineShare = share is { } s
            ? new ProductionLineShare
            {
                Share = s,
                LinesByLanguage = new Dictionary<string, long> { ["C#"] = (long)Math.Round(s * 10_000), ["Ruby"] = 10_000 - (long)Math.Round(s * 10_000) },
                ReadLanguages = ["C#"],
                ListVersion = "1",
            }
            : null,
        Dimensions =
        [
            new DimensionScore("D1", "code-quality", 8.0, 0.95),
            new DimensionScore("D5", "architecture", 8.0, 0.95),
            new DimensionScore("D9", "testing", 8.0, 0.90),
            new DimensionScore("D30", "security-compliance", 8.0, 0.90),
        ],
    };

    private static RubricCatalog Catalog(double? minimum) => new()
    {
        RubricVersion = Version,
        MinProductionLineShare = minimum,
    };

    [Fact]
    public void A_rubric_that_declares_no_minimum_folds_a_bundle_without_a_share_as_before()
    {
        var withShare = CaiScorer.Score(Bundle(0.42), Catalog(null));
        var without = CaiScorer.Score(Bundle(null), Catalog(null));

        Assert.Equal(without.Headline, withShare.Headline);
    }

    [Fact]
    public void A_rubric_that_declares_the_minimum_refuses_a_bundle_that_states_no_share()
    {
        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(Bundle(null), Catalog(0.95)));
        Assert.Contains("productionLineShare", ex.Message);
    }

    [Theory]
    [InlineData(0.9499)]
    [InlineData(0.5)]
    [InlineData(0.0)]
    public void A_share_under_the_minimum_is_refused_not_scored(double share)
    {
        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(Bundle(share), Catalog(0.95)));
        Assert.Contains("95", ex.Message);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void A_share_outside_zero_to_one_is_refused(double share)
    {
        Assert.Throws<ArgumentException>(() => CaiScorer.Score(Bundle(share), Catalog(0.95)));
    }

    [Theory]
    [InlineData(0.95)]
    [InlineData(1.0)]
    public void A_share_at_or_over_the_minimum_folds_to_the_same_number_as_without_the_rule(double share)
    {
        var gated = CaiScorer.Score(Bundle(share), Catalog(0.95));
        var ungated = CaiScorer.Score(Bundle(share), Catalog(null));

        Assert.Equal(ungated.Headline, gated.Headline);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void A_share_that_names_no_list_version_is_refused(string? version)
    {
        var bundle = Bundle(0.97);
        var b = bundle with { ProductionLineShare = bundle.ProductionLineShare! with { ListVersion = version ?? "" } };

        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(b, Catalog(0.95)));
        Assert.Contains("listVersion", ex.Message);
    }

    [Fact]
    public void A_share_without_lines_per_language_is_refused()
    {
        var b = Bundle(0.97) with { ProductionLineShare = new ProductionLineShare { Share = 0.97, ListVersion = "1" } };

        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(b, Catalog(0.95)));
        Assert.Contains("linesByLanguage", ex.Message);
    }

    [Fact]
    public void A_share_its_own_lines_do_not_give_is_refused()
    {
        var bundle = Bundle(0.90);
        var claimed = bundle with { ProductionLineShare = bundle.ProductionLineShare! with { Share = 0.96 } };

        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(claimed, Catalog(0.95)));
        Assert.Contains("9000 of 10000", ex.Message);
    }

    [Fact]
    public void A_read_language_the_lines_do_not_list_is_refused()
    {
        var bundle = Bundle(0.97);
        var b = bundle with { ProductionLineShare = bundle.ProductionLineShare! with { ReadLanguages = ["C#", "Go"] } };

        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(b, Catalog(0.95)));
        Assert.Contains("Go", ex.Message);
    }

    [Fact]
    public void The_minimum_is_held_to_the_share_the_lines_give_not_to_a_rounded_statement()
    {
        // 9,496 of 10,000 lines read: 0.9496, which a producer could round to 0.950 within the tolerance.
        var b = Bundle(0.97) with
        {
            ProductionLineShare = new ProductionLineShare
            {
                Share = 0.950,
                LinesByLanguage = new Dictionary<string, long> { ["C#"] = 9_496, ["Ruby"] = 504 },
                ReadLanguages = ["C#"],
                ListVersion = "1",
            },
        };

        var ex = Assert.Throws<ArgumentException>(() => CaiScorer.Score(b, Catalog(0.95)));
        Assert.Contains("94.96", ex.Message);
    }

    [Fact]
    public void Language_names_match_exactly()
    {
        var bundle = Bundle(0.97);
        var b = bundle with { ProductionLineShare = bundle.ProductionLineShare! with { ReadLanguages = ["c#"] } };

        Assert.Throws<ArgumentException>(() => CaiScorer.Score(b, Catalog(0.95)));
    }

    [Fact]
    public void A_share_rounded_to_three_decimals_is_accepted()
    {
        var b = Bundle(0.97) with
        {
            ProductionLineShare = new ProductionLineShare
            {
                Share = 0.967,
                LinesByLanguage = new Dictionary<string, long> { ["Go"] = 2, ["Rust"] = 1, ["C"] = 27 },
                ReadLanguages = ["Go", "C"],
                ListVersion = "1",
            },
        };

        Assert.True(CaiScorer.Score(b, Catalog(0.95)).Headline > 0);
    }

    [Fact]
    public void The_share_and_the_minimum_round_trip_through_their_wire_names()
    {
        var bundle = EvidenceBundle.Parse("""{"rubricVersion":"rubric-test","productionLineShare":{"share":0.97,"linesByLanguage":{"Go":970,"Rust":30},"readLanguages":["Go"],"listVersion":"1"},"dimensions":[]}""");
        var catalog = RubricCatalog.Parse("""{"rubricVersion":"rubric-test","minProductionLineShare":0.95,"lenses":[],"dimensions":[]}""");

        Assert.Equal(0.97, bundle.ProductionLineShare!.Share);
        Assert.Equal(970, bundle.ProductionLineShare.LinesByLanguage["Go"]);
        Assert.Equal("1", bundle.ProductionLineShare.ListVersion);
        Assert.Equal(["Go"], bundle.ProductionLineShare.ReadLanguages);
        Assert.Equal(0.95, catalog.MinProductionLineShare);
    }
}
