// Captures the visual baseline for docs/plans/cai-owns-its-pages.md.
//
// ★★ THE POINT OF THIS FILE IS THE MANIFEST, NOT THE PICTURES. Pages change because scans change, so a
//    before/after diff of LIVE pages proves nothing: the two captures would differ for reasons that have
//    nothing to do with who composed them. Every capture therefore records the facts the page itself
//    asserts — score, band, rubric version, measured date, commit — so Phase 6 can replay the same inputs
//    and compare like with like, and can say out loud when it cannot.
//
// ★ THE SAMPLE IS SEEDED AND RECORDED, not "a few I happened to open". A baseline nobody can reproduce is
//   an anecdote.
//
// Usage:  node tests/visual/capture.mjs <outDir> [--shapes a,b] [--manifest path]
import { chromium } from '/home/jimmy/RiderProjects/kennel.canine.dev/tools/localdev/ui/node_modules/playwright/index.mjs';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const ORIGIN = 'https://codeassuranceindex.info';
const WIDTHS = [1280, 820, 390];
const THEMES = ['light', 'dark'];
const SEED = 20260922;

const outDir = process.argv[2] ?? './baseline';
const manifestPath = arg('--manifest') ?? path.join(outDir, 'manifest.json');
const onlyShapes = arg('--shapes')?.split(',');

function arg(name) {
  const i = process.argv.indexOf(name);
  return i > 0 ? process.argv[i + 1] : undefined;
}

/** Deterministic PRNG, so the same corpus yields the same sample on any machine. */
function rng(seed) {
  let s = seed >>> 0;
  return () => (s = (s * 1664525 + 1013904223) >>> 0) / 4294967296;
}

/** Which shape a path belongs to — the taxonomy the sitemap actually has, not the one we assumed. */
function shapeOf(p) {
  if (p === '/surveys/') return 'survey-index';
  if (p === '/state-of-the-corpus/') return 'corpus-index';
  if (/^\/surveys\/lang\/[^/]+\/$/.test(p)) return 'survey-language';
  if (/^\/surveys\/[^/]+\/[^/]+\/[^/]+\/$/.test(p)) return 'survey-portrait';
  if (/^\/surveys\/[^/]+\/.+\/$/.test(p)) return 'survey-portrait-deep';
  if (/^\/state-of-the-corpus\/language\/[^/]+\/$/.test(p)) return 'corpus-language';
  if (/^\/state-of-the-corpus\/advisory\/[^/]+\/$/.test(p)) return 'corpus-advisory';
  if (/^\/state-of-the-corpus\/package\/[^/]+\/$/.test(p)) return 'corpus-package';
  if (/^\/state-of-the-corpus\/country\/[^/]+\/$/.test(p)) return 'corpus-country';
  if (/^\/state-of-the-corpus\/[^/]+\/$/.test(p)) return 'corpus-listing';
  return null;
}

/**
 * ★ Repositories chosen to DIFFER, not at random. A baseline of three healthy six-lens repositories would
 *   pass a builder that silently drops a missing lens or cannot draw a one-reading trajectory.
 */
const MUST_INCLUDE = new Set([
  '/surveys/github/ruben-rasmussen/auth/',                       // 18 readings — the longest climb
  '/surveys/github/phoenix-tui/phoenix/',                        // 4 lenses — two not measured
  '/surveys/github/code-assurance-initiative/codeassuranceindex/', // the standard measuring itself
]);

const PER_SHAPE = { 'survey-index': 1, 'corpus-index': 1, 'corpus-listing': 2 };

const sitemap = await (await fetch(ORIGIN + '/sitemap.xml')).text();
const paths = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map(m => m[1].slice(ORIGIN.length));

const byShape = new Map();
for (const p of paths) {
  const s = shapeOf(p);
  if (!s) continue;
  if (!byShape.has(s)) byShape.set(s, []);
  byShape.get(s).push(p);
}

