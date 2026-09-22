#!/usr/bin/env node
// W11 — the corpus pages as a READER gets them. Drives an ALREADY RUNNING Imprint.Site and the
// Watchdog host's public mirrors (it boots nothing; ../run-corpus-e2e.sh owns booting), and
// asserts:
//
//   1. every share on the page appears together with its denominator and its basis, and the
//      counts actually produce the printed percentage;
//   2. the index and the site AGREE about which pages exist — a page the index links to must
//      answer 200, and a page it does not link to must answer 404. Absence is VERIFIED, not
//      tolerated;
//   3. a count published as a FLOOR is accompanied by the number of surveys whose advisory list
//      was cut short;
//   4. no stat band has a SHORT LAST ROW at any width this page is read at (see SLABS below);
//   5. the machine-readable mirrors carry no bare share, and never confuse an unmeasured cell
//      with a measured zero;
//   6. no section is a heading with nothing under it, and no page states no measurement at all.
//
//   node tools/localdev/ui/corpus-e2e.mjs --base http://127.0.0.1:5098 --api http://127.0.0.1:8175 --out <dir>
//
// ★★ SLABS, AND WHY ONE VIEWPORT WAS NOT ENOUGH. A stat band lays out as
//    `repeat(auto-fit, minmax(min(240px,100%), 1fr))` and paints its cell WALLS as a hairline
//    background showing through 1px gaps between opaque cells. `auto-fit` collapses a track only
//    when the WHOLE COLUMN is empty, which never happens once there are more cells than columns —
//    so a short last row is not white space, it is a filled grey slab the width of the missing
//    cells. This driver ran green over two of those in both themes because it looked at one
//    width. The column count is a function of the width, so the defect is too, and a run at a
//    single width can only ever find the counts that happen to be short there.
//
// ★ node_modules DOES NOT EXIST INSIDE A GIT WORKTREE (kennel CLAUDE.md). Playwright is a
//   normal dependency of the MAIN checkout's tools/localdev/ui only, so this file resolves it
//   from there by absolute path — derived from `git rev-parse --git-common-dir`, never hardcoded.

import { execFileSync } from "node:child_process";
import { createRequire } from "node:module";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));

/** The main checkout's UI tooling directory — the ONE place node_modules exists. */
function toolingDirectory(explicit) {
  if (explicit) return explicit;
  const common = execFileSync("git", ["rev-parse", "--path-format=absolute", "--git-common-dir"], {
    cwd: here, encoding: "utf8",
  }).trim();
  return join(dirname(common), "tools", "localdev", "ui");
}

/** The corpus root. */
export const ROOT = "/state-of-the-corpus/";

/** A UTC day `offset` days from today, as `yyyy-MM-dd` — the shape a dated archive page was addressed by. */
function day(offset) {
  const d = new Date(Date.now() + (offset * 86_400_000));
  return d.toISOString().slice(0, 10);
}

/** The survey registry's root — the other family this publisher writes. */
export const SURVEYS = "/surveys/";

/**
 * The top-level pages the corpus INDEX is responsible for offering. Membership here is not a
 * claim that a page exists — it is a claim that the index either links to it or it must not be
 * served at all.
 */
export const CATALOGUE = [
  // ★★ THESE SIX ARE RETIRED AND THEY STAY IN THE CATALOGUE, WHICH IS THE POINT. Their whole
  //    reading is now the first two sections of the sheet (the five topic pages) or its §5 Series
  //    (the archive), so the builder stopped producing them and the sweep withdraws them. Listed
  //    here, judgeExistence turns that into an assertion: the index must not link them AND they must
  //    answer 404. Deleting the entries would make the withdrawal unobservable — a page the sweep
  //    failed to withdraw would simply stop being looked at, and would go on serving figures frozen
  //    on the day it was last built.
  { id: "coverage", path: `${ROOT}coverage/`, h1: null },
  { id: "vulnerabilities", path: `${ROOT}vulnerabilities/`, h1: null },
  { id: "disclosure-policy", path: `${ROOT}disclosure-policy/`, h1: null },
  { id: "secrets", path: `${ROOT}secrets/`, h1: null },
  { id: "supply-chain", path: `${ROOT}supply-chain/`, h1: null },
  { id: "archive", path: `${ROOT}archive/`, h1: null },
  // ★★ AND THE DATED DAY PAGES WITH IT — 56 addresses on the day the archive was retired, and one
  //    more every night had it stayed. There is no fixed list of them to walk, so the two days this
  //    local imprint's seed can possibly have written are probed by name. An archive that was still
  //    being built would serve today's, unlinked, and judgeExistence calls an unlinked 200 a page no
  //    reader can reach.
  { id: "dated-today", path: `${ROOT}${day(0)}/`, h1: null },
  { id: "dated-yesterday", path: `${ROOT}${day(-1)}/`, h1: null },
  { id: "advisories", path: `${ROOT}advisories/`, h1: "Advisories across the corpus", family: "advisories" },
  { id: "packages", path: `${ROOT}packages/`, h1: null, family: "packages" },
  { id: "countries", path: `${ROOT}countries/`, h1: null, family: "countries" },
  // ★ THE LANGUAGE FAMILY IS THE SAME SHAPE OVER A DIFFERENT CUT, and it has to be walked for the
  // same reason the others are: an unwalked page family is an unverified one, which is exactly how
  // every advisory page in this corpus stayed a 404 until real advisory ids existed. Membership
  // here is not a claim that it exists — a reading whose languages all sit under the gate publishes
  // no index at all, and judgeExistence reads that as a verified absence when the index does not
  // link to it either.
  { id: "languages", path: `${ROOT}languages/`, h1: null, family: "languages" },
];

/** The sentence the index prints when this reading has no security half at all. */
export const NO_SECURITY_HALF = "This reading has no security half";

