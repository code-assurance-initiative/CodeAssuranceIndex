namespace Cai.Tests;

/// <summary>
/// The instant the API suites run at, whatever day it is on the machine running them.
/// </summary>
/// <remarks>
/// ★★ THE SUITE WAS A TIME BOMB. The Noise Standard decides its embargo and its submission window against dates
/// in the SIGNED manifest, and it read the wall clock to do it — so the tests asserting "2026-09 is still under
/// embargo" were true until 2026-10-01 and false from then on, five of them at once, with no code having changed.
/// A date in a signed manifest cannot move; the clock the tests run at has to stand still instead.
///
/// ★ 2026-09-29, 12:00 UTC: inside period 2026-09 (drawn 2026-08-15), before its submissions close (09-30) and
/// before it publishes (10-01) — the last day the whole suite is recorded green.
/// </remarks>
internal static class SuiteClock
{
    public static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public static TimeProvider Frozen { get; } = new FrozenTimeProvider(Now);

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
