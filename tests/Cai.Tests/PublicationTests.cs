using Cai.Web.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// What makes a measured codebase PUBLIC on the standard's own site.
/// </summary>
/// <remarks>
/// <para>★★ THE STANDARD NEEDS ITS OWN ANSWER, AND IT IS NOT THE PRODUCER'S. "Published and promoted" is a
/// Watchdog concept — a hidden flag, a gallery opt-in, a climb gate — and a second producer arriving with
/// none of those would either publish everything it sent or nothing. Here the rule is one sentence: a
/// subject is public when its OWNER ORG has granted publication for it, and not before.</para>
///
/// <para>★★ PUBLICATION IS PER SUBJECT, NOT PER DELIVERY, because the page is per subject and shows the
/// SEQUENCE. A per-delivery grant would publish a trajectory with holes in it, and no reader could tell a
/// withdrawn reading from a month in which nothing was measured.</para>
///
/// <para>★ A WITHDRAWAL KEEPS ITS ROW. Grants in this registry are an audit trail rather than editable
/// state, and publication is a grant: what was public, and when it stopped being, is a fact somebody may
/// later need to establish.</para>
/// </remarks>
public sealed class PublicationTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), $"cai-publication-{Guid.NewGuid():N}.db");

    [Fact]
    public void A_subject_is_not_public_until_its_owner_grants_publication()
    {
        var store = Store();

        Assert.Null(store.GetPublication("org-acme", "acme/checkout-api"));
        Assert.Empty(store.ListPublishedSubjects());
    }

    /// <summary>★★ Per subject, so it covers the deliveries that have not been sent yet.</summary>
    [Fact]
    public void A_grant_covers_every_delivery_about_that_subject_including_later_ones()
    {
        var store = Store();
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");

        var published = Assert.Single(store.ListPublishedSubjects());

        Assert.Equal("org-acme", published.OwnerOrgId);
        Assert.Equal("acme/checkout-api", published.Repository);
        Assert.Equal("granted", published.Status);
        Assert.Null(published.WithdrawnAt);
    }

    [Fact]
    public void A_withdrawal_keeps_the_row_and_records_when()
    {
        var store = Store();
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");
        store.WithdrawPublication("org-acme", "acme/checkout-api", "2026-09-20T08:00:00Z");

        var row = store.GetPublication("org-acme", "acme/checkout-api");

        Assert.NotNull(row);
        Assert.Equal("withdrawn", row.Status);
        Assert.Equal("2026-09-01T10:00:00Z", row.GrantedAt);
        Assert.Equal("2026-09-20T08:00:00Z", row.WithdrawnAt);
        Assert.Empty(store.ListPublishedSubjects());
    }

    /// <summary>★ Withdrawing something that was never granted is not an error, and it does not publish it.</summary>
    [Fact]
    public void Withdrawing_a_subject_that_was_never_granted_does_nothing()
    {
        var store = Store();
        store.WithdrawPublication("org-acme", "acme/checkout-api", "2026-09-20T08:00:00Z");

        Assert.Null(store.GetPublication("org-acme", "acme/checkout-api"));
    }

    [Fact]
    public void A_subject_can_be_published_again_after_a_withdrawal()
    {
        var store = Store();
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");
        store.WithdrawPublication("org-acme", "acme/checkout-api", "2026-09-20T08:00:00Z");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-22T09:00:00Z");

        var row = store.GetPublication("org-acme", "acme/checkout-api");

        Assert.NotNull(row);
        Assert.Equal("granted", row.Status);
        Assert.Equal("2026-09-22T09:00:00Z", row.GrantedAt);
        Assert.Null(row.WithdrawnAt);
    }

    /// <summary>
    /// ★★ THE GRANT IS THE OWNER'S. Two orgs can name the same repository string — one is the owner of the
    /// deliveries and the other is not — and a grant from the second must not publish the first's.
    /// </summary>
    [Fact]
    public void Only_the_owning_orgs_grant_publishes_a_subject()
    {
        var store = Store();
        store.GrantPublication("org-someone-else", "acme/checkout-api", "2026-09-01T10:00:00Z");

        Assert.Null(store.GetPublication("org-acme", "acme/checkout-api"));
        Assert.DoesNotContain(store.ListPublishedSubjects(), p => p.OwnerOrgId == "org-acme");
    }

    /// <summary>★ Granting twice is one row, not two — publication is a state, and its history is the row.</summary>
    [Fact]
    public void Granting_twice_leaves_one_row()
    {
        var store = Store();
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-01T10:00:00Z");
        store.GrantPublication("org-acme", "acme/checkout-api", "2026-09-02T10:00:00Z");

        Assert.Single(store.ListPublishedSubjects());
    }

    // ---------------------------------------------------------------- fixtures

    private IRegistryStore Store() => new SqliteRegistryStore(
        Options.Create(new RegistryOptions { DbPath = _path }),
        new StubEnvironment(),
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

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Cai.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
