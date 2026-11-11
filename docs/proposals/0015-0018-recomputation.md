# Proposals 0015 to 0018: the exact recomputation

The effect on published scores that proposals [0015](0015-retire-d39-il-efficiency.md),
[0016](0016-retire-ac7-a11y-enforcement.md), [0017](0017-m1-and-m3-say-what-they-read.md) and
[0018](0018-retire-d9-test-distribution.md) estimated, recomputed exactly by the reference scorer, as each proposal asked
before any decision. The proposals themselves are not edited; this file is their evidence. Taken 2026-11-11.

## Method

- **What was folded:** the latest published delivery of every repository the registry publishes: **6,176**
  repositories, each under the rubric version its delivery names.
- **How:** with `CaiScorer.Score` (`Cai.Scoring`, the same library the `cai` CLI, the API and the calculator call), once
  as published and once per proposal with the change applied to the evidence, then once with all four together. A
  dimension absent from the evidence leaves its lens, exactly as a dimension the rubric no longer lists.
  - 0015: D39's reading removed. 0016: AC7's reading removed. 0018: D9's reading removed.
  - 0017: a delivery carries scores, not findings, so the 2-point naming deduction is restored on M3 (capped at 10) for
    the 155 repositories the corpus measurement of the proposal names
    ([evidence-2026-11-11-corpus.tsv](evidence-2026-11-11-corpus.tsv), column `M3_inconsistent_root_namespaces`). The
    M1 part of 0017 moves no score.
- **Check:** all 6,176 published headlines were reproduced to within 0.05 before any change, so each difference below
  is that proposal alone.
- **Data:** every repository with a reading that any of the four proposals touches (4,683), its headline as published,
  under each proposal alone and under all four, and its band before and after all four, is in
  [`0015-0018-recomputation.tsv`](0015-0018-recomputation.tsv).

Headlines are on the 0-100 scale. "Moves" means a change of 0.05 or more.

## Result

| | 0015 D39 | 0016 AC7 | 0017 M3 | 0018 D9 |
|---|---:|---:|---:|---:|
| Repositories with the reading | 375 | 1,318 | 155 | 3,412 with tests (+ 810, below) |
| Headline moves | 121: 5 up, 116 down | 1,290: 1,168 up, 122 down | 149: all up | 1,526: 159 up, 1,367 down |
| Change, median | **−0.01** | **+0.77** | **+0.57** | **−0.01** |
| Change, 5th-95th percentile | −0.52 to 0.00 | −0.46 to +2.51 | +0.07 to +1.46 | −1.39 to +0.04 |
| Change, extremes | −6.39 to +0.09 | −9.79 to +10.87 | 0.00 to +2.20 | −5.87 to +6.07 |
| Band changes | **2** down (1 Strong → Adequate, 1 Adequate → Weak) | **52**: 49 up (44 Weak → Adequate, 5 Adequate → Strong), 3 down (2 Adequate → Weak, 1 Weak → Critical) | **2** up (Weak → Adequate) | **54**: 5 up, 49 down (43 Adequate → Weak, 5 Strong → Adequate, 1 Strong → Weak) |

All four together touch 4,683 repositories: median 0.00, 5th-95th percentile −1.14 to +1.90, extremes −9.79 to
+18.09, and 160 band changes.

The proposals' estimates hold in direction. They were made on lens means (0-10), not on the headline, so their sizes
are not comparable to the table.

- **0015:** a D39 reading near 10 is removed from a code-health lens it sat above, so most moves are small and down.
- **0016:** a repository moves **up** where AC7's lowest rung (4) held its accessibility lens below AC1 to AC6, and
  **down** where AC7 read above them.
- **0017:** only up, as proposed.
- **0018:** a repository with tests moves **down** where D9's 10 sat above its production-readiness lens, and **up**
  where D9 read 2 (no unit or integration test).

**D9 and the 810 repositories without tests.** Their published deliveries still carry D9 at 0 ("No tests found"), a
reading the measuring producer has since stopped making (a repository without tests has no distribution; D8 reads the
absence). Removing it moves 98 of them up (median 0.00, extremes up to +14.66) and changes 53 bands, all up. That
change arrives with their next measurement whether or not 0018 is accepted, so it is reported here apart and not
counted as 0018's effect.

## Comment

These proposals move published scores, so the comment period is **30 days**, to **2026-12-11** (GOVERNANCE.md, step
3). Comment on the pull request that adds this file. A decision on each proposal follows the comment period.