/**
 * The widths this run reads the pages at, and the arithmetic behind each.
 *
 * ★ CHOSEN FROM THE BREAKPOINT, NOT FROM A DEVICE LIST. A stat band's track count is
 * `floor((W + gap) / (minItem + gap))` with `minItem` 240px and `gap` 1px, over the SECTION's
 * content width W (the viewport less the page gutters, ~80px a side at desktop and less when
 * narrow). So the track count changes at W = 481, 722, 963, 1204:
 *
 *     W  <  481  → 1 track      722 ≤ W < 963  → 3 tracks     W ≥ 1204 → 5 tracks
 *   481 ≤ W < 722 → 2 tracks    963 ≤ W < 1204 → 4 tracks
 *
 * 1280 lands the content at ~1120 ⇒ 4 tracks: it catches every count that is not 1, 2 or 4 —
 * the 5-cell bands, and the 6-cell band the retired archive day pages carried. It CANNOT catch a
 * 3-cell band, because 3 < 4 means the fourth column is wholly empty and auto-fit collapses it.
 * 680 lands the content near
 * the middle of the 2-track band ⇒ 3 and 5 both go short. Two widths, and between them every
 * cell count from 3 to 6 is read at a width where it is short if it can be.
 */
export const VIEWPORTS = [
  { label: "1280", width: 1280, height: 900, why: "content ~1120px ⇒ 4 tracks: catches 5- and 6-cell bands" },
  { label: "680", width: 680, height: 900, why: "content ~630px ⇒ 2 tracks: catches 3- and 5-cell bands" },
];

/** How many detail pages to follow per family. */
const DETAIL_SAMPLE = 3;

/** How many survey pages to follow off the registry index. */
const SURVEY_SAMPLE = 3;

/** `12,345` → 12345. */
export function toNumber(text) {
  return Number(text.replace(/,/g, ""));
}

/** Every `%` in the text, with the 220 characters that follow it. Positions, not a global regex,
 *  because the point is to find the percentages that DON'T match and a non-matching one leaves
 *  no trace in a match list. */
export function shareSites(text) {
  const sites = [];
  for (let i = text.indexOf("%"); i !== -1; i = text.indexOf("%", i + 1)) {
    sites.push({ at: i, before: text.slice(Math.max(0, i - 40), i), window: text.slice(i, i + 220) });
  }
  return sites;
}

/** `% of <basis> (N of M)` — Figure.Headline()'s Ratio shape, anchored at the percent sign. */
const IN_PROSE = /^% of ([^()]{3,160}?) \((\d[\d,]*) of (\d[\d,]*)\)/;

/**
 * The same reading as a STAT CELL: the value alone in the display slot, then an optional label
 * line, then the counts and the basis on their own line.
 *
 * ★★ THE SECOND SHAPE EXISTS BECAUSE THE PAGES STOPPED READING AS PROSE. Every corpus stat cell
 * used to be handed the whole headline — "76.8% of surveys whose dependencies a scanner could
 * resolve (1,161 of 1,511)" — and set it in heading type, which is why the pages were four lines
 * of text per cell. A cell is now built from the figure and splits it: value large, label, then
 * "1,161 of 1,511 surveys whose dependencies a scanner could resolve" beneath. NOTHING A READER
 * IS OWED HAS MOVED — the counts and the phrase naming the population are still within a line of
 * the percentage, which is what this check exists to insist on. Only the punctuation changed, and
 * a checker that reads punctuation rather than the claim reports forty problems that are not
 * there. Both shapes are accepted and both still yield a basis, a numerator and a denominator.
 *
 * ★ THE GAP IS NOT ANCHORED ON A NEWLINE, and the first attempt at this check was — which failed on
 * every page, because the driver reads textContent and a `<br>` contributes no character to it. It
 * is a bounded run of anything but another percent sign: the label, if there is one, and nothing
 * else. What stops a bare percentage pairing with some unrelated count further down the page is not
 * the gap length, it is the ARITHMETIC CHECK below — a numerator and denominator that belong to a
 * different figure do not divide to the printed share, and that is reported as a failure with both
 * numbers in it.
 */
const IN_A_CELL = /^%\s*[^%]{0,120}?\b(\d[\d,]*) of (\d[\d,]*)\s+([^%]{3,160})/;

/** Verdict for one percentage on the page. */
export function judgeShare(site) {
  const value = /(\d[\d,]*(?:\.\d+)?)$/.exec(site.before);
  const prose = IN_PROSE.exec(site.window);
  const cell = prose ? null : IN_A_CELL.exec(site.window);
  if (!prose && !cell) {
    return { ok: false, reason: "no denominator or basis beside it", text: `${value ? value[1] : "?"}%${site.window.slice(1, 60)}` };
  }

  const [, basis, numerator, denominator] = prose
    ? prose
    : [null, cell[3], cell[1], cell[2]];
  const stated = value ? Number(value[1].replace(/,/g, "")) : NaN;
  const n = toNumber(numerator);
  const d = toNumber(denominator);
  const printed = `${value ? value[1] : "?"}% of ${basis} (${numerator} of ${denominator})`;

  if (d === 0) return { ok: false, reason: "denominator is zero — a share of nothing", text: printed };
  if (n > d) return { ok: false, reason: "numerator exceeds its denominator", text: printed };
  const derived = Math.round((n / d) * 1000) / 10;
  if (!Number.isFinite(stated) || Math.abs(derived - stated) > 0.051) {
    return { ok: false, reason: `arithmetic: ${n}/${d} is ${derived}%, the page says ${stated}%`, text: printed };
  }
  return { ok: true, text: printed, basis, numerator: n, denominator: d };
}

/**
 * Does the site agree with the index about one page?
 *
 * A 404 is a PASS when the index does not offer the page — the builder keeping its promise that a
 * page with nothing to say is absent rather than empty. A 404 is a FAILURE when the index links
 * to it, and a 200 is a FAILURE when it does not.
 */
export function judgeExistence({ path, linked, status }) {
  if (linked && status === 200) return { ok: true, note: "linked and served" };
  if (linked) return { ok: false, reason: `the index links to ${path}, which answers ${status} — a dead link` };
  if (status === 404) return { ok: true, note: "absent, and the index does not offer it", verifiedAbsence: true };
  if (status === 200) {
    return {
      ok: false,
      reason: `${path} is served but the index does not link to it — a page no reader can reach, and `
        + "possibly one an earlier sweep left behind with figures on it",
    };
  }
  return { ok: false, reason: `${path} answers ${status}; expected 200 (linked) or 404 (absent)` };
}

