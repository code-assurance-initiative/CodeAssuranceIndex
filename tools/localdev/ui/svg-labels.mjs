// The SVG-label checks shoot.mjs runs INSIDE the page — contrast against what is actually painted behind each
// label, and the size it actually rendered at.
//
// ★ These run in the BROWSER, not in node: Playwright serialises whatever it evaluates, so a helper defined out
//   here is invisible to a callback in there. They are written as ordinary named functions anyway — each one
//   readable and measurable on its own — and shipped into the page as source by `svgLabelSource`, wrapped so
//   that nothing but the one entry point lands on the page's window.

function luminance([r, g, b]) {
  const channel = (v) => (v / 255 <= 0.03928 ? v / 255 / 12.92 : (((v / 255) + 0.055) / 1.055) ** 2.4);
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

function parseColour(colour) {
  const parts = (colour ?? '').match(/[\d.]+/g)?.map(Number);
  return parts && parts.length >= 3 ? { rgb: parts.slice(0, 3), alpha: parts[3] ?? 1 } : null;
}

function contrastRatio(a, b) {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

// The first ancestor that actually paints — crossing shadow boundaries, because the widgets do.
function backdrop(node) {
  for (let el = node; el; el = el.parentElement ?? el.getRootNode()?.host) {
    const painted = parseColour(getComputedStyle(el).backgroundColor);
    if (painted && painted.alpha > 0) { return painted.rgb; }
  }
  return [255, 255, 255];
}

function isLabel(el) {
  return el.tagName.toLowerCase() === 'text' && (el.textContent ?? '').trim() !== '';
}

// A label's fill, when it actually draws something — null when it is fully transparent.
function drawnFill(el) {
  const fill = parseColour(getComputedStyle(el).fill);
  return fill && fill.alpha > 0 ? fill : null;
}

// How much the element's own screen matrix scales what the stylesheet declared.
function screenScale(el) {
  const matrix = el.getScreenCTM?.();
  return matrix ? Math.sqrt(Math.abs((matrix.a * matrix.d) - (matrix.b * matrix.c))) : null;
}

// ★★ THE THRESHOLD IS THE ONE AA ACTUALLY SETS, AND IT DEPENDS ON THE DRAWN SIZE — 3:1 only
//    for large text (24px, or 18.66px bold), 4.5:1 for everything else. An SVG scales its
//    own coordinate system, so "large" has to be measured after that scaling, not read off
//    the stylesheet: a 16px bar label inside a 720-unit viewBox is 8px on a phone and needs
//    4.5:1 there. axe cannot make this call at all — it does not check SVG text.
function aaFloor(el) {
  const style = getComputedStyle(el);
  const size = parseFloat(style.fontSize) * (screenScale(el) ?? 1);
  const bold = (parseInt(style.fontWeight, 10) || 400) >= 700;
  const large = size >= 24 || (size >= 18.66 && bold);
  return { size, floor: large ? 3 : 4.5 };
}

function failing(el, fill) {
  const { size, floor } = aaFloor(el);
  const behind = backdrop(el);
  const contrast = contrastRatio(fill.rgb, behind);
  if (contrast >= floor) { return null; }
  return {
    text: el.textContent.trim().slice(0, 24),
    owner: el.closest('svg')?.getAttribute('class') ?? el.getAttribute('class') ?? 'svg',
    fill: `rgb(${fill.rgb.join(',')})`,
    behind: `rgb(${behind.join(',')})`,
    contrast: Math.round(contrast * 100) / 100,
    needs: floor,
    size: Math.round(size * 10) / 10,
  };
}

// 9px is where a chart label stops being readable on a phone at arm's length. No standard
// fixes it, so the number is reported rather than merely judged.
function undersized(el) {
  const scale = screenScale(el);
  if (scale === null) { return null; }
  const declared = parseFloat(getComputedStyle(el).fontSize);
  const rendered = declared * scale;
  if (rendered >= 9) { return null; }
  return {
    text: el.textContent.trim().slice(0, 24),
    declared: Math.round(declared * 10) / 10,
    rendered: Math.round(rendered * 10) / 10,
  };
}

function inspectLabel(el, report) {
  const fill = drawnFill(el);
  const fault = fill && failing(el, fill);
  if (fault) { report.faint.push(fault); }
  const tooSmall = undersized(el);
  if (tooSmall) { report.tiny.push(tooSmall); }
}

function walkLabels(root, report) {
  for (const el of root.querySelectorAll('*')) {
    if (el.shadowRoot) { walkLabels(el.shadowRoot, report); }
    if (isLabel(el)) { inspectLabel(el, report); }
  }
}

function svgLabelReport() {
  const report = { faint: [], tiny: [] };
  walkLabels(document, report);
  return report;
}

const browserSide = [
  luminance, parseColour, contrastRatio, backdrop, isLabel, drawnFill, screenScale,
  aaFloor, failing, undersized, inspectLabel, walkLabels, svgLabelReport,
];

/** The checks above as one script for `page.addScriptTag`; it defines `window.__svgLabelReport` and nothing else. */
export const svgLabelSource =
  `(() => {\n${browserSide.map(String).join('\n\n')}\nwindow.__svgLabelReport = svgLabelReport;\n})();`;
