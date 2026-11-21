# Proposal 0021: D35 reads the recent history, so a broken coupling leaves the reading

- **Number:** 0021
- **Filed:** 2026-11-21
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurements below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. The problem was reported by a customer of one of those products. The change applies to
  every producer alike. The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes (D35).
- **Status:** Accepted by [decision 0021](../decisions/0021-d35-reads-recent-history.md)

## Problem

D35 ("Change Coupling") reads *"Whether files that change together actually belong together: pairs that repeatedly
co-change in git history despite having no explicit code dependency."* The definition does not say how much history
is read, and the reading producers have used reads all of it. A pair's ratio is counted over up to 10,000 commits. The
pair stays reported while its newest shared commit falls within three years of the analysed commit.

That makes the finding impossible to act on. A team using a producer's product split a frontend file from 8,529 to
896 code lines in ten steps. Its coupling with a server route was still reported as *"111 of 159
commits"* afterwards, as it had been before. Counted over the whole history, a pair that co-changed a hundred times
needs about a hundred more independent changes before its ratio falls under one half. A single shared commit a year
keeps it reported indefinitely. Whatever the team does to the code, the number cannot show it, so the finding asks
for work whose effect no measurement will ever confirm. The team asked for exactly this: say how far back D35 looks,
and look back far enough to be fair but not so far that fixing the code cannot count.

The corpus shows the same thing at scale. Each repository below was measured as it stood a year before its newest
commit, and the pairs reported then were followed through the next year, which none of the rules read. Under
today's reading:

- only **22 %** of the pairs reported were still co-changing a year later (at least two shared commits, and at least
  half of the less-changed file's revisions);
- **23 %** had dissolved: at least one of the files was being worked on (three revisions or more) and the two
  changed together in under a quarter of those revisions;
- **44 %** were dormant: neither file changed three times in that year;
- of the dissolved pairs, **35 %** had left the reading a year later, and of the dormant pairs **25 %**.

## Change

In the next rubric version, D35 reads:

> *"Whether files that change together actually belong together: pairs that repeatedly co-change in recent git
> history despite having no explicit code dependency, surfacing the hidden/logical coupling (and boundaries in the
> wrong place) a static scan can't see. A pair is read over the last 20 changes of the less often changed of its two
> files, and only while the two have changed together within the year before the analysed commit, so a coupling that
> is broken leaves the reading."*

The reading, stated so another producer can reproduce it:

- **The anchor.** The date of the analysed commit, never the wall clock: a pinned measurement of one commit gives
  the same reading on any day. This is the anchor D15 already uses.
- **The window of a pair.** Take the commits a producer counts as revisions of each file. Commits it sets aside on
  both sides of the ratio, such as repository-wide or module-wide sweeps, are not revisions. The window opens at
  the 20th most recent revision of whichever of the two files reaches back further; if that file has fewer than 20
  revisions, the window is its whole history. Revisions and shared commits are counted from that commit on, for
  both files.
- **Recency.** The pair counts only if its newest shared commit lies within 365 days of the anchor.
- **Unchanged.** The floors (at least 10 revisions of each file and 5 shared commits inside the window, a degree of
  at least one half), the commits a producer excludes, and the curve: `score = clamp(10 - 20 x coupled pairs /
  production files, 0, 10)`.
- **The finding says its window.** A reported pair states the count and the window it was taken over, e.g. *"11 of
  the last 20 commits that touched `Bot.cs`, since 2025-09-02"*, so the reader's `git log` can reproduce it and can
  see what one more independent change will do.

## Why 20 revisions and a year

A window counted in revisions adapts to each file's own pace. A busy file is read over its last few weeks, and a
file changed four times a year is read over five years. A pair whose coupling is broken drops under one half after
about ten independent changes of its less-changed file, whatever the repository's pace. A window in days cannot do
that. Read over 90 or 365 days with D35's floors, most real pairs never reach ten revisions inside the window, and
the measurement drops pairs that still co-change (table below). The year of recency is the outer bound: a pair with
no shared commit in a year is not being maintained together, and no team should be told to restructure code that
nobody touches. Recency without the revision window (G365 below) has the same effect on the corpus, but it leaves the
customer's problem in place: one shared commit a year still keeps "111 of 159" on the page unchanged.

## Evidence (decision 0013)

The corpus is every repository with complete (not shallow) git history in a local copy of the published corpus that
the measuring producer pairs: **153 repositories** in eight languages (C#, TypeScript and JavaScript, Python, Java,
Kotlin, Ruby, PHP, Elixir), named with the commit read in [0021-evidence.tsv](0021-evidence.tsv). The rules were
compared on the counting core every reading shares: the production-file filter, the sweep exclusions, the floors and
the degree. The producer's later filters (pairs with an explicit dependency, the neighbourhood base rate, the
folding of cliques into one row) were left out, so the counts are candidate pairs, and today's 4,002 are more than
the producer reports. The comparison needs only that every rule is read the same way. 88 repositories have a year of
history on both sides of the split point and carry the predictive columns.

**The rules compared.** The same rule for every repository, anchored on the analysed commit:

| Rule | Pairs now | Kept of today's | Repos with a pair | Median score change (of those moved) | At T: still co-changing | dissolved | dormant | Today's still-co-changing pairs lost at T |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Today (all history, 3-year recency) | 4,002 | 4,002 | 83 | | 22 % | 23 % | 44 % | 0 of 699 |
| 90 days, floors 10/5 | 213 | 166 | 26 | +0.54 | 33 % | 23 % | 10 % | 680 |
| 90 days, floors 5/3 | 1,523 | 344 | 38 | +0.32 | 28 % | 34 % | 17 % | 614 |
| 365 days, floors 10/5 | 849 | 714 | 40 | +0.40 | 30 % | 31 % | 24 % | 536 |
| 365 days, floors 5/3 | 4,323 | 1,062 | 58 | +0.05 | 18 % | 31 % | 39 % | 421 |
| Since either file's last rewrite (a commit deleting half of a file of 100 lines or more), 3-year recency | 3,299 | 3,153 | 76 | +0.17 | 25 % | 24 % | 39 % | 62 |
| 365 days and since the last rewrite | 717 | 590 | 40 | +0.41 | 35 % | 27 % | 18 % | 552 |
| Recency 365 days alone (G365) | 2,449 | 2,449 | 67 | +0.34 | 32 % | 22 % | 32 % | 106 |
| Last 20 revisions, 3-year recency | 4,248 | 3,923 | 82 | -0.11 | 21 % | 24 % | 43 % | 11 |
| **Last 20 revisions, 365-day recency (proposed)** | **2,621** | **2,392** | **67** | **+0.28** | **31 %** | **23 %** | **31 %** | **116** |
| Last 20 revisions, 180-day recency | 1,870 | 1,697 | 63 | +0.32 | 37 % | 23 % | 23 % | 232 |

**How soon a pair leaves.** Every rule was re-run month by month through the year after T, over today's 3,233 pairs
at T, classed by what that year showed. Each figure is the share that had left the reading by then:

| Rule | Still co-changing (699): left within 12 months | Dissolved (747): within 6 / 12 months | Dormant (1,436): within 6 / 12 months |
|---|---:|---:|---:|
| Today | 4 % | 18 % / 35 % | 14 % / 25 % |
| Recency 365 days alone | 30 % | 69 % / 97 % | 79 % / 97 % |
| **Last 20 revisions, 365-day recency** | **32 %** | **70 % / 98 %** | **79 % / 97 %** |
| Last 20 revisions, 180-day recency | 72 % | 99 % / 100 % | 99 % / 100 % |
| 365 days, floors 10/5 | 91 % | 96 % / 100 % | 98 % / 100 % |

A still-co-changing pair that leaves is not lost for good. Most leave because they had no shared commit in the year
before T, and they come back with their next shared commit.

**Specimen: a large split.** In JustArchiNET/ArchiSteamFarm `Bot.cs` was split from 2,275 lines. Today's reading
reports `Bot.cs` in nine pairs, among them *"101 of 177"* with `CardsFarmer.cs` and *"79 of 144"* with
`ArchiHandler.cs`, both of them built over a history mostly from before the split. The proposed reading keeps the pairs
that still co-change, now stated over the last 20 revisions (`CardsFarmer.cs` 10 of 20, `BotDatabase.cs` 15 of 20,
`BotController.cs` 11 of 20). It drops those that no longer do (`ArchiHandler.cs`, `MobileAuthenticator.cs`,
`GlobalDatabase.cs`, `SteamPICSChanges.cs`). The repository goes from 16 candidate pairs to 7, and D35 from 9.06 to
9.59 on the counting core.

**Concentration.** As with every row-share figure, a few repositories carry most pairs (the top ten carry 69 %
today and 67 % under the proposal). Per repository, the median share of today's pairs kept is 60 % under the proposal
and 0 % under a 365-day window. Sixteen repositories lose their last pair under the proposal and 43 under the
365-day window.

**Never selected on the outcome.** No repository, language or pair was chosen by its finding count. The rule and its
two numbers are the same for every repository, and the predictive classes come from a year no rule reads.

## Effect on published scores

D35 cannot be recomputed from stored data. A published D35 reading stores its pairs and counts but not the history
they were counted over, so its effect arrives with each repository's next measurement, as R10's did under decision 0020.
On the corpus above the reading moves 61 of 153 repositories, by a median of +0.28 among those that move. Most move
up (55), because pairs that no longer co-change leave. Six move down, because 229 pairs that co-change strongly in
their recent revisions but not over their whole history arrive. The measuring producer re-measures its own corpus
under the new version before publishing under it.

## Alternatives considered

- **A window in days, as D15 uses (90 or 365 days).** With D35's floors few real pairs reach ten revisions inside the
  window. The 365-day window loses 536 of today's 699 still-co-changing pairs at once, and 91 % within a year.
  Lowering the floors to keep them (5 revisions, 3 shared) makes the reading noisier than today's (18 % still
  co-changing at 365 days).
- **Counting only after a file's last large rewrite or split.** It recognises the event the customer reported, but a
  split is rare. The rule changes little elsewhere (3,153 of 4,002 kept, still co-changing 25 %), and a pair dissolved
  by a change smaller than a rewrite still never leaves. It also needs a threshold for "large" that would have to be
  justified on its own.
- **Recency alone (365 days).** It has almost the same effect on the corpus, but the count stays the whole history, so
  fixing the code still cannot move it. That was the complaint.
- **30 revisions instead of 20.** Nearly identical on the corpus (2,519 pairs, 32 % still co-changing). 20 lets a
  broken pair leave after about ten independent changes rather than fifteen.
- **180-day recency.** It clears dissolved pairs within six months but loses 72 % of still-co-changing pairs within a
  year.

## Questions for the decider

1. Are 20 revisions and 365 days the right two numbers?
2. Is the measurement on the counting core, with the producer's later filters left out and stated, enough as the
   precondition, given that D35 cannot be recomputed from stored readings?