/**
 * A floor must arrive with the size of the blind spot it is a floor because of.
 *
 * ★ THE COUNT AND ITS BASIS ARE NO LONGER ONE STRING, AND WIDENING THE GAP IS NOT SOFTENING THE
 * CHECK. This looked for `<n> surveys whose advisory list was cut short` with a single space,
 * which is what `Figure.Headline()` writes — and it was the only rendering these pages had while
 * the whole reading was set in a paragraph. Every figure on the corpus root is SPLIT now: the
 * value in the display slot, the population beneath it, and the (i) affordance between them,
 * which reads as a bare "i". So the census phrase arrives as `300 i surveys whose advisory list
 * was cut short` and a single-space probe reported a defect the page does not have.
 *
 * What is still required is exactly what was required before — a NUMBER, and the phrase naming
 * the censored population, close enough together that a reader takes them as one reading. The gap
 * is bounded at eight non-digit characters, which admits the affordance and nothing that could be
 * a sentence: a page stating the phrase with the count somewhere else still fails.
 */
export function judgeFloorDisclosure(text) {
  const claimsFloor = /\ba floor\b/.test(text);
  if (!claimsFloor) return { applicable: false, ok: true };
  const disclosed = /\d[\d,]*[^\d]{0,8}surveys whose advisory list was cut short/.test(text);
  return {
    applicable: true,
    ok: disclosed,
    reason: disclosed
      ? null
      : "the page calls a count a floor but never says how many surveys' advisory lists were cut "
        + "short — a floor with no censored-population count beside it reads as a total",
  };
}

/**
 * A family index that lists no page of its own is not automatically a defect: the country index
 * publishes a gate, and on a small corpus every country is under it. What must never happen is an
 * index that simply ends, leaving a reader with a list of nothing and no account of where the
 * entries went.
 */
export function judgeFamilyIndex(text, detailCount) {
  if (detailCount > 0) return { ok: true, note: `lists ${detailCount} page(s)` };
  const explains = /\b(no page|not listed)\b[^.]{0,120}\bbecause\b/.test(text);
  return explains
    ? { ok: true, empty: true, note: "lists nothing, and says which gate held everything back" }
    : {
        ok: false,
        reason: "lists no page of its own and does not say why — a reader who followed a link from "
          + "the index arrives at a list of nothing with no account of where the entries went",
      };
}

/**
 * "1 dated readings", "1 measured codebases".
 *
 * `Figure` writes a count as `<n> <basis>` and a basis is a plural phrase, so at n=1 every count
 * in the system disagrees with its own noun. Pre-existing, general, and somebody else's to fix —
 * but a reader sees it, so the driver names it rather than letting it pass as prose.
 */
const NOT_A_PLURAL = new Set([
  "across", "always", "analysis", "as", "bus", "gas", "has", "his", "is", "its", "less", "plus",
  "process", "status", "this", "thus", "was", "yes",
]);

/**
 * The words that start a relative clause, and therefore end the run this judge is reading.
 *
 * ★★ THIS JUDGE READ THE WRONG NOUN AND FAILED CORRECT ENGLISH. A basis may qualify its noun —
 * "1 survey whose dependencies a scanner could resolve" counts SURVEYS, and `dependencies`
 * belongs to the clause describing one of them. Taking the last word of the run reported that as
 * a disagreement, and a judge whose failures include correct prose is a judge whose failures stop
 * being read. So the run is governed by the word BEFORE the pronoun — and only that far: when
 * that word is itself plural the disagreement is real and is still reported, which is what keeps
 * this from being a waiver list wearing a grammar rule's clothes.
 */
const OPENS_A_CLAUSE = new Set(["that", "when", "where", "which", "who", "whom", "whose"]);

export function pluralDisagreements(text) {
  const found = [];
  // A basis is a PHRASE ("dated readings", "measured codebases"), so the plural noun is not
  // necessarily the word after the digit — up to three words, and the LAST one is the noun. A
  // digit anywhere in the run ends it, which is what keeps "(1 of 3 surveys)" out: that 1 is a
  // numerator, not a count, and its noun belongs to the denominator.
  for (const m of text.matchAll(/(?<![\d,.])\b1 ((?:[a-z][a-z-]* ){0,2}[a-z][a-z-]*s)\b/g)) {
    const words = m[1].split(" ");
    const clause = words.findIndex((w) => OPENS_A_CLAUSE.has(w));
    // `> 0`, not `>= 0`: a run that OPENS with a pronoun has no head noun in front of it to be
    // governed by, so there is nothing to truncate to and the last word remains the best guess.
    const run = clause > 0 ? words.slice(0, clause) : words;
    const noun = run[run.length - 1];
    // Truncating can leave a singular head ("survey"), which is the whole point and not a finding.
    if (!noun.endsWith("s") || NOT_A_PLURAL.has(noun)) continue;
    found.push(`"1 ${run.join(" ")}"`);
  }
  return [...new Set(found)];
}

/**
 * A stat band whose last row is short.
 *
 * `n > tracks` because a band with fewer cells than tracks has a wholly empty column, which
 * auto-fit collapses; `n % tracks !== 0` because a band that divides evenly fills every row it
 * starts. Anything else leaves `tracks - (n % tracks)` unfilled cells in the last row, and an
 * unfilled cell in this appearance is the hairline background — a grey slab, not white space.
 */
export function judgeBand({ id, cells, tracks }) {
  if (tracks < 1 || cells <= tracks || cells % tracks === 0) return { ok: true, id, cells, tracks };
  const unfilled = tracks - (cells % tracks);
  return {
    ok: false, id, cells, tracks, unfilled,
    reason: `stat band #${id}: ${cells} cells over ${tracks} columns leaves ${unfilled} unfilled `
      + "cell(s) in the last row — a filled grey slab, not white space",
  };
}

