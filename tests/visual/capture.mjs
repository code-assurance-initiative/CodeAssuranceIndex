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

const browser = await chromium.launch();
const entries = [];
let done = 0;

for (const { shape, path: pagePath } of sample) {
  const facts = {};
  for (const theme of THEMES) {
    for (const width of WIDTHS) {
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

/** The facts the page ASSERTS about its own inputs — what Phase 6 must be able to reproduce. */
async function pageFacts(page) {
  return page.evaluate(() => {
    const text = document.body.innerText;
    const grab = re => (text.match(re) ?? [])[1] ?? null;
    return {
      title: document.title,
      canonical: document.querySelector('link[rel=canonical]')?.getAttribute('href') ?? null,
      rubricVersion: grab(/(rubric-\d{4}\.\d{2}\.\d{2})/),
      cai: grab(/\bCAI\s+(\d{1,3})\b/),
      band: grab(/\b(Exemplary|Strong|Adequate|Weak|Critical)\b/),
      measured: grab(/(\d{1,2}\s+\w+\s+20\d\d)/),
      widgets: [...new Set([...document.querySelectorAll('*')]
        .map(e => e.tagName.toLowerCase()).filter(t => t.includes('-')))].sort(),
      elements: document.querySelectorAll('*').length,
    };
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
