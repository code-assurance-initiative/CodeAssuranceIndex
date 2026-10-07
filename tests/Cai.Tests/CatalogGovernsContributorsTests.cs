using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// What a rubric catalog decides about the CONTRIBUTORS of a fold — which of them count, which of them may appear at
/// all, and which quality-bar group each lens follows — instead of leaving it to the producer of the evidence.
/// </summary>
/// <remarks>
/// <para>★★ THE STANDARD SAYS ADVISORY READINGS NEVER MOVE THE NUMBER; THE SCORER ASKED THE BUNDLE. Whether a dimension
/// was left out of the fold was read from the <c>advisory</c> flag the producer wrote, so an engine that omitted the
/// flag folded a language-model reading into the score — and one that set it on a deterministic dimension it scored
/// badly removed that dimension from the score. Both are the producer choosing its own number. A catalog that declares
/// <c>advisory</c> is now the authority, and a bundle may not call advisory what the catalog scores.</para>
/// <para>★ An implementation built from the published text found this, together with the two below: unknown ids were
/// folded as if the rubric defined them, and the lens → quality-bar group map lived only inside this scorer.</para>
/// <para>Every rule is opt-in per catalog. A catalog that declares none of the new fields folds exactly as before, so
/// every published rubric version keeps verifying to the number it was published with (ADR-0004).</para>
/// </remarks>
public sealed class CatalogGovernsContributorsTests
{
    private const string Version = "rubric-test";

    private static EvidenceBundle Bundle(params DimensionScore[] extra) => new()
    {
        RubricVersion = Version,
        QualityBar = "production",
        AnalyzableProjects = 3,
        ProductionLoc = 4000,
        Dimensions =
        [
            new DimensionScore("D1", "code-quality", 8.0, 0.95),
            new DimensionScore("D5", "architecture", 8.0, 0.95),
            new DimensionScore("D9", "testing", 8.0, 0.90),
            new DimensionScore("D30", "security-compliance", 8.0, 0.90),
            .. extra,
        ],
    };

    private static CatalogDimension Dim(string id, string lens, string category, bool? advisory = null, string evaluator = "tool") =>
        new() { Id = id, Lens = lens, Category = category, Family = "dimension", Evaluator = evaluator, Advisory = advisory };

    private static CatalogDimension Meta(string id, string lens, bool? advisory = null, string evaluator = "tool") =>
        new() { Id = id, Lens = lens, Family = "meta", Evaluator = evaluator, Advisory = advisory };

    /// <summary>A catalog in the new shape: every contributor declares whether it is advisory, and the catalog is
    /// closed — it lists every id a bundle may carry.</summary>
    private static RubricCatalog Declaring(params CatalogLens[] lenses) => new()
    {
        RubricVersion = Version,
        RejectUnknownContributors = true,
        Lenses = lenses,
        Dimensions =
        [
            Dim("D1", "codeHealth", "code-quality", advisory: false),
            Dim("D5", "architecture", "architecture", advisory: false),
            Dim("D9", "productionReadiness", "testing", advisory: false),
            Dim("D30", "securityCompliance", "security-compliance", advisory: false),
            Dim("D19", "maturity", "docs", advisory: true, evaluator: "llm"),
            Dim("D20", "maturity", "docs", advisory: false),
            Meta("M4", "maturity", advisory: true, evaluator: "llm"),
            Meta("SC1", "securityCompliance", advisory: true),
            Meta("R1", "codeHealth", advisory: false),
        ],
    };

    /// <summary>A catalog in the shape every rubric was published in until now: no advisory, not closed.</summary>
    private static RubricCatalog Legacy() => new()
    {
        RubricVersion = Version,
        Dimensions =
        [
            Dim("D1", "codeHealth", "code-quality"),
            Dim("D5", "architecture", "architecture"),
            Dim("D9", "productionReadiness", "testing"),
            Dim("D30", "securityCompliance", "security-compliance"),
            Dim("D19", "maturity", "docs", evaluator: "llm"),
        ],
    };

    // ── Advisory ────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_dimension_the_catalog_declares_advisory_stays_out_of_the_number_when_the_bundle_omits_the_flag()
    {
        var without = CaiScorer.Score(Bundle(), Declaring()).Headline;
        var withUnflaggedReading = CaiScorer.Score(Bundle(new DimensionScore("D19", "docs", 0.5, 0.9)), Declaring()).Headline;

        Assert.Equal(without, withUnflaggedReading, 10);
    }

    [Fact]
    public void A_meta_dimension_the_catalog_declares_advisory_stays_out_of_the_number_when_the_bundle_omits_the_flag()
    {
        var bundle = Bundle();
        var withMetas = bundle with
        {
            MetaDimensions = [new MetaDimensionScore("M4", "maturity", 0.5), new MetaDimensionScore("SC1", "securityCompliance", 0.5)],
        };

        Assert.Equal(CaiScorer.Score(bundle, Declaring()).Headline, CaiScorer.Score(withMetas, Declaring()).Headline, 10);
    }

