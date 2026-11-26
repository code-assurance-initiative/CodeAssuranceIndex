# AC6: Visual & motion safety

**Lens:** accessibility · **Scored:** yes

## Property
Whether focus outlines are not removed without a replacement focus style, motion (for example animations and transitions) is conditioned on the user's reduced-motion preference, and colour pairs written as literal values in the repository's own styles (including style tokens and utility classes that resolve to literal colours) meet a contrast ratio of at least 4.5:1. Colours computed at runtime or loaded from outside the repository are not assessed. Static markup readiness, not a WCAG (Web Content Accessibility Guidelines) conformance claim.

## Applies to
- **Web markup:** HTML, and markup written in component and view files, for example JSX, single-file components or Razor views.
- **Styles:** CSS and languages that compile to CSS, for example SCSS, Less or Sass. This includes style blocks inside markup and components, inline styles, utility classes, and styles written in script (for example CSS-in-JS or a component's inline styles).
- **Script code** that starts motion or reads the reduced-motion preference.

## Population
Production code as [production-code.md](../production-code.md) defines it, with these differences:

- **Server-side view templates count.** [production-code.md](../production-code.md) leaves them out. Here a server-side view template (for example a Razor view) is in scope, and its markup and styles count.
- **Only the product's user interface counts.** Markup that the repository's own developer tools build is left out.
- **Hidden elements are left out.** An element that no user can see is left out, along with everything inside it.
- **Components are not elements.** A framework component used as an element is not an element itself. Only the elements it renders count.
- **Rules that match nothing are left out.** A style rule is left out when no element in the repository can match it, for example a rule keyed only on class names that no markup uses. A CSS module's rules always count, because script code refers to their classes.

## Unit and denominator
There are three kinds of unit. Each unit counts once, whether it passes or fails.

- **Colour pair:** one style rule, one element's inline style, or one element's set of colour utility classes that leaves a text colour and a background colour in force, both from literal colours. It counts once, however many elements it matches. The background can be the rule's own or one painted by an enclosing element. When an enclosing element paints the background, that background pairs with the text colour of the rule or element inside it, and the pair counts once. These count as literal colours:
  - a colour value written directly in the repository's own styles, in whatever declaration form sets it;
  - a custom property or style token defined in the repository's own styles, taken as the literal colour it resolves to;
  - a utility class that resolves to a literal colour, taken as that colour. This includes a class whose colour comes from the built-in colour scale of a utility framework the repository depends on (for example `text-gray-500` on `bg-white`).

  A literal colour is fully opaque. A colour with transparency is not a literal colour, and a pair that uses one is not a unit. Transparency here means an alpha below 1, the keyword `transparent`, or a utility class with an opacity modifier (for example `bg-white/10`).

  The ratio is WCAG's: (L1 + 0.05) / (L2 + 0.05). L1 is the relative luminance of the lighter colour and L2 is that of the darker one. A rule that applies under several media conditions is one unit, judged at its lowest ratio across those conditions.

  **Exempt pairs are not units.** An exempt pair is in neither N nor V. A pair is exempt when it is:
  - large text with a ratio of at least 3:1. Large text is at least 18 pt (24 px), or at least 14 pt (about 18.67 px) with a font weight of 700 or more;
  - an icon glyph with a ratio of at least 3:1;
  - a pseudo-element that renders no content;
  - a rule that styles only an inactive (disabled) control.
- **Outline removal:** a declaration or utility class that sets the outline to `none` or `0`, either on an element that takes keyboard focus or on a selector that can match one.
- **Motion declaration:** an animation or transition of a motion property. The motion properties are position, size, transform (translate, rotate, scale), margin, inset, offset and background position. Changes to opacity, colour, filter or shadow are not motion.
  - A transition counts when it moves a motion property and lasts more than 0.01 s.
  - An animation counts when its keyframes change a motion property, or when its keyframes are not defined in the repository. Its duration is not checked.
  - Script calls that animate motion properties count too.

**Denominator N:** the number of units of all three kinds.

**Violations V:** the number of units that fail (see Thresholds).

## Thresholds
- **A colour pair fails** when its ratio is below 4.5:1.
- **An outline removal fails** when no visible focus style replaces it for that element. A replacement is a focus or focus-visible style that draws an outline, ring, box shadow, border or background. When an inline style removes the outline, only a style that draws something other than an outline counts as a replacement.
- **A motion declaration fails** when it is not conditioned on the reduced-motion preference. It is conditioned when any of these is true:
  - it is declared under a `prefers-reduced-motion` media condition;
  - a `prefers-reduced-motion: reduce` rule stops it. That rule is either for the same selector, or a universal reset whose stopping declaration is marked `!important`. The universal reset must be in the same style sheet or in one that the same part of the product loads. A rule stops motion when it sets durations to 0.01 s or less, sets the animation or transition to `none`, pauses the animation, or hides the element (`display: none`);
  - it uses a motion-safe or motion-reduce utility variant;
  - the script code that starts it reads the preference.

## Score
If V = 0, the score is 10.

Otherwise, with d = V / N:

- isolation = max(0, 1 − d / 0.10)
- headroom = min(1, log10(N) / log10(200))
- ceiling = 6 + 2 × isolation × headroom
- score = 1 + (1 − d) × (ceiling − 1), rounded to 2 decimals

The score therefore lies between 1 and the ceiling. Any violation caps it at 8. Once one unit in ten fails, the score is at most 6.

## Not measured when
- The repository has no user-facing web interface, for example a library, command-line tool, worker or headless service.
- The product's markup has fewer than 25 elements, not counting framework components (see Pending re-definition).
- N = 0, meaning no colour pair, outline removal or motion declaration was found.

## What the result is not
- It is not a WCAG conformance claim or an audit of the rendered page.
- It does not assess these colours: colours computed at runtime, third-party style sheets and themes loaded from outside the repository, translucent colours (an alpha below 1, `transparent`, or a utility opacity modifier), and text over images and gradients. A utility class that resolves to an opaque literal colour is assessed, even when its colour scale comes from a utility framework the repository depends on.
- It does not cover flashing content, focus order, or how visible a replacement focus style actually is.
- A 10 means no failing unit was found among the units that could be judged. It does not mean every colour, focus style and motion in the product is safe.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under decision 0013 will replace each one with a threshold on the property.

- **Ceiling terms:** a base ceiling of 6 and a lift of up to 2. The isolation term reaches 0 at a violation density of 0.10, and the headroom term reaches its maximum at a population of 200.
- **Interface size floor:** 25 elements in the product's markup.
- **Style files read:** the first 500 separate style files and single-file components, in path order. Colour pairs, outline removals, focus replacements, motion declarations, keyframes and reduced-motion resets in later files are not read. A file that cannot be read still counts toward the 500.
- **Script files read:** the first 800 script files, in path order. Colour pairs and motion declarations written in script in later files are not read. A file that cannot be read still counts toward the 800.
- **File size:** a style or script file larger than 1 MB is not read.
- **Reset loaders:** to find which parts of the product load a universal reduced-motion reset, at most 5,000 files are read, in path order. A reset that is loaded only from a file past that limit does not condition any motion.
- **Motion duration:** 0.01 s. A transition that lasts 0.01 s or less is not motion, and a reduced-motion rule stops motion when it sets durations to 0.01 s or less. An animation's own duration is not checked.
