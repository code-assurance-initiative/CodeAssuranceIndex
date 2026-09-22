namespace Cai.Pages;

/// <summary>
/// Composes the page the standard publishes about a measured subject.
/// </summary>
/// <remarks>
/// ★★ ONE BUILDER FOR EVERY PRODUCER. That is the whole point of it living here: whoever measured the
/// code, the page about it has the same address, the same structure and the same words, and differs only
/// in the numbers and in who is credited with taking them.
/// </remarks>
public static class SurveyPageBuilder
{
    /// <summary>The first path segment every survey page lives under.</summary>
    /// <remarks>
    /// NOT <c>registry</c>: the registry stores and distributes signed deliveries. These pages are the
    /// public catalogue of subjects that have been measured and whose owners chose to publish.
    /// </remarks>
    public const string Root = "surveys";

    /// <summary>The page for one measured subject.</summary>
    public static SurveyPage Build(SurveyRecord record) =>
        throw new NotImplementedException("Phase 4 of docs/plans/cai-owns-its-pages.md.");
}