/** Read every stat band on the page as the browser has actually laid it out. */
async function bands(page) {
  return page.evaluate(() => [...document.querySelectorAll(".ip-ap-stat-band .ip-grid")].map((g) => ({
    id: (g.closest("section") || {}).id || "(unnamed)",
    cells: g.children.length,
    // The COMPUTED template, so this reads the tracks the browser actually made at this width
    // rather than re-deriving them from the stylesheet — and only the LIVE ones. auto-fit reports
    // a collapsed track as `0px`, and a zero-width track paints nothing, so counting it would
    // make a one-cell band look like a band three cells short of its row.
    tracks: getComputedStyle(g).gridTemplateColumns
      .split(/\s+/).filter((t) => parseFloat(t) > 0.5).length,
  })));
}

/** Sections that promise a reader something and deliver nothing. A section holding only the
 *  page's <h1> is NOT hollow: that is the title. */
async function hollowSections(page) {
  return page.$$eval("main section", (sections) =>
    sections
      .map((section) => {
        if (section.querySelector("section")) return null;
        const heading = section.querySelector("h1,h2,h3,h4,h5,h6");
        if (heading && heading.tagName === "H1") return null;

        const clone = section.cloneNode(true);
        clone.querySelectorAll("h1,h2,h3,h4,h5,h6").forEach((h) => h.remove());
        const body = (clone.textContent || "").replace(/\s+/g, " ").trim();
        const cells = section.querySelectorAll(".ip-grid > *").length;
        if (body.length > 0 || cells > 0) return null;

        const band = section.className.includes("stat-band");
        return {
          id: section.id || "(unnamed)",
          what: band && !heading
            ? "a stat band with no cells — an invisible empty strip"
            : `heading "${heading ? (heading.textContent || "").trim() : "(none)"}" with nothing under it`,
        };
      })
      .filter(Boolean));
}

/** Walk the page the way a reader does — the islands hydrate lazily on an IntersectionObserver. */
async function scrollThrough(page) {
  await page.evaluate(async () => {
    const step = Math.round(window.innerHeight * 0.8);
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y);
      await new Promise((r) => setTimeout(r, 120));
    }
    window.scrollTo(0, 0);
  });
  await page.waitForTimeout(600);
}

/**
 * The text a READER sees under `root`: the light DOM AND the rendered content of every open shadow
 * root, spliced in at the host, in document order.
 *
 * ★★ WHY THIS IS NOT `innerText`. `innerText` STOPS AT A SHADOW BOUNDARY, and every `cai-*` island
 * on these pages attaches an OPEN shadow root and assigns into it. So everything an island rendered
 * was invisible to judgeShare, judgeFloorDisclosure, pluralDisagreements and the "no figure outside
 * the provenance line" check — while the LINK checks kept working, because Playwright's CSS engine
 * DOES pierce an open root. That asymmetry is the dangerous kind: a check that cannot see its
 * subject does not fail, it goes quiet, and quiet reads as a pass. It cost nothing while the
 * islands held a chart and some link cards and almost no percentage; it costs the whole of check 1
 * now that the headline findings, the coverage bar and the by-language table are islands.
 *
 * ★ DOCUMENT ORDER IS LOAD-BEARING. judgeShare reads a 220-character window FORWARD from the `%`,
 * so a splice that appended shadow text after light text would pair a share with a STRANGER's
 * counts — and pass, wrongly, whenever the stranger's arithmetic happened to hold.
 *
 * ★ A HOST'S OWN CHILDREN ARE NOT RENDERED unless a `<slot>` takes them: the light DOM of an island
 * is its pre-hydration placeholder, and reading both would put "Loading…" beside the figure that
 * replaced it, and would double-count anything the placeholder shares with the real content.
 *
 * ★ HIDDEN IS NOT READ. The (i) tooltips sit in the markup at all times at `visibility:hidden`;
 * collecting them puts explanation a reader cannot see into the reading, and feeds percentages that
 * are not on the page into the share check.
 *
 * It takes its `styleOf` and its `skipSelector` through `options` and closes over nothing, for two
 * reasons: Playwright can serialise it into the page for `$eval`, and it is a pure function of a
 * node-shaped object, so what it means by "the reading text" is pinned by tests without a browser.
 */
export function collectReaderText(root, options) {
  const opts = options || {};
  const styleOf = opts.styleOf
    || ((node) => (typeof getComputedStyle === "function" ? getComputedStyle(node) : null));

  // Elements that contribute nothing a reader reads, whatever their display says.
  const SILENT = /^(SCRIPT|STYLE|TEMPLATE|NOSCRIPT|HEAD|LINK|META)$/;
  // The fallback for a node with no computed style — the node-shaped objects the tests build, and
  // anything the browser has not laid out. In the browser the computed `display` decides instead.
  const BLOCKISH = new RegExp("^(ADDRESS|ARTICLE|ASIDE|BLOCKQUOTE|BODY|BR|CAPTION|DD|DETAILS|DIV"
    + "|DL|DT|FIELDSET|FIGCAPTION|FIGURE|FOOTER|FORM|H1|H2|H3|H4|H5|H6|HEADER|HR|LI|MAIN|NAV|OL"
    + "|P|PRE|SECTION|SUMMARY|TABLE|TBODY|TD|TFOOT|TH|THEAD|TR|UL)$");

  const out = [];
  const tagOf = (node) => String(node.tagName || "").toUpperCase();
  const childrenOf = (node) =>
    (node && node.childNodes ? Array.prototype.slice.call(node.childNodes) : []);

  /** Does this element end a line? A separator goes round a BLOCK and never round an INLINE one:
   *  `<div>76.8%</div><div>1,161 of …</div>` must not fuse into "76.8%1,161", and
   *  `<span>76.8</span><span>%</span>` must not split into "76.8 %" — which would leave judgeShare
   *  no number before the percent sign and report every cell as bare. */
  const breaksLine = (node, style) => {
    // ★★ <br> IS INLINE AND IT BREAKS THE LINE, AND THAT COST 34 FALSE PROBLEMS ON THE FIRST REAL
    //    RUN. `getComputedStyle(br).display` is "inline", so asking the computed style made the
    //    walker fuse a stat cell's label into its population — "carry a known-vulnerable
    //    component7 of 10 surveys here…" — and judgeShare, which reads forward from the percent
    //    sign looking for "N of M", saw no number and reported every stat cell on every country,
    //    language and archive page as a bare share. `innerText` gets this right and the tag list
    //    got it right; only the display test did not. It is decided by TAG, before the display is
    //    consulted, because the property is what the element renders as, not how it lays out.
    const tag = tagOf(node);
    if (tag === "BR" || tag === "HR") return true;

    const display = style && typeof style.display === "string" ? style.display : "";
    if (display) return display.indexOf("inline") !== 0 && display !== "contents";
    return BLOCKISH.test(tag);
  };

  const walk = (node) => {
    if (!node) return;
    if (node.nodeType === 3) { out.push(String(node.nodeValue || "")); return; }
    if (node.nodeType === 11) { childrenOf(node).forEach(walk); return; }  // a shadow root
    if (node.nodeType !== 1) return;                                       // comments and the rest

    if (SILENT.test(tagOf(node))) return;
    if (opts.skipSelector && typeof node.matches === "function" && node.matches(opts.skipSelector)) return;

    const style = styleOf(node);
    if (style && (style.display === "none"
      || style.visibility === "hidden" || style.visibility === "collapse")) return;

    const block = breaksLine(node, style);
    if (block) out.push("\n");

    if (tagOf(node) === "SLOT") {
      // A slot renders the host's light children where the slot sits — that is the one way light
      // DOM under a host reaches the reader, and it reaches it HERE, not at the host.
      const assigned = typeof node.assignedNodes === "function" ? node.assignedNodes() : null;
      (assigned && assigned.length ? Array.prototype.slice.call(assigned) : childrenOf(node)).forEach(walk);
    } else if (node.shadowRoot) {
      childrenOf(node.shadowRoot).forEach(walk);
    } else {
      childrenOf(node).forEach(walk);
    }

    if (block) out.push("\n");
  };

  walk(root);
  return out.join("");
}

