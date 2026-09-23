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
    ['survey-portrait', first(/^surveys\/github\//)],
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
          const contrast = ratio(fill.rgb, backdrop(el));
          if (contrast < 3) {
            texts.push({
              text: el.textContent.trim().slice(0, 24),
              owner: el.closest('svg')?.getAttribute('class') ?? el.getAttribute('class') ?? 'svg',
              fill: `rgb(${fill.rgb.join(',')})`,
              behind: `rgb(${backdrop(el).join(',')})`,
              contrast: Math.round(contrast * 100) / 100,
            });
          }
        }
      };
      walk(document);
      return texts;
    });

    // Empty = nothing but (at most) a heading. That is the shape a wrongly-formatted prop produces.
    const empty = islands.filter(i => i.body === 0);
    found.push({ name, url, status, islands: islands.length, emptyIslands: empty.length, faint, file });

    if (status !== 200) { failures++; }
    console.log(`${status === 200 ? 'ok  ' : 'FAIL'} ${name.padEnd(18)} ${String(status).padEnd(4)} ` +
      `islands=${islands.length} empty=${empty.length}  ${url}`);
    if (empty.length > 0) {
      console.log(`     ★ ${empty.length} island(s) rendered NOTHING BUT A HEADING: ${empty.map(i => i.tag).join(', ')}`);
      failures++;
    }
    for (const t of faint) {
      console.log(`     ★ SVG TEXT AT ${t.contrast}:1 — "${t.text}" ${t.fill} on ${t.behind} (${t.owner})`);
      failures++;
    }
  }

  await writeFile(path.join(out, 'shapes.json'), JSON.stringify(found, null, 2));
} finally {
  await browser.close();
}

console.log(`\n${found.length} shapes captured into ${out}`);
process.exit(failures > 0 ? 1 : 0);
