# Proposal 0014: Retire PF2, allocation hygiene

- **Number:** 0014
- **Filed:** 2026-11-06
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence and that computed the
  measurement below, and the Assure, Assert and Assay products of Canine Development built on it; the proposer also
  owns code measured by CAI. Retiring PF2 raises most affected scores slightly (below), for every producer alike. The
  proposer is also the acting maintainer, i.e. the decider ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** yes.
- **Status:** Proposed

## Problem

PF2 ("Allocation hygiene", performance lens, polarity `reward`) is defined as *"Whether the code is written to
minimise allocations so it doesn't pressure its host's memory manager … Reward-only: credited where present, never
penalised where a simpler style is fine."* Implemented to that definition, it reads the density of allocation-aware
idioms (buffer and slice views, pooling, stack or value-type allocation, buffer writers) per thousand production
lines, 6.0 for none rising to 10.

Measured on the latest scan of each repository of one engine's corpus (2026-11-06), PF2 applied to **1,042**
repositories:

| Reading | Repositories | Share |
|---|---:|---:|
| 6 (the floor) | 599 | 58 % |
| 7 | 235 | 23 % |
| 8 | 92 | 9 % |
| 9 | 30 | 3 % |
| 10 (the ceiling) | 86 | 8 % |

- **It reads what kind of code a repository is, not its quality.** The floor holds mature, well-kept frameworks and
  applications (an event-sourcing framework, a Spring Data module, an application framework, a media server, a
  package manager). The ceiling holds libraries that are themselves performance tools (a cache server, a resilience
  library, a reactive-extensions library). Nothing in the reading knows a hot path or any allocation pressure: a
  repository with no allocation-critical path is correctly written without these idioms, and the definition says so.
- **"Never penalised" is untrue in effect.** The floor is 6, not "absent", so a repository with no such idiom lowers
  its performance lens against one that has many. Over the 904 affected repositories that have another performance
  dimension, removing PF2 moves the lens by a median of +9 points (range −20 to +20).
- **It fails the evidence bar of proposal [0013](0013-a-dimension-enters-the-standard-on-evidence.md)** on saturation
  (58 % at one end) and on its construct, before precision is even asked.

## Change

In the next rubric version: PF2 is removed from the catalog. Its id is retired and never reused. The performance lens
is computed from its remaining dimensions (PF1, PF3); a repository where PF2 was the only performance dimension that
applied has no performance lens, exactly as one where none applies.

A producer that wants to keep telling its users about allocation-aware style may do so as its own advice, outside the
standard (0013).

## Effect on published scores

Estimated on the same 1,042 repositories by recomputing the performance lens without PF2 and weighting the change by
the lens's share of the headline: the headline moves by a median of **+0.33** points (range −2.2 to +4.4); 79 % move
up and 16 % down. 136 repositories lose a performance lens that rested on PF2 alone. This is an estimate: the headline
blend is rank-weighted, not linear. **Before a decision, the reference scorer recomputes every published score
under the proposed rubric**, and the decision states the exact distribution and every band that changes.

## Alternatives considered

- **Make PF2 advisory.** It would still enter every report as a reading that describes the population rather than the
  code; under 0013 an advisory dimension must be one readers compare across producers, and nobody needs to compare this.
- **Re-define it around evidence of need** (allocation measured by a benchmark, or idioms counted only on paths a
  benchmark exercises). A worthwhile future proposal; it would be a new dimension with its own evidence, not a repair
  of this one, so this proposal retires the id rather than holding it open.
- **Change the floor to "absent".** It would make "never penalised" true, but it keeps a reading whose construct is
  the kind of code, not its quality.

## Questions for the decider

1. Retire the id outright, or keep PF2 reserved for a re-definition filed under 0013?
2. Is the exact recomputation by the reference scorer (above) a precondition of the decision, as proposed?
