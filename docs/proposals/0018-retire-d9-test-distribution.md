# Proposal 0018: Retire D9, test distribution

- **Number:** 0018
- **Filed:** 2026-11-11
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurement below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. Retiring D9 lowers most affected production-readiness lenses slightly (below), for every
  producer alike. The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes.
- **Status:** Proposed

## Problem

D9 ("Test Distribution", production-readiness lens, scored, ceiling rung Documented) is defined as *"Whether the test
suite has a healthy mix of unit / integration / end-to-end tests."* There is no agreed healthy mix. The "test
pyramid" is a heuristic that teams reasonably invert (a service tested mostly through its API, a UI library tested
mostly in a browser), so a ratio target scores a style. Implementations converge on the one part that is defensible,
"the suite has some test that exercises a part of the code on its own", and at that point the dimension reads 10
almost everywhere tests exist.

The evidence of [decision 0013](../decisions/0013-a-dimension-enters-the-standard-on-evidence.md), over the corpus
named in [evidence-2026-11-11-corpus.tsv](evidence-2026-11-11-corpus.tsv) (5,894 public repositories in 17
languages, the latest completed measurement of each, 2026-09-15 to 2026-11-10, median 2026-10-31):

1. **Activation.** 3,939 repositories carried a D9 reading. 839 of those are "No tests found" at 0, which is not a
   distribution: a repository without tests has none to distribute, and the measuring producer has since stopped
   scoring D9 there (absence of tests is read by D8, automated tests). That
   leaves **3,100** repositories with tests, in all 17 languages (from 96 % of the Rust repositories to 1 % of the
   Ruby ones).
2. **Repositories hit.** "Inverted test pyramid" in 120 repositories on the readings measured before the producer
   withdrew the ratio; on its current implementation the only kind left is "No unit or integration tests".
3. **Saturation.** Of the 3,100: 2,837 (**92 %**) read 9.5 or more, 2,646 (85 %) exactly 10, on readings that still
   carried the ratio deduction. Without it, only the repositories at 2 or below (52, 1.7 %) stay off the ceiling: about
   **98 % at 10**. In every language with at least 40 such repositories, the share at exactly 10 on those older readings is between
   65 % (TypeScript) and 98 % (Dart).
4. **Blind-judge precision.** Not measured: the case rests on the definition and on saturation.
5. **Disputes.** None.
6. **The construct.** The top holds kafdrop, AxonFramework, spring-data-jpa and nearly every other tested repository;
   what keeps a repository off it is a suite made only of end-to-end or BDD tests (examples at 2 or below: reveno,
   AtomVM, btcpayserver, LocalAI). Those are real projects with real test suites whose tests drive the whole system.
   That is a fact worth telling their teams, but it is not a measure on which 3,100 repositories differ.

## Change

In the next rubric version: D9 is removed from the catalog. Its id is retired and never reused. The
production-readiness lens is computed from its remaining dimensions.

A producer that wants to tell a team its suite has no test of a single part may do so as its own advice, outside the
standard (0013).

## Effect on published scores

Estimated on the 3,100 repositories by recomputing the production-readiness lens as the plain mean of its scored
dimensions with and without D9: the lens moves by a median of **−0.24** points on its 0-10 scale (range −1.67 to
+1.00); 2,995 move down, 102 up. No lens is lost. The 839 repositories without tests are not counted here: on the
measuring producer's current implementation D9 no longer applies to them, so retiring it does not move them. This is
an estimate: the headline blend is rank-weighted, not linear. **Before a decision, the reference scorer recomputes
every published score under the proposed rubric**, and the decision states the exact distribution and every band that
changes.

## Alternatives considered

- **Re-define D9 as "the suite has a fast layer"** (a unit or integration test exists). That is what it reads today,
  and at about 98 % on the ceiling it would fail 0013's saturation bar as a new dimension.
- **Keep the ratio.** It scores a style there is no agreement on, and asks teams to change the mix of their tests to
  hit it.
- **Make it advisory.** Nobody needs producers compared on it (0013).

## Questions for the decider

1. Retire the id outright, or keep D9 reserved for a re-definition with its own evidence (for example, test layers
   read from executed tests rather than from file and project names)?
2. Is the exact recomputation by the reference scorer a precondition of the decision, as for proposal 0014?
