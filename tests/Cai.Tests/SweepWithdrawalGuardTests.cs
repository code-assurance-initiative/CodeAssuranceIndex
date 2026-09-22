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
/// A sweep may not delete most of the site because it composed less than last time.
/// </summary>
/// <remarks>
/// <para>★★ THE LOADED GUN THIS DISARMS, FOUND 2026-09-22 WITH THE PUBLISHER ALREADY LIVE. Reconciliation
/// withdraws every page under this system's roots that the current sweep did not compose — which is right,
/// and is the only way an orphan ever leaves. It is also, exactly, "delete everything the standard cannot
/// currently build". The producer's deliveries carry none of the MINOR 1.1 facts yet: no
/// <c>subject.languages</c>, no <c>subject.origin</c>, no <c>evidence.securityReading</c>. So the first
/// sweep after publication is granted would compose no field guides, no language or country pages and no
/// advisory or package pages — and withdraw roughly 3,800 live pages, correctly, by design, in one pass,
/// while nobody was watching.</para>
///
/// <para>★★ IT IS A SHARE AND NOT A COUNT. The corpus grows; a fixed number is wrong at both ends — too
/// small to matter on a big corpus, and a standing veto on a small one.</para>
///
/// <para>★ AND THE REFUSAL IS LOUD. A guard that quietly declines is a sweep that looks like it had nothing
/// to withdraw, which is the state it is refusing to be confused with.</para>
/// </remarks>
public sealed class SweepWithdrawalGuardTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cai-guard-{Guid.NewGuid():N}.db");

    /// <summary>★★ The case that is live right now: everything composed, almost nothing left standing.</summary>
    [Fact]
    public async Task A_sweep_that_would_withdraw_most_of_the_site_withdraws_nothing()
    {
        var store = Store();
        Publish(store, "acme/kept");

        // The site serves twenty pages under this system's roots; this sweep composes one of them.
        var serving = Enumerable.Range(0, 20).Select(i => $"state-of-the-corpus/advisory/cve-2026-{i:0000}").ToList();
        var site = new RecordingSite(serving);

        var sweep = await Sweep(store, site);

        Assert.Empty(site.Withdrawn);
        Assert.Equal(0, sweep.Withdrawn);
    }

    /// <summary>★ An ordinary orphan still goes. The guard is a ceiling, not a veto.</summary>
    [Fact]
    public async Task A_sweep_that_would_withdraw_a_few_pages_still_withdraws_them()
    {
        var store = Store();
        for (var i = 0; i < 40; i++)
        {
            Publish(store, $"acme/repo-{i}");
        }

        var composed = await ComposedPaths(store);
        var serving = composed.Concat(["surveys/github/acme/gone-away"]).ToList();
        var site = new RecordingSite(serving);

        var sweep = await Sweep(store, site);

        Assert.Equal(["surveys/github/acme/gone-away"], site.Withdrawn);
        Assert.Equal(1, sweep.Withdrawn);
    }

    /// <summary>
    /// ★★ A REFUSAL THAT TELLS NOBODY IS THE STATE IT IS REFUSING TO BE CONFUSED WITH. A sweep that declined
    /// to withdraw and a sweep with nothing to withdraw both report zero; the reading has to tell them apart.
    /// </summary>
    [Fact]
    public async Task A_refusal_is_visible_on_the_reading()
    {
        var store = Store();
        Publish(store, "acme/kept");
        var memory = new SiteSweepMemory();

        var serving = Enumerable.Range(0, 20).Select(i => $"state-of-the-corpus/advisory/cve-2026-{i:0000}").ToList();
        await Sweep(store, new RecordingSite(serving), memory);

        Assert.Contains("refused", memory.Describe(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>★ And a sweep that withdrew normally says nothing about a refusal.</summary>
    [Fact]
    public async Task An_ordinary_sweep_reads_as_an_ordinary_sweep()
    {
        var store = Store();
        for (var i = 0; i < 40; i++)
        {
            Publish(store, $"acme/repo-{i}");
        }

        var memory = new SiteSweepMemory();
        var serving = (await ComposedPaths(store)).Concat(["surveys/github/acme/gone-away"]).ToList();
        await Sweep(store, new RecordingSite(serving), memory);

        Assert.DoesNotContain("refused", memory.Describe(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ★★ A BRAKE IS FOR A CATASTROPHE, AND A HANDFUL OF PAGES IS NOT ONE. Without a floor the share alone
    /// refuses ordinary work on a small site — two owned pages and one real orphan is 50% — and a guard that
    /// stops the thing it was not built to stop is a guard somebody deletes. This is pinned because the
    /// service's own tests caught exactly that the moment the share went in.
    /// </summary>
    [Fact]
    public async Task A_small_site_losing_a_page_is_not_a_catastrophe()
    {
        var store = Store();
        Publish(store, "acme/kept");

        var composed = await ComposedPaths(store);
        var serving = composed.Concat(["surveys/github/acme/gone-away"]).ToList();
        var site = new RecordingSite(serving);

        var sweep = await Sweep(store, site);

        Assert.Equal(["surveys/github/acme/gone-away"], site.Withdrawn);
        Assert.Equal(0, sweep.WithdrawalRefused);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset Now = new(2026, 9, 22, 2, 0, 0, TimeSpan.Zero);

    private static Task<SiteSweep> Sweep(IRegistryStore store, ISiteSyndication site, SiteSweepMemory? memory = null) =>
        new SitePublishService(
            store, site, memory ?? new SiteSweepMemory(), new FrozenClock(Now),
            NullLogger<SitePublishService>.Instance)
            .SweepAsync(TestContext.Current.CancellationToken);

    /// <summary>What a sweep against an empty site composes — the wanted set, without withdrawing anything.</summary>
    private static async Task<List<string>> ComposedPaths(IRegistryStore store)
    {
        var probe = new RecordingSite([]);
        await Sweep(store, probe);
        return [.. probe.Published.Select(p => p.Path)];
    }

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

    private sealed class TestHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Cai.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
