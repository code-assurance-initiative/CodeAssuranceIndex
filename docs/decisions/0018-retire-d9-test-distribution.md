# Decision 0018: Retire D9, test distribution

- **Decision:** 0018
- **Date:** 2026-11-11
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0018: Retire D9, test distribution](../proposals/0018-retire-d9-test-distribution.md)
- **Outcome:** Accepted as written; its two questions answered below.
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

- There is no agreed healthy mix of test kinds; a ratio target scores a style teams reasonably choose differently.
- What remains defensible, "the suite has a test of some part on its own", reads 10 for about 98 % of the
  repositories with tests, which would fail decision 0013's saturation argument as a new dimension.
- The fact it can still tell a team (its suite has no unit or integration test) is worth telling, as the producer's
  own advice.

## The exact effect

Recomputed by the reference scorer before this decision, as the proposal asked
([0015-0018-recomputation.md](../proposals/0015-0018-recomputation.md), per repository in the `.tsv` beside it). All
6,176 published headlines were reproduced to within 0.05 first. Of 3,412 repositories with tests and a D9 reading, 1,526 move (159 up, 1,367 down), median −0.01, extremes −5.87 to +6.07; **54 bands change**: 5 up, 49 down (43 Adequate → Weak, 5 Strong → Adequate, 1 Strong → Weak). The 810 repositories whose deliveries still carry the old "No tests found" reading at 0 are reported apart (98 move up, 53 bands up): that reading leaves with their next measurement whether or not D9 is retired.

## Answers to the proposal's questions

1. **Retired outright.** D9's id is never reused. A future dimension reading test layers from executed tests is a new
   measure and gets its own id and its own evidence; reserving D9 for it would suggest a continuity with this reading
   that does not exist.
2. **Yes**, the exact recomputation was a precondition, and it was made (above).

## What took effect

- Proposal 0018's status, and the proposal and decision indexes.
- D9's id is retired and never reused.

## What did not yet take effect

- **The rubric version.** Removing D9 from the catalog takes effect in the next rubric version, published through the rubric gate with this
  decision linked. Until then every published score stands under the rubric version its delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version.
