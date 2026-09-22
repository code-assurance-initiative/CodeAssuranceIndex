// The share judge — the one piece of corpus-e2e.mjs that decides pass or fail, tested without a
// browser. Run: node --test tools/localdev/ui/corpus-e2e.test.mjs
//
// (The importing module resolves Playwright lazily, inside main(), precisely so this file can
// import it from a worktree where node_modules does not exist.)
import { strict as assert } from "node:assert";
import test from "node:test";

import {
  CATALOGUE, VIEWPORTS, asLines, asOneLine, collectReaderText, judgeBand, judgeExistence, judgeFamilyIndex,
  judgeFloorDisclosure, judgeMirrorCsv, judgeMirrorJson, judgeShare, parseCsv, pluralDisagreements,
  shareSites, toNumber,
} from "./corpus-e2e.mjs";

/** The one share on a page, judged the way the driver judges it. */
function judge(text) {
  const sites = shareSites(text);
  assert.equal(sites.length, 1, `expected exactly one percentage in ${JSON.stringify(text)}`);
  return judgeShare(sites[0]);
}

test("a share stated with its basis and both counts passes", () => {
  const verdict = judge("76.8% of surveys whose dependencies resolved (1,219 of 1,588) carry one");
  assert.equal(verdict.ok, true);
  assert.equal(verdict.basis, "surveys whose dependencies resolved");
  assert.equal(verdict.numerator, 1219);
  assert.equal(verdict.denominator, 1588);
});

test("a bare percentage fails — the whole point of the batch", () => {
  const verdict = judge("76.8% carry a known-vulnerable component");
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /no denominator or basis/);
});

test("counts with no basis between them fails", () => {
  const verdict = judge("76.8% (1,219 of 1,588)");
  assert.equal(verdict.ok, false);
});

test("counts that do not produce the stated percentage fail", () => {
  const verdict = judge("35.2% of measured surveys (1,219 of 1,588)");
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /arithmetic/);
});

test("one decimal place of rounding is allowed and nothing more", () => {
  // 1,219 / 1,588 = 76.7632…%, which Figure prints as 76.8.
  assert.equal(judge("76.8% of measured surveys (1,219 of 1,588)").ok, true);
  assert.equal(judge("76.7% of measured surveys (1,219 of 1,588)").ok, false);
});

test("a denominator of zero is refused rather than divided by", () => {
  const verdict = judge("0.0% of measured surveys (0 of 0)");
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /denominator is zero/);
});

test("a numerator larger than its denominator is refused", () => {
  const verdict = judge("150.0% of measured surveys (9 of 6)");
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /exceeds its denominator/);
});

test("every percentage on the page is found, including the non-compliant ones", () => {
  const sites = shareSites(
    "12.5% of measured surveys (1 of 8) and then 99% on its own and 0.0% of measured owners (0 of 3)");
  assert.equal(sites.length, 3);
  assert.deepEqual(sites.map((s) => judgeShare(s).ok), [true, false, true]);
});

test("thousands separators are read as numbers, not dropped", () => {
  assert.equal(toNumber("1,588"), 1588);
  assert.equal(toNumber("6"), 6);
});

// ── the index and the site must agree about which pages exist ────────────────────────────────

test("a page the index links to and the site serves is fine", () => {
  assert.equal(judgeExistence({ path: "/state-of-the-corpus/coverage/", linked: true, status: 200 }).ok, true);
});

test("a 404 is a PASS when the index does not offer the page — absence kept as promised", () => {
  const verdict = judgeExistence({ path: "/state-of-the-corpus/secrets/", linked: false, status: 404 });
  assert.equal(verdict.ok, true);
  assert.equal(verdict.verifiedAbsence, true);
});

test("a 404 the index links to is a dead link, not an absence", () => {
  const verdict = judgeExistence({ path: "/state-of-the-corpus/secrets/", linked: true, status: 404 });
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /dead link/);
});

test("a page served but not linked is a page no reader can reach", () => {
  const verdict = judgeExistence({ path: "/state-of-the-corpus/secrets/", linked: false, status: 200 });
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /no reader can reach/);
});