/**
 * The walker's output as ONE LINE: every boundary it marked becomes a single space.
 *
 * ★★ THE SPACE IS LOAD-BEARING AND IS NOT THE SAME AS DROPPING THE BREAK. `PageNodes.Stat` renders
 * "<p>{label}<br>{population}</p>"; without a separator that reads "component1,161 of 1,511" and
 * `judgeShare`, which reads forward from the percent sign for "N of M", finds a number glued to a
 * word and calls every stat cell a bare share. That is the defect this file already pins.
 */
export const asOneLine = (raw) => raw.replace(/\s+/g, " ").trim();

/**
 * The walker's output with its LINE BOUNDARIES KEPT — one "\n" wherever it marked a break.
 *
 * ★★ A JUDGE THAT READS ACROSS A BOUNDARY READS A SENTENCE NOBODY WROTE. A production run reported
 * thirty disagreements shaped like "1 advisory yargs": the count is in one cell and the package
 * name is in the next, `yargs` is a NAME that happens to end in s, and the prose is correct. The
 * walker had marked the break; `asOneLine` then turned it into a space and the plural judge's
 * three-word window walked over it. So a judge whose rule is about a PHRASE reads this shape
 * instead, and only the judges that deliberately read across cells take the flattened one.
 */
export const asLines = (raw) =>
  raw.replace(/[^\S\n]+/g, " ").replace(/ *\n\s*/g, "\n").trim();

/** The visible reading text: <main> without the chrome, islands included — see collectReaderText
 *  for why this cannot be `innerText`. */
async function readerText(page) {
  return asOneLine(await page.$eval("main", collectReaderText));
}

/** The same reading, with the boundaries the reader can see left in — see `asLines`. */
async function readerLines(page) {
  return asLines(await page.$eval("main", collectReaderText));
}

/** The reading text WITHOUT the provenance section, which always names a population and would
 *  therefore let a page of pure prose pass "does this state a measurement?".
 *
 *  ★ THE SECTION IS SKIPPED DURING THE WALK, not removed from a clone. `cloneNode(true)` does not
 *  carry shadow roots, so cloning first would hand the walker a tree with every island emptied —
 *  the exact blindness collectReaderText exists to end, reintroduced by the one caller that most
 *  needs to see a figure. */
async function textOutsideProvenance(page) {
  return (await page.$eval("main", collectReaderText, { skipSelector: "section#about" }))
    .replace(/\s+/g, " ").trim();
}

