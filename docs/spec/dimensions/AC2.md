# AC2: Forms & labels

**Lens:** accessibility · **Scored:** yes

## Property
Whether form fields (for example text inputs, selects and text areas) have a programmatic label (a label element associated with the field by reference or by wrapping it, aria-label or aria-labelledby), buttons have an accessible name (text content, aria-label or aria-labelledby), links have an accessible name, a non-interactive element (for example a div or span) that has a click handler and declares a control role gives that control an accessible name, fieldsets have a non-empty legend, a field component that takes its label through a property (for example a UI library's text-field component with a label prop) is given that property, and a placeholder is not used as the only label. A reading of the markup as written in source, not of the rendered page; not a WCAG (Web Content Accessibility Guidelines) conformance claim.

## Applies to
The markup for the product's web user interface, in any language and however the source writes it. This covers:

- HTML files;
- markup inside code, for example JSX and TSX, Vue and Svelte single-file components, tagged templates, and HTML written as string literals and inserted into the page;
- server-side view templates, for example Razor, Blade, Twig and ERB, including dialects that write markup without angle brackets (for example Pug or Haml);
- elements built by calls in code, for example DOM factory calls or an HTML builder library.

## Population
The population is production code as [production-code.md](../production-code.md) defines it, with these differences:

- **In addition:** server-side view templates are read (for example Razor, Blade, Twig, ERB). [production-code.md](../production-code.md) otherwise leaves server-side templates out.
- **Left out:** markup that the authors' own developer tools build (for example a report page a measuring script writes). It is not the product's user interface.
- **Hidden elements:** an element hidden from users is not a unit, and nothing inside it is a unit. For example, it has the `hidden` attribute or `aria-hidden="true"`, or a style hides it.
- **Other components:** a component that is not a field component (see Unit) is not judged where it is used. The markup it renders is judged in its own file.

## Unit and denominator
The unit is one element of the kinds below. The denominator is the number of these elements in the population. Each unit is either named or fails, and it fails at most once.

- **Form field:** an `input` of any type except `hidden`, `submit`, `button`, `reset` or `image`, a `select` or a `textarea`. A framework's own form component that renders exactly one such control is also a form field (for example Blazor's `InputText` or `InputSelect`).
- **Button:** a `button` element.
- **Link:** an `a` with an `href`.
- **Custom control:** an element that is not natively interactive, has a click handler and declares a control role (for example `role="button"`). An element inside a button or link is part of that control, not a separate one.
- **Fieldset:** a `fieldset`.
- **Field component:** each use of a component that takes its label through a property, for example a UI library's text field, form-control primitive or switch.

## Thresholds
These rules apply to every unit:

- A placeholder is never a name, and neither is the `name` attribute.
- An `aria-labelledby` names nothing when no element carries the id it points at, or when that element has no text.
- A value bound at run time (a dynamic value) counts as present.
- An element whose attributes or content come from its caller (for example through a spread or an attribute splat) is named when its callers give it a name.

Each kind of unit is named as follows:

- **Form field:** named when it has any of these:
  - an `aria-label`;
  - an `aria-labelledby` that names something;
  - a label associated with it by reference (the label's `for` names the field's id, however that id is produced);
  - a label wrapping it, or a wrapper component that is given the label text and holds only this field.

  A form field inside a reusable component is named when the label each caller gives reaches that field.
- **Button and link:** named by any of these:
  - text content inside it, literal or bound, including text that a component inside it or its caller supplies;
  - an `aria-label` or `aria-labelledby`, on it or on something inside it;
  - an image inside it that has a non-empty alt;
  - an `svg` inside it that has its own name.

  Content made only of decorative icons is not a name, and neither is `alt=""`.
- **Custom control:** named in the same ways as a button.
- **Fieldset:** passes when it has a `legend` with content (text, bound text or a component). It fails when it has no legend or its legend is empty.
- **Field component:** named when it is given its label property, or is otherwise given an accessible name: an `aria-label`, an `aria-labelledby` (for example through the library's labelled-by property, such as `labelId`), or a label associated with it by reference or by wrapping. A group component (for example a radio group) needs its name on the group itself. The text that a combobox-like trigger displays is not a name.

## Score
Let V be the number of failing units and N the denominator.

- If V = 0, the score is 10.
- Otherwise:
  - d = min(V / N, 1)
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, log10(N) / log10(200)), and headroom = 0 when N = 1
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1)

  The score is clamped between 1 and the ceiling, then rounded to 2 decimals.

Any failure caps the score at 8. A single failure in a large, otherwise clean population can come close to 8. At a failure density of 10% or more, the cap is 6. When failures are everywhere, the score falls towards 1.

## Not measured when
- The repository has no product markup in scope.
- The repository has no user-facing web interface, for example a library, command-line tool, worker or headless service. A web API that also serves a full page document does have one.
- The product markup in scope holds fewer than 25 HTML elements. Uses of components do not count towards the 25.
- The denominator is 0 because there are no units.

## What the result is not
- It is not a WCAG conformance claim, and it is not a reading of the rendered page. A label wired at run time, or through a component library's context, can read as missing.
- It does not judge whether a name is meaningful, only whether one is there.
- It does not cover image text alternatives (AC1) or keyboard operability (AC4).

## Pending re-definition
These are parameters of the existing reading's model, and today's scores depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Scale floor:** below 25 HTML elements in the product markup, the dimension is not measured.
- **Score ceiling:** the cap is 6 and rises to 8 through two terms:
  - isolation falls to 0 at a failure density of 10%;
  - population headroom reaches 1 at a population of 200.