test("anything other than 200 or 404 is reported rather than guessed at", () => {
  assert.equal(judgeExistence({ path: "/x/", linked: false, status: 500 }).ok, false);
});

// ── a floor must arrive with the size of the blind spot ──────────────────────────────────────

test("a page that claims no floor is not asked for a truncation count", () => {
  const verdict = judgeFloorDisclosure("6 measured codebases, and nothing was censored.");
  assert.equal(verdict.applicable, false);
  assert.equal(verdict.ok, true);
});

test("a floor stated with the censored population passes", () => {
  const verdict = judgeFloorDisclosure(
    "41 surveys seen carrying it — a floor. 272 surveys whose advisory list was cut short, any of "
    + "which may carry it without being counted here.");
  assert.equal(verdict.applicable, true);
  assert.equal(verdict.ok, true);
});

test("a floor with no censored population beside it fails — it reads as a total", () => {
  const verdict = judgeFloorDisclosure("41 surveys seen carrying it — a floor, and nothing more is said.");
  assert.equal(verdict.applicable, true);
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /cut short/);
});

test("the words alone are not the disclosure — the count has to be there", () => {
  const verdict = judgeFloorDisclosure(
    "41 surveys seen carrying it — a floor. Some surveys whose advisory list was cut short exist.");
  assert.equal(verdict.ok, false);
});

test("a split figure passes: the (i) sits between the count and its population", () => {
  // ★★ WHAT THE PAGE ACTUALLY READS AS NOW. Every figure under the corpus root is split by the
  // island that renders it — the value in the display slot, the population beneath it — and the
  // hint affordance between them reads as a bare "i". A probe demanding a single space between the
  // two halves reported a defect the page did not have, twice, on a run that was otherwise clean.
  const verdict = judgeFloorDisclosure(
    "at least 41 a floor i surveys carrying this advisory 300 i surveys whose advisory list was cut short");
  assert.equal(verdict.applicable, true);
  assert.equal(verdict.ok, true);
});

test("a sentence between the count and the population is still not adjacency", () => {
  // ★ THE GAP IS BOUNDED, WHICH IS WHAT KEEPS THE WIDENING FROM BEING A HOLE. Eight non-digit
  // characters admits an affordance and refuses a clause, so a page that names the censored
  // population while its count is somewhere else entirely still fails.
  const verdict = judgeFloorDisclosure(
    "300 readings were taken, and a floor is published over surveys whose advisory list was cut short.");
  assert.equal(verdict.ok, false);
});

// ── the catalogue is the set of pages the index is answerable for ────────────────────────────

test("the catalogue holds the eight retired addresses and the four family indexes", () => {
  // ★ THE COUNT IS ASSERTED SO ADDING A FAMILY IS A DELIBERATE ACT. The language family was
  // published for a whole run before this list learned about it, and the driver reported a clean
  // sweep over a family it had never opened — which is the same silence as a check that never ran.
  // ★ RECONCILED: THE ARCHIVE MOVED FROM THE FAMILY LIST TO THE RETIRED LIST, and two dated day
  // pages joined it. Removing an entry is how a withdrawal stops being checked, so a retirement
  // ADDS rows here; it never deletes them.
  assert.equal(CATALOGUE.length, 12);
  assert.deepEqual(
    CATALOGUE.map((c) => c.id),
    ["coverage", "vulnerabilities", "disclosure-policy", "secrets", "supply-chain", "archive",
     "dated-today", "dated-yesterday",
     "advisories", "packages", "countries", "languages"]);
  assert.deepEqual(CATALOGUE.filter((c) => c.family).map((c) => c.family),
    ["advisories", "packages", "countries", "languages"]);

  // ★ THE DATED ENTRIES ARE REAL ADDRESSES, not a shape. A typo in the date arithmetic would make
  // them probe a path that was never published and pass as a verified absence for ever.
  for (const id of ["dated-today", "dated-yesterday"]) {
    const entry = CATALOGUE.find((c) => c.id === id);
    assert.match(entry.path, /^\/state-of-the-corpus\/\d{4}-\d{2}-\d{2}\/$/);
  }
  assert.notEqual(
    CATALOGUE.find((c) => c.id === "dated-today").path,
    CATALOGUE.find((c) => c.id === "dated-yesterday").path);
});