/** Everything one page owes a reader, judged in one pass at ONE viewport. */
async function inspect(context, base, spec, out, viewport, { layoutOnly = false } = {}) {
  const page = await context.newPage();
  await page.setViewportSize({ width: viewport.width, height: viewport.height });
  const consoleErrors = [];
  page.on("console", (m) => m.type() === "error" && consoleErrors.push(m.text()));
  page.on("pageerror", (e) => consoleErrors.push(`pageerror: ${e.message}`));

  const problems = [];
  const shoot = async (id) => {
    for (const theme of ["light", "dark"]) {
      // ★★ THE SCHEME IS SET AND THE PAGE IS RELOADED, AND THE OLD WAY WAS LYING ABOUT EVERY ISLAND.
      //    An island themes ITSELF: CaiIsland reads `document.documentElement.dataset.theme`, falls
      //    back to `matchMedia("(prefers-color-scheme: dark)")` at connectedCallback, stamps the
      //    answer on its own host, and thereafter re-renders only when <html data-theme> MUTATES.
      //    Flipping the emulated scheme after load re-themes the page (its tokens are `light-dark()`,
      //    which is pure CSS) and leaves every island on the scheme it woke up in — so the dark
      //    capture showed dark ink on a dark ground, and would have shown it for cai-lens-gauges and
      //    cai-trend just as loudly, on every dark screenshot this driver has ever taken. A reload
      //    makes the capture a real first paint, which is what a reader actually gets.
      await page.emulateMedia({ colorScheme: theme });
      await page.reload({ waitUntil: "load" });
      await scrollThrough(page);
      await page.screenshot({ path: join(out, `${id}.${viewport.label}.${theme}.png`), fullPage: true });
    }

    await page.emulateMedia({ colorScheme: "light" });
    await page.reload({ waitUntil: "load" });
    await scrollThrough(page);
  };

  const response = await page.goto(base + spec.path, { waitUntil: "load", timeout: 30_000 });
  await scrollThrough(page);
  const status = response ? response.status() : 0;
  if (status !== 200) {
    // One cause, one problem. Running the content checks over a 404 body reported five failures
    // for one broken link and buried the line that said what was wrong.
    problems.push(`HTTP ${status}${spec.linkedFrom ? ` — linked from ${spec.linkedFrom}` : ""}`);
    await shoot(spec.id);
    await page.close();
    return { id: spec.id, path: spec.path, status, shares: [], problems, bands: [], outgoing: [], linkCards: [], text: "" };
  }

  const observed = await bands(page);
  for (const band of observed.map(judgeBand).filter((b) => !b.ok)) problems.push(`SLAB: ${band.reason}`);

  if (layoutOnly) {
    for (const error of consoleErrors) problems.push(`console: ${error}`);
    await shoot(spec.id);
    await page.close();
    return { id: spec.id, path: spec.path, status, shares: [], problems, bands: observed, outgoing: [], linkCards: [], text: "" };
  }

  const h1 = await page.$eval("h1", (el) => el.textContent.trim()).catch(() => null);
  if (!h1) problems.push("no <h1> at all");
  if (spec.h1 && h1 !== spec.h1) problems.push(`h1 is ${JSON.stringify(h1)}, expected ${JSON.stringify(spec.h1)}`);

  const text = await readerText(page);
  if (text.length < 200) problems.push(`main carries only ${text.length} characters of reading`);

  // ★ THE DATED-PROVENANCE RULE IS A CORPUS RULE, and scoping it is not softening it. Every corpus
  //   reading is a citable observation of one day, so a corpus page with no date is a figure that
  //   can be quoted next year as though it were today's. A REGISTRY field guide is a different
  //   object: it says in its own words that "every page below carries the date its score was
  //   taken", and holding it to the corpus sentence reported a defect the page does not have. The
  //   registry pages are walked here for the LAYOUT check, which is corpus-independent.
  if (spec.corpus !== false) {
    if (!/measured \d{1,2} [A-Z][a-z]+ \d{4}/.test(text)) problems.push("no dated provenance line");
  }

  const shares = shareSites(text).map(judgeShare);
  for (const share of shares.filter((s) => !s.ok)) problems.push(`SHARE: ${share.reason} — "${share.text}"`);
  if (spec.minShares && shares.length < spec.minShares) {
    problems.push(`only ${shares.length} share(s) on the page; expected at least ${spec.minShares}`);
  }

  const floor = judgeFloorDisclosure(text);
  if (floor.applicable && !floor.ok) problems.push(`FLOOR: ${floor.reason}`);

  for (const plural of pluralDisagreements(await readerLines(page))) {
    problems.push(`PLURAL: ${plural} — a count of one printed against a plural basis`);
  }

  if (spec.corpus !== false) {
    const body = await textOutsideProvenance(page);
    if (!/\d/.test(body)) problems.push("no figure anywhere outside the provenance line — the page states no measurement");
  }

  for (const hollow of await hollowSections(page)) problems.push(`HOLLOW SECTION #${hollow.id} — ${hollow.what}`);
  for (const error of consoleErrors) problems.push(`console: ${error}`);

  const outgoing = await page.$$eval("main a[href]", (as) => as.map((a) => a.getAttribute("href")));
  const linkCards = await page.$$eval("cai-link-cards a[href]", (as) => as.map((a) => a.getAttribute("href")));

  await shoot(spec.id);
  await page.close();

  return { id: spec.id, path: spec.path, status, h1, shares, problems, floor, bands: observed, outgoing, linkCards, text };
}

// ── the machine-readable mirrors ──────────────────────────────────────────────────────────────

/**
 * No field anywhere in the JSON mirror may be a bare share.
 *
 * A ratio has to arrive as numerator + denominator + basis so the consumer does the division and
 * keeps the population: a `share` field is a number that has forgotten what it was a share of, and
 * once it is in a spreadsheet nothing can put that back.
 */
export function judgeMirrorJson(payload) {
  const problems = [];
  let ratios = 0;
  const bare = /(^|[^a-z])(share|pct|percent|percentage|ratio|rate)([^a-z]|$)/i;

  const walk = (node, path) => {
    if (Array.isArray(node)) return node.forEach((item, i) => walk(item, `${path}[${i}]`));
    if (!node || typeof node !== "object") return;

    for (const [key, value] of Object.entries(node)) {
      if (bare.test(key) && typeof value === "number") {
        problems.push(`${path}.${key} is a bare share — a number with no population attached`);
      }
      walk(value, `${path}.${key}`);
    }

    if ("numerator" in node) {
      ratios += 1;
      for (const required of ["denominator", "basis"]) {
        if (!(required in node) || node[required] === null || node[required] === undefined) {
          problems.push(`${path} carries a numerator with no ${required}`);
        }
      }
    }
  };

  walk(payload, "$");
  return { problems, ratios };
}

/** A very small RFC4180-ish reader — enough for the mirror, which quotes and doubles quotes. */
export function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = "";
  let quoted = false;
  const body = text.replace(/^﻿/, "").replace(/\r\n/g, "\n");
  for (let i = 0; i < body.length; i += 1) {
    const c = body[i];
    if (quoted) {
      if (c === '"' && body[i + 1] === '"') { cell += '"'; i += 1; }
      else if (c === '"') quoted = false;
      else cell += c;
    } else if (c === '"') quoted = true;
    else if (c === ",") { row.push(cell); cell = ""; }
    else if (c === "\n") { row.push(cell); rows.push(row); row = []; cell = ""; }
    else cell += c;
  }
  if (cell.length > 0 || row.length > 0) { row.push(cell); rows.push(row); }
  return rows.filter((r) => r.length > 1 || r[0] !== "");
}

/**
 * In the CSV, an unmeasured cell must be EMPTY and a measured zero must be `0`.
 *
 * Those two being confusable is the exact failure this batch exists to prevent: a sentinel like
 * `n/a`, `-` or `0` in the unmeasured slot turns "nobody looked" into "we looked and found none".
 * What is checkable from the file alone is that no sentinel is used and that both forms actually
 * occur — a file with no empty cell has not demonstrated it can say "unmeasured" at all.
 */
