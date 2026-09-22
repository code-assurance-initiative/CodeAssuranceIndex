# CAI owns its pages

**Status:** PROPOSED. Nothing here is written yet.
**Asked for by the owner**, 2026-09-22, after the CAI cards on four sites were found to be drawn
four different ways. The owner's statement of it: *"Kennel should only provide the data. It's CAI's
responsibility to ensure they render the same — and for that it could and should use imprint."*

**The rule this document is written under:** "[PROPOSED]" means NOT WRITTEN. A proposal goes to the
owner before it is in a commit, not alongside one.

---

## 1. Why — and the question that settles it

If a second producer publishes to CAI tomorrow, will its survey pages render like Watchdog's?

**Today: no.** What crosses the wire from a producer is a NODE TREE, not data. Kennel's
`RegistryPageBuilder` returns `SyndicatedPage(path, title, metaDescription, PageNodes.Section([…]))`
and PUTs it to the CMS at `PUT /api/authoring/sites/{siteId}/syndicated/{**path}`. Imprint supplies
styling; **Kennel supplies layout, structure, URLs and prose.**

So a second producer would send its own sections, headings, widget tags, appearance names, link
cards, titles and meta descriptions. Both pages would be correctly themed and structurally
unrelated. The theme guarantees they look like the same SITE. Nothing guarantees they are the same
PAGE.

Four consequences, all live today:

1. **A producer owns the standard's URL namespace.** `RegistryPageBuilder.Root = "surveys"` and
   `CorpusPageBuilder.Root = "state-of-the-corpus"` both live in `Kennel.Watchdog.Core`.
2. **Every producer must learn the CMS's internals.** `PageNodes` is `internal` to
   `Kennel.Watchdog.Syndication`, and its own remarks list what an author must know: appearance
   names (`ip-ap-stat-band`, `ip-ap-table-list`), the 500-element page cap, and that the canonical
   rich-text subset has no `<table>` so a list must be re-rendered as one.
3. **The prose is a producer's too** — page titles, meta descriptions, "How this project compares".
   Two producers would not merely look different; they would SAY different things about the standard.
4. **"State of the corpus" already overclaims.** That builder reads Watchdog's own repository set.
   It is the state of WATCHDOG's corpus, published on the standard's site under a name that means
   everyone's. A second producer's measurements would be absent, with nothing saying so.

★ The strongest evidence is what happened on 2026-09-22 INSIDE one producer: three authors drew the
same CAI card three ways (`wd-survey`, `wd-surveys`, `wd-survey-card`), and it took the owner four
rounds to get them unified. If one producer cannot stay consistent with itself, two will not.

## 2. What is already right

The **data** boundary exists and is well built. `POST /api/registry/deliveries` takes a signed
CAI-delivery package, is gated by `RegistryClaims.ProducerPolicy`, and rejects unknown or retired
keys, bad signatures, unsupported schema MAJORs and non-reproducing packages (422). The registry
stores and distributes; the open reference scorer recomputes a verdict from a package with no
network and no account.

**Nothing in this plan changes that.** The half that never moved is page composition.

Imprint stays the renderer. That part is already right — it is who COMPOSES that is misplaced.

## 3. Where the boundary moves

| Concern | Today | After |
| --- | --- | --- |
| Measure; sign a delivery | Kennel | Kennel |
| Accept a signed delivery | CAI | CAI |
| Decide `/surveys/**`, `/state-of-the-corpus/**` URLs | **Kennel** | **CAI** |
| Compose the page (sections, widgets, props) | **Kennel** (`PageNodes`) | **CAI** |
| Page titles, meta descriptions, prose | **Kennel** | **CAI** |
| PUT pages to the CMS | **Kennel** (`HttpRegistrySyndication`) | **CAI** |
| Render and serve | Imprint | Imprint |

---

## 4. Phase 0 — the field mapping (DONE, below)

What a page needs, against what a 1.0 delivery carries. Read off
`schemas/cai-delivery-1.0.schema.json` and `PublicContent.GalleryCard` on 2026-09-22.

