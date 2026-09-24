using System.Text.Json;
using Cai.Delivery;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The prose a survey is made of travels in the delivery, or the standard cannot publish it.
/// </summary>
/// <remarks>
/// <para>★★ THE PRODUCER WROTE IT AND NEVER SENT IT. Every run bundle on the producer's disk holds a
/// <c>changelog.md</c> — 19,835 of 20,013 of them — and about half hold a <c>system-overview.md</c>,
/// a description of what the system IS, written from the codebase's whole history. The producer's own
/// public survey showed both; the standard's survey page shows neither, because MINOR 1.1 has nowhere
/// to put prose. Nothing is dropping it — it was never picked up.</para>
///
/// <para>★★ SO IT GOES IN THE ENVELOPE, NOT BESIDE IT. A page composed from a URL the producer serves
/// would be the standard quoting a number it cannot check — the whole reason composition moved here.
/// Prose in the signed package is prose a reader can verify came from the measurement it claims to
/// describe, by the same signature that covers the score.</para>
///
/// <para>★★ AND IT IS ABSENT, NEVER EMPTY, WHEN A RUN WAS NOT NARRATED. Narration needs the model
/// route, and roughly half the corpus was scanned without it — a survey that reports empty prose says
/// "there is nothing to say about this system", which is a claim about the codebase rather than about
/// the scan.</para>
/// </remarks>
public sealed class NarrationRidesInTheDeliveryTests
{
    [Fact]
    public void A_delivery_can_carry_the_changelog_and_the_system_overview()
    {
        var payload = new DeliveryPayload
        {
            Narration = new DeliveryNarration
            {
                Changelog = "## Score\n\n- CAI 47 → 48 (+0.8)",
                SystemOverview = "# System overview\n\nDart Frog is a web framework…",
            },
        };

        var json = JsonSerializer.Serialize(payload);
        using var round = JsonDocument.Parse(json);
        var narration = round.RootElement.GetProperty("narration");

        Assert.Equal("## Score\n\n- CAI 47 → 48 (+0.8)", narration.GetProperty("changelog").GetString());
        Assert.StartsWith("# System overview", narration.GetProperty("systemOverview").GetString(), StringComparison.Ordinal);
    }

    /// <summary>★★ A run that was never narrated carries NO narration block at all.</summary>
    [Fact]
    public void A_delivery_without_narration_says_nothing_rather_than_nothing_much()
    {
        var json = JsonSerializer.Serialize(new DeliveryPayload());

        Assert.DoesNotContain("narration", json, StringComparison.Ordinal);
    }

    /// <summary>★ And a half-narrated run carries the half it has.</summary>
    [Fact]
    public void A_changelog_without_an_overview_is_carried_on_its_own()
    {
        var json = JsonSerializer.Serialize(new DeliveryPayload
        {
            Narration = new DeliveryNarration { Changelog = "## Score" },
        });

        Assert.Contains("changelog", json, StringComparison.Ordinal);
        Assert.DoesNotContain("systemOverview", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE SCHEMA IS THE GATE, NOT THE RECORD. The registry validates every package against the
    /// embedded JSON Schema before it stores it, so a field added to the C# type and not to the schema
    /// is dead on the wire — which is exactly how `subject.languages` was lost once already.
    /// </summary>
    [Fact]
    public void The_schema_describes_the_narration_block()
    {
        var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath()));
        var payload = schema.RootElement.GetProperty("$defs").GetProperty("payload");

        Assert.True(payload.GetProperty("properties").TryGetProperty("narration", out var narration),
            "the payload must describe its narration block");
        var properties = narration.GetProperty("properties");
        Assert.True(properties.TryGetProperty("changelog", out _));
        Assert.True(properties.TryGetProperty("systemOverview", out _));
    }

    /// <summary>★ Adding a field additively is a MINOR bump, and the version says so.</summary>
    [Fact]
    public void The_format_version_moved_to_one_point_two()
    {
        Assert.Equal("1.2", DeliverySchema.Current);
        Assert.Equal(1, DeliverySchema.SupportedMajor);
    }

    private static string SchemaPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "schemas")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "schemas", "cai-delivery-1.0.schema.json");
    }
}
