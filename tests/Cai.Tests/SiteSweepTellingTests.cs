using Cai.Web.Registry;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Whether the standard's pages are still being published, told to somebody.
/// </summary>
/// <remarks>
/// <para>★★ THIS IS A SURFACE THAT WAS LOST AND HAD TO BE REBUILT, NOT A NEW IDEA. The producer's admin
/// console carried a corpus-publishing section asking whether the corpus PUBLISHED is the corpus measured.
/// For months the answer was no — 2,398 codebases published against 3,485 measured — and the sweep said so
/// in its own log every hour while nothing anywhere rendered it. A gate that fires and tells nobody is not a
/// gate. When the sweep moved here the probe could not move with it, so the telling is rebuilt on this side.</para>
/// <para>★★ AND A SWEEP THAT HAS NEVER RUN IS NOT A HEALTHY ONE. "Nothing has gone wrong yet" and "nothing
/// has happened" are the same silence, and only one of them is fine. An unswept publisher reports that it
/// has never swept rather than reporting nothing.</para>
/// </remarks>
public sealed class SiteSweepTellingTests
{
    [Fact]
    public void A_publisher_that_has_never_swept_says_so()
    {
        var memory = new SiteSweepMemory();

        Assert.Null(memory.Last);
        Assert.Contains("never", memory.Describe(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_sweep_is_remembered_with_what_it_did_and_when()
    {
        var memory = new SiteSweepMemory();
        var at = new DateTimeOffset(2026, 9, 22, 2, 0, 0, TimeSpan.Zero);

        memory.Record(new SiteSweep(Built: 23, Changed: 4, Withdrawn: 1, Failed: 0), at);

        var last = memory.Last;
        Assert.NotNull(last);
        Assert.Equal(23, last.Sweep.Built);
        Assert.Equal(at, last.At);
        Assert.Contains("23", memory.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ A SWEEP THAT FAILED PAGES IS NOT A SWEEP THAT WORKED. The counts are the whole point: a publisher
    /// that ran and could not push is indistinguishable, from the outside, from one that had nothing to do.
    /// </summary>
    [Fact]
    public void A_sweep_that_could_not_publish_some_pages_says_how_many()
    {
        var memory = new SiteSweepMemory();
        memory.Record(new SiteSweep(Built: 23, Changed: 0, Withdrawn: 0, Failed: 3), DateTimeOffset.UtcNow);

        Assert.Contains("3 failed", memory.Describe(), StringComparison.Ordinal);
    }

    /// <summary>★ The newest sweep is the one reported — a publisher is judged by its last pass.</summary>
    [Fact]
    public void The_latest_sweep_is_the_one_remembered()
    {
        var memory = new SiteSweepMemory();
        memory.Record(new SiteSweep(1, 1, 0, 0), new DateTimeOffset(2026, 9, 21, 2, 0, 0, TimeSpan.Zero));
        memory.Record(new SiteSweep(23, 4, 1, 0), new DateTimeOffset(2026, 9, 22, 2, 0, 0, TimeSpan.Zero));

        Assert.Equal(23, memory.Last!.Sweep.Built);
    }
}