const next = rng(SEED);
const sample = [];
for (const [shape, all] of [...byShape].sort()) {
  if (onlyShapes && !onlyShapes.includes(shape)) continue;
  const want = PER_SHAPE[shape] ?? 3;
  const sorted = [...all].sort();                       // stable order before sampling
  const picked = sorted.filter(p => MUST_INCLUDE.has(p.toLowerCase()));
  while (picked.length < Math.min(want, sorted.length)) {
    const candidate = sorted[Math.floor(next() * sorted.length)];
    if (!picked.includes(candidate)) picked.push(candidate);
  }
  for (const p of picked) sample.push({ shape, path: p });
}

console.log(`shapes ${byShape.size} · pages ${sample.length} · captures ${sample.length * WIDTHS.length * THEMES.length}`);
fs.mkdirSync(outDir, { recursive: true });

const factsOnly = process.argv.includes('--facts-only');
const browser = await chromium.launch();
const entries = [];
let done = 0;

for (const { shape, path: pagePath } of sample) {
  const facts = {};
  for (const theme of factsOnly ? [THEMES[0]] : THEMES) {
    for (const width of factsOnly ? [WIDTHS[0]] : WIDTHS) {
      const ctx = await browser.newContext({
        viewport: { width, height: 1000 },
        colorScheme: theme,
        deviceScaleFactor: 1,
        reducedMotion: 'reduce',
      });
      const page = await ctx.newPage();
      try {
        await page.goto(ORIGIN + pagePath, { waitUntil: 'networkidle', timeout: 45000 });
        // The theme is pinned rather than inferred: the sheet honours an explicit data-theme over the
        // media query, and a capture that depended on the query alone would drift with the OS.
        await page.evaluate(t => document.documentElement.setAttribute('data-theme', t), theme);
        // Scroll the whole page so every lazy island hydrates; an unhydrated island captures as empty.
        await page.evaluate(() => new Promise(done => {
          let y = 0;
          const step = setInterval(() => {
            window.scrollBy(0, 1200);
            if ((y += 1200) > document.body.scrollHeight + 2000) { clearInterval(step); window.scrollTo(0, 0); done(); }
          }, 30);
        }));
        await page.waitForTimeout(700);

        if (!Object.keys(facts).length) Object.assign(facts, await pageFacts(page));

        const name = `${shape}__${slug(pagePath)}__${width}__${theme}.png`;
        const file = path.join(outDir, name);
        if (factsOnly) { await ctx.close(); continue; }
        await page.screenshot({ path: file, fullPage: true });
        const bytes = fs.readFileSync(file);
        entries.push({
          shape, path: pagePath, width, theme, file: name,
          bytes: bytes.length,
          sha256: crypto.createHash('sha256').update(bytes).digest('hex'),
        });
      } catch (e) {
        entries.push({ shape, path: pagePath, width, theme, error: String(e.message).slice(0, 160) });
        console.log('ERROR', pagePath, width, theme, String(e.message).slice(0, 90));
      }
      await ctx.close();
    }
  }
  Object.assign(sample.find(s => s.path === pagePath), { facts });
  console.log(`  ${++done}/${sample.length}  ${shape}  ${pagePath}`);
}

await browser.close();

/**
 * The facts the page ASSERTS about its own inputs — what Phase 6 must be able to reproduce.
 *
 * ★★ THE FIRST VERSION LOOKED FOR "CAI 97.6" AND FOUND NOTHING, on every portrait, because the score is
 *    rendered as a bare stat figure and the label sits beneath it. A manifest whose most important field
 *    is silently null is worse than no manifest: it reads as "this page has no score" rather than "I did
 *    not look in the right place". Each field below is now read from a phrase the PAGE actually writes,
 *    and `missing` says out loud which ones did not resolve.
 */
