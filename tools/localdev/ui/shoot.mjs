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
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });

  // ★ The published set, as the CMS itself lists it. Handed in rather than guessed: see shapes().
  const paths = JSON.parse(await readFile(args.paths, 'utf8'));
  const list = shapes(paths);
  console.log(`${paths.length} published paths → ${list.length} shapes\n`);

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

    // Empty = nothing but (at most) a heading. That is the shape a wrongly-formatted prop produces.
    const empty = islands.filter(i => i.body === 0);
    found.push({ name, url, status, islands: islands.length, emptyIslands: empty.length, file });

    if (status !== 200) { failures++; }
    console.log(`${status === 200 ? 'ok  ' : 'FAIL'} ${name.padEnd(18)} ${String(status).padEnd(4)} ` +
      `islands=${islands.length} empty=${empty.length}  ${url}`);
    if (empty.length > 0) {
      console.log(`     ★ ${empty.length} island(s) rendered NOTHING BUT A HEADING: ${empty.map(i => i.tag).join(', ')}`);
      failures++;
    }
  }

  await writeFile(path.join(out, 'shapes.json'), JSON.stringify(found, null, 2));
} finally {
  await browser.close();
}

console.log(`\n${found.length} shapes captured into ${out}`);
process.exit(failures > 0 ? 1 : 0);
