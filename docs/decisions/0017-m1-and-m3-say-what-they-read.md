# Decision 0017: M1 and M3 say what they read; project naming leaves M3

- **Decision:** 0017
- **Date:** 2026-11-11
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0017: M1 and M3 say what they read; project naming leaves M3](../proposals/0017-m1-and-m3-say-what-they-read.md)
- **Outcome:** Accepted as written, both parts; its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA and the Assure, Assert and Assay products of
Canine Development, and whose engine computed the corpus measurement behind the proposal. The change applies to every
producer alike and is measured over a named corpus another producer can reproduce
([evidence-2026-11-11-corpus.tsv](../proposals/evidence-2026-11-11-corpus.tsv)). Nobody else can decide today, so
nobody recused.

## Comment

None. The comment period is paused until the steering group exists
([decision 0019](0019-the-comment-period-is-paused-until-the-steering-group-exists.md)): there is nobody to comment
yet. The decision is recorded, its reasons are written and it is reversible; the steering group may revisit it when it
exists.

## Reasons

- Whether documentation is current is M4's reading; inside M1 it could only be a proxy that asks a team to touch a
  correct README, and counts the same drift twice.
- Consistent project naming is a convention, not organisation. It deducts from deliberately organised repositories
  whose projects sit under more than one root, and its only remedy, a mass rename, makes no product better.

## The exact effect

Recomputed by the reference scorer before this decision, as the proposal asked
([0015-0018-recomputation.md](../proposals/0015-0018-recomputation.md), per repository in the `.tsv` beside it). All
6,176 published headlines were reproduced to within 0.05 first. M1 moves no score. For M3, the 155 repositories whose naming deduction is restored: 149 move, all up, median +0.57, extremes 0.00 to +2.20; **2 bands change**, both up (Weak → Adequate).

## Answers to the proposal's questions

1. **Yes**, the new M3 wording stands as proposed: *"Whether the repository is organised deliberately: production code
   and tests are set apart in a way its ecosystem or the repository itself declares."* A separation the repository
   states in its own build or test configuration is credited.
2. **Yes**, the exact recomputation was a precondition, and it was made (above).

## What took effect

- Proposal 0017's status, and the proposal and decision indexes.

## What did not yet take effect

- **The rubric version.** The new M1 and M3 definitions takes effect in the next rubric version, published through the rubric gate with this
  decision linked. Until then every published score stands under the rubric version its delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version.
