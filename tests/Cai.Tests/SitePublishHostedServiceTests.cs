using System.Reflection;
using Cai.Pages;
using Cai.Web.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The loop that keeps the site in step with the registry: it sweeps on start, and a failed sweep is logged
/// rather than ending the loop.
/// </summary>
/// <remarks>
/// ★★ A CRASHED PUBLISHER NOBODY NOTICES IS THE LARGE PROBLEM. The site being an hour stale is small; a
/// background loop that died on its first exception and left /health green is the failure this type exists to
/// avoid — so that is the case asserted, not only the happy one.
/// </remarks>
public sealed class SitePublishHostedServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task It_sweeps_as_soon_as_it_starts_and_records_the_sweep()
    {
        var memory = new SiteSweepMemory();
        using var provider = Services(EmptyStore(), memory);
        var service = Hosted(provider, new CapturingLogger());

        await service.StartAsync(Ct);
        await Until(() => memory.Last is not null);
        await service.StopAsync(Ct);

        Assert.Equal(SuiteClock.Now, memory.Last!.At);
        Assert.Equal(0, memory.Last.Sweep.Failed);
    }

    [Fact]
    public async Task A_failed_sweep_is_logged_and_the_loop_keeps_running()
    {
        var logger = new CapturingLogger();
        using var provider = Services(DispatchProxy.Create<IRegistryStore, ThrowingStore>(), new SiteSweepMemory());
        var service = Hosted(provider, logger);

        await service.StartAsync(Ct);
        await Until(() => logger.Errors.Count > 0);

        Assert.Contains("Site publish sweep failed", logger.Errors);
        Assert.False(service.ExecuteTask!.IsCompleted, "the loop must survive a failed sweep");

        await service.StopAsync(Ct);
    }

    private static SitePublishHostedService Hosted(IServiceProvider provider, ILogger<SitePublishHostedService> logger) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SyndicationOptions { TickMinutes = 60 }),
            logger);

    private static ServiceProvider Services(IRegistryStore store, SiteSweepMemory memory) =>
        new ServiceCollection()
            .AddSingleton(store)
            .AddSingleton<ISiteSyndication>(new EmptySite())
            .AddSingleton(memory)
            .AddSingleton(SuiteClock.Frozen)
            .AddSingleton<ILogger<SitePublishService>>(NullLogger<SitePublishService>.Instance)
            .AddScoped<SitePublishService>()
            .BuildServiceProvider();

    private static IRegistryStore EmptyStore() => new SqliteRegistryStore(
        Options.Create(new RegistryOptions
        {
            DbPath = Path.Combine(Path.GetTempPath(), "cai-hosted-tests", $"{Guid.NewGuid():N}.db"),
        }),
        new TempHost(),
        NullLogger<SqliteRegistryStore>.Instance);

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "the loop did not get there within 10 seconds");
            await Task.Delay(20, Ct);
        }
    }

    private sealed class EmptySite : ISiteSyndication
    {
        public Task<bool> PublishAsync(SurveyPage page, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> WithdrawAsync(string path, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<IReadOnlyList<string>?> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>?>([]);
    }

    /// <summary>A registry that is down: every call throws.</summary>
    public class ThrowingStore : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("registry unavailable");
    }

    private sealed class CapturingLogger : ILogger<SitePublishHostedService>
    {
        private readonly List<string> _errors = [];

        public IReadOnlyList<string> Errors
        {
            get { lock (_errors) { return [.. _errors]; } }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                lock (_errors) { _errors.Add(formatter(state, exception)); }
            }
        }
    }

    private sealed class TempHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Cai.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
