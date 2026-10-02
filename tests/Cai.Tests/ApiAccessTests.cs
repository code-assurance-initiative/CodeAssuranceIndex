using System.Net;
using Cai.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The API's access guard, decided one request at a time.
/// </summary>
/// <remarks>
/// ★★ THE OPEN DEFAULT IS ONE CONFIG SWITCH AWAY FROM PARTNER-ONLY, and every handler trusts this method to
/// make that call. Nothing in the integration suite ever flips the switch, so until these, the deny path —
/// the only path that exists to say no — had never run.
/// </remarks>
public sealed class ApiAccessTests
{
    private const string PartnerKey = "partner-secret";
    private static readonly IPAddress Remote = IPAddress.Parse("203.0.113.7");

    [Fact]
    public void An_open_API_admits_anyone()
    {
        var request = Request(requirePartnerKey: false, Remote, header: null);

        ApiAccess.EnsureAllowed(request);
    }

    [Fact]
    public void Partner_only_admits_the_co_located_caller_on_loopback()
    {
        var request = Request(requirePartnerKey: true, IPAddress.Loopback, header: null);

        ApiAccess.EnsureAllowed(request);
    }

    [Fact]
    public void Partner_only_admits_a_remote_caller_presenting_the_key()
    {
        var request = Request(requirePartnerKey: true, Remote, header: PartnerKey);

        ApiAccess.EnsureAllowed(request);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-key")]
    [InlineData("PARTNER-SECRET")] // ★ ordinal: a key that differs only in case is a different key
    public void Partner_only_refuses_a_remote_caller_without_the_exact_key(string? header)
    {
        var request = Request(requirePartnerKey: true, Remote, header);

        Assert.Throws<ApiAccess.ForbiddenException>(() => ApiAccess.EnsureAllowed(request));
    }

    /// <summary>
    /// ★ NO CONFIGURED KEY IS NOT AN EMPTY KEY. An operator who switches the API to partner-only before setting
    /// a key must get a closed API, never one that an empty header opens.
    /// </summary>
    [Fact]
    public void Partner_only_with_no_key_configured_refuses_even_an_empty_header()
    {
        var request = Request(requirePartnerKey: true, Remote, header: "", configuredKey: null);

        Assert.Throws<ApiAccess.ForbiddenException>(() => ApiAccess.EnsureAllowed(request));
    }

    private static DefaultHttpContext Request(
        bool requirePartnerKey, IPAddress remote, string? header, string? configuredKey = PartnerKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Api:RequirePartnerKey"] = requirePartnerKey ? "true" : "false",
            ["RateLimit:PartnerKey"] = configuredKey,
        };
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Connection.RemoteIpAddress = remote;
        if (header is not null)
        {
            context.Request.Headers["X-CAI-Partner"] = header;
        }

        return context;
    }
}
