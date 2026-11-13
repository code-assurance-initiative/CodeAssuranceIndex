# Proposal 0020: D4 and R10 score the share of lines in a copy

- **Number:** 0020
- **Filed:** 2026-11-13
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurements below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. The change applies to every producer alike. The proposer is also the acting maintainer,
  i.e. the decider ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes (D4 and R10).
- **Status:** Accepted by [decision 0020](../decisions/0020-d4-and-r10-score-the-share-of-lines-in-a-copy.md)

## Problem

D4 ("Code Duplication") is defined as *"Copy-pasted code that should be shared instead."* The definition names what is
read and not what a score stands for, and the reading producers have used counts **groups of copies per thousand
production lines**: a group is one block and its copies, whatever their size. R10, the same reading over the frontend,
uses the same count on the same curve.

Counting groups gets three things wrong.

1. **Size is ignored.** A six-line pair and a three-hundred-line copied class each count as one group. In
   dromara/Sa-Token, 211 groups gave D4 9.08 while 18 % of its production lines sit in a copy.
2. **The number depends on how a copy is grouped.** A copy that was edited after it was pasted is reported as an
   edited copy of a member, which no block group counts, so it was missing from the score altogether; and the same
   duplication reported in more, or fewer, rows moves the score without the code changing.
3. **It does not separate repositories.** Over the corpus below, 84 % of repositories score 9 or more and 0.2 % below
   4: the reading hardly tells a repository with a few short pairs from one where a fifth of the code is copied.

The share of lines in a copy has none of these faults. It weighs a copy by its size, it counts a line once however many
rows cite it, and it is the figure other duplication measurements publish (a percentage of duplicated lines), so a
reader can compare it with them.

## Change

In the next rubric version:

- **D4** reads *"How much of the production code is copy-pasted: the share of production lines inside a reported copy
  (every copy of a duplicated block or of an edited member, each line once), 10 at none and 0 at a quarter of the
  lines."*
- **R10** reads the same over the frontend: *"The share of the scored frontend lines inside a near-exact copy-pasted
  block of substantial extent (the D4 clone algorithm over JS/TS tokens, D-386; each line once, 10 at none and 0 at a
  quarter of the lines, as D4): ..."*, the rest of its definition unchanged.

The reading, stated so another producer can reproduce it:

- **The lines.** Every location of every reported copy counts: each copy of a duplicated block, and each member of a
  reported edited copy (a member copied and then edited, which no block links). The original counts as much as the
  paste, since nothing records which is which. A row that only aggregates blocks already counted (a pair or family of
  members that share them) adds no line.
- **Each copy at most as long as the shortest.** The copies of one block are the same tokens, so no copy is longer than
  the shortest one but by formatting. A copy counts at most as many lines as the shortest copy of its row. Without
  this, a row whose matching window chains through a table that repeats itself (one copy 3,915 lines, its counterpart
  37) would decide a repository's score on its own.
- **Once each.** A line two rows cover counts once, so grouping the same copy differently cannot move the score.
- **The denominator.** The production lines the dimension measures. A copied line in a file the producer counts as
  test or generated code is outside the share, as it is outside the denominator. A signature file (F# `.fsi`, OCaml
  `.mli`, Python `.pyi`, Ruby `.rbi`) restates the declarations of its implementation by the language's rule and is
  not compared with it.
- **The curve.** `score = clamp(10 - 0.4 x share%, 0, 10)`: 10 with no copied line, 5 at 12.5 %, 0 at 25 % or more,
  linear between. No floor and no smoothing: a share means the same over 2,000 lines as over 2,000,000.

## Why a quarter

The zero point should be a fact about the code, not a percentile of today's corpus, so that it does not move when the
corpus does. At 25 % one production line in four sits in a copy; with two copies per group that is one line in eight
existing only to be kept in step with another. The critical gate (a contributor below 4 caps its lens band at
Adequate) is then reached at 15 %. Measured over the corpus below:

| Zero point | Median D4 | At 0 | Below 4 (band capped) | 9 or more |
|---|---:|---:|---:|---:|
| Today (groups per KLoC) | 9.60 | 0.0 % | 0.2 % | 84 % |
| **25 % (proposed)** | **8.83** | **2.9 %** | **7.0 %** | **43 %** |
| 33 % | 9.12 | 1.6 % | 4.2 % | 55 % |
| 50 % | 9.41 | 0.7 % | 2.2 % | 69 % |

