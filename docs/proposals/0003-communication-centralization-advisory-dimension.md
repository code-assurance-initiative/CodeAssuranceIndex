# Proposal 0003: Communication centralization as an advisory architecture dimension

- **Number:** 0003
- **Filed:** 2026-11-05
- **Proposer:** Jimmy Borch, for the CAI team
- **Declared interests:** the proposer maintains the CCA engine (formerly Watchdog), a commercial implementation that
  produces CAI evidence and already computes the measure this proposal would name. The proposer benefits from a
  richer standard. The dimension proposed here is **advisory**, so it enters no score and confers no scoring
  advantage on any implementation; the proposer gains credibility for the standard, not a number.
- **Moves scores:** **no.** The dimension is advisory (unscored). The reference scorer takes `!advisory`, so it enters
  no lens and no headline; every published score is byte-identical. It adds a named, published concern and one
  plain-language card.
- **Status:** Proposed.

## Problem

The catalog names architectural **entanglement** (AX3, dependency cycles) but not **communication centralization**:
whether a codebase funnels through a single god-module that the whole system reaches through. That is a real,
measurable modularity risk — it is one of the sub-metrics third-party labs publish (SIG's Sigrid reports it), and it
was the hinge of the public ArtCraft/PhotoCraft analysis: a reviewer's spaghetti diagram is, in graph terms, extreme
afferent-coupling concentration.

The CCA engine now measures it (the Gini coefficient of afferent coupling over the language-neutral module graph) and
carries the raw value in its run artifact. But there is no governed way to surface it to the person reading a report,
or into the index, without a catalog entry — and an implementation must not mint catalog content on its own
([decision 0002](../decisions/0002-rubric-versions-are-published-by-the-initiative.md)). Hence this proposal.

## Change

Add one **advisory** architecture meta-dimension to the catalog:

```json
{
  "id": "AX11",
  "name": "Communication centralization",
  "lens": "architecture",
  "evaluator": "tool",
  "whatItMeasures": "How concentrated afferent coupling is on hub modules — whether the whole system reaches through a single central module whose change ripples everywhere.",
  "family": "meta",
  "scoringPolarity": "deduction",
  "advisory": true
}
```

- **Measure.** The Gini coefficient of afferent coupling (each module's incoming-dependency count) over the
  language-neutral module dependency graph. Deterministic; computed from the same graph the cycle check already reads.
- **When it speaks.** It raises an advisory card and a finding **only in the extreme tail**: afferent-coupling Gini
  `>= 0.80`, which is the p90 of a sample of 3,000 real public codebases of 15+ modules (measured 2026-11-05; median
  `~0.66`). Below that it says nothing. The threshold is calibrated from the corpus distribution and re-derived when
  that distribution is re-measured — never per-repository.
- **Plain language (the user-facing wording).** "A large part of your code reaches through one central module. If you
  change it, you risk breaking many things at once. It is worth seeing whether it can be split up."

## Effect on published scores

**None.** `advisory: true` means the scorer excludes it (`!advisory`), so AX11 enters no lens roll-up and no headline;
re-scoring any historical bundle with the new catalog yields identical numbers. The only change a reader sees is a new
advisory card where the measure is in the extreme tail, and the matching finding in plain words.

Why advisory and not scored: centralization is **not "lower is better."** A shared kernel that many modules depend on
is normal and often healthy (the corpus median sits at 0.66). A 0–10 score would penalise that healthy middle, which
would select on a shape that is not itself a defect. Flagging only the extreme tail, and never contributing to a
score, is the honest treatment.

## Alternatives considered

1. **A scored dimension.** Rejected: it would move every published architecture score and would penalise the normal
   shared-core design, selecting on a shape that is not a defect.
2. **Leave the measure in the implementation's artifact only.** Rejected: it never reaches the person reading the
   report or the index. The point is to tell that person whether their architecture funnels through one module.
3. **Fold it into AX5 (architecture & structure).** Rejected: AX5 is scored and asks a different question
   (recognisable, scale-appropriate structure); a finding there would move the score and conflate two concerns.

## Questions for the decider

- **The id** — `AX11` is proposed, the next in the architecture-model meta sequence.
- **The extreme-tail threshold** — `0.80` (the corpus p90) is proposed; the corpus and the method are reproducible,
  and the decider may prefer a different percentile.
- **Advisory vs scored** — the proposal argues advisory, for the polarity reason above; the decider may disagree.

## What the CCA engine will do on its own side (noted, not part of the proposal)

The engine already emits the raw measure (`centralization`, plus a `highlyCentralized` flag) in its
`namespace-graph.json` artifact. On acceptance it will emit the AX11 advisory card and the plain-language finding,
behind the rubric version this decision mints — not before.
