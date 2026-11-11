# Proposal 0016: Retire AC7, accessibility enforcement, and leave its advice to the markup dimensions

- **Number:** 0016
- **Filed:** 2026-11-11
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurement below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. Retiring AC7 raises most affected accessibility lenses (below), for every producer alike.
  The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes.
- **Status:** Accepted by [decision 0016](../decisions/0016-retire-ac7-a11y-enforcement.md)

## Problem

AC7 ("A11y enforcement", accessibility lens) is defined as *"Whether accessibility is ENFORCED in the toolchain: an
accessibility checker configured over the markup … and an automated accessibility assertion wired into tests or CI …
on the Documented→Verified→Prevented ladder."* It is the one accessibility dimension that does not read the markup.
AC1 to AC6 read the markup itself.

The evidence of [decision 0013](../decisions/0013-a-dimension-enters-the-standard-on-evidence.md), over the corpus
named in [evidence-2026-11-11-corpus.tsv](evidence-2026-11-11-corpus.tsv) (5,894 public repositories in 17
languages, the latest completed measurement of each, 2026-09-15 to 2026-11-10, median 2026-10-31):

1. **Activation.** 1,187 repositories (20.1 %), in all 17 languages: from 50 % of the TypeScript repositories and 41 %
   of the JavaScript ones down to 7 % of the Erlang ones.
2. **Repositories hit.** One kind, "accessibility enforcement below the top rung", in 1,179 repositories.
3. **Saturation.** 1,097 of 1,187 (**92 %**) sit on the lowest rung, 4. 66 read 6, 16 read 8, 8 read 10. The median
   is 4 in every one of the 17 languages.
4. **Blind-judge precision.** Not measured. A rung is a fact about configuration files, so precision is not in
   question; the construct is.
5. **Disputes.** Two: one declined, one open.
6. **The construct.** The top (10) holds large product front ends with a design system or a dedicated platform team
   (supabase, LibreChat, carbon-design-system, langflow); the floor (4) holds nearly everything else, including
   libraries whose only markup is a documentation site or an admin page. What separates them is whether a project
   has a front-end toolchain big enough to carry an accessibility linter and a CI step, which is the kind and size of
   the project. It says nothing about whether the markup is accessible: a repository whose pages pass every check of
   AC1 to AC6 still reads 4, and one with a configured linter still reads 10 over markup that fails them.

So AC7 is a reading of the population, not of the quality the lens names, and the accessibility lens already reads
that quality directly.

## Change

In the next rubric version: AC7 is removed from the catalog. Its id is retired and never reused. The accessibility
lens is computed from AC1 to AC6; a repository where AC7 was the only accessibility dimension that applied has no
accessibility lens, exactly as one where none applies.

What AC7 told its readers is not lost: a producer that reports an AC1 to AC6 result may, as its own advice outside
the standard (0013), name the checker or CI assertion that would have prevented that class of result. The advice
then sits next to the defect it would have prevented, instead of being scored on its own.

## Effect on published scores

Estimated on the 1,187 repositories by recomputing the accessibility lens as the plain mean of its scored dimensions
with and without AC7: the lens moves by a median of **+0.31** points on its 0-10 scale (range −2.50 to +3.00); 992
move up, 174 down (those with a rung above 4). 10 repositories lose an accessibility lens that rested on AC7 alone.
This is an estimate: the headline blend is rank-weighted, not linear. **Before a decision, the reference scorer
recomputes every published score under the proposed rubric**, and the decision states the exact distribution and
every band that changes.

## Alternatives considered

- **Fold the rung into AC1 to AC6 as a credit or a cap.** It would carry the same population reading into six
  dimensions that today measure the markup; a toolchain fact would move a markup score.
- **Make it advisory.** Under 0013 an advisory dimension must be one producers are compared on; 92 % on one rung
  compares nobody.
- **Keep it and lower the floor's weight.** The saturation is the problem, not the weight.

## Questions for the decider

1. Is the advice described under Change (a producer naming the checker next to an AC1 to AC6 result) worth stating
   in the catalog's guidance for producers, or left entirely to producers?
2. Is the exact recomputation by the reference scorer a precondition of the decision, as for proposal 0014?
