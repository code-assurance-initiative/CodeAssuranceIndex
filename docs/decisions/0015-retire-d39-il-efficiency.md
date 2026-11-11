# Decision 0015: Retire D39, IL efficiency

- **Decision:** 0015
- **Date:** 2026-11-11
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0015: Retire D39, IL efficiency](../proposals/0015-retire-d39-il-efficiency.md)
- **Outcome:** Accepted as written; its question answered below.
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

- D39 has no definition beyond its name, so no other producer could implement it to the same meaning.
- It reads a compiled .NET artefact, so it can apply to one platform only, and what it reads (large methods) is read
  in source, in every language, by D1.
- 94 % of the repositories it applies to read 9.5 or more; it separates almost nobody, and its one remedy (split
  methods to fit a size) does not make a product better by itself.

## The exact effect

Recomputed by the reference scorer before this decision, as the proposal asked
([0015-0018-recomputation.md](../proposals/0015-0018-recomputation.md), per repository in the `.tsv` beside it). All
6,176 published headlines were reproduced to within 0.05 first. Of 375 repositories with a D39 reading, 121 move (5 up, 116 down), median −0.01, extremes −6.39 to +0.09; **2 bands change**, both down (1 Strong → Adequate, 1 Adequate → Weak).

## Answer to the proposal's question

1. **Yes**, the exact recomputation was a precondition, and it was made (above).

## What took effect

- Proposal 0015's status, and the proposal and decision indexes.
- D39's id is retired and never reused.

## What did not yet take effect

- **The rubric version.** Removing D39 from the catalog takes effect in the next rubric version, published through the rubric gate with this
  decision linked. Until then every published score stands under the rubric version its delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version.
