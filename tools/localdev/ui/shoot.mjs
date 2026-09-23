// Screenshot the standard's published pages, so somebody can LOOK at them.
//
// ★★ THIS IS NOT AN ASSERTION RUNNER, AND THAT IS DELIBERATE. What the node trees contain is already
//    held by tests — the producer diffs, the island contract, the page-shape checks. What no test can
//    answer is whether imprint turns those trees into a page a reader can use. So this takes pictures
//    of every shape and prints where they are; the verdict is a human looking at them.
//
// ★ ONE SHOT PER SHAPE, not per page: 6,000 portraits are one shape, and a screenshot run that
//   scales with the corpus is one nobody waits for.
//
// ★ Playwright is resolved from the MAIN kennel checkout by absolute path — node_modules does not
//   exist inside a git worktree, and a relative import silently picks up a different browser build.
import { chromium } from '/home/jimmy/RiderProjects/kennel.canine.dev/tools/localdev/ui/node_modules/playwright/index.mjs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const args = Object.fromEntries(
  process.argv.slice(2).reduce((pairs, arg, i, all) =>
    arg.startsWith('--') ? [...pairs, [arg.slice(2), all[i + 1]]] : pairs, []));

const base = (args.base ?? 'http://127.0.0.1:5098').replace(/\/$/, '');
const out = args.out ?? './shots';

// ★★ A SECOND READING IS A DIFFERENT READING. Every defect this harness has caught so far was found
//    in one viewport and one theme — 1280px, light. A page that is right there can still be wrong in
//    the dark scheme (the site follows prefers-color-scheme through CSS light-dark(), so the browser
//    decides it, not a class) or at a phone width, where a table or a stat band has nowhere to go.
//    Both are a flag rather than a separate script, so the run stays one command and the shapes,
//    the emptiness check and the published-path list cannot drift between variants.
const width = Number(args.width ?? 1280);
const height = Number(args.height ?? 900);
const theme = args.theme === 'dark' ? 'dark' : 'light';

/**
 * The shapes the standard publishes — one representative of each, chosen from the site's OWN list of
 * published paths.
 *
 * ★★ READ FROM THE PUBLISHED SET, NOT FROM LINKS ON A PAGE. The first version of this walked
 *    `a[href]` from the indexes and found four shapes out of ten: the links that matter live INSIDE
 *    islands, which render their own markup, so a page-level query sees a fraction of them. A
 *    screenshot run that silently skips six shapes is worse than one that fails — it reports success
 *    over the pages nobody looked at.
 *
 * ★ ONE SHOT PER SHAPE, not per page: six thousand portraits are one shape, and a run that scales
 *   with the corpus is one nobody waits for.
 */
