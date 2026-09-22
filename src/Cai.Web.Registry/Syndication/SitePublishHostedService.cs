using Microsoft.Extensions.Options;

namespace Cai.Web.Registry;

/// <summary>
/// Ticks <see cref="SitePublishService"/> so the site keeps up with what has been published to the registry.
/// </summary>
/// <remarks>
/// <para>Registered only when <see cref="SyndicationOptions.IsConfigured"/>, so a zero-config boot and every
/// test stay inert.</para>
/// <para>A tick failure is logged and the loop continues — the site being one hour stale is a small problem,
/// and a crashed publisher nobody notices is a large one.</para>
/// <para>★ SINGLE-INSTANCE. Two replicas sweeping at once would push the same pages and race each other's
/// withdrawals.</para>
/// </remarks>
public sealed class SitePublishHostedService(
    IServiceScopeFactory scopes,
    IOptions<SyndicationOptions> options,
    ILogger<SitePublishHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(5, options.Value.TickMinutes);
        logger.LogInformation("Site publish loop started (tick {TickMinutes}m)", minutes);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<SitePublishService>();
                var sweep = await service.SweepAsync(stoppingToken).ConfigureAwait(false);

                // Only a sweep that DID something is worth a line; the steady state is "nothing changed", and
                // logging that every hour would bury the sweeps that mattered.
                if (sweep.Changed > 0 || sweep.Withdrawn > 0 || sweep.Failed > 0)
                {
                    logger.LogInformation(
                        "Site sweep: {Built} built, {Changed} changed, {Withdrawn} withdrawn, {Failed} failed",
                        sweep.Built, sweep.Changed, sweep.Withdrawn, sweep.Failed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Site publish sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
