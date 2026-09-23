// Settle a contrast question from the PIXELS, when the DOM cannot answer it.
//
//   node pixel-contrast.mjs --out <dir> [--base http://127.0.0.1:5098] [--path /state-of-the-corpus/]
//                           [--select 'button[data-theme-toggle]@1280' ...]
//
// ★★ WHY THIS EXISTS. axe's colour-contrast rule abstains — `incomplete`, not `pass` and not
//    `violation` — whenever it cannot resolve what is behind an element: a gradient, an image, or a
//    translucent header. `shoot.mjs` reports how many nodes it abstained on, and deliberately
//    reports NO ratio for them, because the obvious way to produce one is wrong: resolving the
//    element and measuring its computed colour against the first painted ancestor answers with ONE
//    LAYER, and the reason axe abstained is that there is no one layer. Done that way, this site's
//    theme toggle measured 3.45:1 and read as an AA failure on every page. It is not one. The
//    pixels say 6.05:1 in the light theme and 7.47:1 in the dark.
//
// ★ HOW. Screenshot the element itself at deviceScaleFactor 4, WHILE SCROLLED so the translucent
//   header is over page content — the state axe could not resolve — then inset 28% to drop the
//   button's own ring and the page around it. What is left is the glyph and the surface directly
//   under it, which is the pair the rule is about. The background is the modal colour; the ink is
//   the colour furthest from it in luminance that still covers 2% of the core, which excludes
//   antialiased edges without excluding a thin stroke.
//
// ★ Playwright resolves from the MAIN kennel checkout by absolute path — node_modules does not
//   exist inside a git worktree (CLAUDE.md).
import { chromium } from '/home/jimmy/RiderProjects/kennel.canine.dev/tools/localdev/ui/node_modules/playwright/index.mjs';
import { mkdir } from 'node:fs/promises';

const args = Object.fromEntries(
  process.argv.slice(2).reduce((pairs, arg, i, all) =>
    arg.startsWith('--') ? [...pairs, [arg.slice(2), all[i + 1]]] : pairs, []));

const base = (args.base ?? 'http://127.0.0.1:5098').replace(/\/$/, '');
const where = args.path ?? '/state-of-the-corpus/';
const out = args.out ?? './chrome-pixels';

// Each target is `selector@width`: the width matters, because a burger only exists narrow.
const targets = (args.select ? [args.select] : [
  'button[data-theme-toggle]@1280',
  '.ip-nav-burger@390',
]).map((spec) => {
  const [selector, width] = spec.split('@');
  return { selector, width: Number(width ?? 1280) };
});

const channel = (v) => (v / 255 <= 0.03928 ? v / 255 / 12.92 : (((v / 255) + 0.055) / 1.055) ** 2.4);
const luminance = ([r, g, b]) => (0.2126 * channel(r)) + (0.7152 * channel(g)) + (0.0722 * channel(b));
const ratio = (a, b) => {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
};

await mkdir(out, { recursive: true });
const browser = await chromium.launch();

for (const theme of ['light', 'dark']) {
  for (const { selector, width } of targets) {
    const page = await browser.newPage({
      viewport: { width, height: 900 }, colorScheme: theme, deviceScaleFactor: 4,
    });
    await page.goto(`${base}${where}`, { waitUntil: 'networkidle' });
    await page.evaluate(() => window.scrollTo(0, 400));
    await page.waitForTimeout(300);

    const element = await page.$(selector);
    if (!element) {
      console.log(`${theme.padEnd(5)} ${selector} — not present at ${width}px`);
      await page.close();
      continue;
    }

    const box = await element.boundingBox();
    // Read the pixels through the page itself: a canvas of the clipped shot, so no image decoder
    // is needed here and the numbers come from the same renderer that drew it.
    const shot = await element.screenshot();
    const pixels = await page.evaluate(async (data) => {
      const blob = new Blob([new Uint8Array(data)], { type: 'image/png' });
      const bitmap = await createImageBitmap(blob);
      const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
      const context = canvas.getContext('2d');
      context.drawImage(bitmap, 0, 0);
      const inset = 0.28;
      const x = Math.round(bitmap.width * inset);
      const y = Math.round(bitmap.height * inset);
      const w = Math.max(1, bitmap.width - (2 * x));
      const h = Math.max(1, bitmap.height - (2 * y));
      const { data: rgba } = context.getImageData(x, y, w, h);
      const counts = new Map();
      for (let i = 0; i < rgba.length; i += 4) {
        const key = `${rgba[i]},${rgba[i + 1]},${rgba[i + 2]}`;
        counts.set(key, (counts.get(key) ?? 0) + 1);
      }
      return { total: (w * h), counts: [...counts.entries()] };
    }, [...shot]);

    const counts = pixels.counts.map(([key, n]) => ({ rgb: key.split(',').map(Number), n }));
    const background = counts.reduce((a, b) => (a.n >= b.n ? a : b));
    const ink = counts
      .filter(c => c.n / pixels.total >= 0.02)
      .reduce((a, b) => (Math.abs(luminance(a.rgb) - luminance(background.rgb))
        >= Math.abs(luminance(b.rgb) - luminance(background.rgb)) ? a : b));

    const measured = ratio(ink.rgb, background.rgb);
    console.log(`${theme.padEnd(5)} ${selector.padEnd(28)} ${Math.round(box.width)}x${Math.round(box.height)}px  `
      + `ink rgb(${ink.rgb}) on rgb(${background.rgb})  ${measured.toFixed(2)}:1  `
      + `${measured >= 4.5 ? 'passes AA for text' : 'UNDER 4.5:1'}`);
    await page.close();
  }
}

await browser.close();
