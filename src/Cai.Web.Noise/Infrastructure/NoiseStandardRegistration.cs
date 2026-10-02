using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cai.Web.Noise;

/// <summary>Registers the Noise Standard's own services: its store, the roles that store fills, and its health.</summary>
public static class NoiseStandardRegistration
{
    /// <summary>
    /// Register the Noise Standard's store — resolvable under every role interface — and its readiness check.
    /// </summary>
    /// <remarks>
    /// <para>★ ONE INSTANCE, SIX DOORS. The roles forward to the same singleton rather than each constructing a
    /// store, so a handler declaring <c>INoiseCostStore</c> and a page declaring <c>INoiseStore</c> are talking to
    /// the same database connection string and the same initialisation — the narrowing is about what a caller may
    /// reach for, not about which store it gets.</para>
    ///
    /// <para>★★ THE STORE IS THE MODULE'S OWN BUSINESS. The host asks for the standard, not for SQLite: which
    /// database the submission register and the verdict record live in is a decision this project owns, and
    /// naming the implementation in the host is what let both of them be in-memory for as long as they were — a
    /// restart forgot that a vendor had submitted, which is the hole the no-withdrawal rule exists to close.</para>
    ///
    /// <para>★★ THE STANDARD ITSELF, AS A READINESS POINT. /health was green for two days while every noise
    /// endpoint returned 500 — see NoiseStandardHealthCheck for why. Registering the check here means a host
    /// cannot wire the standard up and forget its probe.</para>
    /// </remarks>
    public static IServiceCollection AddNoiseStandard(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ★ THE STANDARD'S CLOCK IS A SERVICE, because the embargo and the submission window are decided by
        //   it against dates in the signed manifest. Read straight off the wall, those decisions change on the
        //   day a period publishes — and so did every test that asserted one, the day 2026-09 lifted.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<INoiseStore, SqliteNoiseStore>();
        services.AddSingleton<INoiseSubmissionStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddSingleton<INoiseJudgingStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddSingleton<INoisePublicationStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddSingleton<INoiseDisputeStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddSingleton<INoiseFindingStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddSingleton<INoiseCostStore>(sp => sp.GetRequiredService<INoiseStore>());
        services.AddHealthChecks().AddCheck<NoiseStandardHealthCheck>("noise-standard");
        return services;
    }
}
