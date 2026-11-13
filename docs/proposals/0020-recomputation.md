# Proposal 0020: the recomputation

The effect on published headlines that [proposal 0020](0020-d4-and-r10-score-the-share-of-lines-in-a-copy.md)
describes, recomputed by the reference scorer before any decision. The proposal itself is not edited; this file is its
evidence. Taken 2026-11-13.

## Method

- **What was folded:** the latest published delivery of every repository the registry publishes: **6,176**
  repositories, each under the rubric version its delivery names.
- **How:** with `CaiScorer.Score` (`Cai.Scoring`, the same library the `cai` CLI, the API and the calculator call). For
  each repository whose latest measurement stored D4 rows, the delivery was scored twice with only D4 replaced: once
  by D4 under today's reading of that measurement, once by D4 under the proposed reading of the same measurement, so
  each difference is the change of reading alone and not a newer scan.
- **The proposed D4** was computed from the measurement's stored rows: every copy of every duplicated block and edited
  copy, each copy at most as long as its row's shortest copy, each line once, signature files left out, over the
  measurement's production lines. Stored rows carry line spans but not file text, so the blank lines inside a span were
  removed by the ratio measured on full rescans of 30 repositories (0.923, range 0.77 to 1.00); there the estimate is
  within 0.07 of the exact score at the median and 0.39 at worst.
- **Check:** all 6,176 published headlines were reproduced to within 0.05 before any change.
- **Not recomputed:** 1,876 deliveries whose repository has no stored D4 row (no copy under either reading, so no
  change), and **R10**, whose stored rows hold first lines only (see the proposal).
- **Data:** every recomputed repository, its headline as published, under today's reading and under the proposal, and
  both bands, is in [`0020-recomputation.tsv`](0020-recomputation.tsv); the per-repository share and both D4 scores
  are in [`0020-evidence.tsv`](0020-evidence.tsv).

Headlines are on the 0-100 scale. "Moves" means a change of 0.05 or more.

## Result

| | D4 |
|---|---:|
| Repositories recomputed | 4,300 |
| Headline moves | **1,815, all down** |
| Change among those that move, median | **-0.13** |
| Change, 5th-95th percentile (all 4,300) | -0.45 to 0.00 |
| Moves by size | 1,626 under 0.5; 177 from 0.5 to 2; 10 from 2 to 5; 2 of 5 or more |
| Change, extremes | -8.66 to +0.02 |
| Band changes | **19, all down**: 6 Strong to Adequate, 11 Adequate to Weak, 2 Weak to Critical |

Every move is down because the proposed reading counts what today's reading counted plus the size of each copy and
the edited copies; no repository is credited with less copying than before. The two largest moves are
CSBiology/ProteomIQon (-8.66; three quantification modules copied from one another) and Thorium/Owin.Compression
(-8.45; one module copied between two framework builds), both read at source.

## Comment

The decision on this proposal is taken under the interim rule
([decision 0011](../decisions/0011-interim-pragmatic-governance.md)). No comment period runs: it is paused until the
steering group exists ([decision 0019](../decisions/0019-the-comment-period-is-paused-until-the-steering-group-exists.md)).
