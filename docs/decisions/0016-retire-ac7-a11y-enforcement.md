# Decision 0016: Retire AC7, accessibility enforcement, and leave its advice to the markup dimensions

- **Decision:** 0016
- **Date:** 2026-11-11
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0016: Retire AC7, accessibility enforcement, and leave its advice to the markup dimensions](../proposals/0016-retire-ac7-a11y-enforcement.md)
- **Outcome:** Accepted as written; its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA and the Assure, Assert and Assay products of
Canine Development, and whose engine computed the corpus measurement behind the proposal. The change applies to every
producer alike and is measured over a named corpus another producer can reproduce
([evidence-2026-11-11-corpus.tsv](../proposals/evidence-2026-11-11-corpus.tsv)). Nobody else can decide today, so
nobody recused.

## Comment

None before adoption. This change moves published scores, so under the interim rule the **30-day** comment period runs
after adoption, to **2026-12-11**. An unresolved objection from a commenter not affiliated with the proposer reopens it.

## Reasons

- 92 % of the repositories AC7 applies to sit on its lowest rung, with the median on that rung in all 17 languages: a
  reading of the population, not of the quality the lens names.
- It reads whether a project has a front-end toolchain large enough to carry a linter and a CI step, not whether its
  markup is accessible; AC1 to AC6 read that directly.
- The advice it carried is worth keeping, and is better placed next to the AC1 to AC6 result it would have prevented,
  as the producer's own advice (decision 0013).

## The exact effect

Recomputed by the reference scorer before this decision, as the proposal asked
([0015-0018-recomputation.md](../proposals/0015-0018-recomputation.md), per repository in the `.tsv` beside it). All
6,176 published headlines were reproduced to within 0.05 first. Of 1,318 repositories with an AC7 reading, 1,290 move (1,168 up, 122 down), median +0.77, extremes −9.79 to +10.87; **52 bands change**: 49 up (44 Weak → Adequate, 5 Adequate → Strong), 3 down (2 Adequate → Weak, 1 Weak → Critical).

## Answers to the proposal's questions

1. **Left entirely to producers.** The catalog's guidance does not mention the advice: the standard holds what must be
   comparable, and a producer's advice is its own extension (decisions 0012 and 0013).
2. **Yes**, the exact recomputation was a precondition, and it was made (above).

## What took effect

- Proposal 0016's status, and the proposal and decision indexes.
- AC7's id is retired and never reused.

## What did not yet take effect

- **The rubric version.** Removing AC7 from the catalog, and computing the accessibility lens from AC1 to AC6, takes effect in the next rubric version, published through the rubric gate with this
  decision linked. Until then every published score stands under the rubric version its delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version.