// ── a family index with nothing behind it ────────────────────────────────────────────────────

test("a family index that lists pages is fine", () => {
  assert.equal(judgeFamilyIndex("anything at all", 3).ok, true);
});

test("an empty family index that names its gate is CORRECT, not broken", () => {
  const verdict = judgeFamilyIndex(
    "100.0% of countries the corpus could place at all (3 of 3) have no page, because they hold "
    + "fewer than ten measured repositories.", 0);
  assert.equal(verdict.ok, true);
  assert.equal(verdict.empty, true);
});

test("an empty family index that just stops is a dead end", () => {
  const verdict = judgeFamilyIndex("The corpus by country. Country Repositories Owners.", 0);
  assert.equal(verdict.ok, false);
  assert.match(verdict.reason, /does not say why/);
});

test("a count of what was left out is not enough without the reason", () => {
  assert.equal(judgeFamilyIndex("3 countries have no page. Country Repositories Owners.", 0).ok, false);
});

// ── a stat band with a short last row is a grey slab ─────────────────────────────────────────

test("a band that divides evenly into its columns is fine", () => {
  assert.equal(judgeBand({ id: "corpus", cells: 4, tracks: 2 }).ok, true);
  assert.equal(judgeBand({ id: "corpus", cells: 6, tracks: 3 }).ok, true);
});

test("a band with fewer cells than columns is fine — auto-fit collapses the empty column", () => {
  assert.equal(judgeBand({ id: "policy", cells: 3, tracks: 4 }).ok, true);
});

test("five cells over four columns leaves three unfilled — the slab", () => {
  const verdict = judgeBand({ id: "controls", cells: 5, tracks: 4 });
  assert.equal(verdict.ok, false);
  assert.equal(verdict.unfilled, 3);
  assert.match(verdict.reason, /grey slab/);
});

test("three cells over two columns is short too — invisible at 1280, plain at 680", () => {
  const verdict = judgeBand({ id: "policy", cells: 3, tracks: 2 });
  assert.equal(verdict.ok, false);
  assert.equal(verdict.unfilled, 1);
});

test("six cells over four columns leaves two unfilled", () => {
  assert.equal(judgeBand({ id: "day", cells: 6, tracks: 4 }).unfilled, 2);
});

test("the two widths are chosen to bracket the 240px track breakpoints", () => {
  assert.deepEqual(VIEWPORTS.map((v) => v.width), [1280, 680]);
  // floor((W + 1) / 241) over the CONTENT width, which is the viewport less the page gutters.
  const tracks = (contentWidth) => Math.floor((contentWidth + 1) / 241);
  assert.equal(tracks(1120), 4);   // 1280 viewport
  assert.equal(tracks(630), 2);    // 680 viewport
});

// ── singular against a plural basis ──────────────────────────────────────────────────────────

test("a count of one against a plural basis is caught", () => {
  assert.deepEqual(pluralDisagreements("Taken over 1 dated readings, measured 10 September 2026."),
    ['"1 dated readings"']);
});

test("a plural count is not a disagreement", () => {
  assert.deepEqual(pluralDisagreements("54 dated readings"), []);
});

test("a singular noun after 1 is left alone, and so are words that merely end in s", () => {
  assert.deepEqual(pluralDisagreements("1 reading, and 1 process, and 1 analysis"), []);
});

test("a numerator of one inside a share is not a count and is not flagged", () => {
  assert.deepEqual(pluralDisagreements("33.3% of measured surveys (1 of 3)"), []);
});

