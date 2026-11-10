using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// ★★ BACKGROUND WORK NEVER TAKES THE STANDARD DOWN WITH IT. On 2026-11-10 the site publisher threw once (an Imprint
/// timeout), .NET's default <see cref="BackgroundServiceExceptionBehavior.StopHost"/> stopped the whole process, and
/// the rubric API, the UI and the registry were gone for four hours because a CMS was slow. A publisher that dies
/// is logged at Critical; the API it shares a process with keeps answering.
/// </summary>
public sealed class BackgroundWorkNeverStopsTheHostTests(RegistryUnconfiguredFixture fx)
    : IClassFixture<RegistryUnconfiguredFixture>
{
    [Fact]
    public void A_background_service_that_throws_does_not_stop_the_host()
    {
        var options = fx.Factory.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal(BackgroundServiceExceptionBehavior.Ignore, options.BackgroundServiceExceptionBehavior);
    }
}