function shapes(paths) {
  const first = (match) => paths.find(p => match.test(p));

  return [
    ['surveys-index', 'surveys'],
    // ★★ THREE PORTRAITS, BECAUSE A PORTRAIT'S SHAPE IS ITS NUMBER OF MEASUREMENTS. One reading is
    //    the most common state in production — every repository's first survey — and it is the page
    //    that published "1 measurements over time" for months; two readings is the shortest trend
    //    the chart will draw. Neither had ever been rendered here: the seed gave every subject the
    //    same three deliveries, so only the three-point form was ever looked at.
    ['survey-portrait', first(/^surveys\/github\/acme-(?!once|twice)/)],
    ['survey-portrait-once', first(/^surveys\/github\/acme-once\//)],
    ['survey-portrait-twice', first(/^surveys\/github\/acme-twice\//)],
    // ★★ AND THE PAGE ALMOST EVERY SUBJECT WOULD GET TODAY. Every subject in the live registry holds
    //    a MINOR 1.0 delivery — no languages, no origin, no security reading — so this is what the
    //    standard publishes for them the moment publication is granted. It had never been drawn.
    ['survey-portrait-thin', first(/^surveys\/github\/acme-thin\//)],
    ['field-guide', first(/^surveys\/lang\//)],
    ['corpus-sheet', 'state-of-the-corpus'],
    ['corpus-languages-index', first(/^state-of-the-corpus\/languages$/)],
    ['corpus-language', first(/^state-of-the-corpus\/language\//)],
    ['corpus-countries-index', first(/^state-of-the-corpus\/countries$/)],
    ['corpus-country', first(/^state-of-the-corpus\/country\//)],
    ['advisory-index', first(/^state-of-the-corpus\/advisories$/)],
    ['advisory', first(/^state-of-the-corpus\/advisory\//)],
    ['package-index', first(/^state-of-the-corpus\/packages$/)],
    ['package', first(/^state-of-the-corpus\/package\//)],
  ].filter(([, p]) => Boolean(p)).map(([name, p]) => [name, `/${p}/`]);
}

// ★ axe-core comes from the MAIN kennel checkout by absolute path, for the same reason Playwright does.
const axeSource = await readFile(
  '/home/jimmy/RiderProjects/kennel.canine.dev/tools/localdev/ui/node_modules/axe-core/axe.min.js', 'utf8');

const browser = await chromium.launch();
let failures = 0;
const found = [];

try {
  await mkdir(out, { recursive: true });
  const page = await browser.newPage({ viewport: { width, height }, colorScheme: theme });

  // ★ The published set, as the CMS itself lists it. Handed in rather than guessed: see shapes().
  const paths = JSON.parse(await readFile(args.paths, 'utf8'));
  const list = shapes(paths);
  console.log(`${paths.length} published paths → ${list.length} shapes · ${width}px · ${theme}\n`);

  for (const [name, href] of list) {
    const url = href.startsWith('http') ? href : `${base}${href}`;
    const response = await page.goto(url, { waitUntil: 'networkidle' });
    const status = response?.status() ?? 0;

    // ★ SCROLL BEFORE SHOOTING. The header is translucent, so what a full-page shot shows at the top
    //   depends on scroll position — and an unscrolled capture reports a page nobody sees.
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await page.waitForTimeout(400);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(200);

    const file = path.join(out, `${name}.png`);
    await page.screenshot({ path: file, fullPage: true });

    // ★★ AN ISLAND THAT RENDERED ONLY ITS HEADING IS THE FAILURE THIS RUN EXISTS TO CATCH, and the
    //    first version of this check missed it. It measured textContent, and these islands render
    //    their `heading` prop — so a trend chart with no line and a gauge board with no gauges both
    //    counted as "has text" while the page showed a title over a blank gap. The check now asks
    //    whether anything was rendered BESIDE the heading: element children, or text beyond it.
    const islands = await page.$$eval('*', nodes => nodes
      .filter(n => n.tagName.toLowerCase().startsWith('cai-'))
      .map(n => {
        const tag = n.tagName.toLowerCase();
        const root = n.shadowRoot ?? n;
        const heading = root.querySelector('h1, h2, h3, h4')?.textContent?.trim() ?? '';
        const text = (root.textContent ?? '').trim();
        const beyondHeading = text.replace(heading, '').trim();
        return {
          tag,
          defined: Boolean(customElements.get(tag)),
          elements: root.querySelectorAll('*').length,
          body: beyondHeading.length,
        };
      }));

    // ★★ SVG TEXT WITH NO `fill` IS BLACK, AND IN THE DARK SCHEME BLACK IS THE BACKGROUND. The trend
    //    chart's end label — the one number a reader looks for — declared font and weight and no
    //    fill, so it inherited the SVG default and rendered at 1.1:1 on #0F1115. Nothing caught it:
    //    the island had rendered, the text was in the DOM, axe does not check SVG text, and every
    //    previous look at this page was in the light scheme, where black is correct.
    //
    //    So this asks the RENDER, not the stylesheet: for every drawn <text>, the resolved fill
    //    against the first painted background above it. 3:1 is the large-text floor and these are
    //    all chart labels; anything under it is reported with both colours so the fix is obvious.
    const faint = await page.evaluate(() => {
      const luminance = ([r, g, b]) => {
        const channel = (v) => (v / 255 <= 0.03928 ? v / 255 / 12.92 : (((v / 255) + 0.055) / 1.055) ** 2.4);
        return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
      };
      const parse = (colour) => {
        const parts = (colour ?? '').match(/[\d.]+/g)?.map(Number);
        return parts && parts.length >= 3 ? { rgb: parts.slice(0, 3), alpha: parts[3] ?? 1 } : null;
      };
      const ratio = (a, b) => {
        const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
        return (hi + 0.05) / (lo + 0.05);
      };

      // The first ancestor that actually paints — crossing shadow boundaries, because the widgets do.
      const backdrop = (node) => {
        for (let el = node; el; el = el.parentElement ?? el.getRootNode()?.host) {
          const painted = parse(getComputedStyle(el).backgroundColor);
          if (painted && painted.alpha > 0) { return painted.rgb; }
        }
        return [255, 255, 255];
      };

      const texts = [];
      const walk = (root) => {
        for (const el of root.querySelectorAll('*')) {
          if (el.shadowRoot) { walk(el.shadowRoot); }
          if (el.tagName.toLowerCase() !== 'text') { continue; }
          if ((el.textContent ?? '').trim() === '') { continue; }
          const fill = parse(getComputedStyle(el).fill);
          if (!fill || fill.alpha === 0) { continue; }
          // ★★ THE THRESHOLD IS THE ONE AA ACTUALLY SETS, AND IT DEPENDS ON THE DRAWN SIZE — 3:1 only
          //    for large text (24px, or 18.66px bold), 4.5:1 for everything else. An SVG scales its
          //    own coordinate system, so "large" has to be measured after that scaling, not read off
          //    the stylesheet: a 16px bar label inside a 720-unit viewBox is 8px on a phone and needs
          //    4.5:1 there. axe cannot make this call at all — it does not check SVG text.
          const style = getComputedStyle(el);
          const matrix = el.getScreenCTM?.();
          const zoom = matrix ? Math.sqrt(Math.abs((matrix.a * matrix.d) - (matrix.b * matrix.c))) : 1;
          const size = parseFloat(style.fontSize) * zoom;
          const bold = (parseInt(style.fontWeight, 10) || 400) >= 700;
          const large = size >= 24 || (size >= 18.66 && bold);
          const floor = large ? 3 : 4.5;
          const contrast = ratio(fill.rgb, backdrop(el));
          if (contrast < floor) {
            texts.push({
              text: el.textContent.trim().slice(0, 24),
              owner: el.closest('svg')?.getAttribute('class') ?? el.getAttribute('class') ?? 'svg',
              fill: `rgb(${fill.rgb.join(',')})`,
              behind: `rgb(${backdrop(el).join(',')})`,
              contrast: Math.round(contrast * 100) / 100,
              needs: floor,
              size: Math.round(size * 10) / 10,
            });
          }
        }
      };
      walk(document);
      return texts;
    });

    // ★★ AND HOW BIG IT ACTUALLY CAME OUT. An SVG scales its whole coordinate system to the container,
    //    so a label set in "12px" inside a 720-unit viewBox is 12px on a desktop and 6.5px on a phone —
    //    the same markup, the same stylesheet, and a number a reader cannot see. Nothing in the DOM
    //    changes, so this is only ever visible in a render at that width. Measured through the element's
    //    own screen matrix rather than assumed from the CSS.
    const tiny = await page.evaluate(() => {
      const found = [];
      const walk = (root) => {
        for (const el of root.querySelectorAll('*')) {
          if (el.shadowRoot) { walk(el.shadowRoot); }
          if (el.tagName.toLowerCase() !== 'text') { continue; }
          if ((el.textContent ?? '').trim() === '') { continue; }
          const matrix = el.getScreenCTM?.();
          if (!matrix) { continue; }
          const scale = Math.sqrt(Math.abs((matrix.a * matrix.d) - (matrix.b * matrix.c)));
          const declared = parseFloat(getComputedStyle(el).fontSize);
          const rendered = declared * scale;
          // 9px is where a chart label stops being readable on a phone at arm's length. No standard
          // fixes it, so the number is reported rather than merely judged.
          if (rendered < 9) {
            found.push({
              text: el.textContent.trim().slice(0, 24),
              declared: Math.round(declared * 10) / 10,
              rendered: Math.round(rendered * 10) / 10,
            });
          }
        }
      };
      walk(document);
      return found;
    });

    // ★★ AND WHAT AXE MAKES OF IT, IN BOTH THEMES — nothing had ever run it over these pages. The
    //    producer's console has had an axe gate for months; the standard's own pages, which are the
    //    public artefact, had none. Run TWICE, at the top and scrolled: the header is translucent, so
    //    what sits behind a heading depends on scroll position and an unscrolled pass reports clean
    //    (CLAUDE.md, "RUN AXE AFTER SCROLLING").
    const violations = [];
    const unchecked = [];
    for (const offset of [0, 400]) {
      await page.evaluate((y) => window.scrollTo(0, y), offset);
      await page.waitForTimeout(150);
      await page.addScriptTag({ content: axeSource });
      const run = await page.evaluate(async () => {
        const result = await window.axe.run(document, {
          runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] },
        });
        // ★★ WHAT AXE COULD NOT DECIDE IS NOT THE SAME AS WHAT IT PASSED, and only the violations
        //    array is usually read. Its colour-contrast rule moves an element to `incomplete` the
        //    moment it cannot resolve what is behind it — a gradient, an image, a translucent
        //    overlay — so a page can report zero violations while nobody has checked its text at
        //    all. Carried through as its own line rather than folded in or dropped.
        // ★★ WHAT AXE COULD NOT DECIDE IS NOT WHAT IT PASSED, and only the violations array is
        //    usually read. Its colour-contrast rule moves an element to `incomplete` the moment it
        //    cannot resolve what is behind it — a gradient, an image, a translucent header — so a
        //    page can report zero violations while some of its text was never checked at all.
        //
        //    ★★ THE COUNT IS REPORTED AND NO RATIO IS. An earlier version of this resolved each node
        //    and measured its computed colour against the first painted ancestor, which put the
        //    theme toggle at 3.45:1 and read as a site-wide AA failure. It was not one: the pixels
        //    say 6.05:1 in light and 7.47:1 in dark. A backdrop walk answers with ONE LAYER, and the
        //    reason axe abstained is that there is no one layer — the header is translucent. A
        //    number derived from the assumption axe refused to make is a guess wearing a number, and
        //    it manufactures findings. `pixel-contrast.mjs` beside this file settles one properly.
        window.__axeIncomplete = result.incomplete
          .filter(v => v.id === 'color-contrast')
          .flatMap(v => v.nodes.map(n => ({ id: v.id, where: [n.target].flat().join(' ') })));
        // ★ The first node's selector and axe's own summary travel WITH the count. A violation id and a
        //   tally sends the next person back to the browser to find out what it was about.
        return result.violations.map(v => ({
          id: v.id,
          impact: v.impact,
          nodes: v.nodes.length,
          where: v.nodes[0]?.target?.join(' ') ?? '',
          why: (v.nodes[0]?.failureSummary ?? '').split('\n').filter(Boolean).pop() ?? '',
        }));
      });
      for (const v of run) {
        if (!violations.some(seen => seen.id === v.id)) { violations.push(v); }
      }
      const undecided = await page.evaluate(() => window.__axeIncomplete ?? []);
      for (const v of undecided) {
        if (!unchecked.some(seen => seen.where === v.where)) { unchecked.push(v); }
      }
    }
    await page.evaluate(() => window.scrollTo(0, 0));

    // Empty = nothing but (at most) a heading. That is the shape a wrongly-formatted prop produces.
    const empty = islands.filter(i => i.body === 0);
    found.push({ name, url, status, islands: islands.length, emptyIslands: empty.length, faint, tiny, violations, unchecked, file });

    if (status !== 200) { failures++; }
    console.log(`${status === 200 ? 'ok  ' : 'FAIL'} ${name.padEnd(18)} ${String(status).padEnd(4)} ` +
      `islands=${islands.length} empty=${empty.length}  ${url}`);
    if (empty.length > 0) {
      console.log(`     ★ ${empty.length} island(s) rendered NOTHING BUT A HEADING: ${empty.map(i => i.tag).join(', ')}`);
      failures++;
    }
    for (const t of faint) {
      console.log(`     ★ SVG TEXT AT ${t.contrast}:1, NEEDS ${t.needs}:1 — "${t.text}" ${t.size}px `
        + `${t.fill} on ${t.behind} (${t.owner})`);
      failures++;
    }
    // ▲ REPORTED EVERY RUN, AND DELIBERATELY NOT A FAILURE. The cause is known and written down in the
    //   renderer (imprint `cai-trend.js`: "an SVG drawn 329px wide draws them at 5.5px... reported as
    //   unreadable by a person rather than caught by a rule") and the answer is a design decision about
    //   what a 720-unit chart does at phone width — hide the axis and lean on the sentence that already
    //   states the endpoints, or re-scale the labels from a measured container width. Until somebody
    //   decides, failing the run every time would teach a reader to ignore the whole report, and passing
    //   silently would publish 5px text. So it says the number, loudly, and leaves the exit code alone.
    for (const v of violations) {
      console.log(`     ★ AXE ${v.impact ?? 'unknown'}: ${v.id} on ${v.nodes} node(s) — ${v.where}`);
      if (v.why) { console.log(`       ${v.why}`); }
      failures++;
    }
    if (unchecked.length > 0) {
      const names = [...new Set(unchecked.map(v => v.where.replace(/^.*[ ,]/, '')))];
      console.log(`     ▲ AXE COULD NOT DECIDE contrast on ${unchecked.length} node(s): `
        + `${names.slice(0, 4).join(', ')}${names.length > 4 ? `, +${names.length - 4} more` : ''}`);
      console.log('       (not a finding — settle one with pixel-contrast.mjs, which reads the render)');
    }
    if (tiny.length > 0) {
      const worst = tiny.reduce((a, b) => (a.rendered <= b.rendered ? a : b));
      console.log(`     ▲ ${tiny.length} SVG label(s) RENDER UNDER 9px — smallest "${worst.text}" `
        + `declared ${worst.declared}px, drawn at ${worst.rendered}px (open: see the plan)`);
    }
  }

  await writeFile(path.join(out, 'shapes.json'), JSON.stringify(found, null, 2));
} finally {
  await browser.close();
}

console.log(`\n${found.length} shapes captured into ${out}`);
process.exit(failures > 0 ? 1 : 0);
