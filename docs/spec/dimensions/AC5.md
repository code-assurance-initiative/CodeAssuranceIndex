# AC5: ARIA correctness

**Lens:** accessibility · **Scored:** yes

## Property
Whether ARIA is used correctly — valid non-abstract roles, the ARIA state a role requires, valid (non-misspelled) aria-* attribute names, in-enum values for token-typed aria-* attributes, and no aria-hidden on (or wrapping) a focusable element. Static markup readiness, not a WCAG conformance claim.

## Applies to
Markup (HTML) in any file kind that holds it. For example: HTML pages, the markup in UI component files (JSX and TSX, Vue, Svelte, Razor components) and server-side view templates.

## Population
- The repository's markup that is production code as [production-code.md](../production-code.md) defines it, with two differences:
  - **Added:** server-side view templates (for example Razor or Blade views) are in scope because they hold the product's markup.
  - **Left out:** the authors' own developer tools (for example a page that a measuring script builds in the developer's browser).
- **Not rendered:** everything inside an element the page does not render is out of scope. The hiding element itself stays in scope. An element hides its contents with, for example, the `hidden` attribute, an inline `display: none`, or a class that the page's styles hide outright. Those styles can be the repository's own or a CSS framework's (for example `d-none`). A class that hides an element only visually (for example `sr-only`) or only at some breakpoints does not take its contents out of scope. If the repository's scripts reveal a hidden element later, that element counts as rendered, so its contents stay in scope. aria-hidden alone does not take anything out of scope.
- **Components:** a framework component's tag (for example `<Card>` or `<Tabs.Panel>`) is not an HTML element. Its markup is read where it is written.
- **Not judged:** a value set by an expression at render time (for example `role={r}` or `:aria-hidden="open"`). Only literal values are judged, unless a check below says otherwise. An element's attributes are "open" when they can come from an object at render time (for example a JSX spread, a Vue `v-bind="attrs"`, a Razor attribute splat or a template directive in attribute position). On such an element, the checks that need an attribute to be absent do not fire.

## Unit and denominator
- **Usage site** (denominator, P), any one of:
  - an HTML element in scope that has a `role` attribute with at least one token, or any `aria-*` attribute;
  - a toggle made of two controls, counted once per pair. The two controls are clickable sibling elements, each rendered on a different value of one state, and each one's own click handler writes that state. Neither control has `aria-expanded`, `aria-pressed`, `aria-checked` or `aria-selected`;
  - an icon-font ligature inside a named control. This is an element with no child elements whose class names an icon font and whose whole text is one lower-case ligature word (for example `close` or `chevron_left`). Its nearest operable ancestor has an `aria-label` or `aria-labelledby`.
- **Violation** (numerator, V): one breach of a rule under Thresholds. Every breach counts, and one element can add several violations unless a rule says otherwise. Violations are counted across the whole repository, with no cap per file.
- **Role:** an element's role is the first token of its `role` attribute, compared without regard to case. It is judged only when it is made of ASCII letters.
- **Focusable:** an element is focusable when HTML puts it in the sequential focus order. That means it has a `tabindex` of 0 or more, or it has no `tabindex` and is an element HTML makes focusable by default. For example: `a` or `area` with `href`, `button`, `select`, `textarea`, `input` of any type except `hidden`, and `summary`. A negative `tabindex` or the `hidden` attribute makes an element not focusable.
- **Native role:** the role HTML gives an operable element. `a` or `area` with `href` is a link. `button` and `summary` are buttons. `select` is a combobox, `textarea` is a textbox and `option` is an option. An `input` takes its role from its type:
  - `checkbox` is a checkbox and `radio` is a radio;
  - `button`, `submit`, `reset` and `image` are buttons;
  - `range` is a slider, `number` is a spinbutton and `search` is a searchbox;
  - any other type except `hidden` is a textbox.

## Thresholds
Each of these is one violation:
1. **Abstract role:** the role is `command`, `composite`, `input`, `landmark`, `range`, `roletype`, `section`, `sectionhead`, `select`, `structure`, `widget` or `window`.
2. **Invalid role:** the role is not abstract and is not one of the WAI-ARIA 1.2 concrete roles. The roles `comment`, `mark` and `suggestion` are also valid.
3. **Missing required state.** Each of these roles requires a state:
   - `checkbox`, `radio`, `switch`, `menuitemcheckbox` and `menuitemradio` require `aria-checked`;
   - `combobox` requires `aria-expanded`;
   - `slider` requires `aria-valuenow`;
   - `scrollbar` requires `aria-controls` and `aria-valuenow`.

   An `input` with a literal type of `checkbox` or `radio` already supplies `aria-checked`. One with a literal type of `range` already supplies `aria-valuenow`. An element that lacks one or more required states adds one violation, however many are missing. This check does not fire when the element's attributes are open.
4. **Unknown attribute:** an `aria-*` name that is neither a WAI-ARIA 1.2 state or property nor one of these WAI-ARIA 1.3 additions: `aria-description`, `aria-colindextext`, `aria-rowindextext`, `aria-braillelabel` and `aria-brailleroledescription`. The deprecated `aria-dropeffect` and `aria-grabbed` are also valid. Names are compared without regard to case.
5. **Out-of-enum value:** a literal value that is not blank and is not one of the allowed values below. Case is ignored.
   - `aria-checked` and `aria-pressed`: true, false, mixed, undefined.
   - `aria-expanded`, `aria-selected` and `aria-hidden`: true, false, undefined.
   - `aria-disabled`, `aria-readonly`, `aria-required`, `aria-busy`, `aria-atomic`, `aria-multiline`, `aria-multiselectable` and `aria-modal`: true, false.
   - `aria-current`: true, false, page, step, location, date, time.
   - `aria-haspopup`: true, false, menu, listbox, tree, grid, dialog.
   - `aria-invalid`: true, false, grammar, spelling.
   - `aria-live`: off, polite, assertive.
   - `aria-orientation`: horizontal, vertical, undefined.
   - `aria-sort`: ascending, descending, none, other.
   - `aria-autocomplete`: inline, list, both, none.
