# Decision 0021: D35 reads the recent history, so a broken coupling leaves the reading

- **Decision:** 0021
- **Date:** 2026-11-21
- **Decided by:** Jimmy Borch (owner), for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0021: D35 reads the recent history, so a broken coupling leaves the reading](../proposals/0021-d35-reads-recent-history.md)
- **Outcome:** Accepted as written; its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person. He maintains CCA and the Assure, Assert and Assay products of
Canine Development, his engine computed the corpus measurement behind the proposal, and the problem was reported by
a customer of one of those products. The change applies to every producer alike, and the corpus it was measured over
is named with its commits ([0021-evidence.tsv](../proposals/0021-evidence.tsv)) so another producer can reproduce it.
Nobody else can decide today, so nobody recused.

## Comment

None. The comment period is paused until the steering group exists (decision 0019).

## Reasons

- A reading counted over the whole history cannot respond to a change in the code. A pair that co-changed a
  hundred times needs about a hundred independent changes before it leaves, and one shared commit a year keeps it
  reported indefinitely. A finding that no fix can move is not actionable.
- Under today's reading, 44 % of the pairs reported in the corpus a year before the newest commit were dormant over
  the following year and 23 % had dissolved. Of those, only 25 % and 35 % had left the reading a year later. Under
  the accepted reading, 97 % and 98 % leave within that year, while 68 % of the pairs that were still co-changing
  never leave in it.
- A window counted in revisions of the less often changed file adapts to each file's pace. A window in days loses
  most pairs that still co-change, because few files reach D35's floors inside it.
- The window and the recency bound are anchored on the analysed commit, so a pinned measurement gives the same
  reading on any day, as D15's does.

## Answers to the proposal's questions

1. **Yes**, 20 revisions and 365 days. Twenty lets a broken pair leave after about ten independent changes of its
   less often changed file, and thirty was not measurably better. A year is the outer bound: a pair with no shared
   commit in a year is not being maintained together. 180 days loses most of the pairs that still co-change within a
   year.
2. **Yes**, the measurement on the counting core meets the precondition, with the later filters left out and stated.
   A published D35 reading stores no history and cannot be recomputed, so the exact effect arrives with each
   repository's next measurement, as R10's did under decision 0020. Holding the decision for an exact recomputation
   would hold it for a measurement that cannot be made.

## What took effect

- Proposal 0021's status, and the proposal and decision indexes.

## What did not yet take effect

- **The rubric version.** D35's new definition, the text quoted in the proposal's *Change* section, takes effect in
  the next rubric version, published through the rubric gate with this decision linked. Until then every published
  score stands under the rubric version its delivery names.
- **Producers' implementations.** Each producer follows when it measures under that version. For it to take effect,
  a producer:
  1. reads each pair over the last 20 revisions of whichever of its two files reaches back further, counting
     revisions and shared commits from that commit on (the whole history when that file has fewer than 20);
  2. counts the pair only while its newest shared commit lies within 365 days of the analysed commit's date, which
     replaces any longer recency bound;
  3. anchors both bounds on the analysed commit's date, never on the wall clock;
  4. keeps the floors, the exclusions and the curve unchanged;
  5. states in each reported pair the count and the window it was taken over;
  6. records the change as a new measurement version of its engine, and stamps the deliveries it measures under the
     new rubric version.