// ★★ THE JUDGE READ THE WRONG NOUN, AND THE PAGE WAS RIGHT. A basis may qualify its noun with a
//    relative clause — "1 survey whose dependencies a scanner could resolve" counts SURVEYS, and
//    `dependencies` belongs to the clause describing one. Taking the last word of the run makes
//    that read as a disagreement, and a judge that fails correct English is a judge whose failures
//    stop being read. The run is therefore governed by the word BEFORE the relative pronoun — and
//    only that far: when the word before it is itself plural, the disagreement is real and stands.
test("a relative clause does not lend its noun to the count", () => {
  assert.deepEqual(
    pluralDisagreements("1 survey whose dependencies a scanner could resolve"), []);
  assert.deepEqual(
    pluralDisagreements("1 owner that publishes advisories"), []);
});

// ★★ THE WALKER MARKS EVERY LINE AND THE CALLER WAS ERASING THEM. A production run reported
//    thirty disagreements of the shape "1 advisory yargs" — correct prose, because the count and
//    the package name are in different cells and `yargs` is a NAME that happens to end in s. The
//    walker had put a break between them; `readerText` then collapsed every newline to a space and
//    the three-word window walked straight across it. A boundary the reader can see must survive
//    into the text the judges read, or the judge is reading a sentence nobody wrote.
test("a line boundary stops the plural window, and the collapsed reading is unchanged otherwise", () => {
  const cell = el("DIV", [
    el("P", [text("1 advisory")]),
    el("P", [text("yargs")]),
  ]);

  assert.deepEqual(pluralDisagreements(asLines(collectReaderText(cell, FAKE_STYLE))), []);
  // The same boundary must still leave a SPACE for the judges that read across it, or the <br>
  // defect this file already pins comes straight back.
  assert.match(asOneLine(collectReaderText(cell, FAKE_STYLE)), /1 advisory yargs/);
});

test("a plural basis broken across two lines is still caught on its own line", () => {
  const cell = el("DIV", [
    el("P", [text("1 advisories")]),
    el("P", [text("yargs")]),
  ]);

  assert.deepEqual(
    pluralDisagreements(asLines(collectReaderText(cell, FAKE_STYLE))), ['"1 advisories"']);
});

test("a relative clause does not excuse a plural head noun either", () => {
  assert.deepEqual(
    pluralDisagreements("1 surveys whose dependencies a scanner could resolve"),
    ['"1 surveys"']);
});

// ── the JSON mirror carries no bare share ────────────────────────────────────────────────────

test("a ratio as numerator + denominator + basis passes", () => {
  const verdict = judgeMirrorJson({ corpus: { vulnerable: { numerator: 5, denominator: 6, basis: "surveys" } } });
  assert.deepEqual(verdict.problems, []);
  assert.equal(verdict.ratios, 1);
});

test("a bare share field fails wherever it is nested", () => {
  const verdict = judgeMirrorJson({ corpus: { byLanguage: { csharp: { share: 0.768 } } } });
  assert.equal(verdict.problems.length, 1);
  assert.match(verdict.problems[0], /bare share/);
});

test("a numerator with no denominator or basis fails", () => {
  const verdict = judgeMirrorJson({ a: { numerator: 5 } });
  assert.equal(verdict.problems.length, 2);
});

test("arrays are walked, not skipped", () => {
  const verdict = judgeMirrorJson({ rows: [{ ok: 1 }, { percentage: 12.5 }] });
  assert.equal(verdict.problems.length, 1);
  assert.match(verdict.problems[0], /\$\.rows\[1\]\.percentage/);
});

// ── the CSV keeps unmeasured and zero apart ──────────────────────────────────────────────────

test("quoted cells and doubled quotes are read correctly", () => {
  const rows = parseCsv('a,b\n"one, two","he said ""hi"""\n');
  assert.deepEqual(rows, [["a", "b"], ["one, two", 'he said "hi"']]);
});

test("an empty unmeasured cell beside a measured zero passes", () => {
  const verdict = judgeMirrorCsv([["scope", "surveys", "critical"], ["corpus", "", "0"]]);
  assert.deepEqual(verdict.problems, []);
  assert.equal(verdict.empties, 1);
  assert.equal(verdict.zeroes, 1);
});

