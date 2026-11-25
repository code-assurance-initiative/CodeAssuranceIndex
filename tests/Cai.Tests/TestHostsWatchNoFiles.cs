using System.Runtime.CompilerServices;

namespace Cai.Tests;

/// <summary>
/// Every in-process test host (the web apps under test) would otherwise watch its configuration files for changes, one
/// inotify instance each. A shared Linux box allows 128 per user, so a full run beside other work failed at random with
/// "The configured user limit (128) on the number of inotify instances has been reached". No test changes a
/// configuration file while a host runs, so the hosts watch none.
/// </summary>
internal static class TestHostsWatchNoFiles
{
    [ModuleInitializer]
    internal static void Initialize() =>
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
}
