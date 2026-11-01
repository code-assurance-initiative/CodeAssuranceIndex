# Proposal 0005: Contributors no longer read by a language model are catalogued as tools, and their descriptions say what is measured

- **Number:** 0005
- **Filed:** 2026-11-01
- **Proposer:** Jimmy Borch, for the CAI team
- **Declared interests:** the proposer maintains Watchdog, a commercial engine and today the only producer whose
  surveys this site publishes. Watchdog is the implementation that stopped asking a model; this proposal makes the
  catalog describe what Watchdog now does. Every other producer reads the same catalog, and nothing below favours one
  way of computing a contributor over another: `evaluator` says which reproducibility class a reading is in, not how
  it is computed.
- **Moves scores:** no. No contributor changes lens, category, weight, polarity or `advisory`. The reference scorer
  folds on `advisory` alone ([cai-scoring.md](../spec/cai-scoring.md) §2.1), and `evaluator` and `whatItMeasures` are
  descriptive. The one `advisory` declaration this adds (M4, `false`) states what every M4 reading has carried since
  measure-2026.10.8, so no bundle scores differently under it.
- **Status:** Accepted by [decision 0005](../decisions/0005-migrated-contributors-are-read-by-tool.md), 2026-11-01

## Problem

Discussed in [issue #32](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/32).

The current catalog ([`rubric-2026.10.5`](../../rubrics/rubric-2026.10.5/rubric-catalog.json)) marks 16 contributors
`evaluator: "llm"`: D19, D20, D21, D22, D24, D25, DM8, ED5, ES3, LA1, LA2, LA3, LA4, LA5, LA6 and M4. That was true
when it was published. It is not true now: between 2026-10-21 and 2026-10-31 Watchdog replaced every one of these
readings with a deterministic rule and deleted the model from its engine. A third party can now reproduce each reading
from the code and the engine version, which is the property `evaluator: "tool"` names. A catalog that publishes a
provenance the readings no longer have is wrong, in the direction that undersells their reproducibility.

Nine of the descriptions also name the method instead of the measurement: "LLM-confirmed" (DM8, LA1), "confirmed by
language model" (ED5), "LLM-judged" (LA2 to LA6) and "(LLM-judged, advisory)" (M4). A description that names how a
producer computes something is the catalog describing one implementation. It should say what is measured;
`evaluator` already carries the class, and `advisory` already carries whether it counts.

Two contributors declare no `advisory`, so the bundle decides (§2.1):

- **ES3** decides per reading by design: its card is advisory only while a language its classifier does not yet cover
  leaves an ambiguous field, and scored otherwise. That was its behaviour with the model too. It should stay undeclared.
- **M4** says "advisory" in its description, but its readings have carried `advisory: false` since measure-2026.10.8.
  An owner decision of 2026-10-08 (before this proposal) made M4's deterministic drift count and only the model's own
  verdict advisory; the model's verdict no longer exists, so every M4 reading is the deterministic one. The
  description and the readings disagree, and a reader of the catalog is told the opposite of what the scorer does.

## Evidence that each reading is now deterministic

Each migration was adjudicated before it shipped, against a pre-registered gate of 95 % precision on sampled findings,
and rescanned for byte-identical output. A kind of finding that missed the gate was withheld (read, counted as an
engine gap, never reported), not shipped below it.

| Contributor | Measurement version | Precision adjudicated (shipped kinds) | Withheld |
|---|---|---|---|
| D19 Documentation Quality | measure-2026.10.25 | build/run example 8/8, map 1/1 | 6 kinds |
| D20 ADR Quality | measure-2026.10.22 | 39/40 (97.5 %) | none |
| D21 Naming Consistency | measure-2026.10.20 | acronym spelling 5/5, awaitable naming 1/1 | other kinds |
| D22 Internal API Consistency | measure-2026.10.20 | 1/1 (one row on 98 repositories; not an estimate) | none |
| D24 Comment Value | measure-2026.10.9 | 39/40 (97.5 %) | none |
| D25 ADR Conformance | measure-2026.10.23 | no row on either population | none |
| DM8 Value-object opportunities | measure-2026.10.27 | no kind passed | the rule (no row) |
| ED5 Idempotency | measure-2026.10.26 | 23/24 (95.8 %) | none |
| ES3 PII in the event store | measure-2026.10.15 | 9/9 (small population) | none |
| LA1 Personal-data handling | measure-2026.10.15 | 39/40 (97.5 %) | none |
| LA2, LA5, LA6 (WCAG 1.1.1, 2.4.4, 2.4.6) | measure-2026.10.28 | 20/20 together (Wilson 84-100 %) | none |
| LA3 Vulnerability-disclosure policy | measure-2026.10.24 | 41/41 (Wilson 91-100 %) | none |
| LA4 Dev/test/prod separation | measure-2026.10.24 | no kind passed | the rule (no row) |
| M4 Documentation accuracy | measure-2026.10.8 | links 6/6, versions 1/1 | other claim kinds |

DM8 and LA4 run and emit no row today; they are still read by a tool, and a later measurement version may ship a kind
that passes.

## Change

A new rubric version, published by the Initiative under [decision 0002](../decisions/0002-rubric-versions-are-published-by-the-initiative.md),
differs from `rubric-2026.10.5` in exactly these fields and in nothing else.

### 1. `evaluator`

For all 16 contributors listed above: `"llm"` is replaced by `"tool"`.

### 2. `whatItMeasures`

| Id | Replaced | Replacing |
|---|---|---|
| DM8 | Whether clusters of primitives that travel together (a missing value object) are extracted — a low-weight suggestion, LLM-confirmed when configured. | Whether clusters of primitives that travel together (a missing value object) are extracted. A low-weight suggestion. |
| ED5 | Whether retry-prone mutations (command handlers + message/event consumers) are idempotent so an at-least-once redelivery or client retry doesn't double-apply the effect — heuristic at-risk detection confirmed by language model, advisory. | Whether retry-prone mutations (command handlers and message or event consumers) are idempotent, so an at-least-once redelivery or a client retry does not apply the effect twice. |
| LA1 | LLM-confirmed personal-data handling surface — fields that are genuine personal data about a real person (not fiction/test/config), surfaced for the GDPR Art. 25/32 self-assessment. Advisory. | The personal-data handling surface: fields that hold genuine personal data about a real person (not fiction, test or configuration data), surfaced for the GDPR Art. 25/32 self-assessment. |
| LA2 | LLM-judged quality of image alt text — whether each alt is a meaningful equivalent rather than a filename, generic word or placeholder. Advisory; complements the deterministic 'alt present?' check. | Whether each image's alt text is a meaningful equivalent rather than a filename, a generic word or a placeholder. Complements the 'alt present?' check. |
| LA3 | LLM-judged whether a SECURITY.md / security.txt is a real coordinated vulnerability-disclosure policy (contact + how to report + handling), not just a stub. Advisory. | Whether a SECURITY.md or security.txt is a real coordinated vulnerability-disclosure policy (a contact, how to report, how reports are handled), not a stub. |
| LA4 | LLM-judged whether configuration separates development/test/production (distinct per-environment settings) rather than commingling them. Advisory. | Whether configuration separates development, test and production (distinct per-environment settings) rather than mixing them. |
| LA5 | LLM-judged descriptiveness of link/button text — whether each conveys its purpose out of context rather than a vague 'click here'/'read more'. Advisory; complements the deterministic 'has a name?' check. | Whether each link's or button's text conveys its purpose out of context, rather than a vague 'click here' or 'read more'. Complements the 'has a name?' check. |
| LA6 | LLM-judged descriptiveness of heading/label text — whether each meaningfully describes its section or field rather than a placeholder ('Heading', 'Untitled', 'Label'). Advisory. | Whether each heading or label describes its section or field, rather than a placeholder ('Heading', 'Untitled', 'Label'). |
| M4 | Whether the README actually describes the code that exists (LLM-judged, advisory). | Whether the project's current-state documentation describes the code that exists. |

The word "Advisory" leaves the descriptions with the method words: whether a contributor counts is its `advisory`
field, and a description that repeats it can only fall out of step with it, as M4's did.

### 3. `advisory`

M4 declares `advisory: false`. ES3 stays undeclared, so its bundle keeps deciding per reading. Every other contributor
keeps the `advisory` value it has (`true` for the other 14).

## Effect on published scores

None, by construction: §2.1 folds on `advisory`, which changes for no contributor whose bundles say otherwise. M4's
new declaration (`false`) is the value every M4 reading has carried since measure-2026.10.8. A re-score of the current
corpus under the new catalog must give byte-identical survey numbers; the decision should record that check.

## Alternatives considered

- **Leave the catalog until contributors are promoted to scored.** That publishes a false provenance for at least a
  cycle, and promotion is a separate question (below).
- **Promote the migrated advisory contributors to scored in the same change.** Rejected. Whether a deterministic D19
  or LA3 should count in its lens moves published scores and needs evidence from at least one production cycle of the
  deterministic reading. Each such promotion should be its own later proposal, on that evidence.
- **Retire the contributors whose rule ships no row (DM8, LA4).** Rejected: they still run and disclose what they did
  not judge, and retiring a contributor is a larger change than its evidence calls for.

## Questions for the decider

1. Should M4 declare `advisory: false`, as proposed, or stay undeclared like ES3? Declaring it makes the catalog the
   authority; leaving it undeclared changes nothing in the numbers either.
