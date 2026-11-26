# AC3: Page structure

**Lens:** accessibility · **Scored:** yes

## Property
Whether pages declare a language (well-formed BCP-47) and a non-empty title, expose exactly one main landmark and a sane heading order with non-empty headings, keep zoom enabled, title their iframes, give data tables header cells, and avoid meta-refresh. Static markup readiness, not a WCAG conformance claim.

## Applies to
Markup (HTML) that the authors write as markup, in whatever file holds it. For example: HTML files, component files that hold markup (JSX/TSX, Vue, Svelte, Razor) and server-side view templates. It also covers a page's title and viewport when a framework declares them in code (for example a route module that exports its title or viewport settings).

Markup that a script builds element by element at runtime is not in scope.

## Population
Production code as [production-code.md](../production-code.md) defines it, with these differences:

- **Added:** server-side view templates and component files are read for their markup.
- **Left out:** the authors' own developer tools (for example a page that a measuring script builds). Markup the authors hide from everyone is also left out, whether it is hidden with an attribute, an inline style or a stylesheet rule. This covers the hidden element and everything inside it.
- **Not checked:** a component tag. It stands for the markup the component renders and is not an HTML element.

The page-level checkpoints (language, title, main landmark, navigation labels) apply only to a **full document**, meaning markup with an `<html>` root. They are not asked of:

- a document that renders nothing, because its body holds only scripts, templates, hidden content, or a form made only of hidden inputs.
- an email body, or a document that a desktop application shows as a bundled resource and that no web server serves.
- the language of a page whose `lang` a build step of the repository writes into the shipped page.

A redirect stub is a page that forwards at once, either with a zero-second meta refresh to a target or with a script that replaces the location. It is not asked for a title or a main landmark, but it is still asked for its language.

**Pages whose body or title another file supplies:**
- **Body from a layout or runtime:** when the body comes from a layout, a routed component or a runtime mount that has its own `<main>`, the main landmark is asked of the file that writes that body, not of the host document.
- **Layout that writes no `<main>`:** the landmark is asked of each page the layout renders that writes none. If no page writes one, it is asked once of the layout.
- **Head from an include or framework:** a title supplied this way counts as present.
- **Component frameworks:** where the only full document hosts routed components (for example a Blazor app), the main landmark is asked of each layout that renders the routed body. The title is asked of each routable page that renders markup.

## Unit and denominator
The unit is a **structural checkpoint**. Each checkpoint is checked once:

| Checkpoint | Where | Fails when |
|---|---|---|
| Page language | each full document | `<html>` has no `lang`; a literal `lang` is not well-formed; or a literal `lang` is fixed while the text direction (`dir`) follows the locale at runtime |
| Document title | each full document | `<head>` has no `<title>` and nothing else supplies one; or the `<title>` is empty and no binding fills it |
| Main landmark | each full document (or the page or layout named above) | there is no `<main>` or `role="main"`, or there is more than one visible one |
| Navigation labels | each full document with 2 or more visible navigation landmarks | any of them has no accessible name |
| Heading | each `h1` to `h6` | its level is more than one deeper than the heading before it in the same rendering; it has no accessible name |
| Iframe | each `<iframe>` | it has no non-empty `title`, `aria-label` or `aria-labelledby` |
| Viewport | each viewport declaration, in markup or through a framework | it restricts zoom |
| Meta refresh | each `<meta http-equiv="refresh">` | its delay is above 0 seconds |
| Data table | each `<table>` that has a data cell (`<td>`) | it has no header cell (`<th>`) |
| Table row | each `<tr>` | it holds text, a text value or a non-cell element directly, outside any `<td>` or `<th>` |

More detail on some checkpoints:
- **Heading name:** a heading has no accessible name when nothing inside it supplies one. A name can come from text, bound text, a label, an image, a slot or a component, or from a script that fills the heading with text.
- **First heading:** the first heading of each rendering root is never out of order. A heading in a branch that never renders next to this one does not count as the heading before it.
- **Which tables count:** a table with `role="presentation"` or `role="none"` is not a data table. Neither is a table whose header row comes from the caller or from a runtime placeholder. Nor is a table in which every row is exactly one cell wide, as long as it has at least two rows.
- **Dynamic values:** a value written as an expression, binding or placeholder that the markup cannot settle passes. So does an attribute spread that may supply the value.

N is the number of checkpoints across all in-scope documents. V is the number of failures. A heading that both breaks the order and has no accessible name counts as two failures, so V can exceed N.

## Thresholds
- **Well-formed language tag:** a primary language subtag of 2 or 3 letters, followed by zero or more subtags of letters and digits, each joined by a hyphen (for example `en`, `en-US`, `zh-Hant-CN`). `english` and `en_US` fail.
- **Zoom restricted:** `user-scalable=no`, `user-scalable=0`, or a maximum scale below 2.
- **Heading order:** a jump of more than one level fails. A jump of exactly one level does not fail, and neither does any step back up.
- **Navigation labels:** asked only when a document has 2 or more visible navigation landmarks.
- **Meta refresh:** a delay of exactly 0 seconds passes. Any longer delay fails.

## Score
- If V = 0, the score is 10.
- Otherwise:
  - d = min(V / N, 1)
  - isolation = max(0, 1 - d / 0.10)
  - headroom = min(1, log10(N) / log10(200))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 - d) × (ceiling - 1), kept between 1 and the ceiling and rounded to 2 decimals.

So any failure caps the score at 8. The cap drops to 6 when failures reach 10 % or more of the checkpoints, or when there is only one checkpoint. The score falls toward 1 as failures grow.

## Not measured when
- the repository has no web markup.
- its only markup is in the authors' developer tools.
- it ships no user-facing web interface (for example a library, command-line tool, worker or headless service). A web API that serves a full page document counts as having one.
- its product markup holds fewer than 25 HTML elements, not counting component tags.
- there are no checkpoints.

## What the result is not
- **Not WCAG conformance:** the result is not a WCAG conformance claim or an audit of rendered pages.
- **Static markup only:** structure a script injects at runtime is not seen, except where the page is put together from files the repository holds.
- **Not the visual outline:** heading order follows the markup, not the visual hierarchy users see.
- **Not a full language check:** language tags are checked for their shape only, not against the language registry. A private-use tag (`x-...`) fails the shape check.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under decision 0013 will replace each one with a threshold on the property.

- **Scale floor:** a repository with fewer than 25 HTML elements in its product markup (component tags not counted) is not measured.
- **Curve constants:** the ceilings of 6 and 8, the 10 % failure share at which the higher ceiling disappears, the 200 checkpoints at which headroom reaches its maximum, and the floor of 1.
