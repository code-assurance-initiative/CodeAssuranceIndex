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
| `PrimaryLanguage` | not in the schema | add `subject.languages.primary` at **MINOR 1.1** |
| `SecondaryLanguages` | not in the schema | add `subject.languages.secondary[]` at 1.1 |
| `Tags` (app kind, architecture style) | not in the schema | add `subject.tags[]` at 1.1 |
| Bus factor row ("1 of 3 developers") | `evidence.busFactor` is a producer-worded STRING | add `evidence.keyPersonRisk.{busFactor,authorCount}` at 1.1; keep the string for back-compat |
| **Measured-at** | `payload.issuedAt` is when CAI SIGNED | add `payload.subject.measuredAt` at 1.1 — the two are not the same fact and the page's honesty depends on the measurement date |
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

### Phase 1 — the acceptance test, written first

A test in CAI that publishes **two deliveries for the same subject from two different producers** and
asserts the generated node trees are identical except for attribution.

This is the only thing that answers the question this plan exists for, and writing it first means the
move is measured against it rather than declared against it.

**Done when:** the test exists and FAILS (nothing generates pages in CAI yet).

### Phase 2 — the pixel baseline, captured BEFORE anything moves

★★ Required by the owner: *verify with Playwright that the updated result is pixel-perfect the same
as before.*

Capture from prod, today, before a line moves:

- every distinct page SHAPE under `/surveys/**` and `/state-of-the-corpus/**` — index, per-repo
  portrait, language field guide, corpus index, corpus language page, advisory page
- 3 real repositories per shape, chosen to differ: one with a long history, one single-scan, one
  with a missing lens
- widths 1280 / 820 / 390, **both themes**
- full-page PNG, after scrolling to force every lazy island to hydrate

**Done when:** the baseline set is committed under `tests/visual/baseline/` with the delivery ids and
commit shas that produced it recorded beside it.

★ THE COMPARISON NEEDS A FROZEN INPUT. Pages change because scans change, so a before/after diff of
live pages proves nothing. The baseline records WHICH deliveries produced it, and Phase 6 replays
exactly those.

### Phase 3 — move the vocabulary

`PageNodes` and `PageProse` → CAI. Already documented as "shared vocabulary … belongs to no one
publisher"; `internal` to Kennel today, which is exactly what forces a second producer to reimplement
them.

**Done when:** `grep -rn "PageNodes\|PageProse" kennel/src` returns nothing.

### Phase 4 — move the builders

`RegistryPageBuilder`, `CorpusPageBuilder`, `CorpusSheet`, `CorpusDetailPages`, `LanguageBoardRow`
→ CAI, re-sourced from the registry rather than `PublicContent.BuildGalleryCardsAsync`.

★ Not a file move. "Published and promoted" is a Watchdog concept today (`Hidden`, `GalleryOptIn`,
the climb gate). CAI needs its own, defined on the registry: a delivery is published if its owner org
granted publication, full stop.

**Done when:** Phase 1's test passes AND `RegistryPageBuilder` does not exist in Kennel.

### Phase 5 — move the sweep and the CMS client

`IRegistrySyndication`, `HttpRegistrySyndication`, `RegistryPublishService`,
`RegistryPublishHostedService`, `RegistrySweepMemory`, `RegistryIngest*` → CAI. The CMS bearer token
moves with them.

**Done when:** Kennel holds no CMS credential and no knowledge that a CMS exists —
`grep -rn "syndicat" kennel/src` returns nothing.

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
