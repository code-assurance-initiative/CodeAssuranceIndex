using Cai.Pages;

namespace Cai.Web.Registry;

/// <summary>
/// The port to the site that serves the standard's pages: push one, withdraw one, and ask what it is
/// currently serving.
/// </summary>
/// <remarks>
/// <para>★★ THE LIST IS WHAT MAKES THE PUBLISHER RECONCILABLE RATHER THAN APPEND-ONLY. A subject that stops
/// being published must stop having a page, and the only way to know which pages are now orphans is to
/// compare what the site holds against what the standard would compose today.</para>
/// <para>★★ AND <see cref="ListAsync"/> RETURNS NULL WHEN IT COULD NOT ASK. An unanswerable question is not
/// an empty answer: an empty list means the site serves nothing, and a reconciliation against nothing
/// deletes everything. The two must not look alike from here.</para>
/// </remarks>
public interface ISiteSyndication
{
    /// <summary>
    /// Push one page.
    /// </summary>
    /// <returns>True when the site's content actually changed — a re-push of identical content returns
    /// false, which is how a sweep reports real work instead of traffic.</returns>
    Task<bool> PublishAsync(SurveyPage page, CancellationToken cancellationToken);

    /// <summary>Withdraw one page by path. Returns true when a page was there to remove.</summary>
    Task<bool> WithdrawAsync(string path, CancellationToken cancellationToken);

    /// <summary>The paths the site currently serves, or null when the site could not be asked.</summary>
    Task<IReadOnlyList<string>?> ListAsync(CancellationToken cancellationToken);
}
