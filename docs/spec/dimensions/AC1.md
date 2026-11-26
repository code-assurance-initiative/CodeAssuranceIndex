# AC1: Text alternatives

**Lens:** accessibility · **Scored:** yes

## Property
Whether non-text content carries a text alternative — img/area/input[type=image] have alt, a meaningful svg has a title or aria-label, video has a captions track, and object/embed/canvas have a name or fallback content. Static markup readiness, not a WCAG conformance claim.

## Applies to
Web markup (HTML) that the product renders, in any file that holds it. This includes HTML pages, markup written inside UI components (for example JSX, TSX, Vue or Svelte files) and server-side view templates (for example Razor views). Styles (CSS) matter only where they hide an element.

## Population
The population is production code as [production-code.md](../production-code.md) defines it, with these differences:

- **Also included:** server-side view templates. [production-code.md](../production-code.md) leaves them out, but they hold the markup the product renders.
- **Also excluded:** markup in the authors' own developer tools and scripts (for example a page that a measuring script builds). It is not the product's UI.
- **Framework components:** where a framework component is used (for example `<Avatar src=…>`), that use is not an element of this dimension. Its markup is read in the file that defines the component. There is one exception: an icon component (a component that only draws an icon, for example one from an icon library) with no name of its own, when it is non-text content of an unnamed link or button (see the content rules).

## Unit and denominator
A **naming attribute** is `aria-label` or `aria-labelledby`. Where a rule says so, `title` also counts.

**Hidden elements:** an element hidden from assistive technology passes every rule. Examples are `aria-hidden="true"`, the `hidden` attribute, or a style that does not display the element. An element whose attributes are all passed in from elsewhere (for example a spread of props) also passes.

**Element units.** Each of these is a unit, whether it passes or fails:

- **`img`:** passes when it has an `alt` attribute (`alt=""` marks it decorative), a naming attribute, or `role="presentation"` / `role="none"` while not focusable. It also passes when the component's own script sets the `alt` of that same element.
- **`area` with `href`, and `input type="image"`:** passes with a non-empty `alt`, a naming attribute, or a presentational role while not focusable. `alt=""` does not name a link or a button.
- **`svg role="img"`:** passes with a `<title>` child or a naming attribute.
- **`video` that declares its media** (a `src` or a `<source>` child): passes with a `<track>` whose kind is captions or subtitles, or that has no kind. A decorative video is not a unit. A video is decorative when it is muted and either has no controls or loops, or when it has `role="img"`. A video with no declared media (its stream is set at run time) is not a unit.
- **`audio` that declares its media:** passes with a captions or subtitles track, or with a transcript linked through `aria-describedby` or `aria-details`. Muted audio with no controls is not a unit.
- **`object` and `canvas`:** pass with a naming attribute or `title`, with any inner fallback content, or with a presentational role while not focusable.
- **`embed`:** passes with a naming attribute or `title`, or with a presentational role while not focusable.

**Content units.** Each rule below names content that fails. An element covered by these rules is a unit only when it fails. An inline svg with no role, an icon glyph or a ligature that fails no rule is not a unit. If the failing element is already an element unit, it still counts once and fails once.

1. **Only content of an unnamed link or button.** A control is an `a` with `href`, a `button`, or an element with role `button` or `link`. A control is unnamed when all of these are true:
   - neither it nor anything inside it has a naming attribute or `title`;
   - it holds no text, including text filled in at run time;
   - it holds no image with a non-empty `alt` and no named svg;
   - it holds no content whose text the file does not show (a component other than an icon component, or a slot).

   In an unnamed control, each of these items is a failing unit: an image with `alt=""`, an svg with no name, an icon component with no name, and an empty icon-font glyph. A control that holds an image with no `alt` is judged by the `img` rule only.
2. **Icon-font ligature.** A ligature fails when it is not inside a link or button that has a naming attribute. A ligature is a native element that meets all of these conditions:
   - its class names an icon or symbol font;
   - it has no child elements;
   - its whole text is one lower-case word (for example `<span class="material-icons">play_arrow</span>`).

   Assistive technology reads that word. A ligature with its own naming attribute or `role="img"` is not a unit.
3. **Empty icon-font glyph inside a link or button.** An empty glyph is an element with no content that draws an icon-font character through styles (for example `<i class="fa fa-save"></i>`). Inside a link or button, it fails unless the glyph has a role, or the glyph or the control has a naming attribute. Text inside the control does not save it: the glyph's generated character becomes part of the name read from that text. For example, `<button><i class="fa fa-save"></i> Save</button>` fails.
4. **Svg alone in its parent.** An inline svg outside any link or button fails when it has no role and no name (no `<title>`, `<text>`, naming attribute or `title`). Its parent must also be a plain HTML element that holds only this svg, with no text and no naming attribute or `title`. Content handed to a component is not judged.
5. **One fixed alt over different images.** An image with a literal, non-empty `alt` and no presentational role fails in either of two cases:
   - its source changes with each row of the list it repeats over;
   - in the same file, the same `alt` text (ignoring case and surrounding spaces) sits on images that have 2 or more different sources. Each such image fails.

The **denominator** n is the number of element units plus the number of failing content units that are not element units. The **count** v is the number of units that fail.

## Thresholds
- **Scale floor:** 25 HTML elements in the product's markup, not counting uses of framework components. Below this, the dimension is not measured.
- **Shared alt:** 2 or more different image sources under one alt text in one file.

## Score
- If v = 0, the score is 10.
- Otherwise, with density d = min(v / n, 1):
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, max(0, log10(n) / log10(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), kept between 1 and the ceiling, rounded to 2 decimals.

A single failure caps the score at 8. When density is 10 % or more, or when n is 1, the ceiling is 6. The score is 1 when every unit fails.

## Not measured when
- The production code holds no web markup, or its only markup is in its developer tools.
- The repository is not a user-facing web UI (for example a library, command-line tool, worker or headless service). A web API counts as a UI only when it also serves a full page.
- The product's markup holds fewer than 25 HTML elements (the scale floor).
- The denominator is 0.

## What the result is not
- It is not a WCAG conformance claim.
- It checks that an alternative exists, not that it describes the content well. An alt of "image" passes.
- It does not see content added at run time (images inserted by script, or media streams set at run time). It also does not see what a framework component renders where it is used.
- Whether content is decorative is read from the markup (`alt=""`, a presentational role, a muted silent video). The author's declaration is taken as given and not judged.