### Carried today — no change needed

| Page element | GalleryCard | Delivery 1.0 |
| --- | --- | --- |
| Repository, owner, display | `Owner` `Name` `Display` | `payload.subject.repository` (split on `/`) |
| Forge | — | `payload.subject.host` |
| Commit | — | `payload.subject.commit`, `evidence.commit` |
| Headline CAI | `HeadlineScore` `BestScore` | `verdict.cai`, `evidence.headlineScore` |
| Band word | `Band` | `verdict.band` |
| Per-lens scores | `CodeHealth`…`EventSourcing` | `verdict.lenses[]` — **richer**: score, band, weight, contribution, criticalGated |
| Rubric version | `RubricVersion` | `payload.rubricVersion`, `evidence.rubricVersion` |
| Production LoC | `ProductionLoc` | `evidence.productionLoc` |
| Projects | — | `evidence.analyzableProjects` |
| Rebuild cost | `CostApprox` | `evidence.rebuildCost` |
| Quality bar | `QualityBar` | `evidence.qualityBar` |
| Survey depth / fit | dimensions resolved | `evidence.surveyFit`, `evidence.dimensions[]` |
| **Producer attribution** | — *(absent, because there is one)* | `payload.producer.{name,scanner,scannerVersion}` |

★ `BandHex` is deliberately NOT carried and must not be. The hue is presentation; CAI derives it
from the band word and its own cutlines. A producer shipping a colour is a producer deciding how the
standard looks.

### Gaps — must be closed before Phase 3

| Page element | Gap | Proposed |
| --- | --- | --- |
| `PrimaryLanguage` | not in the schema | **DONE 2026-09-22** — `subject.languages.primary` at MINOR 1.1 |
| `SecondaryLanguages` | not in the schema | **DONE 2026-09-22** — `subject.languages.secondary[]` at 1.1 |
| `Tags` (app kind, architecture style) | not in the schema | add `subject.tags[]` at 1.1 |
| Bus factor row ("1 of 3 developers") | `evidence.busFactor` is a producer-worded STRING | add `evidence.keyPersonRisk.{busFactor,authorCount}` at 1.1; keep the string for back-compat |
| ~~Measured-at~~ | ★★ **NOT A GAP — THIS ROW WAS WRONG.** `payload.measurement.scannedAt` has carried it all along ("when the code was scanned; may precede issuedAt"), and its doc-comment says exactly this. Found by reading the payload rather than this plan. The DISTINCTION still mattered: the first portrait dated itself by the SIGNATURE, which tells a reader the code was looked at on a day nobody looked at it — caught by a test before it reached a page. | none needed |
| `SourceUrl` | — | DERIVE from `subject.host` + `subject.repository`; do not carry |

MINOR 1.1 is forward-compatible by the schema's own rule ("a higher MINOR is forward-compatible and
unknown fields are ignored"), so 1.0 producers keep working and their pages simply render fewer rows.

### Deliberately NOT carried — CAI derives these

`Series` · `History` · `FirstScore` · `FirstAt` · `ScanCount` · `Delta`

★★ **The climb must not be shipped.** CAI holds every delivery for a subject over time, so it can
derive the trend itself. Watchdog computing it today is precisely the shortcut that makes the page
Watchdog's: two producers computing "the climb" their own way would put two different trend lines on
one standard. One holder of the sequence, one derivation.

---

## 5. Phases

Each phase states a DONE that is an observation or a command, never a report.

### Phase 1 — the acceptance test, written first  ·  **DONE 2026-09-22 (CAI `65569a5`), RED as intended**

A test in CAI that publishes **two deliveries for the same subject from two different producers** and
asserts the generated node trees are identical except for attribution.

This is the only thing that answers the question this plan exists for, and writing it first means the
move is measured against it rather than declared against it.

**Done when:** the test exists and FAILS (nothing generates pages in CAI yet).

### Phase 2 — the pixel baseline, captured BEFORE anything moves

★★ Required by the owner: *verify with Playwright that the updated result is pixel-perfect the same
as before.*