6. **Empty reference:** an id-reference attribute is present but blank. These attributes are `aria-labelledby`, `aria-describedby`, `aria-controls`, `aria-owns`, `aria-activedescendant`, `aria-details`, `aria-errormessage` and `aria-flowto`.
7. **Dangling reference:** an `aria-labelledby` or `aria-describedby` token names an id that no element in the repository's markup has and that the page's scripts do not create.
8. **State on a generic element:** a `div` or `span` with no `role` has `aria-expanded`, `aria-pressed`, `aria-checked` or `aria-selected`. Values set by an expression count too. Each such attribute is one violation.
9. **aria-hidden on focusable content:** a literal `aria-hidden="true"` on an element the page renders, where either:
   - the element itself is focusable; or
   - the element is not `inert` and has a descendant still in the tab order. A descendant is in the tab order when it is focusable and is not `disabled`, not `inert` and not itself aria-hidden.

   This rule does not apply when the element is the closed state of a region that a control in the repository opens. It also does not apply when the element sits under an `inert` or unrendered ancestor.
10. **Bound aria-hidden over focusable content:** `aria-hidden` is set by an expression on an element that has a descendant still in the tab order. A descendant with no `tabindex` counts. A descendant does not count when its `tabindex`, `disabled`, `inert` or `hidden` is set by an expression. The rule does not apply when the same state also removes the element from rendering or from the tab order (for example a bound `inert` or `hidden`, `v-show`, or a bound display style or hiding class).
11. **Wrong owned element:** each container role may own only certain roles:
    - `menu` and `menubar` may own `menuitem`, `menuitemcheckbox` and `menuitemradio`;
    - `listbox` may own `option`;
    - `tree` may own `treeitem`;
    - `tablist` may own `tab`.

    Inside the container, a focusable element's role is its explicit role. If it has no role, or has `none`, `presentation` or `generic`, its native role is used instead. A focusable element with no native role is "focusable with no role". One violation is added for each element whose role is an operable role, or "focusable with no role", and is not one the container may own. The operable roles are button, link, checkbox, radio, switch, textbox, searchbox, combobox, slider, spinbutton, tab, option, menuitem, menuitemcheckbox, menuitemradio and treeitem. For example, a plain `<button>` inside a `menu` is a violation.
    - Elements that are not focusable and have no role, or have `none`, `presentation` or `generic`, are looked through. So is `group`, except in a tablist.
    - An owned item is not looked into, and neither is an element with any other explicit role (for example a heading or a nested composite).
12. **Tab outside a tablist:** an element with role `tab` has no ancestor with role `tablist`, in a document that declares 2 or more tabs. A tab at the root of its document is excluded.
13. **Tab with no selection state:** an element with role `tab` has no `aria-selected`. A value set by an expression counts as present.
14. **Unnamed role:** a `dialog`, `alertdialog` or `radiogroup` has no `aria-label`, `aria-labelledby` or `title`, and is not a `fieldset` with a `legend`.
15. **Toggle state not exposed:** a toggle pair (see Unit) in which neither control has an accessible name.
16. **Ligature exposed:** an icon-font ligature inside a named control (see Unit) that has no `aria-hidden`.

## Score
The score uses V violations over P usage sites:
- If V = 0, the score is 10.
- Otherwise:
  - d = min(1, V / P)
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, max(0, log10(P) / log10(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), clamped to [1, ceiling] and rounded to 2 decimals.

Any violation caps the score at 8. When the density d is 10 % or more, the cap is 6. The score falls toward 1 as d nears 1.

## Not measured when
- The markup in scope has no usage site (P = 0).
- The repository has no web markup, or its only markup is the authors' developer tools.
- The repository has no user-facing web UI (for example a library, command-line tool, worker or headless service). A web API that serves a full page document counts as having a UI.
- The product's markup has fewer than 25 HTML elements. Rendered and unrendered elements both count. Components and the authors' developer tools do not (see Pending re-definition).
- An engine cannot read the repository's UI markup. The engine reports the dimension as not measured and names this as a gap in its own reading, not as a fact about the repository.

## What the result is not
- It is not a WCAG conformance claim or an audit of the rendered page.
- It does not see ARIA that scripts set at run time. A valid literal role is not proof that the role matches the element's behaviour.
- It does not judge whether a role allows each attribute, except for the four states on a generic `div` or `span`.
- It does not judge what a component renders at the places where the component is used.
- A score of 10 means no breach of the rules above was found. It does not mean the ARIA is complete.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.
- **Scale floor:** 25 HTML elements in the product's markup. Rendered and unrendered elements both count. Components and the authors' developer tools do not. Below this, the dimension is not measured.
- **Score ceiling:** the 10 % density at which a lone violation's ceiling drops to 6, and the 200 usage sites at which the ceiling reaches 8.
- **Tab-set size:** a document must declare 2 or more tabs before a tab outside a tablist counts.