test("a sentinel in an unmeasured cell fails — a spreadsheet reads it as a value", () => {
  const verdict = judgeMirrorCsv([["scope", "surveys"], ["corpus", "n/a"], ["x", ""]]);
  assert.equal(verdict.problems.length, 1);
  assert.match(verdict.problems[0], /must be EMPTY/);
});

test("a CSV with no empty cell at all has not shown it can say 'unmeasured'", () => {
  const verdict = judgeMirrorCsv([["scope", "surveys"], ["corpus", "6"]]);
  assert.equal(verdict.problems.length, 1);
  assert.match(verdict.problems[0], /does not distinguish|demonstrates it can say/);
});

// ── the stat-cell shape ────────────────────────────────────────────────────────────────────────
// ★★ THE SAME CLAIM IN A DIFFERENT SHAPE, AND THE CHECKER HAD TO LEARN IT. Every corpus stat cell
// used to be handed Figure.Headline() whole and set it in heading type, which is why the pages read
// as four lines of prose per cell and the owner asked for an overview instead of a headache. A cell
// is now built from the figure and splits it — value large, label, counts and basis beneath. The
// counts and the population are still within a line of the percentage, which is the whole of what
// this check insists on; only the punctuation moved. A checker reading punctuation rather than the
// claim reported forty problems that were not there, on every page at once.

test("a share split across a stat cell is compliant — the counts and the basis are still beside it", () => {
  const [site] = shareSites(
    "76.8%\ncarry at least one known-vulnerable component\n"
    + "1,161 of 1,511 surveys whose dependencies a scanner could resolve");
  const verdict = judgeShare(site);

  assert.equal(verdict.ok, true, verdict.reason);
  assert.equal(verdict.numerator, 1161);
  assert.equal(verdict.denominator, 1511);
  assert.equal(verdict.basis, "surveys whose dependencies a scanner could resolve");
});

test("a stat cell with no label of its own is compliant too", () => {
  const [site] = shareSites("92.9%\n26 of 28 measured codebases");
  const verdict = judgeShare(site);

  assert.equal(verdict.ok, true, verdict.reason);
  assert.equal(verdict.denominator, 28);
});

test("a stat cell whose arithmetic does not hold still fails", () => {
  // The shape being accepted must not mean the numbers stop being checked: 1,161 of 1,511 is
  // 76.8%, and a cell claiming 96.8% over those counts is exactly the defect this driver exists
  // for, wearing the new shape.
  const [site] = shareSites(
    "96.8%\ncarry at least one known-vulnerable component\n"
    + "1,161 of 1,511 surveys whose dependencies a scanner could resolve");

  assert.equal(judgeShare(site).ok, false);
});

test("a bare percentage in a cell is still refused", () => {
  const [site] = shareSites("76.8%\ncarry at least one known-vulnerable component\nof the corpus");

  assert.equal(judgeShare(site).ok, false);
});

// ── the reading text crosses a shadow boundary ───────────────────────────────────────────────
// ★★ WHY THESE EXIST. `innerText` STOPS AT A SHADOW ROOT, and every `cai-*` island on these pages
// attaches an open one. So for as long as the driver read `main.innerText`, every figure an island
// rendered was invisible to judgeShare, judgeFloorDisclosure, pluralDisagreements and the "states
// no measurement at all" check — while the LINK checks kept working, because Playwright's CSS
// engine does pierce an open root. A check that cannot see its subject does not fail; it goes
// quiet, and quiet reads as a pass. These tests pin the reading to what a READER sees, and they
// pin it here rather than in the browser because the walker is a pure function of a node.
//
// The fakes below are the smallest node-shaped objects the walker needs: nodeType, tagName,
// childNodes, shadowRoot, and a per-node style the walker is handed through `styleOf`.

const text = (value) => ({ nodeType: 3, nodeValue: value });

const el = (tagName, childNodes = [], extra = {}) =>
  ({ nodeType: 1, tagName, childNodes, shadowRoot: null, style: { display: "block" }, ...extra });

const shadow = (childNodes) => ({ nodeType: 11, childNodes });

