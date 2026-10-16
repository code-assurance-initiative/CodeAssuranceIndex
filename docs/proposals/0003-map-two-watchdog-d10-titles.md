# Proposal 0003: Map two Watchdog D10 result titles the engine now emits (excessive-mocking, flaky-test)

- **Number:** 0003
- **Filed:** 2026-10-16
- **Proposer:** the Watchdog maintainers, as a scanner owner
- **Declared interests:** the proposer builds Watchdog, the scanner whose mapping (`mappings/watchdog.json`) this
  changes. Accepting it credits Watchdog with plants it currently scores as missed. That is the conflict, and it is
  why the change is asked for here instead of committed.
- **Moves scores:** YES. On the training set it turns four Watchdog FN into TP (below). It also changes how a
  Watchdog result of these two titles is scored on any holdout; the proposer has not read and must not read a holdout,
  so that effect is unmeasured by the proposer.
- **Status:** Proposed

## Problem

`mappings/watchdog.json` (scanner-benchmark, at the Watchdog pin `7f9dd6b8c5` and still at `2cbefb0`, the current
`main`) maps D10 results to concepts by message title. Two titles the Watchdog engine now emits are mapped to no
concept, so the harness scores them `uncovered`, `(unmapped)`, whatever they sit on:

| Title (message prefix) | Emitted since | Mapping today |
|---|---|---|
| `Mock-dominated test: ` | engine `0f851ee162`, 2026-10-11 (`engine/src/Scanner/Testing/D10/TestQualityAnalyzer.cs:435`) | `excessive-mocking` has `"rules": []` and an `unevidenced` entry: "TestQualityAnalyzer.cs emits no mock-dominance row at rubric-2026.10.1". True when written; no longer true. |
| `Races the wall clock: ` | engine `e898069807` (`TestQualityAnalyzer.cs:405`) | `flaky-test` D10 messages are `^Fixed-sleep synchronisation: ` and `^Depends on a live external host: ` only. |

Found when Watchdog's key-disagreement tool was about to file training-set entry `bench-csharp-tests` TQ-007 as a
key dispute: the engine had fired on the entry's exact lines, and the harness had not read the result.

## By the definitions (taxonomy.json), not by the number

- `excessive-mocking`: "A test whose setup is mostly mocks, testing wiring rather than behaviour." The engine raises
  `Mock-dominated test` when a test builds at least 3 framework doubles that outnumber the real objects it builds AND
  every assertion it makes is an interaction check on a double. Message as emitted on TQ-007: "This test sets up 6
  test doubles (for example `Substitute.For<IRateSource>()`) against 3 real object(s) of its own, and its only
  verification is 6 check(s) that the doubles were called. It pins how the code is wired, not what it does". That is
  the definition, stated as a measured condition. The look-alike TRP-007 (one boundary substitute, assertions on the
  converted amount) is not fired on.
- `flaky-test`: "A test that passes and fails across identical runs." `Races the wall clock` is a test whose
  expectation and the code under test read two independent real clocks, so its outcome depends on whether a
  boundary (second, local vs UTC date) falls between them: the same run passes or fails by time of day. That is
  non-determinism across identical runs, the same family as the two flaky-test titles already mapped.

## Proposed change

In `mappings/watchdog.json`:

1. `concepts.excessive-mocking.rules`: replace `[]` with
   `[{"rule": "^(?:D10)$", "messages": ["^Mock-dominated test: "], "source": "engine/src/Scanner/Testing/D10/TestQualityAnalyzer.cs:435 (>=3 framework doubles outnumbering the real objects the test builds, every assertion an interaction check)"}]`
   and remove the `unevidenced` entry for `excessive-mocking` / `D10`.
2. `concepts.flaky-test.rules[1].messages`: add `"^Races the wall clock: "` (source `TestQualityAnalyzer.cs:405`).
3. Bump the mapping's `version` per the harness's mapping-versioning rule, so results scored before and after are not
   compared silently.

## Effect on recorded figures (training set, Watchdog engine `d1fb149074`, kept scans, measured by the proposer)

Every result of these two titles across the 25 L2 units:

| Unit | Result | Today | After |
|---|---|---|---|
| bench-csharp-tests | D10 `Mock-dominated test` QuoteServiceTests.cs:18 | uncovered; TQ-007 (must-fire, excessive-mocking) FN | TQ-007 TP |
| bench-ts-codehealth | D10 `Mock-dominated test` quote-routes.test.ts:16 | uncovered; CH-048 (must-fire, excessive-mocking) FN | CH-048 TP |
| bench-csharp-tests | D10 `Races the wall clock` RateTableTests.cs:59 | uncovered; TQ-006 (must-fire, flaky-test) FN | TQ-006 TP |
| bench-ts-readiness | D10 `Races the wall clock` share-links.test.ts:11 | uncovered; FLK-001 (must-fire, flaky-test) FN | FLK-001 TP |
| bench-csharp-readiness | D10 `Races the wall clock` TrackingPollerTests.cs:13 | clean-fp, attributed `(unmapped)` | clean-fp, attributed `flaky-test` (still noise; count unchanged) |

Totals: TP +4 (csharp +2, typescript +2), FN -4, traps fired 0 -> 0, noise count unchanged (one existing noise row
moves from `(unmapped)` to `flaky-test`). No must-not-fire entry of either concept is fired on by either title.
Holdout effect: not measured by the proposer (see interests).

## Alternatives considered

A key dispute (proposal 0001). Rejected:

the keys are right: each entry is a defect of its concept and the engine found it on the entry's lines. Disputing
them would move a correct key to fit a scanner. The gap is only that the harness mapping does not read the result.
Watchdog's `tools/bench/bench.py case` now refuses to file such an entry as a key case and names it a mapping gap.

## Questions for the decider

1. This proposal moves scores and comes from the scanner it credits; the acting maintainer also maintains that
   scanner. Should it wait for a commenter not affiliated with Watchdog (or the steering group), per
   [GOVERNANCE.md](../GOVERNANCE.md), rather than be decided by the acting maintainer alone?
2. Which part of the mapping version should the bump change, given that the change adds rules and removes no
   attribution that exists today?