async function pageFacts(page) {
  return page.evaluate(() => {
    const text = document.body.innerText;
    const grab = re => (text.match(re) ?? [])[1] ?? null;

    const facts = {
      title: document.title,
      canonical: document.querySelector('link[rel=canonical]')?.getAttribute('href') ?? null,
      // ★★ READ STRUCTURALLY, NOT BY PHRASE. Two attempts failed here: "CAI 97.6" is not how the page
      //    writes it, and "Where 97.6 sits on the scale" IS on the page but lives inside an island's
      //    shadow root, which innerText does not pierce. What the light DOM has is a stat band whose
      //    level-2 headings ARE the figures, each paired with the label beneath it — so the figures are
      //    read from there, with their labels, and nothing is inferred from prose that may move.
      stats: [...document.querySelectorAll('.ip-ap-stat-band')].flatMap(band => {
        const figures = [...band.querySelectorAll('h2')].map(h => h.textContent.trim());
        const labels = [...band.querySelectorAll('p')].map(e => e.textContent.trim()).filter(Boolean);
        return figures.map((figure, i) => ({ figure, label: labels[i] ?? null }));
      }),
      band: grab(/\b(Exemplary|Strong|Adequate|Weak|Critical)\b/),
      rubricVersion: grab(/(rubric-\d{4}\.\d{2}\.\d{2})/),
      // "Measured at commit 658c7b49f7" — the exact code a score is about, which is what makes a
      // reading reproducible at all.
      commit: grab(/commit\s+([0-9a-f]{6,40})\b/i),
      measuredAt: grab(/taken on (\d{1,2}\s+\w+\s+20\d\d)/) ?? grab(/(\d{1,2}\s+\w+\s+20\d\d)/),
      // The trend's own caption is inside the island too, so the reading COUNT comes from the stat
      // band's third figure — the one labelled "measurements over time".
      readings: null,
      widgets: [...new Set([...document.querySelectorAll('*')]
        .map(e => e.tagName.toLowerCase()).filter(t => t.includes('-')))].sort(),
      elements: document.querySelectorAll('*').length,
    };

    // The score and the reading count, named off the labels the page itself prints beside them.
    const labelled = re => facts.stats.find(s => s.label && re.test(s.label))?.figure ?? null;
    // ★ NO "FIRST FIGURE" FALLBACK. It made the surveys index report cai=6,276 — which is the count of
    //   published codebases, not a score — and a language page report cai=325. A manifest that labels a
    //   population as a CAI is not a weaker record, it is a false one, and Phase 6 would be comparing
    //   against a lie. A figure is the score only when the page's own label names a band beside it.
    facts.cai = labelled(/Exemplary|Strong|Adequate|Weak|Critical/);
    facts.readings = labelled(/measurements over time/);

    // ★ Say which ones did not resolve, rather than leaving a null to be read as an absence in the
    //   data. A manifest whose most important field is silently null reads as "this page has no
    //   score" when it means "I did not look in the right place" — which is what happened twice.
    facts.missing = Object.entries(facts)
      .filter(([k, v]) => v === null && k !== 'canonical')
      .map(([k]) => k);
    return facts;
  });
}

function slug(p) { return p.replace(/^\/|\/$/g, '').replace(/[^a-z0-9]+/gi, '-').toLowerCase(); }

const manifest = {
  capturedAt: new Date().toISOString(),
  origin: ORIGIN,
  seed: SEED,
  widths: WIDTHS,
  themes: THEMES,
  note: 'Baseline for docs/plans/cai-owns-its-pages.md Phase 2. Phase 6 replays the deliveries behind '
      + 'these pages and requires a zero-pixel diff; a page whose facts below have changed in the '
      + 'meantime CANNOT be compared and must be re-based, not waved through.',
  pages: sample,
  captures: entries,
};
fs.mkdirSync(path.dirname(manifestPath), { recursive: true });
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
console.log(`\nmanifest -> ${manifestPath}`);
console.log(`captured ${entries.filter(e => !e.error).length}, failed ${entries.filter(e => e.error).length}`);
