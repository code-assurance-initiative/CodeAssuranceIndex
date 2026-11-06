# Proposal 0008: Correct the Watchdog mapping for its engine's rule-id rename, and require a mapping to cover every rule its scanner emits

- **Number:** 0008
- **Filed:** 2026-11-05
- **Proposer:** Jimmy Borch, for the CAI team (drafted with Claude Code)
- **Declared interests:** the proposer maintains CCA (formerly Watchdog), the engine `mappings/watchdog.json` maps, and
  today the only producer the benchmark publishes results for. The correction raises CCA's measured recall on the
  training set. It changes no answer key, no taxonomy concept, no scoring rule and nothing another scanner's mapping
  reads.
- **Moves scores:** yes. Benchmark results for the Watchdog/CCA scanner only (recall and noise per dimension); no CAI
  score of any codebase moves.
- **Kind:** a defect correction against the benchmark's own contract: a mapping that cannot place rows its scanner
  emits scores real detections as misses.
- **Status:** Accepted by [decision 0008](../decisions/0008-a-scanner-mapping-covers-every-rule-its-scanner-emits.md)

## Problem

Issue: [#41](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/41).

The benchmark scores a scanner through its mapping file ([CONTRACT.md](https://github.com/code-assurance-initiative/scanner-benchmark/blob/main/docs/CONTRACT.md),
"`mappings/<scanner>.json`"): a result belongs to a concept when a rule item of that concept accepts it. For the CCA
engine's own semgrep rules the title is `<severity>: <rule id>`, and every own-rule pattern read `(?:watchdog-)?<id>`.

On 2026-11-02/03 the engine renamed its own rule ids from `watchdog-…` to `canine-…` (the product rename that
[decision 0007](../decisions/0007-producers-are-named-by-their-products.md) records for producers). No pattern can
match `Medium: canine-<id>: …`. Measured on 2026-11-05 against the engine on the branch that carries the fix:

- **219 of the 223 own rule ids the engine can emit reach no concept** under the pinned mapping (scanner-benchmark
  `7f9dd6b8`): a plant one of them finds scores as a miss.
- **21 own rules were never in the mapping at all**, rename or not: rules added after the mapping was last built
  (rubric-2026.10.1), among them the C# SQL-literal interpolation rule, C# reflected XSS, Razor `MarkupString`, C# open
  redirect, NoSQL operator injection and Newtonsoft type-name deserialization. Their recall has never been measured.
- Because both arms of every comparison used the same blind mapping, no comparison moved: the producer's own
  regression gates could not see the defect. It was found by an external tester whose deliberately vulnerable
  application exposed rules that should have fired.

## Change

1. **`mappings/watchdog.json`** (and its generator `mappings/watchdog-build/discrim.py`): every own-rule pattern
   accepts both prefixes, `(?:canine-|watchdog-)?<id>`, so results frozen under the old ids still score; the 21 own
   rules are mapped to the concept their own CWE names (listed in the pull request,
   [scanner-benchmark#3](https://github.com/code-assurance-initiative/scanner-benchmark/pull/3), branch `mapping-canine-ids`, commit `328f5e2`). Built from the same
   rubric-2026.10.1 engine facts as the current file: only rule patterns of 38 concepts and `offConcept` differ. A test
   (`OwnRuleIdPrefixes`, `tests/test_watchdog_mapping.py`) pins both prefixes.
2. **CONTRACT.md, `mappings/<scanner>.json`**, add after the format block:

   > A mapping covers every rule its scanner can emit: each emitted rule either reaches a concept or is listed under
   > `offConcept`. A producer publishing benchmark results states the coverage check it ran (the rule list it read and
   > the mapping commit), and a result whose scanner emitted a rule its mapping does not place is not a recall
   > measurement for that rule's concept.

   The producer-side check CCA runs is `tools/bench/own_rule_mapping.py` in its engine repository, which refuses to
   score while any own rule id is unplaced outside a ratchet that may only shrink.

## Effect on published scores

Measured on the training set (training-set-2026, 25 units), re-scoring the scans of CCA's latest main bench arm
(`base-8a9770ffc8-t0144`) with the corrected mapping, without rescanning:

| Language | Recall before | Recall after | Noise before | Noise after |
|---|---|---|---|---|
| csharp | 190/225 (84.4 %) | 195/225 (86.7 %) | 114/357 | 113/364 |
| typescript | 115/159 (72.3 %) | 120/159 (75.5 %) | 73/212 | 69/216 |

Ten plants the engine already found are credited. No answer key changes, so no other scanner's result moves. The
published 2026-10-07 baseline and its 1.7 addendum were scored before the rename and are unaffected; a results
addendum under the corrected mapping would be filed separately.

## Alternatives considered

- **Revert the engine's rule ids to `watchdog-`.** Rejected: decision 0007 names producers by their products, and the
  ids are user-visible in findings.
- **Edit the mapping without the coverage rule.** Rejected: the defect would recur with the next rename or the next
  new rule, invisibly, as it did here twice (the rename, and 21 rules added without mapping).

## Questions for the decider

1. Is the coverage rule (Change 2) a contract change requiring a contract version bump, or a clarification of what a
   mapping already means?
2. Should a results addendum under the corrected mapping be published now, or with the next scored run?