**DONE 2026-09-22.** 25 pages · 150 captures · 0 failures · 90 MiB. Script `tests/visual/capture.mjs`,
manifest `tests/visual/baseline-manifest.json`, images at
`~/Hentet/kennel/cai-owns-its-pages/baseline/` (outside the repository; the manifest records each
file's sha256).

★★ **TEN SHAPES, NOT THE SIX THIS PLAN FIRST NAMED.** The sitemap has `survey-index`,
`survey-language`, `survey-portrait` (6,271), `survey-portrait-deep` (nested forge groups, 5),
`corpus-index`, `corpus-language` (20), `corpus-advisory` (2,916), `corpus-package` (811),
`corpus-country` (42) and `corpus-listing` (4). Three of those were not on the list this plan was
written from, which is the baseline earning its keep before a line of the move was written.

The sample is seeded and recorded rather than "a few pages I happened to open", and the three
portraits are chosen to DIFFER — 31.3 to 97.6, Weak through Exemplary, 4 to 24 readings, three
different rubric versions. A baseline of three healthy six-lens repositories would pass a builder
that silently drops a missing lens or cannot draw a one-reading trajectory.

★★ **AND NOT EVERY SHAPE CAN BE PIXEL-COMPARED — say it here rather than discover it as a wall of
red in phase 6.** A survey portrait is pinned to a commit and a rubric version, both recorded, so the
same inputs replay and the render compares pixel for pixel. An AGGREGATE page — corpus index,
language, package, country, advisory — is a reading over the whole corpus on the day it was taken,
and the corpus changes daily: those will differ between any two captures for reasons that have
nothing to do with who composed them. They are verified by the node-tree diff over a frozen dataset.
Six of the twenty-five pages are pixel-comparable; the manifest's `comparability` block names which
and why.

★ Two attempts at recording the facts were wrong before the third was right, and both failed
SILENTLY. "CAI 97.6" is not how a page writes its score; "Where 97.6 sits on the scale" is on the
page but inside an island's shadow root, which `innerText` does not pierce. The score is read
structurally from the stat band, whose level-2 headings ARE the figures, each paired with its label —
and there is deliberately no "first figure" fallback, because that one reported the surveys index as
`cai=6,276`, which is a count of codebases. A manifest that labels a population as a score is not a
weaker record, it is a false one.

### Phase 3 — move the vocabulary

`PageNodes` and `PageProse` → CAI. Already documented as "shared vocabulary … belongs to no one
publisher"; `internal` to Kennel today, which is exactly what forces a second producer to reimplement
them.

**Done when:** CAI's vocabulary reproduces the golden captured from the producer's copy, byte for
byte.

★ THE PLAN FIRST SAID "done when `grep PageNodes kennel/src` is empty", AND THAT WAS WRONG: the
producer's builders still call it until phase 5, so the deletion belongs there. A phase whose exit
condition cannot be met until a later phase is a phase that never closes.

**DONE 2026-09-22** (CAI `d9aba92`, kennel golden alongside it). `Figure`, `FigureKind`, `FigureLead`,
`FigureSplit` and `Basis` moved with it, which this plan did not anticipate: "a number together with
what it is a number OF — the population it was taken over" is the STANDARD's concept, not a
producer's, and it is what `CLAUDE.md`'s *never select on the outcome* rests on.

### Phase 4 — move the builders

`RegistryPageBuilder`, `CorpusPageBuilder`, `CorpusSheet`, `CorpusDetailPages`, `LanguageBoardRow`
→ CAI, re-sourced from the registry rather than `PublicContent.BuildGalleryCardsAsync`.

★ Not a file move. "Published and promoted" is a Watchdog concept today (`Hidden`, `GalleryOptIn`,
the climb gate). CAI needs its own, defined on the registry: a delivery is published if its owner org
granted publication, full stop.

**Done when:** Phase 1's test passes AND `RegistryPageBuilder` does not exist in Kennel.

**Progress, 2026-09-22.**

| Piece | State |
|---|---|
| Survey portrait (`SurveyPageBuilder`) | **done** — phase 1's neutrality test passes |
| Language field guides + surveys index (`SurveyCorpus`, `FieldGuideBuilder`, `SurveyIndexBuilder`) | **done** (`f61e151`) |
| The corpus AGGREGATION (`CorpusReading`, `AdvisoryCut`) | **done** (`a5f92c9`) |
| The corpus sheet §1 §2 §3 §4 (`CorpusSheetBuilder`, `CorpusCuts`) | **done** (`41d38ea`, `2722119`, `2509c43`) |
| Advisory + package pages and indexes (`CorpusAdvisoryPages`) | **done** (`39370d1`) |
| Per-language + per-country pages and indexes (`CorpusGroupPages`) | **done** (`813fb83`) |
| The corpus sheet §5 Series | **blocked on a store — see below** |
| Kennel's builders deleted | not started — belongs with phase 5 |

★★ **§5 NEEDS SOMETHING THIS PLAN DID NOT NAME: AN APPEND-ONLY RECORD OF READINGS.** The producer
draws its trend through `metrics.corpus_snapshots`, and its own remarks say why that is not optional:
*"snapshots are append-only precisely so that a line drawn through them is checkable point by point;
deriving one from today's data would draw a line that never happened, and no reader could tell the
difference."*

It is tempting to say the standard is exempt, because it holds every delivery ever and can re-fold
"the corpus as of day D" from the deliveries issued on or before D — reproducible by anyone holding
the same packages, which is strictly better than a snapshot table. **It is not exempt.** A
re-derivation is only faithful if the PUBLISHED SET never changes, and publication is a grant an
owner org can withdraw: a codebase that was in the reading on the day would silently drop out of a
reading recomputed later, which is precisely "a line that never happened". So CAI records each
reading it publishes, append-only, and may additionally state that it is reproducible.

That store belongs with phase 5, which is where the reading starts being taken on a schedule.

★ **ONE DELIBERATE DIFFERENCE FROM THE PRODUCER'S PAGES, FLAGGED BECAUSE IT CHANGES WHAT IS
PUBLISHED:** the median column in §3 and §4 carries no band ink. A band word is read off a rubric's
cutlines, and the codebases in one row were scored under many rubric versions whose cutlines a later
one may have moved — banding their MEDIAN publishes a word no single rubric backs. `Cai.Scoring.Bands`
deliberately offers no way to band a bare number for exactly that reason, and these pages are not
going to be the thing that works around it.

★★ **THE PLAN'S PHASE-0 MAPPING WAS TAKEN OVER THE RECORD TYPES AND MISSED THE GATE.**
`schemas/cai-delivery-1.0.schema.json` is embedded in `Cai.Web.Registry` and validated over every
inbound package *before any cryptography*, and its `subject` block declares
`additionalProperties: false`. So `subject.languages`, added to `DeliverySubject` in phase 4's first
commit, was **dead on the wire**: 400 at ingest, with no symptom but a producer whose deliveries stop
arriving. Fixed at `49e21cc`, which also landed MINOR **1.1** — the doc comments had been claiming
1.1 while `DeliverySchema.Current` still said `"1.0"`.

★★ **AND THE CORPUS FAMILY NEEDED A SECOND FIELD THE MAPPING NEVER LOOKED FOR.** The corpus pages
are an aggregate of per-codebase SECURITY facts — vulnerable dependencies by severity, disclosure
policy, committed secrets, the four supply-chain controls, the advisories a survey could see — and a
1.0 delivery carries none of them: `evidence.dimensions` holds *scores*, never what was found. That
is `evidence.securityReading`, also at 1.1. The reason to put it under `evidence` rather than beside
the verdict is that it is the producer's open record and is never folded into the CAI — the same
standing as `rebuildCost`, `busFactor` and `topology` — and `evidence` is the one block the schema
declares `additionalProperties: true`.

★ **STILL OPEN: WHERE A CODEBASE COMES FROM.** `state-of-the-corpus/country/*` (42 pages) is cut by
the owning forge account's free-text profile line, which only the producer ever sees
(`ICorpusOriginSource`). Nothing in a delivery carries it. It needs `subject.owner` facts — declared
something / resolved country — and the DENOMINATORS with them, because "a country share taken over
the accounts that happen to fill in a profile field measures who fills in a profile field, not where
software is written".

### ★★ OPEN FOR THE OWNER — peak or latest?

Found by phase 6's node diff, 2026-09-22, and it is the one difference that changes a published
number.

The producer publishes a repository's **PEAK** measurement as the face of its page: a regression is
never promoted to the current score, so the specimen page shows **72.4** while the newest reading on
its own trend line is **71.0**. The standard as built publishes the **LATEST** reading, and says so
in the words beside it — *"its most recent published measurement, taken on …"*.

Both are defensible and only one can be on the page:

- **Peak** is what the gallery's redemption arc is built on, and it never punishes a codebase for
  being measured again. It also means the number beside the chart is not the last point on the chart.
- **Latest** is what "measured at a pinned commit, on a stated day" already promises, and it is the
  only reading a survey page's own prose supports.

It changes the number every regressed codebase publishes, so it is not the implementer's call. Held
by `ProducerPortraitDiffTests.The_headline_differs_because_the_producer_publishes_the_peak_and_the_standard_the_latest`
so the two cannot quietly diverge while it is open.

### Phase 5 — move the sweep and the CMS client

`IRegistrySyndication`, `HttpRegistrySyndication`, `RegistryPublishService`,
`RegistryPublishHostedService`, `RegistrySweepMemory`, `RegistryIngest*` → CAI. The CMS bearer token
moves with them.

**Done when:** Kennel holds no CMS credential and no knowledge that a CMS exists —
`grep -rn "syndicat" kennel/src` returns nothing.

**KENNEL'S HALF IS DONE** (kennel `4766141f2` + `b9858c7c4`): 18,842 lines out, `grep -rn "syndicat"
kennel/src` finds one unrelated line of seed prose, whole suite 29 projects / 8,236 passed / 0 failed.

★★ **THE CUTOVER ORDER MATTERS AND IS NOT INTERCHANGEABLE.** Both publishers sweep the same two roots
and each withdraws what the other published, so they must never run at once:

1. **Promote kennel first.** Its publisher is gone, so nothing sweeps. The site serves static files —
   the pages do not disappear, they stop being updated.
2. **Then deploy CAI.** The publisher starts, the pages resume, composed by the standard.

Deploying CAI first would have the two fighting over `surveys/**` and `state-of-the-corpus/**`.

**CAI's half is done** (`e81b296`): `ISiteSyndication`, `HttpSiteSyndication`, `SitePublishService`,
`SitePublishHostedService`, `SyndicationOptions`, and the append-only readings store (`52ce1d2`).
Registered only when it has a site, an id and a token — credentials are not a feature switch.

**Kennel's half is NOT started, deliberately, and the order matters.** Deleting the producer's
builders destroys the only thing the new pages can be diffed against, and phase 6 has only just
begun. It also needs a decision the plan did not separate:

★★ **`CorpusMirror` IS DATA, NOT RENDERING, AND IT STAYS.** It serves
`/api/public/state-of-the-corpus` and `…​.csv` from the producer's own metrics — the machine-readable
mirror of a reading, which is exactly what "Kennel should only provide the data" asks it to keep
doing. It sits in the `Corpus/` folder beside the page builders and would have gone out with them
in a folder-shaped deletion.

Same question for `RegistryIngest*`, `RegistryScanNotifier` and `ScanIngestedFanOut`: those push
DELIVERIES to the standard's registry, which is the producer's job and not the CMS's. Only the CMS
client, the page builders, the sweep and the vocabulary leave.

### Phase 6 — pixel-perfect verification

Replay the Phase 2 deliveries into preprod's registry, let CAI's sweep publish, and capture the SAME
matrix. Then:

1. **Node-tree diff** — old builder output vs new, over the same deliveries. Deterministic, catches
   structure, runs in CI. Any difference must be named and justified before a pixel is looked at.
2. **Pixel diff** — per image, count differing pixels. **The gate is zero.** A non-zero diff is
   either a defect or an explicitly recorded, owner-approved intentional change; nothing is waved
   through as "close enough".
3. Both themes, all three widths, every shape.

★ Known allowlist, to be agreed BEFORE the run rather than after a failure: a rendered timestamp
(the corpus dateline) legitimately differs between captures. Everything else is a defect until
somebody says otherwise.

**Done when:** the owner has seen the diff summary, and any non-zero cell is explained.

**STEP 1 IS DONE for all three families.** Three goldens frozen in the producer's own repository
(`68f48f5ce`, `2119cf12b`, `d1b85ed16`) and diffed in CAI (`0de5319`, `23b058e`, `0083d15`).

What it found, none of which a reading of the code had:

| Family | Finding | Outcome |
|---|---|---|
| portrait | "About this page" ported as `Section("note", null, …)` — an appearance the theme does not style, and no anchor to link to | fixed |
| portrait | the stat band's language cell missing entirely | fixed |
| portrait | headline is the PEAK in the producer, the LATEST in the standard | **open — owner** |
| index | corpus count read "measured codebases" where the producer says "**published** measured codebases" | fixed |
| guide | producer says "projects" in the title and stat band where its own index says "codebases" | deliberate: the standard says codebases throughout, named in a test |
| sheet | `masthead`, §6 `elsewhere` and the `about` note all missing | fixed |
| sheet | §5 handed `cai-trend` a `heading` prop the island does not read, and put the movements in a band beside it instead of in its `figures` row | fixed |

Everything else matches: the nine corpus addresses, the sections in order with their appearances, the
islands and their tags, the trend island's props name for name, the rest of every stat band, the
addresses and the titles.

★ The aggregate pages' FIGURES are deliberately not diffed, and the reason is in the data rather than
the effort: the producer folds its sheet from its own metrics database over every repository it has
ever surveyed, and the standard folds it from the published deliveries it holds. Those are different
populations by construction, so a figure-for-figure diff would report the difference between two
corpora and call it a defect in a page.

**Step 2 (pixel) has NOT begun, and needs something only the owner can give:** a preprod CMS token
and site id for CAI's `Syndication` section, so the sweep can publish into preprod and the same
capture matrix can be re-run. Nothing here should point a publisher at a live site on its own.

### Phase 7 — close the door

The CMS syndication API currently accepts an arbitrary node tree from anyone holding a token. While
that is true, consistency is held by discipline — and discipline is what failed four times in one
session inside one producer.

Either scope the CMS token to CAI alone, or keep it open and accept that layout is negotiable.
**[PROPOSED] — this is the owner's call, not the implementer's.**

### Phase 8 — cutover

Kennel stops sweeping; CAI starts. The two must not both run.

★ This is an ordered cutover, not a deploy: Kennel's sweep is disabled and confirmed stopped BEFORE
CAI's is enabled, or two writers race over ~6,400 pages.
★ The nightly promote is PAUSED (`~/.config/kennel/autopromote=false`); prod moves on the owner's
Promote button. The Kennel half of this reaches prod only when that is pressed.

---

## 6. What to flag before approving

- **A large move with no user-visible change.** Every page should be identical afterwards, which
  makes it easy to do badly and hard to notice. Phases 1 and 2 exist for that reason and must come
  first.
- **~6,400 survey pages** plus the corpus family. The sweep already handles that from Kennel, so it
  is a move rather than a new capability — but the first CAI-side sweep rewrites every page, which
  is a large publish.
- **The measurement question is separate and bigger.** A corpus aggregated across producers with
  different cadences and different repository selection is a SAMPLING question. `CLAUDE.md`'s *never
  select on the outcome* says the frame must be pre-registered in `METHOD.md` before a figure is
  published over it. Moving the page builder does not answer it, and the two should stay apart so
  neither hides the other.
- **Not in scope:** the widget CSS and templates in imprint. Those are the renderer's and are
  already shared by every card.
