using Cai.Web.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The append-only record of corpus readings the trend line is drawn through.
/// </summary>
/// <remarks>
/// <para>★★ THE LINE IS READ OUT OF WHAT WAS RECORDED AT THE TIME AND IS NEVER RECOMPUTED. It is tempting
/// to say the standard is exempt — it holds every delivery ever, so it can re-fold "the corpus as of day
/// D" from the deliveries issued on or before D, reproducible by anyone holding the same packages. It is
/// not exempt: publication is a grant an owner org can WITHDRAW, so a codebase that was in the reading on
/// the day would silently drop out of a reading recomputed later. That is exactly the line that never
/// happened, and no reader could tell the difference.</para>
/// <para>★ A CORRECTION IS A NEW READING ON A NEW DAY, NEVER AN EDIT TO AN OLD ONE.</para>
/// </remarks>
public sealed class CorpusReadingStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cai-readings-{Guid.NewGuid():N}.db");

    [Fact]
    public void A_recorded_reading_is_never_rewritten()
    {
        var store = Store();
        Assert.True(store.RecordCorpusReading(Reading("2026-09-20T02:00:00Z", codebases: 3400, median: 52.6)));

        // The same instant arriving again — a re-run of the sweep, a retry, a second process.
        Assert.False(store.RecordCorpusReading(Reading("2026-09-20T02:00:00Z", codebases: 9999, median: 99.9)));

        var only = Assert.Single(store.ListCorpusReadings());
        Assert.Equal(3400, only.Codebases);
        Assert.Equal(52.6, only.MedianCai);
    }

    [Fact]
    public void Readings_come_back_in_the_order_they_were_taken()
    {
        var store = Store();
        store.RecordCorpusReading(Reading("2026-09-21T02:00:00Z", 3405, 52.7));
        store.RecordCorpusReading(Reading("2026-09-19T02:00:00Z", 3390, 52.5));
        store.RecordCorpusReading(Reading("2026-09-20T02:00:00Z", 3400, 52.6));

        Assert.Equal(
            ["2026-09-19T02:00:00Z", "2026-09-20T02:00:00Z", "2026-09-21T02:00:00Z"],
            store.ListCorpusReadings().Select(r => r.TakenAt));
    }

    /// <summary>
    /// ★★ THE REASON THE STORE EXISTS. A subject's publication being withdrawn changes what the NEXT
    /// reading will count and nothing about the readings already taken.
    /// </summary>
    [Fact]
    public void Withdrawing_a_subject_does_not_change_a_reading_already_recorded()
    {
        var store = Store();
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");
        store.RecordCorpusReading(Reading("2026-09-20T02:00:00Z", codebases: 3400, median: 52.6));

        store.WithdrawPublication("org-acme", "acme/checkout-api", "2026-09-21T08:00:00Z");

        var only = Assert.Single(store.ListCorpusReadings());
        Assert.Equal(3400, only.Codebases);
        Assert.Equal(52.6, only.MedianCai);
    }

    /// <summary>★ A corpus with nothing in it has no median, and null is not zero.</summary>
    [Fact]
    public void A_reading_of_an_empty_corpus_has_no_median()
    {
        var store = Store();
        store.RecordCorpusReading(Reading("2026-09-20T02:00:00Z", codebases: 0, median: null));

        Assert.Null(Assert.Single(store.ListCorpusReadings()).MedianCai);
    }

    // ---------------------------------------------------------------- fixtures

    private static CorpusReadingRecord Reading(string takenAt, int codebases, double? median) =>
        new(takenAt, codebases, median, $$"""{"codebases":{{codebases}}}""");

    private IRegistryStore Store() => new SqliteRegistryStore(
        Options.Create(new RegistryOptions { DbPath = _path }),
        new TestHost(),
        NullLogger<SqliteRegistryStore>.Instance);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private sealed class TestHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Cai.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
