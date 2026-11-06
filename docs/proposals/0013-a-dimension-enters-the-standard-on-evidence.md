# Proposal 0013: A dimension enters the standard on evidence, and the standard holds only what must be comparable

- **Number:** 0013
- **Filed:** 2026-11-06
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence, and the Assure, Assert
  and Assay products of Canine Development built on it; the proposer also owns code measured by CAI. This proposal
  raises the bar every producer's candidate dimension must clear, the proposer's included, and moves no score. The
  proposer is also the acting maintainer, i.e. the decider ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** no.
- **Status:** Proposed

## Problem

The catalog has two kinds of dimension, scored and advisory (`advisory: true`, folded by no lens). It says nothing
about **what deserves to be in the catalog at all**, or **what a dimension must show before it is added or kept**.
Two consequences are visible today:

1. **Dimensions enter on argument, not measurement.** A definition can be accepted whose reading, once measured over
   a corpus, turns out to describe what *kind* of code a repository is rather than its quality. The allocation-hygiene
   dimension (PF2) is the worked example: over the latest measurement of the 1,042 repositories where it applied,
   58 % sat at its floor, the repositories at its ceiling were libraries that are themselves performance tools, and
   the "reward-only, never penalised" polarity its definition promises was untrue in effect, because the floor
   reading lowers the lens. Proposal [0014](0014-retire-pf2-allocation-hygiene.md) asks to retire it; this proposal
   asks that the next one never get in without the numbers that would have shown it.
2. **The catalog has no line between the standard and a producer's own extras.** A producer's advice, tasks and
   engine-specific signals do not need to be comparable across producers, and governing them in CAI only couples the
   standard to producers ([decision 0012](../decisions/0012-the-standard-names-no-producer.md)).

## Change

Add a section to [GOVERNANCE.md](../GOVERNANCE.md), "What the catalog holds, and the evidence a dimension needs":

> **What the catalog holds.** A **scored** dimension is the standard: every producer measures it to the same
> definition, and it moves the score. An **advisory** dimension is in the catalog only when producers must be
> comparable on it — a candidate on its way to being scored, or a descriptive reading that readers compare across
> producers. Anything that needs no comparison across producers — advice, remediation tasks, a producer's own signals
> — is the producer's extension on top of the standard, outside the catalog, and is never read as a CAI result.
>
> **Evidence before a proposal.** A proposal to add a dimension, or to change what one measures, is filed only with a
> measurement of an implementation of exactly the proposed definition over a corpus: at least 200 repositories in at
> least three languages, the latest measurement of each, never a hand-picked set. It reports, per language:
>
> 1. **Activation** — the share of repositories it applies to.
> 2. **Repositories hit** per (dimension, kind) — never a share of rows, which measures the draw, not the definition.
> 3. **Saturation** — the share at the scale's floor and at its ceiling. More than half at either end is a reading
>    of the population, not of quality, and needs an argument why it is not.
> 4. **Blind-judge precision** — on a random sample of at least 50 of its results, the share that independent judges
>    who did not see the proposal call valid. At least four of five judges agree on each sample, and at least 75 %
>    are valid.
> 5. **Disputes** — where results were disputed by the people or agents fixing them, the share disputed and the
>    share of disputes upheld. Upheld disputes above 25 % block a scored proposal.
> 6. **The construct** — named repositories from the top and the bottom of its scale, and why they differ in the
>    quality the dimension names rather than in what kind of code they are.
>
> A dimension proposed as **advisory** must report all six; the bars in 4 and 5 apply to a **scored** proposal.
> The same evidence is the basis of a proposal to **re-define or retire** a dimension, and any dimension may be
> re-opened on it.

The thresholds above are the proposer's; the decision may set others (questions below).

## Effect on published scores

None. It governs how dimensions enter and leave; it changes none.

## Alternatives considered

- **No bar, judgement only.** That is today's rule, and PF2 is what it lets in.
- **Remove advisory dimensions from the catalog entirely,** leaving all non-scored readings to producers. Rejected:
  a candidate on its way to being scored is exactly what producers should measure the same way before it counts, and
  an advisory reading readers compare across producers (hub concentration, proposal 0010) loses its value without a
  shared definition.
- **Require the evidence only for scored proposals.** Rejected: an advisory dimension still enters every report, and
  a reader cannot tell it was never measured.

## Questions for the decider

1. Are 200 repositories in three languages, a 50-result blind sample, 75 % precision and a 25 % upheld-dispute
   ceiling the right first bars?
2. Does an existing dimension that would fail the bar today stay until someone files a retirement, or must every
   scored dimension publish the six measures by a set date?