/** The reading as a caller gets it: the walker's output collapsed the way readerText collapses it. */
const FAKE_STYLE = { styleOf: (node) => node.style || null };

const read = (root) =>
  collectReaderText(root, { styleOf: (node) => node.style || null }).replace(/\s+/g, " ").trim();

const SHARE = "76.8% of surveys whose dependencies resolved (1,161 of 1,511)";

test("a figure rendered inside an open shadow root is part of the reading", () => {
  const island = el("CAI-FINDINGS", [], { shadowRoot: shadow([el("P", [text(SHARE)])]) });
  const reading = read(el("MAIN", [island]));

  assert.match(reading, /76\.8%/);
  const sites = shareSites(reading);
  assert.equal(sites.length, 1, `the share must be visible to the share check: ${JSON.stringify(reading)}`);
  assert.equal(judgeShare(sites[0]).ok, true);
});

test("light, shadow and light again come back in DOCUMENT ORDER", () => {
  // ★ ORDER IS NOT COSMETIC. judgeShare reads a 220-character window FORWARD from the `%`, so a
  // splice that appended all shadow text after all light text would pair a share with a stranger's
  // counts — and pass, wrongly, whenever the stranger's arithmetic happened to hold.
  const reading = read(el("MAIN", [
    el("P", [text("before")]),
    el("CAI-BAR", [], { shadowRoot: shadow([el("P", [text("inside")])]) }),
    el("P", [text("after")]),
  ]));

  assert.equal(reading, "before inside after");
});

test("two islands keep their own counts — order proved by the judge, not by eye", () => {
  const island = (share) => el("CAI-STAT", [], { shadowRoot: shadow([el("P", [text(share)])]) });
  const reading = read(el("MAIN", [
    island("76.8% of surveys whose dependencies resolved (1,161 of 1,511)"),
    island("12.5% of measured codebases (1 of 8)"),
  ]));

  const verdicts = shareSites(reading).map(judgeShare);
  assert.equal(verdicts.length, 2);
  assert.deepEqual(verdicts.map((v) => v.ok), [true, true], verdicts.map((v) => v.reason).join(" | "));
  assert.deepEqual(verdicts.map((v) => v.denominator), [1511, 8]);
});

test("a host's pre-hydration placeholder is NOT read — the shadow content replaces it", () => {
  const island = el("CAI-FINDINGS", [el("P", [text("Loading the findings…")])], {
    shadowRoot: shadow([el("P", [text(SHARE)])]),
  });
  const reading = read(el("MAIN", [island]));

  assert.match(reading, /76\.8%/, "the rendered content must be there");
  assert.doesNotMatch(reading, /Loading the findings/, "the light-DOM placeholder is not rendered");
});

test("a visibility:hidden subtree is skipped — a tooltip is not part of the reading", () => {
  // The (i) tooltips sit in the markup at all times and only become visible on hover. Collecting
  // them would put explanation a reader cannot see into the reading, and would feed the share
  // check percentages that are not on the page.
  const reading = read(el("MAIN", [
    el("P", [text(SHARE)]),
    el("SPAN", [text("99% of nothing at all")], { style: { display: "inline", visibility: "hidden" } }),
  ]));

  assert.match(reading, /76\.8%/, "the visible share must still be read");
  assert.doesNotMatch(reading, /99%/);
  assert.equal(shareSites(reading).length, 1);
});

test("a display:none subtree is skipped too", () => {
  const reading = read(el("MAIN", [
    el("P", [text("visible")]),
    el("DIV", [text("collapsed away")], { style: { display: "none" } }),
  ]));

  assert.equal(reading, "visible");
});

test("adjacent blocks are separated — a share must not fuse with the counts beneath it", () => {
  // "76.8%" + "1,161 of …" concatenating to "76.8%1,161 of …" would make the cell shape
  // unreadable to judgeShare, which is the shape most of these pages are now written in.
  const reading = read(el("MAIN", [
    el("DIV", [text("76.8%")]),
    el("DIV", [text("1,161 of 1,511 surveys whose dependencies resolved")]),
  ]));

  assert.doesNotMatch(reading, /%1,161/);
  const sites = shareSites(reading);
  assert.equal(sites.length, 1, `expected one readable share in ${JSON.stringify(reading)}`);
  assert.equal(judgeShare(sites[0]).ok, true);
});

