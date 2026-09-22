using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;
using Cai.Scoring;
using Cai.Web.Registry;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// Where a measured codebase comes FROM, and what may honestly be published about it.
/// </summary>
/// <remarks>
/// <para>★★ THE DENOMINATOR IS THE WHOLE POINT. The only origin signal that exists is the owning forge
/// account's free-text profile line, and about half of owners leave it blank. A country share taken over
/// the accounts that happen to fill in a profile field measures who fills in a profile field, not where
/// software is written — so the country map never travels alone: it travels with how many owners were
/// considered, how many declared anything, and how many resolved.</para>
/// <para>★★ AND AN OWNER IS COUNTED ONCE. Repositories per country is not a headcount: one prolific
/// account can put forty codebases in a country that has three people in it, so the two figures are
/// carried separately and a page shows both.</para>
/// </remarks>
public sealed class SubjectOriginTests
{
    [Fact]
    public void A_delivery_that_says_where_its_owner_is_passes_the_registrys_own_ingest_gate()
    {
        var violations = DeliveryPackageSchema.Validate(Document(Payload(1, Denmark)));

        Assert.Empty(violations);
    }

    [Fact]
    public void The_origin_round_trips_through_the_wire_form()
    {
        var round = DeliveryPackage.Parse(new DeliveryPackage { Payload = Payload(1, Denmark) }.ToJson());

        var origin = round.Payload.Subject.Origin;
        Assert.NotNull(origin);
        Assert.Equal("Denmark", origin.Country);
        Assert.True(origin.Declared);
        Assert.Equal("countryName", origin.ResolvedBy);
    }

    /// <summary>
    /// ★★ "Declared nothing" and "declared something unplaceable" are different facts about a population,
    /// and collapsing them into one "unknown" is how an unresolved share stops being reportable.
    /// </summary>
    [Fact]
    public void An_owner_who_declared_something_unplaceable_is_never_mapped_to_a_country()
    {
        var cut = CorpusReading.From(
            [
                // ★ THREE DIFFERENT OWNERS. The first draft of this test gave all three the same owner and
                //   then expected three owners — one account cannot declare three different locations, and
                //   the fixture was asserting something that cannot happen.
                Record(1, Denmark, owner: "acme"),
                Record(2, new SubjectOrigin { Declared = true, UnresolvedReason = "nonGeographic" }, owner: "beta"),
                Record(3, new SubjectOrigin { Declared = false, UnresolvedReason = "blank" }, owner: "gamma"),
            ],
            TakenAt).Origins;

        Assert.Equal(["Denmark"], cut.CodebasesByCountry.Keys);
        Assert.Equal(3, cut.OwnersConsidered);
        Assert.Equal(2, cut.OwnersDeclared);
        Assert.Equal(1, cut.OwnersResolved);
        Assert.Equal(1, cut.CodebasesResolved);
        Assert.Equal(2, cut.CodebasesDeclared);
    }

    /// <summary>★ One prolific account is one owner and forty codebases, and a page shows both.</summary>
    [Fact]
    public void An_owner_is_counted_once_however_many_codebases_it_publishes()
    {
        var cut = CorpusReading.From(
            [Record(1, Denmark, owner: "acme"), Record(2, Denmark, owner: "acme"), Record(3, Denmark, owner: "other")],
            TakenAt).Origins;

        Assert.Equal(3, cut.CodebasesByCountry["Denmark"]);
        Assert.Equal(2, cut.OwnersByCountry["Denmark"]);
        Assert.Equal(2, cut.OwnersConsidered);
    }

    /// <summary>
    /// ★ The same owner NAME on two different forges is two owners: an account is only unique within a
    /// provider, and folding them would put one account's codebases in another account's country.
    /// </summary>
    [Fact]
    public void The_same_owner_name_on_two_forges_is_two_owners()
    {
        var cut = CorpusReading.From(
            [Record(1, Denmark, owner: "acme"), Record(2, Denmark, owner: "acme", host: "gitlab.com")],
            TakenAt).Origins;

        Assert.Equal(2, cut.OwnersConsidered);
        Assert.Equal(2, cut.OwnersByCountry["Denmark"]);
    }

    /// <summary>★ A delivery that says nothing about its owner is still a codebase in the corpus.</summary>
    [Fact]
    public void A_delivery_with_no_origin_at_all_is_considered_and_declared_nothing()
    {
        var cut = CorpusReading.From([Record(1, Denmark, owner: "acme"), Record(2, null, owner: "beta")], TakenAt).Origins;

        Assert.Equal(2, cut.OwnersConsidered);
        Assert.Equal(1, cut.OwnersDeclared);
        Assert.Equal(1, cut.CodebasesResolved);
    }

    /// <summary>
    /// ★★ TWO DELIVERIES ABOUT ONE ACCOUNT DESCRIBE ONE ACCOUNT. A later delivery that says nothing about
    /// its owner must not erase what an earlier one said — otherwise an owner's country would blink in and
    /// out of the corpus depending on which of its repositories was measured most recently.
    /// </summary>
    [Fact]
    public void An_owner_keeps_the_origin_a_delivery_gave_it_when_another_says_nothing()
    {
        var cut = CorpusReading.From(
            [Record(1, Denmark, owner: "acme"), Record(2, null, owner: "acme")],
            TakenAt).Origins;

        Assert.Equal(1, cut.OwnersConsidered);
        Assert.Equal(1, cut.OwnersResolved);
        Assert.Equal(1, cut.OwnersByCountry["Denmark"]);
        // The CODEBASE count is unaffected: only one of the two said where it was from.
        Assert.Equal(1, cut.CodebasesResolved);
        Assert.Equal(2, cut.Codebases);
    }

    // ---------------------------------------------------------------- fixtures

    private static readonly DateTimeOffset TakenAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    private static readonly SubjectOrigin Denmark = new()
    {
        Country = "Denmark",
        Declared = true,
        ResolvedBy = "countryName",
    };

    private static JsonElement Document(DeliveryPayload payload)
    {
        var pair = DeliveryKeyPair.Generate("cai-ed25519-test");
        using var signer = new DeliverySigner(pair);
        return JsonSerializer.Deserialize<JsonElement>(signer.SignPackage(payload).ToJson());
    }

    private static SurveyRecord Record(int ordinal, SubjectOrigin? origin, string owner = "acme", string host = "github.com") =>
        SurveyRecord.From([Payload(ordinal, origin, owner, host)]);

    private static DeliveryPayload Payload(int ordinal, SubjectOrigin? origin, string owner = "acme", string host = "github.com") => new()
    {
        DeliveryId = $"cd_origin_{ordinal}",
        IssuedAt = "2026-09-01T10:00:00Z",
        RubricVersion = "rubric-2026.08.15",
        Subject = new DeliverySubject
        {
            Repository = $"{owner}/repo-{ordinal}",
            Host = host,
            Origin = origin,
        },
        Producer = new DeliveryProducer { Name = "watchdog.canine.dev" },
        Verdict = new DeliveryVerdict { Cai = 70, Band = "Strong" },
        Evidence = new EvidenceBundle { RubricVersion = "rubric-2026.08.15", ProductionLoc = 1000 },
    };
}