At 50 % the reading would saturate again (69 % at 9 or more); at 25 % it uses the scale while flooring only the
repositories where a quarter of the code is copied.

## Evidence (decision 0013)

The corpus is every repository of the published corpus whose latest measurement stored D4 rows: **4,539
repositories in 17 languages**, named with their commits in [0020-evidence.tsv](0020-evidence.tsv) (scans from
2026-06-18 to 2026-11-13). Their rows were re-read under the proposed reading. A stored row carries its line spans but
not the file text, so blank lines inside a span are not known; the share is corrected by the ratio of non-blank to
cited lines measured on full rescans (0.923, range 0.77 to 1.00 over 30 repositories), and on those rescans the
corrected estimate is within 0.07 of the exact score at the median and 0.39 at worst.

1. **Activation.** D4 applies to every repository with production source; 4,539 carry rows, in all 17 languages.
2. **Repositories hit.** The share has a median of 2.9 %, a 90th percentile of 12.0 % and a 99th of 43.6 %. Per
   language the median runs from 0.7 % (TypeScript, whose duplication R10 reads) and 1.2 % (Ruby) to 5.6 % (Dart) and
   6.0 % (F#).
3. **Saturation.** As in the table above: 84 % of repositories at 9 or more today, 43 % under the proposal.
4. **Precision, weighted by lines.** A score of lines needs precision of lines, not of rows. 40 rows drawn with
   probability proportional to the lines they count, read at source by the proposer (not blind): 33 real copies, 1
   generated code a producer has since recognised, 6 not copy-paste (4 generated bindings without a generated
   marker, in one repository; 1 vendored upstream code; 1 converter test input). That is 15 % of counted lines not a
   copy. The open cases are filed with the producer that measured them; none is a fault of the reading, and each moves
   the share only where it already moved the count.
5. **Disputes.** 217 disputes of D4 and R10 rows have been filed with the measuring producer: 183 declined, 24 upheld
   (the row was wrong and the producer fixed it, 12 % of those decided) and 10 open. Each disputes a row; none disputes
   the reading.
6. **The construct.** Re-measured exactly on 60 local corpus repositories with the measuring producer's engine, before
   and after: every row is the same in both runs but the signature-file rows removed (one repository), and only the
   score moves. The largest shares read at source are committed copies: the same scripts in three agent folders, a
   book's code once per translation, an application written three ways, year-by-year and backup folders, billing code
   copied between two build flavours.

**R10.** Measured exactly on 37 of those repositories with a frontend: median R10 8.76 today, 5.82 under the
proposal, 8 at 0. The largest shares are whole UI components, views and value objects copied between modules of small
template and sample repositories, plus one compiled `.js` committed beside its `.ts` (filed).

## Effect on published scores

**D4**, recomputed by the reference scorer over every published delivery: see
[0020-recomputation.md](0020-recomputation.md). In short, of 6,176 published headlines (all reproduced first), 4,300
carry a D4 reading with stored rows; 1,815 move, all down, by a median of 0.13 among those that move; 19 bands change.

**R10** cannot be recomputed from stored data: the measuring producer stores R10 rows by their first line only, so the
copied lines of a published R10 reading are not known until the repository is measured again. Its effect arrives with
each repository's next measurement, as every measurement change does; the exact sample above shows its size.

## Alternatives considered

- **Keep counting groups, and count an edited copy as a group when none of its lines is in a counted group.** It closes
  the gap of point 2 for edited copies but keeps points 1 and 3: a six-line pair still weighs as much as a copied class.
  Measured on 25 repositories: +40 % groups, mean D4 9.30 to 8.91.
- **Count groups weighted by their lines.** That is the share without "once each": overlapping rows would count a line
  twice, and the number would again depend on grouping.
- **Count only the copies beyond the first of each group.** It needs a choice of which copy is the original, which the
  code does not record.
- **A zero point at 33 % or 50 %.** See the table above.

## Questions for the decider

1. Is the zero point right at 25 %?
2. Is a recomputation of D4 from stored rows, with the blank-line correction stated, enough as the precondition the
   earlier proposals asked for, given that R10 cannot be recomputed until it is measured again?