const SENTINELS = new Set(["n/a", "na", "-", "--", "null", "nil", "none", "nan", "?", "unknown", "unmeasured", "n.a."]);

export function judgeMirrorCsv(rows) {
  const problems = [];
  if (rows.length < 2) return { problems: ["the CSV has no data rows"], empties: 0, zeroes: 0 };

  const header = rows[0];
  let empties = 0;
  let zeroes = 0;
  for (const [r, row] of rows.slice(1).entries()) {
    for (const [c, raw] of row.entries()) {
      const cell = (raw ?? "").trim();
      if (cell === "") { empties += 1; continue; }
      if (cell === "0") { zeroes += 1; continue; }
      if (SENTINELS.has(cell.toLowerCase())) {
        problems.push(`row ${r + 2}, column '${header[c] ?? c}' says '${cell}' — an unmeasured cell must be EMPTY, `
          + "never a sentinel a spreadsheet will read as a value");
      }
    }
  }
  if (empties === 0) {
    problems.push("no cell in the CSV is empty — nothing here demonstrates it can say 'unmeasured' at all, "
      + "so this run does not distinguish an unmeasured cell from a measured zero");
  }
  return { problems, empties, zeroes };
}

/** Print one page's line and its detail. */
function report(result, label) {
  const tag = label ? ` @${label}` : "";
  console.log(`${result.problems.length === 0 ? "✓" : "✗"} ${(result.id + tag).padEnd(26)} ${result.status}  ${result.shares.length} share(s)${result.floor?.applicable ? "  [floor]" : ""}`);
  for (const share of result.shares) console.log(`      ${share.ok ? "·" : "!"} ${share.text}`);
  for (const problem of result.problems) console.log(`      ✗ ${problem}`);
}

