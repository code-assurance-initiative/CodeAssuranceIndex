using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Cai.Web.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// A page about the WHOLE corpus is not republished while the corpus is refilling.
/// </summary>
/// <remarks>
/// <para>★★ THE PUBLIC REGRESSION THIS PREVENTS, AND IT WAS AN HOUR AWAY. Every aggregate — the corpus
/// sheet, the surveys index, the language and country families, the advisory and package families — is
/// composed over the subjects granted AT THAT MOMENT. Publication is granted per subject as each one is
/// re-delivered, which during a migration means one scan at a time. So the first granted subject would
/// rebuild "6,276 published measured codebases" as a reading over ONE, push it over the sheet the site
/// is serving, and climb back over the weeks the re-delivery takes. Every reading honest about its own
/// population, and the page wrong the whole way: nobody asked for the corpus to be re-described as it
/// refilled.</para>
///
/// <para>★★ PORTRAITS ARE NOT HELD. A portrait describes its own subject and is right the moment it
/// arrives; only the pages that describe the whole need the whole to be there. Holding those too would
/// stall the migration it is protecting.</para>
///
/// <para>★ AND A HELD PAGE IS STILL WANTED, so the reconciliation below it cannot mistake "not published
/// yet" for "nothing stands behind this any more" and withdraw the version the site is serving.</para>
/// </remarks>
public sealed class SweepHoldsTheAggregatesTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cai-hold-{Guid.NewGuid():N}.db");

    /// <summary>★★ The cutover, exactly: thousands of pages served, a handful of subjects granted.</summary>
    [Fact]
    public async Task A_corpus_refilling_does_not_republish_the_pages_about_the_whole()
    {
        var store = Store();
        Publish(store, "acme/first-one-back");

        // The site serves six hundred portraits from the producer's last publish; one subject is granted.
        var serving = Enumerable.Range(0, 600).Select(i => $"surveys/github/acme/repo-{i}").ToList();
        var site = new RecordingSite(serving);

        var sweep = await Sweep(store, site);

        var published = site.Published.Select(p => p.Path).ToList();
        Assert.Contains("surveys/github/acme/first-one-back", published);
        Assert.DoesNotContain("state-of-the-corpus", published);
        Assert.DoesNotContain("surveys", published);
        Assert.True(sweep.AggregatesHeld > 0, "the sweep must say how many pages it held");
    }

    /// <summary>★★ And it never withdraws what it is holding — the site keeps the reading it has.</summary>
    [Fact]
    public async Task A_held_page_is_not_withdrawn_for_being_held()
    {
        var store = Store();
        Publish(store, "acme/first-one-back");

        var serving = Enumerable.Range(0, 600).Select(i => $"surveys/github/acme/repo-{i}")
            .Concat(["state-of-the-corpus", "surveys"])
            .ToList();
        var site = new RecordingSite(serving);

        await Sweep(store, site);

        Assert.DoesNotContain("state-of-the-corpus", site.Withdrawn);
        Assert.DoesNotContain("surveys", site.Withdrawn);
    }

    /// <summary>
    /// ★★ ONCE THE CORPUS IS BACK, THE AGGREGATES PUBLISH AGAIN — otherwise this is not a migration
    /// guard, it is a permanent veto on the pages the standard exists to publish.
    /// </summary>
    [Fact]
    public async Task A_corpus_that_has_caught_up_publishes_the_pages_about_the_whole()
    {
        var store = Store();
        for (var i = 0; i < 40; i++)
        {
            Publish(store, $"acme/repo-{i}");
        }

        // Forty granted against forty served: the site describes the corpus it actually has.
        var serving = Enumerable.Range(0, 40).Select(i => $"surveys/github/acme/repo-{i}").ToList();
        var site = new RecordingSite(serving);

        var sweep = await Sweep(store, site);

        Assert.Contains("state-of-the-corpus", site.Published.Select(p => p.Path));
        Assert.Equal(0, sweep.AggregatesHeld);
    }

    /// <summary>
    /// ★ A SITE WITH NOTHING ON IT IS NOT A MIGRATION. The first publish of a new estate has no served
    /// portraits to be a fraction of, and must be allowed to publish its aggregates — otherwise the
    /// standard could never publish a corpus page at all.
    /// </summary>
    [Fact]
    public async Task A_first_publish_onto_an_empty_site_publishes_everything()
    {
        var store = Store();
        Publish(store, "acme/only-one");
        var site = new RecordingSite([]);

        var sweep = await Sweep(store, site);

        Assert.Contains("state-of-the-corpus", site.Published.Select(p => p.Path));
        Assert.Equal(0, sweep.AggregatesHeld);
    }

    /// <summary>★ The hold is visible on the reading, like the withdrawal refusal beside it.</summary>
    [Fact]
    public async Task The_hold_is_visible_on_the_reading()
    {
        var store = Store();
        Publish(store, "acme/first-one-back");
        var memory = new SiteSweepMemory();

        var serving = Enumerable.Range(0, 600).Select(i => $"surveys/github/acme/repo-{i}").ToList();
        await Sweep(store, new RecordingSite(serving), memory);

        Assert.Contains("held", memory.Describe(), StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset Now = new(2026, 9, 24, 5, 0, 0, TimeSpan.Zero);

    private static Task<SiteSweep> Sweep(IRegistryStore store, ISiteSyndication site, SiteSweepMemory? memory = null) =>
        new SitePublishService(
            store, site, memory ?? new SiteSweepMemory(), new FrozenClock(Now),
            NullLogger<SitePublishService>.Instance)
            .SweepAsync(TestContext.Current.CancellationToken);

    private static void Publish(IRegistryStore store, string repository)
    {
        var payload = new DeliveryPayload
        {
            DeliveryId = $"cd_{Guid.NewGuid():N}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject { Repository = repository, Host = "github.com", Commit = "3f9a1c2" },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z", ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict { Cai = 72.4, Band = "Strong" },
            Evidence = new EvidenceBundle { RubricVersion = "rubric-2026.08.15", ProductionLoc = 1500 },
        };

        store.InsertDelivery(new DeliveryRecord(
            payload.DeliveryId, "org-acme", repository, payload.Subject.Commit, payload.Subject.Host,
            payload.Producer.Name, payload.RubricVersion, payload.Verdict.Cai, payload.Verdict.Band,
            payload.IssuedAt, "k1", "sha", "sig",
            new DeliveryPackage { Payload = payload }.ToJson(), "2026-09-01T10:00:01Z"));
        store.GrantPublication("org-acme", repository, "2026-09-01T10:00:00Z");
    }

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

    private sealed class FrozenClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingSite(IReadOnlyList<string>? serving) : ISiteSyndication
    {
        public List<SurveyPage> Published { get; } = [];

        public List<string> Withdrawn { get; } = [];

        public Task<bool> PublishAsync(SurveyPage page, CancellationToken cancellationToken)
        {
            Published.Add(page);
            return Task.FromResult(true);
        }

        public Task<bool> WithdrawAsync(string path, CancellationToken cancellationToken)
        {
            Withdrawn.Add(path);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<string>?> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(serving);
    }
}
