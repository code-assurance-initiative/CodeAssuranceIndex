# Proposal 0014: the exact recomputation

The effect on published scores that [proposal 0014](0014-retire-pf2-allocation-hygiene.md) estimated, recomputed exactly
by the reference scorer, as the proposal asked before any decision. The proposal itself is not edited; this file is its
evidence. Taken 2026-11-06.

## Method

- **What was folded:** the latest published delivery of every repository the registry publishes: **6,144**
  repositories, each under the rubric version its delivery names.
- **How:** with `CaiScorer.Score` (`Cai.Scoring`, the same library the `cai` CLI, the API and the calculator call), once
  as published and once with PF2's reading removed from the evidence. A dimension absent from the evidence leaves its
  lens, exactly as a dimension the rubric no longer lists.
- **Check:** all 6,144 published headlines were reproduced to within 0.05 before PF2 was removed, so the difference
  below is PF2 alone.
- **Data:** every repository with a PF2 reading, its headline with and without PF2 and its band before and after, is in
  [`0014-recomputation.tsv`](0014-recomputation.tsv).

## Result

| | |
|---|---|
| Repositories with a PF2 reading | 1,084 of 6,144 |
| Headline moves (≥ 0.05) | 970: 815 up, 155 down |
| Change, median | **+0.33** |
| Change, 5th–95th percentile | −0.35 to +1.45 |
| Change, extremes | −4.25 to +4.71 |
| Band changes | **24**: 16 up (12 Weak → Adequate, 3 Adequate → Strong, 1 Strong → Exemplary), 8 down (7 Adequate → Weak, 1 Strong → Adequate) |

The proposal's estimate (median +0.33) holds. A repository moves **down** where PF2 read above its lens's other
dimensions; it moves **up** where PF2's floor reading (6.0) held its lens below them, which is the "never penalised"
contradiction the proposal names.

## Comment

This moves published scores, so the comment period is **30 days**, to **2026-12-06** (GOVERNANCE.md, step 3). Comment on
the pull request that adds this file.