test("INLINE siblings are not separated — the value keeps its percent sign", () => {
  // The other half of the separator rule: `<span>76.8</span><span>%</span>` must stay one token,
  // or judgeShare reads no number before the `%` at all and reports every cell as bare.
  const inline = (value) => el("SPAN", [text(value)], { style: { display: "inline" } });
  const reading = read(el("MAIN", [
    el("DIV", [inline("76.8"), inline("%"), inline(" of measured surveys (1,161 of 1,511)")]),
  ]));

  assert.equal(reading, "76.8% of measured surveys (1,161 of 1,511)");
  const sites = shareSites(reading);
  assert.equal(sites.length, 1, `expected one readable share in ${JSON.stringify(reading)}`);
  assert.equal(judgeShare(sites[0]).ok, true);
});

test("the walker survives being serialised into the page — it closes over nothing", () => {
  // ★ THE ONE FAILURE MODE THAT WOULD ONLY SHOW IN THE BROWSER. `page.$eval` sends this function to
  // the page as SOURCE: a reference to anything at module scope — a shared regex, a helper — throws
  // a ReferenceError in there and nowhere here, on a driver whose whole job is to notice silence.
  // `new Function` rebuilds it in a scope with no module bindings, which is the same test.
  const rebuilt = new Function(`return (${collectReaderText.toString()});`)();
  const island = el("CAI-FINDINGS", [], { shadowRoot: shadow([el("P", [text(SHARE)])]) });

  assert.equal(
    rebuilt(el("MAIN", [island]), { styleOf: (node) => node.style || null }).replace(/\s+/g, " ").trim(),
    SHARE);
});

test("the provenance section is skipped by selector, and an island inside it goes with it", () => {
  // textOutsideProvenance skips during the walk rather than removing from a clone, because
  // cloneNode does not carry shadow roots — see the note on that function.
  const about = el("SECTION", [el("P", [text("Taken over 54 dated readings")])], {
    matches: (selector) => selector === "section#about",
  });
  const root = el("MAIN", [el("P", [text("no measurement here")]), about]);

  const whole = collectReaderText(root, { styleOf: (n) => n.style || null }).replace(/\s+/g, " ").trim();
  const outside = collectReaderText(root, { styleOf: (n) => n.style || null, skipSelector: "section#about" })
    .replace(/\s+/g, " ").trim();

  assert.match(whole, /54 dated readings/);
  assert.equal(outside, "no measurement here");
  assert.equal(/\d/.test(outside), false, "with the provenance gone the page states no measurement");
});

test("★ a <br> breaks the line even though it computes as inline — a stat cell's label must not fuse into its population", () => {
  // ★★ THE FIRST REAL RUN'S ONLY DEFECT, AND IT WAS THE DRIVER'S. PageNodes.Stat renders
  //    "<p>{label}<br>{population}</p>", innerText turned that <br> into a newline, and the walker
  //    that replaced innerText asked the computed style instead — which says "inline". The result
  //    was "carry a known-vulnerable component7 of 10 surveys here whose…", and judgeShare read
  //    forward from the % looking for "N of M", found a number glued to a word, and called every
  //    stat cell on every country and language page a bare share: 34 problems, none of
  //    them on a page.
  // The browser really does compute display:inline for a <br>, so the fake says so too.
  const cell = el("P", [
    text("carry a known-vulnerable component"),
    el("BR", [], { style: { display: "inline" } }),
    text("1,161 of 1,511 surveys whose dependencies a scanner could resolve"),
  ]);

  const reading = read(cell);

  assert.ok(
    !/component1,161/.test(reading),
    `the label fused into the population: ${JSON.stringify(reading)}`);
  assert.match(reading, /component 1,161 of 1,511/);
});