async function main(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i += 2) args[argv[i].replace(/^--/, "")] = argv[i + 1];
  const base = (args.base || "http://127.0.0.1:5098").replace(/\/$/, "");
  const api = (args.api || "").replace(/\/$/, "");
  const out = args.out || join(here, "out", "corpus-e2e");
  const require = createRequire(join(toolingDirectory(args.tooling), "package.json"));
  const { chromium } = require("playwright");

  mkdirSync(out, { recursive: true });
  console.log(`corpus e2e → ${base}${api ? `  (mirrors: ${api})` : ""}`);
  console.log(`screenshots → ${out}`);
  for (const v of VIEWPORTS) console.log(`  width ${v.width}: ${v.why}`);
  console.log("");

  const browser = await chromium.launch();
  const context = await browser.newContext();
  const [wide, narrow] = VIEWPORTS;
  const results = [];
  const problems = [];
  let sharesSeen = 0;
  let verifiedAbsences = 0;
  const visited = [];

  const push = (result, label) => {
    results.push(result);
    sharesSeen += result.shares.length;
    report(result, label);
  };

  // ── the index, the authority on everything below it ────────────────────────────────────────
  const index = await inspect(context, base, { id: "index", path: ROOT, h1: "The state of the corpus", minShares: 1 }, out, wide);
  push(index);
  visited.push({ id: "index", path: ROOT });

  const linked = new Set(index.linkCards.filter(Boolean));
  const saysNoSecurityHalf = index.text.includes(NO_SECURITY_HALF);
  console.log(`\nthe index offers ${linked.size} page(s); it ${saysNoSecurityHalf ? "SAYS" : "does not say"} "${NO_SECURITY_HALF}"`);

  if (saysNoSecurityHalf && linked.size > 0) {
    // ★ RECONCILED: THE ONE EXEMPTION HERE WAS THE ARCHIVE CARD, and it is gone with the archive.
    //   It was offered whenever ANY reading existed, security half or not, so it legitimately
    //   survived this branch; nothing under the corpus root does now. Every card left is gated on a
    //   detail page this same build produced, and a reading with no security half produces none.
    const offered = [...linked];
    if (offered.length > 0) {
      problems.push(`the index says "${NO_SECURITY_HALF}" and still offers ${offered.join(", ")}`);
    }
  }
  if (!saysNoSecurityHalf && linked.size === 0) {
    problems.push("the index offers no page at all and does not explain why — a reader is left at a dead end");
  }
  if (saysNoSecurityHalf && !/absent rather than empty/.test(index.text)) {
    problems.push("the index says the reading has no security half but does not say the pages are absent rather than empty");
  }

  // ── every catalogue page: linked ⇒ 200 and inspected, unlinked ⇒ must be 404 ────────────────
  const families = [];
  for (const entry of CATALOGUE) {
    const isLinked = linked.has(entry.path);
    const probe = await context.request.get(base + entry.path);
    const verdict = judgeExistence({ path: entry.path, linked: isLinked, status: probe.status() });
    if (!verdict.ok) {
      problems.push(verdict.reason);
      console.log(`✗ ${entry.id.padEnd(26)} ${verdict.reason}`);
      continue;
    }
    if (verdict.verifiedAbsence) {
      verifiedAbsences += 1;
      console.log(`✓ ${entry.id.padEnd(26)} 404  absent, and the index does not offer it`);
      continue;
    }
    const result = await inspect(context, base, entry, out, wide);
    push(result);
    visited.push({ id: entry.id, path: entry.path });
    if (entry.family) families.push({ entry, result });
  }

  // ── one level deeper: the detail pages each family index lists ─────────────────────────────
  for (const { entry, result } of families) {
    const detail = [...new Set(result.outgoing.filter((h) => h && h.startsWith(ROOT) && h !== entry.path))]
      .slice(0, DETAIL_SAMPLE);
    const verdict = judgeFamilyIndex(result.text, detail.length);
    if (!verdict.ok) {
      problems.push(`${entry.path} ${verdict.reason}`);
      console.log(`✗ ${entry.id.padEnd(26)} ${verdict.reason}`);
      continue;
    }
    if (verdict.empty) {
      console.log(`✓ ${entry.id.padEnd(26)} ${verdict.note}`);
      continue;
    }
    console.log(`  ${entry.id}: following ${detail.length} of its listed pages`);
    for (const [n, path] of detail.entries()) {
      const id = `${entry.family}-${n + 1}`;
      push(await inspect(context, base, { id, path, linkedFrom: entry.path }, out, wide));
      visited.push({ id, path });
    }
  }

  // ── the OTHER family this publisher writes: the survey registry ────────────────────────────
  // Its pages carry stat bands of their own and are published by the same sweep, so a band defect
  // reaches ~2,400 of them. They are walked here for that reason, not because the corpus index
  // owns them.
  const registry = await inspect(context, base, { id: "surveys", path: SURVEYS, corpus: false }, out, wide);
  push(registry);
  visited.push({ id: "surveys", path: SURVEYS, corpus: false });
  const surveyPaths = [...new Set(registry.outgoing.filter((h) => h && h.startsWith(SURVEYS) && h !== SURVEYS))]
    .slice(0, SURVEY_SAMPLE);
  if (surveyPaths.length === 0) {
    problems.push(`${SURVEYS} lists no survey page — the registry index has nothing behind it`);
  }
  for (const [n, path] of surveyPaths.entries()) {
    const id = `survey-${n + 1}`;
    push(await inspect(context, base, { id, path, linkedFrom: SURVEYS, corpus: false }, out, wide));
    visited.push({ id, path, corpus: false });
  }

  // ── the SECOND WIDTH: layout only, over everything already seen ────────────────────────────
  console.log(`\nre-reading ${visited.length} page(s) at ${narrow.width}px — ${narrow.why}`);
  const narrowResults = [];
  for (const spec of visited) {
    const result = await inspect(context, base, spec, out, narrow, { layoutOnly: true });
    narrowResults.push(result);
    if (result.problems.length > 0) report(result, narrow.label);
  }
  const narrowProblems = narrowResults.reduce((sum, r) => sum + r.problems.length, 0);
  if (narrowProblems === 0) console.log(`  (nothing new at ${narrow.width}px)`);

  // ★ A second width that laid out identically to the first asserted nothing. Say so rather than
  //   counting it as coverage — the whole reason this pass exists is that one width is blind.
  const allBands = [...results, ...narrowResults].flatMap((r) => r.bands || []);
  // ★ A slab check that met no stat band did not pass — it never ran. Say which, because a
  //   selector that has quietly stopped matching looks exactly like a page with no defect.
  if (allBands.length === 0) {
    problems.push("no stat band was found on any page at either width — the short-last-row check "
      + "never ran, and its silence is not a result");
  }
  const trackShape = (rs) => rs.flatMap((r) => (r.bands || []).map((b) => `${r.id}#${b.id}:${b.tracks}`)).sort().join("|");
  const wideShape = trackShape(results);
  if (wideShape !== "" && wideShape === trackShape(narrowResults)) {
    console.log(`  note: every band kept its column count between ${wide.width}px and ${narrow.width}px `
      + "— on this corpus the two widths read the same layout");
  }

  await browser.close();

  // ── the machine-readable mirrors ───────────────────────────────────────────────────────────
  let mirrors = null;
  if (api) {
    console.log("\nthe mirrors");
    const jsonResponse = await fetch(`${api}/api/public/state-of-the-corpus`);
    if (jsonResponse.status !== 200) {
      problems.push(`the JSON mirror answers ${jsonResponse.status}`);
      console.log(`✗ json   HTTP ${jsonResponse.status}`);
    } else {
      const payload = await jsonResponse.json();
      const verdict = judgeMirrorJson(payload);
      for (const problem of verdict.problems) problems.push(`MIRROR json: ${problem}`);
      console.log(`${verdict.problems.length === 0 ? "✓" : "✗"} json   ${verdict.ratios} ratio(s), each as numerator + denominator + basis`);
      for (const problem of verdict.problems) console.log(`      ✗ ${problem}`);
      mirrors = { ratios: verdict.ratios };
    }

    const csvResponse = await fetch(`${api}/api/public/state-of-the-corpus.csv`);
    if (csvResponse.status !== 200) {
      problems.push(`the CSV mirror answers ${csvResponse.status}`);
      console.log(`✗ csv    HTTP ${csvResponse.status}`);
    } else {
      const rows = parseCsv(await csvResponse.text());
      const verdict = judgeMirrorCsv(rows);
      for (const problem of verdict.problems) problems.push(`MIRROR csv: ${problem}`);
      console.log(`${verdict.problems.length === 0 ? "✓" : "✗"} csv    ${rows.length - 1} row(s), ${verdict.empties} empty cell(s), ${verdict.zeroes} measured zero(es)`);
      for (const problem of verdict.problems) console.log(`      ✗ ${problem}`);
      mirrors = { ...mirrors, rows: rows.length - 1, empties: verdict.empties, zeroes: verdict.zeroes };
    }
  } else {
    problems.push("no --api given, so the JSON and CSV mirrors were not checked at all");
  }

  const pageProblems = results.reduce((sum, r) => sum + r.problems.length, 0) + narrowProblems;
  const failures = pageProblems + problems.length;
  writeFileSync(join(out, "result.json"), JSON.stringify({
    base, api, viewports: VIEWPORTS, sharesSeen, verifiedAbsences, failures, mirrors,
    indexClaims: { saysNoSecurityHalf, linked: [...linked] },
    agreement: problems,
    results: results.map(({ text, ...rest }) => rest),
    narrow: narrowResults.map(({ text, ...rest }) => rest),
  }, null, 2));

  for (const problem of problems) console.log(`✗ ${problem}`);
  console.log(
    `\n${sharesSeen} share(s) inspected across ${results.length} page(s) at ${VIEWPORTS.length} widths, `
    + `${allBands.length} stat band reading(s), ${verifiedAbsences} verified absence(s), ${failures} problem(s).`);

  if (sharesSeen === 0) {
    console.log("NO SHARE WAS FOUND ON ANY PAGE — this run asserts nothing about the rule.");
    return 3;
  }
  return failures === 0 ? 0 : 1;
}

if (process.argv[1] && process.argv[1].endsWith("corpus-e2e.mjs")) {
  main(process.argv.slice(2)).then((code) => process.exit(code));
}
