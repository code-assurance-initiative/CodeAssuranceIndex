using System.Text.Json;
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
/// The sweep: the standard composes every page it stands behind and reconciles the site against them.
/// </summary>
/// <remarks>
/// <para>★★ WITHDRAWAL IS BY RECONCILIATION, NOT BY EVENT. A subject that stops being published raises
/// nothing; the only way to know which pages are now orphans is to compare what the site serves against
/// what the standard would compose today.</para>
/// <para>★★ WHICH IS WHY EVERY "I COULD NOT FIND OUT" MUST FAIL CLOSED. An unanswerable question is not an
/// empty answer: a site that could not be listed, and a corpus that could not be read, both compare the
/// site against nothing — and a reconciliation against nothing deletes everything.</para>
/// </remarks>
public sealed class SitePublishServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cai-sweep-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Only_published_subjects_get_a_page()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        Deliver(store, "org-beta", "beta/private-thing");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite([]);
        await Sweep(store, site);

        Assert.Contains(site.Published, p => p.Path == "surveys/github/acme/checkout-api");
        Assert.DoesNotContain(site.Published, p => p.Path.Contains("private-thing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_page_the_site_serves_that_nothing_stands_behind_is_withdrawn()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite(["surveys/github/acme/gone-away", "surveys/github/acme/checkout-api"]);
        await Sweep(store, site);

        Assert.Equal(["surveys/github/acme/gone-away"], site.Withdrawn);
    }

    /// <summary>★★ The site may serve pages from other producers and other systems. Ownership is by address.</summary>
    [Fact]
    public async Task A_path_this_system_does_not_own_is_never_touched()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite(["about", "verify", "blog/why-we-measure", "surveys-of-other-things"]);
        await Sweep(store, site);

        Assert.Empty(site.Withdrawn);
    }

    /// <summary>
    /// ★★ "COULD NOT ASK" IS NOT "THE SITE SERVES NOTHING" — and the case that actually needs a test is the
    /// site FAILING, not answering null.
    /// </summary>
    /// <remarks>
    /// ★ The first version of this test handed the sweep a null listing and asserted nothing was withdrawn.
    /// That passes with the null rule REMOVED, because a null listing carries no paths to withdraw either
    /// way — it was asserting a structural truth and calling it a guard. What can actually go wrong is the
    /// listing THROWING: mishandled, it aborts the sweep, so nothing is published either, and the site goes
    /// stale while every log line blames the CMS.
    /// </remarks>
    [Fact]
    public async Task When_the_site_cannot_be_listed_the_sweep_still_publishes_and_withdraws_nothing()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite(["surveys/github/acme/gone-away"]) { FailListing = true };
        var sweep = await Sweep(store, site);

        Assert.Empty(site.Withdrawn);
        Assert.Equal(0, sweep.Withdrawn);
        // The pages it COULD publish still went out: not knowing what to delete is not a reason to stop.
        Assert.NotEmpty(site.Published);
    }

    /// <summary>
    /// ★★ A CORPUS THAT COMPOSED NOTHING WITHDRAWS NOTHING. An empty composition and "the registry could not
    /// be read" are indistinguishable from here, and a reconciliation against an empty set deletes the whole
    /// site.
    /// </summary>
    [Fact]
    public async Task When_nothing_is_published_the_site_is_left_alone()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");

        var site = new FakeSite(["surveys/github/acme/checkout-api", "state-of-the-corpus"]);
        var sweep = await Sweep(store, site);

        Assert.Empty(site.Withdrawn);
        Assert.Equal(0, sweep.Built);
    }

    [Fact]
    public async Task A_repush_of_identical_content_is_not_counted_as_work()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite([]) { Changes = false };
        var sweep = await Sweep(store, site);

        Assert.True(sweep.Built > 0);
        Assert.Equal(0, sweep.Changed);
    }

    /// <summary>★ A page that could not be pushed is not a page the next step may delete.</summary>
    [Fact]
    public async Task A_push_that_fails_is_counted_and_its_page_is_not_withdrawn()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var site = new FakeSite(["surveys/github/acme/checkout-api"]) { FailOn = "surveys/github/acme/checkout-api" };
        var sweep = await Sweep(store, site);

        Assert.Equal(1, sweep.Failed);
        Assert.Empty(site.Withdrawn);
    }

    /// <summary>★ One sweep, one recorded reading — the point the trend line is drawn through.</summary>
    [Fact]
    public async Task The_sweep_records_the_reading_it_published()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        await Sweep(store, new FakeSite([]));

        var reading = Assert.Single(store.ListCorpusReadings());
        Assert.Equal(1, reading.Codebases);
        Assert.Equal(72.4, reading.MedianCai);
    }

    /// <summary>★ And a sweep that published nothing records nothing — a zero reading is a false point.</summary>
    [Fact]
    public async Task A_sweep_that_published_nothing_records_no_reading()
    {
        var store = Store();
        Deliver(store, "org-acme", "acme/checkout-api");

        await Sweep(store, new FakeSite([]));

        Assert.Empty(store.ListCorpusReadings());
    }

    [Fact]
    public void Ownership_is_asserted_by_address()
    {
        Assert.True(SitePublishService.Owns("surveys/github/acme/checkout-api"));
        Assert.True(SitePublishService.Owns("state-of-the-corpus"));
        Assert.True(SitePublishService.Owns("state-of-the-corpus/language/csharp"));
        Assert.False(SitePublishService.Owns("surveys-of-other-things"));
        Assert.False(SitePublishService.Owns("state-of-the-corpus-elsewhere"));
        Assert.False(SitePublishService.Owns("about"));
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset Now = new(2026, 9, 22, 2, 0, 0, TimeSpan.Zero);

    private static Task<SiteSweep> Sweep(IRegistryStore store, ISiteSyndication site) =>
        new SitePublishService(
            store, site, new SiteSweepMemory(), new FrozenClock(Now), NullLogger<SitePublishService>.Instance)
            .SweepAsync(TestContext.Current.CancellationToken);

    private static void Deliver(IRegistryStore store, string ownerOrgId, string repository)
    {
        var payload = new DeliveryPayload
        {
            DeliveryId = $"cd_{Guid.NewGuid():N}",
            IssuedAt = "2026-09-01T10:00:00Z",
            RubricVersion = "rubric-2026.08.15",
            Subject = new DeliverySubject
            {
                Repository = repository,
                Host = "github.com",
                Commit = "3f9a1c2",
                Languages = new SubjectLanguages { Primary = "csharp" },
            },
            Producer = new DeliveryProducer { Name = "watchdog.canine.dev", Scanner = "watchdog-surveyor" },
            Measurement = new DeliveryMeasurement { ScannedAt = "2026-08-30T09:00:00Z", ProductionLoc = 1500 },
            Verdict = new DeliveryVerdict { Cai = 72.4, Band = "Strong" },
            Evidence = new EvidenceBundle
            {
                RubricVersion = "rubric-2026.08.15",
                ProductionLoc = 1500,
                Dimensions = [new DimensionScore("D1", "code-quality", 7.5, 0.95)],
                SecurityReading = new SecurityReading { VulnMeasurable = true, DisclosureMeasured = true },
            },
        };

        var package = new DeliveryPackage { Payload = payload };
        store.InsertDelivery(new DeliveryRecord(
            payload.DeliveryId,
            ownerOrgId,
            repository,
            payload.Subject.Commit,
            payload.Subject.Host,
            payload.Producer.Name,
            payload.RubricVersion,
            payload.Verdict.Cai,
            payload.Verdict.Band,
            payload.IssuedAt,
            KeyId: "k1",
            CanonicalSha256: "sha",
            SignatureValue: "sig",
            PackageJson: package.ToJson(),
            PublishedAt: "2026-09-01T10:00:01Z"));
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

    private sealed class FakeSite(IReadOnlyList<string>? serving) : ISiteSyndication
    {
        public List<SurveyPage> Published { get; } = [];

        public List<string> Withdrawn { get; } = [];

        public bool Changes { get; init; } = true;

        public string? FailOn { get; init; }

        public bool FailListing { get; init; }

        public Task<bool> PublishAsync(SurveyPage page, CancellationToken cancellationToken)
        {
            if (FailOn is { } path && page.Path == path)
            {
                throw new HttpRequestException($"publish '{path}' failed: 503");
            }

            Published.Add(page);
            return Task.FromResult(Changes);
        }

        public Task<bool> WithdrawAsync(string path, CancellationToken cancellationToken)
        {
            Withdrawn.Add(path);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<string>?> ListAsync(CancellationToken cancellationToken) =>
            FailListing
                ? throw new HttpRequestException("list failed: 503")
                : Task.FromResult(serving);
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
