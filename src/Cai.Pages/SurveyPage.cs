namespace Cai.Pages;

/// <summary>
/// One page the standard publishes about a measured subject: where it lives, what it is called, and the
/// node tree the CMS renders.
/// </summary>
/// <remarks>
/// ★ THE NODE TREE IS THE STANDARD'S, NOT THE RENDERER'S AND NOT THE PRODUCER'S. It is authoring
/// vocabulary — sections, stacks, headings, widgets — and carries no markup: the site supplies form, so a
/// published page picks up every future theme change for free and this can never drift into being a second
/// renderer.
/// </remarks>
/// <param name="Path">The address under the standard's own site, with no leading or trailing slash.</param>
/// <param name="Title">The page title, which is also what search engines and link previews quote.</param>
/// <param name="MetaDescription">The description quoted beside that title.</param>
/// <param name="Node">The authoring node spec for the page's single root section.</param>
public sealed record SurveyPage(
    string Path,
    string Title,
    string MetaDescription,
    IReadOnlyDictionary<string, object?> Node);
