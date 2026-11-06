# Decision 0008: Correct the Watchdog/CCA mapping for the rule-id rename, and require a mapping to cover every rule its scanner emits

- **Decision:** 0008
- **Date:** 2026-11-06
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0008: Correct the Watchdog mapping for its engine's rule-id rename, and require a mapping to cover every rule its scanner emits](../proposals/0008-a-scanner-mapping-covers-every-rule-its-scanner-emits.md)
  ([pull request #42](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/42),
  [issue #41](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/41))
- **Outcome:** Accepted as written, its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA, the engine the mapping `mappings/watchdog.json`
is for, and which is today the only producer the benchmark publishes results for. **The conflict runs in the
decider's favour here**: the correction raises CCA's measured recall (csharp 84.4 % → 86.7 %, typescript
72.3 % → 75.5 %). That is the kind of change this page is most wary of, so the reasons below turn on the defect being
real and contract-defined, not on the recall going up, and the post-adoption comment period is open to an unaffiliated
objection precisely because the change favours the decider's engine. Nobody else can decide today, so nobody recused.

## Comment

None before adoption. This change **moves benchmark scores** for the CCA scanner (recall and noise per dimension; no
CAI score of any codebase moves), so under the interim rule the **30-day** comment period runs after adoption, to
**2026-12-06**. An unresolved objection from a commenter not affiliated with the proposer reopens the decision.

## Reasons

- It is a defect against the benchmark's own contract, not a tuning. A mapping scores a result as a detection only
  when a rule item of the right concept accepts it; after the engine renamed its own rule ids `watchdog-…` → `canine-…`
  (the producer rename of [decision 0007](0007-producers-are-named-by-their-products.md)), **219 of 223** own rule ids
  matched no pattern, so real detections scored as misses. A mapping that cannot place rows its own scanner emits is
  broken by the contract's definition.
- The blindness was symmetric, so no regression gate could see it — both arms of every comparison used the same blind
  mapping. It was found only by an external tester whose planted defects exposed rules that should have fired. A
  standard that can be blinded by a producer's own rename, invisibly, must close that hole, not just patch this
  instance.
- The coverage obligation (Change 2) is what closes it for good: a producer states the rule list and mapping commit it
  read, and a result whose scanner emitted an unplaced rule is not a recall measurement for that concept.

## Answers to the proposal's questions

1. **The coverage rule is a clarification, recorded as contract text, not a breaking contract bump.** It makes
   explicit what "a mapping for a scanner" already meant — that it places the scanner's rules — and changes the
   behaviour of no existing, correct mapping. The benchmark repository records it as a contract clarification in
   CONTRACT.md with the date and this decision; it need not rev the contract's major version. A producer that cannot
   yet meet it is not retroactively disqualified; its affected results are marked not-measured for the unplaced rules.
2. **The results addendum waits for the next scored run.** The corrected mapping takes effect now for the producer's
   own gates, but a *published* results addendum is filed with the next scored run, not retrofitted onto the
   2026-10-07 baseline, so every published number keeps a single, datable provenance. The baseline and its 1.7
   addendum, scored before the rename, stand unchanged.

## What took effect

- Proposal 0008's status and the proposal and decision indexes in this repository.

## What did not yet

- **The mapping fix and the contract text live in the benchmark repository, not here.** They take effect when
  [scanner-benchmark#3](https://github.com/code-assurance-initiative/scanner-benchmark/pull/3) (branch
  `mapping-canine-ids`, commit `328f5e2`: both rule-id prefixes accepted, the 21 unmapped own rules placed) and the
  CONTRACT.md coverage paragraph are merged there. This decision accepts them; the benchmark repository implements
  them.
- **The producer-side coverage gate is engine-side** (`tools/bench/own_rule_mapping.py` in the CCA engine), and its
  rollout to prod follows the engine's own promote, which is separately gated.
- No results addendum is published yet (answer 2).
