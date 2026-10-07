# CAI scoring — the fold, for implementers

- Status: **Normative.** This document states how an evidence bundle folds into a CAI under a rubric catalog, at the
  level of detail a second implementation needs to reproduce the reference scorer number for number. Where the
  published narrative at [codeassuranceindex.info/spec](https://codeassuranceindex.info/spec/) summarises a rule, this
  document is the precise form of it; where the two disagree, report it as a defect ([CHALLENGE.md](../CHALLENGE.md) §2).
- Reference implementation: [`src/Cai.Scoring`](../../src/Cai.Scoring). Conformance: [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md), Part A.

## 1. Inputs

A fold takes two documents and nothing else:

- an **evidence bundle** — what a producer measured in one codebase at one commit;
- the **rubric catalog** for the version the bundle names in `rubricVersion` — the frozen rules. A fold without the
  catalog is not a CAI; there is no default rubric.

### 1.1 The evidence bundle

| Field | Meaning |
|---|---|
| `rubricVersion` | The rubric the evidence was measured under. Selects the catalog. |
| `dimensions[]` | Deterministic measurements, each `{id, category?, score, confidence, coverage?, advisory?}`. |
| `metaDimensions[]` | Measurements that feed a lens directly, each `{id, lens, score?, advisory?}`. |
| `qualityBar` | The quality bar the codebase is judged against (§6). Absent ⇒ `production`. |
| `analyzableProjects`, `productionLoc` | Inputs to the architecture surface floor (§4.2). |
| `headlineScore` | The producer's claimed CAI. Never an input to the fold; only compared against it (§7). |

**`score`** is 0–10. **`confidence`** and **`coverage`** are 0–1 and mean different things:

- **Confidence 0 means not measured.** A contributor with confidence 0 carries no weight in its category. A producer
  that did not measure a dimension should leave it out of the bundle; it must never send it as `score: 0`, which reads
  as a measured failure.
- **Coverage 0 means measured, and nothing reached.** The effective score is `score × coverage`, so coverage 0 is a
  measured zero: it folds, and it can trigger the critical gate (§4.3). Coverage absent ⇒ 1.

A meta-dimension with `score` absent (null) is not measured and contributes nothing.

### 1.2 The rubric catalog

| Field | Meaning |
|---|---|
| `lenses[]` | `{key, label, group?}` — the ten lenses, and optionally the quality-bar group each follows (§6). |
| `dimensions[]` | Every contributor the rubric defines: `{id, lens, family, category?, evaluator, advisory?, …}`. `family` is `dimension` or `meta`. |
| `scoring` | The score-moving constants (§4–§6). Absent ⇒ the defaults listed with each rule below. |
| `rejectUnknownContributors` | `true` ⇒ the catalog is **closed** (§2.2). |

Fields marked `?` are declarations a catalog may or may not make. Each rule below says what applies when a catalog
does not make one; in every case that is the behaviour the rubric versions published before the field were computed
under, so they keep reproducing.

## 2. Which contributors count

### 2.1 Advisory contributors

An **advisory** contributor is reported but never folded: it does not enter its category or lens, and it cannot
trigger the critical gate. Readings made by a language model are advisory, as are readings the rubric publishes as
evidence only.

- **When the catalog declares `advisory` for a contributor, the catalog decides.** A contributor the catalog declares
  advisory is left out whatever the bundle says. A bundle that marks advisory a contributor the catalog declares
  scored is **refused**: whether a measurement counts is the rubric's decision, not the producer's.
- When the catalog does not declare it, the bundle's `advisory` flag decides (absent ⇒ scored).

### 2.2 Closed catalogs

A catalog with `rejectUnknownContributors: true` lists every contributor a bundle may carry under it. The fold is
refused when the bundle carries:

- a dimension id the catalog does not define;
- an id the catalog defines as `family: meta`, sent in `dimensions[]`;
- a meta-dimension id the catalog does not define as `family: meta`;
- a meta-dimension whose `lens` differs from the lens the catalog gives it.

A catalog that is not closed folds unknown contributors under the category or lens the bundle states.

### 2.3 Categories

Each dimension folds into a **category**. When the catalog assigns one, that assignment governs; a bundle that states
a different one is refused. When the catalog assigns none, the bundle's must be present. Each category belongs to one
lens:

| Category | Lens |
|---|---|
| `code-quality`, `explicit-debt` | `codeHealth` |
| `architecture` | `architecture` |
| `docs`, `git-mining` | `maturity` |
| `testing`, `dependencies`, `security` | `productionReadiness` |
| `security-compliance` | `securityCompliance` |

A category the reference scorer does not implement fails the fold rather than being guessed.

## 3. Stage 1 — categories

For each category, over its scored (non-advisory) dimensions:

```
category = 10 × Σ (score × coverage × confidence) / Σ confidence
```

A category whose scored dimensions sum to zero confidence has no score and is left out of everything that follows.

## 4. Stage 2 — lenses

### 4.1 The within-lens fold

A lens's items are its category scores (0–100) and its scored meta-dimensions at `score × 10`. The lens score is the
**worst-first ordered weighted average** of its items with decay `q = scoring.withinLensQ` (default 0.75):

```
sort the items ascending (worst first; equal values keep their input order)
weight of the item at rank r (r = 0 for the worst) = q^r, normalised so the weights sum to 1
lens = Σ weight × item
```

A lens with no items has no score.

### 4.2 The architecture surface floor

Applied to `architecture` only, after 4.1:

- `analyzableProjects = 0` ⇒ the lens has no score (nothing to grade is not a pass).
- `analyzableProjects < scoring.architectureSurface.minProjects` (default 2) **and**
  `productionLoc < scoring.architectureSurface.minProductionLoc` (default 1500) ⇒ the lens is capped at
  `scoring.architectureSurface.lowSurfaceCap` (default 69).

### 4.3 The critical gate

A scored contributor below `scoring.criticalGate` (default 4.0 on the 0–10 scale) — a dimension with confidence > 0
whose `score × coverage` is below it, or a meta-dimension whose score is — **caps its lens's band at Adequate**. The
gate changes the band, never the number, and the result names the contributors that triggered it.

## 5. Stage 3 — the headline

The headline is the worst-first ordered weighted average (as 4.1) of the lens scores that exist, with decay
`q = scoring.acrossLensQ` (default 0.55). Each lens's weight and contribution (`score × weight`) are part of the result,
and the headline is their sum.

**A conditional lens is in the headline exactly when it has a score**, i.e. when the bundle carries at least one scored
contributor for it. The five core lenses (`codeHealth`, `architecture`, `maturity`, `productionReadiness`,
`securityCompliance`) apply to every codebase; the five conditional ones (`domainModelling`, `eventDriven`,
`eventSourcing`, `accessibility`, `performance`) apply where the codebase has what they grade. Deciding that is part of
measuring, and so the producer's: the scorer never adds a lens the evidence does not carry, and never removes one it
does. A producer must not leave a conditional lens out because it scored badly; that is a measurement withheld.

## 6. Bands and the quality bar

Bands are read off **unrounded** values with cutlines `scoring.bands` (defaults: Exemplary 90, Strong 70, Adequate 50,
Weak 25; below 25 Critical).

- **The headline** is always banded on the baseline cutlines, then capped at one band above the band of the weakest
  measured category (the headline never out-promises its weakest part).
- **A lens** is banded on cutlines shifted by its quality bar:
  `shift = scoring.qualityBar.offsets[bar] × scoring.qualityBar.lensGroupFactors[group]`, applied to all four lines,
  with the Exemplary line capped at `exemplaryCeiling` and the Weak line floored at `poorFloor`. Then the critical
  gate (§4.3) applies.

**The quality bar** is the bundle's `qualityBar`. It is a declaration about the codebase — how critical its owner says
it is — and the scorer never infers it. Accepted spellings: `template`, `poc`, `one-off`, `prototype` ⇒ prototype;
`preview`, `alpha`, `beta` ⇒ preview; `production`; `mission-critical`. Absent or unrecognised ⇒ production. A producer
may estimate a bar for its own presentation, but must not write an estimate into the bundle as if the owner had
declared it. The bar moves band lines only; it never moves a score.

**The group** of a lens is the catalog's `lenses[].group` when declared. Otherwise:

| Lens | Group |
|---|---|
| `codeHealth`, `architecture` | `foundational` |
| `maturity`, `productionReadiness` | `operational` |
| `securityCompliance` | `safety` |
| the five conditional lenses | `default` |

A group the reference scorer does not implement fails the fold.

## 7. Precision

The fold is computed in IEEE 754 double precision. Results are stated as follows:

| Value | Stated to | Rounding |
|---|---|---|
| Headline, lens scores, contributions, category scores | 2 decimals | half to even |
| Lens weights | 4 decimals | half to even |
| Presentation (pages, reports) | 1 decimal | half to even |

Bands and the critical gate are decided on unrounded values. **Two implementations conform when every value they
state agrees at the stated precision** for every conformance vector. `verify` — checking a producer's claimed
`headlineScore` against a fresh fold — accepts a difference of at most 0.5.

## 8. What does not enter the fold

Findings, suppressions, declarations other than the quality bar, contract profiles, narration and every descriptive
field a bundle may carry (such as how much of the survey resolved). They can change what a report says. They cannot
change the number.
