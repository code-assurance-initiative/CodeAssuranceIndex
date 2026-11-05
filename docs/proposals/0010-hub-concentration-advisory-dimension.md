# Proposal 0010: Hub concentration as an advisory architecture dimension

- **Number:** 0010
- **Filed:** 2026-11-05
- **Revised:** 2026-11-05 — rewritten after an independent blind review of the first draft (three reviewers,
  convergent). The changes are substantive and are summarised in [After review](#after-review) at the foot of this
  file so the decision can see what moved and why. (First drafted as a proposal numbered 0003 in an earlier pull
  request; renumbered to 0010 because 0003 was already taken by an accepted decision. The earlier pull request is
  closed in favour of this one.)
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains the CCA engine (formerly Watchdog), a commercial implementation that
  produces CAI evidence and already computes the graph this proposal reads. The proposer also owns code measured by
  CAI. The proposer benefits from a richer standard. The dimension proposed here is **advisory**, so it enters no
  score and confers no scoring advantage on any implementation; the gain sought is a more useful standard, not a
  number. The proposer is also the acting maintainer, i.e. the decider — see [Governance](#governance).
- **Moves scores:** **no.** The dimension is advisory (unscored). The reference scorer folds only `!advisory`
  contributors, so it enters no lens and no headline; every published score is byte-identical. It adds a named,
  published concern and one plain-language card.
- **Status:** Proposed.

## Problem

The catalog names architectural **entanglement** (AX3, dependency cycles) but not **hub concentration**: whether a
codebase's incoming dependencies converge on a very small number of modules — in the extreme, a single module the
whole system reaches through. That shape is a real, measurable modularity property; it is one of the sub-metrics
third-party labs publish (SIG's Sigrid reports it), and it was the hinge of the public ArtCraft/PhotoCraft analysis,
where a reviewer's "spaghetti" diagram is, in graph terms, dependency convergence on a few hub modules.

Today a reader of a CAI report cannot see this. The honest difficulty is twofold:

1. **Concentration alone is not a defect.** A shared kernel that many modules depend on is normal and usually
   healthy. The thing that makes a hub dangerous is that it *changes often*: a volatile module everything depends on
   ripples its churn across the system. A measure that flags concentration without regard to volatility cannot tell a
   healthy shared kernel from a god-module, and would mislabel the former.
2. **The engine already scores the dangerous case — but only for one ecosystem.** The scored dimension **D5
   (Coupling)** already combines afferent coupling *with* instability (its `unstable hub` rule fires on
   `instability > 0.8 AND afferent > 0`) and already **exempts recognised shared kernels** — so D5 penalises exactly
   the volatile-hub case and spares the healthy one. But D5 reads the **.NET project-reference graph** (`.csproj`,
   `.vbproj`, `.fsproj`); it is silent on Rust, Go, Python, the JVM languages and the rest of the estate, and it
   works at *project* granularity, not at module/namespace granularity.

So there is a genuine gap, but a narrow one: a **language-neutral, module-granularity** view of where a codebase
converges, for the estate D5 does not reach — and it must not repeat the mistake of calling concentration a defect.
This proposal fills that gap as an **advisory, descriptive** dimension that names the convergence and *defers the
risk judgement to D5 and D35*, rather than asserting risk from concentration alone.

See issue [TODO: link the governing issue](../../../issues) (required by the filing process; opened before this
revision is decided).

## Change

Add one **advisory** architecture meta-dimension to the catalog:

```json
{
  "id": "AX11",
  "name": "Hub concentration",
  "lens": "architecture",
  "evaluator": "tool",
  "whatItMeasures": "Whether incoming dependencies converge on a very small number of hub modules — in the extreme, a single module most of the system depends on. Descriptive and advisory: it names where the code converges and the hub(s) by name; it does not judge whether that is a risk. The risk judgement belongs to D5 (Coupling), which conditions on volatility and exempts shared kernels, and to D35 (Change Coupling). Reads the language-neutral namespace/module graph, so it covers every language the code model reaches, including those D5's .NET project graph does not.",
  "family": "meta",
  "scoringPolarity": "deduction",
  "advisory": true
}
```

The operative rule is part of the standard and is pinned in the rubric version this decision mints — it does **not**
live only in an implementation. The version records every parameter below, so the card is reproducible from the
published standard alone and cannot change silently when an implementation changes:

- **The statistic.** The **top-1 afferent share**: the single most-depended-on module's incoming-dependency count
  divided by the total of all incoming-dependency counts. (The engine already carries both terms — `MaxFanIn` and the
  named `FanInHub` — in its namespace-graph artifact.) The share answers the question the reader actually asks — *does
  my system funnel through one module* — which a whole-distribution inequality statistic (a Gini coefficient) does
  not: three co-equal hubs and one dominant hub can share the same Gini. The report also lists the top few hubs by
  name and shows the Gini as a secondary descriptor, but **the Gini is not the firing statistic**.
- **The graph.** The **language-neutral namespace/module dependency graph** (`NamespaceDependencyGraphBuilder` over
  the neutral code model), the same graph the modularity measure reads — **not** the .NET project-reference graph that
  D5 and AX3's cycle check read. The two graphs give different numbers; this one is at module/namespace granularity
  and spans every language the code model covers.
- **The exemption.** A hub recognised as a **shared kernel** (the same `SharedKernelDetection` rule D5, AX4, AX5 and
  D23 already apply) does not raise the card. Concentration *on a shared kernel* is the normal, healthy case and the
  standard treats it the same way everywhere.
- **When it speaks.** Only above a pinned threshold on the top-1 share, and only on graphs of **15+ modules** (below
  that the share is unstable). Below the threshold, or on a smaller graph, it says nothing.
- **The threshold is not yet set, and it cannot be set honestly from the data in hand.** The first draft fired at an
  afferent-coupling **Gini** `>= 0.80`, cited as "the p90 of 3,000 real public codebases of 15+ modules". A
  re-measurement on 2026-11-05 shows that basis does not hold. The distribution that yields Gini median `0.653` and
  p90 `0.807` is `27,494` namespace graphs of 15+ modules found across the engine's local bench tree — but that set is
  dominated by the engine's own test fixtures, concept cases and repeated re-scans of the same handful of repositories.
  Deduplicated to **distinct real public repositories** of 15+ modules, the local tree holds only **single digits**
  (3 by strict owner/repo identity). A percentile taken over 27k fixture-and-re-scan graphs measures the engine's own
  draw, not the population of real codebases — the exact error the measurement-integrity rules forbid. So:
  - **No threshold is proposed as calibrated.** The `0.80` Gini figure is withdrawn as a calibration basis; the top-1
    share's broad-set p90 on the same (non-representative) tree is `~0.40`, recorded here only as a provisional,
    non-authoritative observation.
  - **Calibration is a condition of acceptance.** Before the card fires, the top-1 share must be measured over a
    **deduplicated corpus of distinct real repositories** — one latest graph per repository from the onboarded
    public-corpus scan — and a top-decile (p90) threshold derived on that. The decision pins the resulting number in
    the rubric version. A tracking task for that measurement is filed alongside this proposal (engine backlog
    `01a1ade5-f452-739d-9434-2e6bd9e912f4`), and its completion is a precondition of acceptance.
  - Until a representative threshold is pinned, AX11 is defined but **dormant** (it computes and stores the share but
    raises no card).
- **Plain language (the user-facing wording).** Descriptive, volatility-aware, no unconditional advice:

  > "A large share of your modules depend on **{hub}** (and {n} other hubs). That convergence is normal for a shared
  > kernel. It becomes a maintenance risk only if those hubs change often — see **Coupling** and **Change Coupling**,
  > which weigh exactly that."

  The card never says "split it up." Whether to split a hub depends on how volatile it is, which this dimension does
  not measure; the scored dimensions that do (D5, D35) carry that advice.

## Effect on published scores

**None.** `advisory: true` means the reference scorer excludes it (it folds only `!advisory` contributors), so AX11
enters no lens roll-up and no headline; re-scoring any historical bundle with the new catalog yields byte-identical
numbers. The only change a reader sees is a new advisory card — naming the hub(s) — where the top-1 share is in the
top decile, and only once the threshold is calibrated and pinned.

Why advisory and not scored: concentration is **not "lower is better."** The healthy shared kernel and the dangerous
god-module sit at the *same* concentration; only volatility tells them apart, and that is D5's and D35's job, where it
is already scored correctly. A 0–10 score on concentration alone would penalise the healthy shared-kernel design —
selecting on a shape that is not itself a defect. Flagging only the top decile, exempting shared kernels, deferring
the risk judgement, and contributing to no score is the honest treatment.

## Alternatives considered

1. **A scored dimension on concentration.** Rejected: it would move every published architecture score and would
   penalise the normal shared-core design, selecting on a shape that is not a defect (see above).
2. **A whole-distribution Gini as the firing statistic** (the first draft). Rejected: Gini measures inequality across
   all modules, not single-hub dominance, so it cannot bear the "one central module" claim, and its high tail is
   indistinguishable from a healthy many-small-plus-one-kernel shape. The top-1 share answers the actual question.
3. **Extend D5 to read the language-neutral graph** so the scored, volatility-aware measure covers every language.
   Rejected *for now*: D5 is scored, so widening its graph moves published numbers across the whole corpus and forces
   a measurement restatement — a heavy, score-moving change. It is the right long-term direction and should be its own
   proposal; this advisory dimension delivers the language-neutral *visibility* with no number moving.
4. **Surface D5's existing finding only, change nothing in the catalog.** Rejected: D5 is .NET-only and project-level,
   so it leaves most of the estate and all finer-grained convergence invisible, which is the gap here.
5. **Fold it into AX5 (architecture & structure).** Rejected: AX5 is scored and asks a different question; a finding
   there would move the score and conflate two concerns.
6. **Leave the measure in the implementation's artifact only.** Rejected: it never reaches the person reading the
   report or the index, and an implementation must not mint catalog content on its own
   ([decision 0002](../decisions/0002-rubric-versions-are-published-by-the-initiative.md)).

## Questions for the decider

- **The id and name** — `AX11`, "Hub concentration", the next in the architecture-model meta sequence.
- **The firing statistic** — top-1 afferent share is proposed, over whole-distribution Gini, for the reason in
  Alternative 2. The decider may prefer top-*k* share (e.g. the top 3 hubs combined) instead of top-1.
- **The threshold** — not set, and deliberately so: the local bench tree cannot calibrate it honestly (it holds only
  single-digit distinct real repositories of 15+ modules; see the threshold note above). The decider should treat a
  measurement over a deduplicated distinct-real-repository corpus as a **precondition of acceptance**, and pin the
  resulting top-decile (p90) top-1-share number in the rubric version. The decider may also prefer a different
  percentile or a fixed floor once the real distribution is seen.
- **The overlap with D5** — the proposal scopes AX11 as descriptive and defers all risk judgement to D5/D35. The
  decider should confirm that division, and whether extending D5 to the neutral graph (Alternative 3) should be filed
  as a follow-up.
- **Advisory vs scored** — the proposal argues advisory, for the polarity reason above; the decider may disagree.

## Governance

- **Process.** Filing step 1 (a governing issue) is [TODO: link] and must be linked before decision. This is a new,
  named, published dimension — material content, not a correction-that-moves-no-number — so it is **not** a standstill
  carve-out and takes the full route: the 14-day comment period on the pull request, then a numbered decision.
- **Comment period.** Filed 2026-11-05; the earliest a decision may be taken is **2026-11-19**.
- **Conflicted decider.** The proposer is the acting maintainer and the sole decider, maintains the CCA engine that
  produces this evidence, and owns code CAI measures. Per `GOVERNANCE.md`, a change may be accepted by the acting
  maintainer only if no unresolved objection remains from a commenter **not affiliated with the proposer**. Because no
  unaffiliated reviewer yet exists, the honest status is that this proposal **waits for the steering group, or for an
  unaffiliated reviewer to comment and be satisfied**, before it can be accepted — even though it moves no score. The
  decision record must state this conflict explicitly.

## What the CCA engine will do on its own side (noted, not part of the proposal)

The engine already carries the raw terms — `MaxFanIn`, the named `FanInHub`, and the `centralization` Gini as a
secondary descriptor — in its `namespace-graph.json` artifact, and already applies `SharedKernelDetection`. On
acceptance, and only behind the rubric version this decision mints, it will compute the top-1 share, apply the pinned
threshold and the 15-module gate, and emit the AX11 advisory card with the hub named — not before.

## After review

The first draft was read by three independent blind reviewers; all three returned *accept with amendments* and
converged on the same points. This revision answers them:

- **The statistic did not match the claim.** A Gini measures whole-distribution inequality, not single-hub dominance;
  it cannot say "one central module." → switched the firing statistic to the **top-1 afferent share**; Gini demoted to
  a secondary descriptor.
- **It ignored volatility and overlapped D5.** The scored dimension D5 already measures the dangerous case
  (afferent × instability, shared-kernel exempt) and a concentration-only flag would mislabel a healthy shared kernel
  as a god-module. → AX11 is now **descriptive**, **exempts shared kernels**, **defers the risk judgement to D5/D35**,
  and the card drops the "split it up" advice.
- **The operative rule lived only in the engine.** → every parameter (statistic, graph, exemption, gate, threshold,
  corpus) is **pinned in the rubric version**; the implementation note carries nothing load-bearing.
- **The graph unit was self-contradictory** (the draft said both "the graph the cycle check reads" and
  "namespace-graph"). → stated once: the **language-neutral namespace/module graph**, explicitly not D5/AX3's project
  graph.
- **Governance was incomplete.** → added the [Governance](#governance) section: the governing issue, the 14-day
  window (earliest decision 2026-11-19), the conflicted-decider standstill, and that this is not a carve-out.
- **"Extreme tail" overstated p90.** → called the top decile what it is.
- **The calibration basis did not survive re-measurement.** Checking the cited "3,000 real public codebases" against
  the actual files showed the 0.80 figure comes from ~27k fixture-and-re-scan graphs, not a representative repository
  sample (distinct real repos of 15+ modules number in the single digits locally). → withdrew the number as a
  calibration basis, made a measurement over a deduplicated real-repository corpus a precondition of acceptance, and
  left AX11 dormant until then.
