# Decision 0020: D4 and R10 score the share of lines in a copy

- **Decision:** 0020
- **Date:** 2026-11-13
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0020: D4 and R10 score the share of lines in a copy](../proposals/0020-d4-and-r10-score-the-share-of-lines-in-a-copy.md)
- **Outcome:** Accepted as written; its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA and the Assure, Assert and Assay products of
Canine Development, and whose engine computed the corpus measurement behind the proposal. The change applies to every
producer alike, and the corpus it was measured over is named with its commits
([0020-evidence.tsv](../proposals/0020-evidence.tsv)) so another producer can reproduce it. Nobody else can decide
today, so nobody recused.

## Comment

None. The comment period is paused until the steering group exists
([decision 0019](0019-the-comment-period-is-paused-until-the-steering-group-exists.md)): there is nobody to comment
yet. The decision is recorded, its reasons are written and it is reversible; the steering group may revisit it when it
exists.

## Reasons

- A count of groups weighs a six-line pair like a copied class, leaves out a copy that was edited after it was pasted,
  and moves when the same duplication is grouped differently. The share of lines in a copy weighs a copy by its size
  and counts a line once however many rows cite it.
- The count does not separate repositories: 84 % of the corpus scores 9 or more on it. Under the share 43 % does, and
  the repositories it floors are those where a quarter of the code is copied.
- The share is the figure other duplication measurements publish, so a reader can compare a CAI reading with them.
- D4 and R10 read the same construct over different languages; one reading and one curve for both means the same
  amount of copied code scores the same whatever it is written in.

## The exact effect

Recomputed by the reference scorer before this decision
([0020-recomputation.md](../proposals/0020-recomputation.md), per repository in the `.tsv` beside it). All 6,176
published headlines were reproduced to within 0.05 first. Of the 4,300 deliveries with stored D4 rows, **1,815
headlines move, all down**, by a median of 0.13 among those that move (1,626 by less than 0.5, 2 by 5 or more);
**19 bands change**, all down (6 Strong to Adequate, 11 Adequate to Weak, 2 Weak to Critical). The 1,876 deliveries
without a stored D4 row do not move. R10 cannot be recomputed from stored data and moves with each repository's next
measurement.

## Answers to the proposal's questions

1. **Yes**, the zero point is 25 %: `score = clamp(10 - 0.4 x share%, 0, 10)`. It is a fact about the code (one
   production line in four inside a copy) rather than a percentile of today's corpus, it removes the saturation of the
   count, and it caps a lens band only from a 15 % share, which is 7 % of the corpus. At 50 % the reading would
   saturate again (69 % at 9 or more).
2. **Yes**, the recomputation of D4 from stored rows meets the precondition, with the blank-line correction and its
   measured error stated as the proposal states them. For R10 the exact sample of 37 repositories stands in for it:
   a published R10 reading cannot be recomputed until the repository is measured again, and holding the decision for
   that would hold D4 back as well while giving nothing the next measurement will not.

## What took effect

- Proposal 0020's status, and the proposal and decision indexes.

## What did not yet take effect

- **The rubric version.** The new D4 and R10 definitions take effect in the next rubric version, published through
  the rubric gate with this decision linked. Until then every published score stands under the rubric version its
  delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version.
