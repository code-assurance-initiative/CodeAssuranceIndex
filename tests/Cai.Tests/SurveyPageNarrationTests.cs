using System.Text.Encodings.Web;
using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// What this system IS, and how it got here — the two documents the producer writes for every narrated
/// run and, until delivery MINOR 1.2, had nowhere in the package to put.
/// </summary>
/// <remarks>
/// <para>★★ THE STANDARD'S SURVEY SHOWED NEITHER WHILE THE PRODUCT'S SHOWED BOTH. A page that carries a
/// score, eight lens gauges and a trend line, and cannot say what the codebase DOES, is a number about a
/// stranger. The prose travels inside the signed payload, so the same signature that covers the score
/// covers the description — prose a reader cannot check against the measurement it describes would be the
/// standard repeating a claim instead of publishing evidence.</para>
///
/// <para>★★ AND ABSENT RENDERS NOTHING. Narration needs a model route and about half of any large corpus
/// is scanned without one. A heading over a blank gap is the failure this page has already had once, and
/// it is indistinguishable from a broken renderer to everyone but the person who wrote it.</para>
/// </remarks>
public sealed class SurveyPageNarrationTests
{
    [Fact]
    public void The_portrait_says_what_the_system_is_and_how_it_got_here()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "## The shape of it\n\nA payments checkout API in C#.",
            Changelog = "- Duplication fell in the cart\n- Test coverage rose",
        }));

        Assert.Contains("What this system is", json, StringComparison.Ordinal);
        Assert.Contains("A payments checkout API in C#.", json, StringComparison.Ordinal);
        Assert.Contains("How this codebase got here", json, StringComparison.Ordinal);
        Assert.Contains("<li>Duplication fell in the cart</li>", json, StringComparison.Ordinal);
    }

    /// <summary>★★ NO HEADING OVER A BLANK GAP — the one failure mode this page has already had.</summary>
    [Fact]
    public void A_survey_that_was_never_narrated_renders_neither_heading()
    {
        foreach (var narration in new DeliveryNarration?[]
        {
            null,
            new DeliveryNarration(),
            new DeliveryNarration { SystemOverview = "   ", Changelog = "" },
        })
        {
            var json = Json(Page(narration));

            Assert.DoesNotContain("What this system is", json, StringComparison.Ordinal);
            Assert.DoesNotContain("How this codebase got here", json, StringComparison.Ordinal);
        }
    }

    /// <summary>★ Half a narration renders that half alone, and does not imply the other.</summary>
    [Fact]
    public void Half_a_narration_renders_that_half_alone()
    {
        var json = Json(Page(new DeliveryNarration { SystemOverview = "A payments API." }));

        Assert.Contains("What this system is", json, StringComparison.Ordinal);
        Assert.DoesNotContain("How this codebase got here", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ AFTER THE MEASUREMENT, BEFORE THE INVITATION. The page's claim is that it is evidence first:
    /// the numbers are the evidence, the prose explains them, and the invitation comes after both. Prose
    /// placed above the score would make the page an introduction to a project rather than a measurement
    /// of one.
    /// </summary>
    [Fact]
    public void The_prose_follows_the_measurement_and_precedes_the_invitation()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "A payments API.",
            Changelog = "- It moved",
        }));

        var lenses = json.IndexOf("cai-lens-gauges", StringComparison.Ordinal);
        var overview = json.IndexOf("What this system is", StringComparison.Ordinal);
        var changelog = json.IndexOf("How this codebase got here", StringComparison.Ordinal);
        var invite = json.IndexOf("Survey your own repository", StringComparison.Ordinal);

        Assert.True(lenses < overview, "the measurement is read before the prose about it");
        Assert.True(overview < changelog, "what it is comes before how it got here");
        Assert.True(changelog < invite, "an invitation before the evidence is an advert");
    }

    /// <summary>
    /// ★★ THE PROSE IS THE PRODUCER'S AND THE PAGE IS THE STANDARD'S. A delivery is a document from
    /// somebody else about a repository whose contents nobody here controls; markup inside it is TEXT.
    /// </summary>
    [Fact]
    public void Markup_in_the_producers_prose_reaches_the_page_as_text()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "<script>alert(1)</script>",
            Changelog = "- [x](javascript:alert(1))",
        }));

        Assert.DoesNotContain("<script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE LATEST FILING'S PROSE, NEVER AN OLDER ONE'S. A survey describes the reading it leads with;
    /// borrowing last quarter's description for this quarter's number would attribute prose to a
    /// measurement it was not written about, and no reader could tell.
    /// </summary>
    [Fact]
    public void An_older_readings_prose_is_not_borrowed_for_a_newer_one()
    {
        var record = SurveyRecord.From(
        [
            Reading("2026-05-02T09:00:00Z", 6.4, new DeliveryNarration { SystemOverview = "The old description." }),
            Reading("2026-07-01T10:32:04Z", 7.5, narration: null),
        ]);

        var json = Json(SurveyPageBuilder.Build(record));

        Assert.DoesNotContain("The old description.", json, StringComparison.Ordinal);
        Assert.DoesNotContain("What this system is", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE DOCUMENT'S OWN TITLE IS A STUTTER UNDER THE SECTION'S. Both documents open with a title —
    /// "# Health changelog", "## What service-0 is" — written for a file that arrives on its own. Under a
    /// section already headed "How this codebase got here" it renders as two headings in a row saying the
    /// same thing, which the first render showed plainly and no assertion about node counts would have.
    /// The section names the section; the document's leading title goes.
    /// </summary>
    [Fact]
    public void The_documents_own_title_does_not_repeat_the_sections()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "## What service-0 is\n\nA payments API.",
            Changelog = "# Health changelog\n\n- It moved",
        }));

        Assert.DoesNotContain("What service-0 is", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Health changelog", json, StringComparison.Ordinal);
        // ★ Only the LEADING one: a heading further down is a section of the document, not its title.
        Assert.Contains("A payments API.", json, StringComparison.Ordinal);
    }

    /// <summary>★ And a heading that is NOT first stays — "How it is put together" is part of the
    /// document, not a second name for it.</summary>
    [Fact]
    public void A_heading_inside_the_document_survives()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "It does payments.\n\n## How it is put together\n\n- A queue",
        }));

        Assert.Contains("How it is put together", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ WHOSE PROSE IT IS, SAID ON THE PAGE THAT PUBLISHES IT. Everything else on a survey is the
    /// standard's own fold of published evidence; this is a description WRITTEN BY THE PRODUCER'S model
    /// from the codebase's history, and a reader who cannot tell the two apart reads generated prose as
    /// the standard's judgement. The producer's own copy carried a note saying so — addressed to an
    /// internal reviewer, and stripped before it could be published — so the disclosure has to be made
    /// here, in the standard's words, or it is not made at all.
    /// </summary>
    [Fact]
    public void The_page_says_whose_prose_this_is()
    {
        var json = Json(Page(new DeliveryNarration
        {
            SystemOverview = "A payments API.",
            Changelog = "- It moved",
        }));

        Assert.Contains(Attribution, json, StringComparison.Ordinal);
        // ★ And it NAMES the producer, because "the producer" is not an attribution.
        Assert.Contains("watchdog.canine.dev", json, StringComparison.Ordinal);
    }

    /// <summary>★ And no note where there is no prose — a footnote under a section that is not there is
    /// the same blank-gap failure one line further down.</summary>
    [Fact]
    public void The_note_does_not_appear_without_the_prose()
    {
        var json = Json(Page(narration: null));

        Assert.DoesNotContain(Attribution, json, StringComparison.Ordinal);
    }

    /// <summary>The distinctive half of the note — the producer's NAME is on the page anyway, under
    /// "About this page", so asserting on that alone would pass with no note at all.</summary>
    private const string Attribution = "Written by";

    private static SurveyPage? Page(DeliveryNarration? narration) => SurveyPageBuilder.Build(SurveyRecord.From(
        [Reading("2026-05-02T09:00:00Z", 6.4, null), Reading("2026-07-01T10:32:04Z", 7.5, narration)]));

    /// <summary>
    /// The page as JSON, with angle brackets left alone.
    /// </summary>
    /// <remarks>
    /// ★★ THE DEFAULT ENCODER WOULD MAKE THE ESCAPING TEST PASS VACUOUSLY. System.Text.Json escapes
    /// <c>&lt;</c> to <c>\u003C</c> for HTML-embedding safety, so "the page does not contain
    /// &lt;script&gt;" is true of a page that contains nothing else. Relaxed here so the assertion reads
    /// the characters the CMS will be handed.
    /// </remarks>
    private static string Json(SurveyPage? page)
    {
        Assert.NotNull(page);
        return JsonSerializer.Serialize(
            page.Node, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    private static DeliveryPayload Reading(string issuedAt, double codeHealth, DeliveryNarration? narration) =>
        DeliveryTestHelp.Build(
            new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                Commit = "3f9a1c2",
                QualityBar = "production",
                AnalyzableProjects = 3,
                ProductionLoc = 35954,
                Dimensions =
                [
                    new DimensionScore("D1", "code-quality", codeHealth, 0.95),
                    new DimensionScore("D5", "architecture", 7.1, 0.95),
                    new DimensionScore("D9", "testing", 7.0, 0.85),
                    new DimensionScore("D30", "security", 7.6, 0.90),
                ],
            },
            new DeliveryBuildRequest
            {
                DeliveryId = $"cd_{issuedAt}",
                IssuedAt = issuedAt,
                Subject = new DeliverySubject { Repository = "acme/checkout-api", Commit = "3f9a1c2", Host = "github.com" },
                Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor", ScannerVersion = "3.1.0" },
                Narration = narration,
            });
}
