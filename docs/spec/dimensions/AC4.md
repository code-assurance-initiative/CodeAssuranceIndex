# AC4: Keyboard semantics

**Lens:** accessibility · **Scored:** yes

## Property
Whether interactive behaviour can be reached and used from the keyboard: a non-interactive element (one with no native control behaviour, for example a div or span) that has a click handler has an accessibility role, a tab index and a keyboard handler; an element the repository's own style sheets give a pointer cursor has at least one of those three; an element whose only event handlers are a mouse enter/leave pair or a double-click can receive focus; no element has a positive tab index; no anchor lacks an href; and no link whose href is a placeholder (an empty fragment or a script URL, for example '#' or 'javascript:') acts as a button, that is, performs an action instead of navigating, through a click handler or through the script URL itself. A reading of the markup and styles as written in source, not of the running page; not a WCAG (Web Content Accessibility Guidelines) conformance claim.

## Applies to
Markup and styles, in any language that holds them:

- **Markup:** HTML pages and fragments, markup written inside component files (for example JSX or TSX, Vue and Svelte single-file components, Razor components), and server-side view templates (for example Razor views).
- **Styles:** CSS, including style sheet dialects that compile to CSS (for example SCSS). This dimension reads styles only to find which elements the repository's own style sheets give a pointer cursor (`cursor: pointer`) or hide.

## Population
The population is production code as [production-code.md](../production-code.md) defines it. This dimension differs from that page in these ways:

- **Server-side view templates are in.** [production-code.md](../production-code.md) leaves them out. This dimension adds them back and reads their HTML markup as written (for example Razor or other framework views).
- **The authors' own developer tools are out.** Pages built by the repository's own developer tools (for example a measuring script's report page) are not part of the product's interface.
- **Only HTML elements are read.** A framework component used as a tag (for example `<MyButton>`) is not an HTML element. Its keyboard behaviour is defined where the component is declared, and that declaration is read there. There is one exception: a positive tab index counts when it is written on a tag whose attributes reach the rendered element, for example a hyphenated custom element.
- **Hidden elements are out.** An element that is not rendered is left out, along with everything inside it. This covers the `hidden` attribute, an inline `display: none`, and an element the repository's own style sheets hide.

## Unit and denominator
**Unit: one interactive element.** Each of these elements counts once, however many requirements apply to it:

- Every native control: an element with native control behaviour, for example a `button`, `select`, `textarea` or `summary`, an `a` or `area` with an href, or an `input` that is not hidden.
- Every element that one of the property's requirements applies to:
  - a non-interactive element with a click handler
  - a non-interactive element that the repository's style sheets give a pointer cursor
  - an element whose only event handlers are a mouse enter/leave pair, or a double-click handler
  - an element with a positive tab index
  - an anchor without an href
  - an anchor with a placeholder href that performs an action, either through a click handler or through a script URL in the href

**Counted: violations.** Each requirement an element fails is one violation, so one element can give more than one.

**Denominator:** the number of interactive elements across all files in the population.

These are not violations, because they are not the interactive behaviour the property describes:

- A click handler that only stops the event from spreading or only prevents its default action.
- A click handler on a backdrop whose click closes the dialog above it.
- A click handler that only moves focus to a control that is keyboard-operable.
- A click handler whose action a keyboard-operable control on the same page also performs.
- An element with a pointer cursor that wraps a control a keyboard user can focus, or that is the visible face of a native field, when the cursor serves that control.
- An anchor that is only a fragment destination: it has an id or name and no content.
- An anchor whose navigation comes from a framework routing attribute.
- An empty anchor with no click handler that exists only so that script can set a download address on it and click it.

## Thresholds
- **Positive tab index:** a value greater than 0. A value of 0 or below is not a violation.
- **Placeholder href:** an href that is exactly `#` as a literal, or that starts with `javascript:` (in any case).
- **Hover-only:** the element has both a mouse-enter and a mouse-leave handler. A lone enter handler or a lone leave handler is not hover-only.
- **Pointer cursor:** the repository's own style sheets give the element `cursor: pointer`, through any selector (for example a class rule).

## Score
Let V be the number of violations, N the number of interactive elements, and d = min(1, V / N).

- If V = 0, the score is **10**.
- Otherwise:
  - isolation = max(0, 1 − d / 0.10)
  - headroom = clamp(log10(N) / log10(200), 0, 1)
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), kept between 1 and the ceiling, rounded to 2 decimals.

So any violation caps the score at 8. A score above 6 needs a density below 10 % and more than one interactive element. A density of 100 % scores 1.

## Not measured when
- The repository has no web markup in its production code.
- The repository's only markup is in its own developer tools.
- The repository has no user-facing web interface, for example it is a library, a command-line tool, a worker or a headless service. A web API counts as having an interface only when it also serves a full HTML page.
- Its production markup holds fewer than 25 HTML elements, not counting framework component tags.
- The population holds no interactive element.

## What the result is not
- It is not a reading of the running page. It cannot see behaviour attached at run time or focus that script manages.
- It is not a test of what a framework component renders.
- It is not a WCAG conformance claim, and it is not an audit with assistive technology.
- A 10 means none of the listed requirements failed in the markup as written. It does not mean every control can be operated.

## Pending re-definition
These values are parameters of the existing reading's model, and today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Scale floor:** the dimension is not measured when the production markup holds fewer than 25 HTML elements.
- **Density knee:** the part of the ceiling above 6 drops to nothing once the violation density reaches 10 %.
- **Population saturation:** the ceiling's headroom grows with log10(N) and stops growing at 200 interactive elements.
- **Ceiling band:** any violation scores between 1 and 6, or up to 8 when the violations are few in a large, otherwise clean base.
