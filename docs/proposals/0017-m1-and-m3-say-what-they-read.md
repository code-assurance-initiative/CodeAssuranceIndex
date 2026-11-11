# Proposal 0017: M1 and M3 say what they read; project naming leaves M3

- **Number:** 0017
- **Filed:** 2026-11-11
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurement below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. The change raises M3 on the repositories named below, for every producer alike. The
  proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes (M3 only).
- **Status:** Accepted by [decision 0017](../decisions/0017-m1-and-m3-say-what-they-read.md)

## Problem

Two maturity definitions name a quality that is either read elsewhere or is not a quality.

- **M1** ("Documentation (README)") is defined as *"Whether the repo and its projects have a README, and whether it's
  substantive and current."* Whether a README is **current** is what M4 reads (documentation that disagrees with the
  code). Read inside M1 it can only be a proxy, such as "many files changed since the README was last touched", which
  asks a team to touch a correct README to clear it, and counts the same drift twice where M4 also sees it.
- **M3** ("Folder & project structure") is defined as *"Whether the repo is organised deliberately: src/test
  separation and consistent project naming."* **Consistent project naming** is a convention, not organisation. The
  reading it leads to (do most projects share a root namespace) deducts from a repository whose projects are named
  for what they are, and its only remedy is a mass rename that makes no product better.

The evidence of [decision 0013](../decisions/0013-a-dimension-enters-the-standard-on-evidence.md) for the naming
reading, over the corpus named in [evidence-2026-11-11-corpus.tsv](evidence-2026-11-11-corpus.tsv) (5,894 public
repositories in 17 languages, the latest completed measurement of each, 2026-09-15 to 2026-11-10, median
2026-10-31; column `M3_inconsistent_root_namespaces`):

1. **Activation.** M3 applies to every repository. The naming reading can only fire where a repository declares three
   or more projects with a namespace, so in practice on .NET and a few JVM and JavaScript monorepos.
2. **Repositories hit.** "Inconsistent root namespaces" in **155** repositories: C# 88, Visual Basic 35, F# 13,
   JavaScript 6, TypeScript 6, Rust 2, Python 2, Java 2, Kotlin 1.
3. **Saturation.** M3 as a whole: 74 % at 10. The naming reading is a single 2-point deduction.
4. **Blind-judge precision.** Not measured: the reading is exact (a namespace count); the construct is in question.
5. **Disputes.** None.
6. **The construct.** Hit: semantic-kernel, ardalis/CleanArchitecture, MaterialDesignInXamlToolkit, Paket, Playnite.
   They are well-kept, deliberately organised repositories whose projects sit under more than one root namespace
   (for example a product name beside a library name, or samples and tools beside the product). Their organisation is no worse than
   that of a repository whose projects share one prefix; the reading tells them apart by naming alone.

## Change

In the next rubric version:

- **M1** reads *"Whether the repository and its projects have a README, and whether it is substantive."* The word
  "current" leaves; currency is M4's.
- **M3** reads *"Whether the repository is organised deliberately: production code and tests are set apart in a way
  its ecosystem or the repository itself declares."* Project naming leaves M3, and no other dimension takes it.

## Effect on published scores

- **M1:** none intended. A producer that read currency inside M1 stops doing so; the measuring producer already has.
- **M3:** the 155 repositories above gain the 2-point naming deduction back on M3 (e.g. 6 → 8, 8 → 10). Recomputing
  the maturity lens as the plain mean of its scored dimensions, those 155 move by up to +0.67 points on its 0-10
  scale; no repository moves down. This is an estimate: the headline blend is rank-weighted, not linear. **Before a
  decision, the reference scorer recomputes every published score under the proposed rubric**, and the decision
  states the exact distribution and every band that changes.

## Alternatives considered

- **Keep naming and make the reading smarter** (allow samples, product prefixes). Every refinement still scores a
  convention; the question is whether naming is organisation at all, and it is not.
- **Move naming to an advisory dimension.** Nobody needs producers compared on it (0013).

## Questions for the decider

1. Is the wording of the new M3 definition right, in particular "a way its ecosystem or the repository itself
   declares", which credits a separation the repository states in its own build or test configuration?
2. Is the exact recomputation by the reference scorer a precondition of the decision, as for proposal 0014?
