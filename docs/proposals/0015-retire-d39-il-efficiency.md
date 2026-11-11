# Proposal 0015: Retire D39, IL efficiency

- **Number:** 0015
- **Filed:** 2026-11-11
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurement below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. Retiring D39 lowers most affected code-health lenses very slightly (below), for every
  producer alike. The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes.
- **Status:** Accepted by [decision 0015](../decisions/0015-retire-d39-il-efficiency.md)

## Problem

D39 ("IL Efficiency", code-health lens, scored, ceiling rung Verified) has no definition: its `whatItMeasures` is
its own name, "IL Efficiency". Implemented, it counts authored methods whose compiled .NET intermediate language
exceeds a size budget and deducts for them.

The evidence of [decision 0013](../decisions/0013-a-dimension-enters-the-standard-on-evidence.md), over the corpus
named in [evidence-2026-11-11-corpus.tsv](evidence-2026-11-11-corpus.tsv) (5,894 public repositories in 17
languages, the latest completed measurement of each, 2026-09-15 to 2026-11-10, median 2026-10-31):

1. **Activation.** 353 repositories (6.0 %), and only .NET ones: F# 170 (53 % of the F# repositories), C# 165
   (41 %), Visual Basic 10, and the .NET part of 8 JavaScript or TypeScript repositories. It cannot apply to any of
   the other 13 languages, because it reads a compiled .NET assembly.
2. **Repositories hit.** One kind, "authored methods exceed the IL budget", in 200 repositories.
3. **Saturation.** 332 of 353 (94 %) read 9.5 or more, 183 (52 %) exactly 10. Six read below 8.5.
4. **Blind-judge precision.** Not measured. This proposal rests on the definition and the construct, not on whether
   individual results are true; the decider may ask for it.
5. **Disputes.** Four, all declined.
6. **The construct.** The bottom holds a validation library, a procedural generator, an F# expression optimiser and a
   decompression port (FluentValidation, MarkovJunior, Linq.Expression.Optimizer, unz); the top holds a benchmarking
   library, a CMS and a message bus (BenchmarkDotNet, OrchardCore, MassTransit). What separates them is how much logic
   sits in a few large methods, which D1 (complexity) and the size dimensions already read in source, in every
   language. The IL size of a method adds the compiler's lowering (async state machines, closures, F# pattern
   compilation) to that, a property of the language's compiler rather than of the code, and a likely reason F#
   repositories reach 10 less often than C# ones (43 % against 63 %).
   The remedy it implies, splitting methods to fit a budget, does not make a product better by itself.

So D39 measures, on one platform only, a variant of what a language-neutral dimension already measures, plus a
compiler artefact; and it has no definition another producer could implement.

## Change

In the next rubric version: D39 is removed from the catalog. Its id is retired and never reused. The code-health lens
is computed from its remaining dimensions.

A producer that wants to keep telling .NET users about large compiled methods may do so as its own advice, outside
the standard (0013).

## Effect on published scores

Estimated on the 353 repositories by recomputing the code-health lens as the plain mean of its scored dimensions
with and without D39: the lens moves by a median of **−0.05** points on its 0-10 scale (range −0.39 to +0.22); 293
move down, 23 up. No lens is lost. This is an estimate: the headline blend is rank-weighted, not linear. **Before a
decision, the reference scorer recomputes every published score under the proposed rubric**, and the decision states
the exact distribution and every band that changes.

## Alternatives considered

- **Write a definition for it.** Any definition true to the implementation is .NET-only and overlaps D1; one that is
  language-neutral is D1.
- **Make it advisory.** Under 0013 an advisory dimension must be one producers are compared on; a per-platform
  compiler measure is not.

## Questions for the decider

1. Is the exact recomputation by the reference scorer a precondition of the decision, as for proposal 0014?
