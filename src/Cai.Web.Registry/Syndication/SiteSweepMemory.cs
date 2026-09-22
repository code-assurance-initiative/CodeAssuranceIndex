using System.Globalization;

namespace Cai.Web.Registry;

/// <summary>What one sweep did, and when it did it.</summary>
/// <param name="Sweep">The counts.</param>
/// <param name="At">The instant the sweep finished.</param>
public sealed record SweptAt(SiteSweep Sweep, DateTimeOffset At);

/// <summary>
/// What the publisher last did, kept where something outside the process can read it.
/// </summary>
/// <remarks>
/// <para>★★ A GATE THAT FIRES AND TELLS NOBODY IS NOT A GATE, and this exists because one did. The
/// producer's console carried a corpus-publishing section asking whether the corpus PUBLISHED is the corpus
/// measured; for months the answer was no — 2,398 codebases published against 3,485 measured — and the
/// sweep said so in its own log every hour while nothing anywhere rendered it. The log line is not the
/// telling: journald on a deployed host is not a surface anyone reads on a normal day.</para>
///
/// <para>★★ AND A SWEEP THAT HAS NEVER RUN IS NOT A HEALTHY ONE. "Nothing has gone wrong yet" and "nothing
/// has happened" are the same silence and only one of them is fine, so an unswept publisher reports that it
/// has never swept rather than reporting nothing at all — which is what a health surface with no reading
/// looks like.</para>
///
/// <para>★ SINGLETON, beside a service created fresh per sweep: what the last sweep did has to outlive the
/// sweep that did it, or every sweep is the first one.</para>
/// </remarks>
public sealed class SiteSweepMemory
{
    private SweptAt? _last;

    /// <summary>The last sweep, or null when this process has never completed one.</summary>
    public SweptAt? Last => Volatile.Read(ref _last);

    /// <summary>Remember a completed sweep.</summary>
    public void Record(SiteSweep sweep, DateTimeOffset at) =>
        Volatile.Write(ref _last, new SweptAt(sweep, at));

    /// <summary>
    /// The one sentence a health surface shows.
    /// </summary>
    /// <remarks>
    /// ★ THE COUNTS ARE THE WHOLE POINT. A publisher that ran and could not push is indistinguishable, from
    /// outside, from one that had nothing to do — unless the failures are stated.
    /// </remarks>
    public string Describe()
    {
        if (Last is not { } last)
        {
            return "the site has never been swept by this process";
        }

        var when = last.At.UtcDateTime.ToString("u", CultureInfo.InvariantCulture);
        return $"{last.Sweep.Built} built, {last.Sweep.Changed} changed, {last.Sweep.Withdrawn} withdrawn, "
             + $"{last.Sweep.Failed} failed, at {when}";
    }
}