    [Fact]
    public void A_bundle_may_not_call_advisory_a_dimension_the_catalog_scores()
    {
        // The other half of the loophole: marking a badly-scored deterministic dimension advisory removed it.
        var hiding = Bundle(new DimensionScore("D20", "docs", 0.5, 0.9) { Advisory = true });

        var e = Assert.Throws<ArgumentException>(() => CaiScorer.Score(hiding, Declaring()));
        Assert.Contains("D20", e.Message, StringComparison.Ordinal);
        Assert.Contains("advisory", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_catalog_that_declares_no_advisory_still_takes_the_bundles_flag()
    {
        // The compatibility half: every rubric published before the field folds exactly as it did.
        var unflagged = CaiScorer.Score(Bundle(new DimensionScore("D19", "docs", 0.5, 0.9)), Legacy()).Headline;
        var flagged = CaiScorer.Score(Bundle(new DimensionScore("D19", "docs", 0.5, 0.9) { Advisory = true }), Legacy()).Headline;

        Assert.NotEqual(unflagged, flagged);
        Assert.Equal(CaiScorer.Score(Bundle(), Legacy()).Headline, flagged, 10);
    }

    // ── Closed catalogs ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_closed_catalog_refuses_a_dimension_id_it_does_not_define()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            CaiScorer.Score(Bundle(new DimensionScore("D999", "code-quality", 9.0, 0.9)), Declaring()));
        Assert.Contains("D999", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_catalog_refuses_a_meta_dimension_sent_as_a_dimension()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            CaiScorer.Score(Bundle(new DimensionScore("R1", "code-quality", 9.0, 0.9)), Declaring()));
        Assert.Contains("R1", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_catalog_refuses_a_meta_dimension_it_does_not_define()
    {
        var bundle = Bundle() with { MetaDimensions = [new MetaDimensionScore("Q9", "codeHealth", 9.0)] };

        var e = Assert.Throws<ArgumentException>(() => CaiScorer.Score(bundle, Declaring()));
        Assert.Contains("Q9", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_catalog_refuses_a_meta_dimension_in_another_lens_than_it_defines()
    {
        var bundle = Bundle() with { MetaDimensions = [new MetaDimensionScore("R1", "architecture", 9.0)] };

        var e = Assert.Throws<ArgumentException>(() => CaiScorer.Score(bundle, Declaring()));
        Assert.Contains("R1", e.Message, StringComparison.Ordinal);
        Assert.Contains("architecture", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_catalog_that_is_not_closed_still_folds_an_id_it_does_not_define()
    {
        var score = CaiScorer.Score(Bundle(new DimensionScore("D999", "code-quality", 9.0, 0.9)), Legacy());

        Assert.InRange(score.Headline, 0, 100);
    }

    // ── Lens groups ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_catalog_that_declares_a_lens_group_moves_that_lenss_band_lines()
    {
        // Under a prototype bar, an operational lens follows the bar fully and a foundational one only partly, so the
        // same score can read differently: 40 is Adequate on the operational lines (prototype: 32 / 52) and Weak on the
        // foundational ones (42.8 / 62.8). Declaring the group in the catalog must be what decides it.
        var bundle = Bundle() with { QualityBar = "prototype" };
        var asBuiltIn = Declaring(new CatalogLens("productionReadiness", "Production Readiness"));
        var asFoundational = Declaring(new CatalogLens("productionReadiness", "Production Readiness") { Group = "foundational" });

        var tested = bundle with { Dimensions = [.. bundle.Dimensions.Where(d => d.Id != "D9"), new DimensionScore("D9", "testing", 4.0, 0.9)] };
        var builtIn = CaiScorer.Score(tested, asBuiltIn).Lenses.Single(l => l.Lens == "productionReadiness");
        var declared = CaiScorer.Score(tested, asFoundational).Lenses.Single(l => l.Lens == "productionReadiness");

        Assert.Equal(builtIn.Score, declared.Score, 10);
        Assert.NotEqual(builtIn.Band, declared.Band);
    }

    [Fact]
    public void A_catalog_that_declares_no_lens_group_uses_the_groups_the_scorer_has_always_used()
    {
        var bundle = Bundle() with { QualityBar = "prototype" };
        var explicitDefaults = Declaring(
            new CatalogLens("codeHealth", "Code Health") { Group = "foundational" },
            new CatalogLens("architecture", "Architecture") { Group = "foundational" },
            new CatalogLens("productionReadiness", "Production Readiness") { Group = "operational" },
            new CatalogLens("securityCompliance", "Security & Compliance") { Group = "safety" });

        var a = CaiScorer.Score(bundle, Declaring()).Lenses.Select(l => (l.Lens, l.Band));
        var b = CaiScorer.Score(bundle, explicitDefaults).Lenses.Select(l => (l.Lens, l.Band));

        Assert.Equal(a, b);
    }

    [Fact]
    public void A_catalog_that_declares_a_lens_group_this_scorer_does_not_implement_is_refused()
    {
        var catalog = Declaring(new CatalogLens("codeHealth", "Code Health") { Group = "decorative" });

        var e = Assert.Throws<ArgumentException>(() => CaiScorer.Score(Bundle(), catalog));
        Assert.Contains("decorative", e.Message, StringComparison.Ordinal);
    }

    // ── The wire ────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_new_fields_round_trip_and_a_catalog_without_them_gains_none()
    {
        var declared = Declaring(new CatalogLens("codeHealth", "Code Health") { Group = "foundational" });
        Assert.Equal(declared.ToJson(), RubricCatalog.Parse(declared.ToJson()).ToJson());

        var legacyJson = Legacy().ToJson();
        Assert.DoesNotContain("\"advisory\"", legacyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"group\"", legacyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("rejectUnknownContributors", legacyJson, StringComparison.Ordinal);
    }
}
