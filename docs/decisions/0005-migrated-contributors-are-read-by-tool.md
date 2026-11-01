# Decision 0005: Contributors no longer read by a language model are catalogued as tools

- **Decision:** 0005
- **Date:** 2026-11-01
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, and today the whole team, until the steering
  group exists: [GOVERNANCE.md](../GOVERNANCE.md))
- **Proposal:** [0005: Contributors no longer read by a language model are catalogued as tools, and their descriptions say what is measured](../proposals/0005-migrated-contributors-are-read-by-tool.md)
  ([pull request #33](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/33),
  [issue #32](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/32))
- **Outcome:** Accepted as written. The proposal's one question is answered below: M4 declares `advisory: false`.
- **Supersedes:** nothing.

## Numbering

The proposal was filed as 0004. [Pull request #31](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/31)
had already claimed 0004 on 2026-10-25 for a different proposal, which is not yet decided and so was not in the index on
`main`. Numbers go in filing order, so #31 keeps 0004 and this proposal is 0005. It was renumbered before it was merged;
its text is otherwise as filed.

## Conflicts and recusals

The proposer and the decider are the same person, who also maintains Watchdog, the engine whose change the proposal
describes and today the only producer. Nobody else could decide, so nobody recused.

What limits the conflict is the shape of the change. It moves no published score (below). `evaluator` and
`whatItMeasures` are descriptive: the reference scorer reads neither. And the change describes Watchdog's readings
less favourably to Watchdog in one respect: a catalog that says `tool` invites anyone to reproduce a reading and
dispute it where it differs, which `llm` did not. The steering group, once it exists, can confirm or replace this
decision.

## Comment

None was taken. The proposal was filed and accepted on the same day, without the 14-day comment period
[GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided) sets for a change that moves no number. That
is a departure from the published process, recorded as decisions 0001 to 0003 recorded theirs.

Why it was accepted anyway: the catalog in force describes a provenance the readings no longer have, and one
description (M4's) tells a reader the opposite of what the scorer does. Leaving a known misstatement in the published
standard for two weeks serves no reader. Objections remain welcome: anyone may file a proposal to amend or replace this
decision, and it will be decided under the full comment period.

## Reasons

- `evaluator` names the reproducibility class of a reading. Every reading of the 16 contributors is now reproducible
  from the code and the engine version, each adjudicated against a pre-registered 95 % precision gate before it
  shipped (the proposal's evidence table). `tool` is what they are.
- A description that names a producer's method describes one implementation. The catalog describes measurements;
  `evaluator` and `advisory` already carry the method class and whether a reading counts.
- Repeating "Advisory" in prose can only fall out of step with the `advisory` field, as M4's did.

## Answer to the proposal's question

**M4 declares `advisory: false`.** Declaring it makes the catalog the authority for whether an M4 reading counts, which
is the purpose of the `advisory` field ([cai-scoring.md](../spec/cai-scoring.md) §2.1). Leaving it undeclared would keep
a contributor whose readings are all scored in the class of contributors that decide per reading, and ES3 is the only
contributor that genuinely does: its reading is advisory only while a language its classifier does not cover leaves a
field ambiguous. ES3 stays undeclared.

## Effect on published scores

None, and none can occur:

- A bundle is scored under the catalog of the rubric version it names (`rubricVersion`, cai-scoring.md §1). Every
  delivery already issued names an earlier version and keeps its catalog, byte for byte.
- A delivery under the new version is scored by `advisory` alone. The declarations in the new catalog equal those of
  `rubric-2026.10.5` for every contributor except M4, and M4's readings have carried `advisory: false` since Watchdog's
  measurement version `measure-2026.10.8`, so a bundle scores the same under either catalog.
- `tools/rubric/classify-rubric-change.py` classifies the new version as score-relevant, because a declared `advisory`
  changed (M4, undeclared → `false`). That is the classifier being cautious about the field, not a number moving; this
  decision is the accepted decision the rubric gate asks a score-relevant version to link.

## What takes effect

- Proposal 0005's status, and the proposal and decision indexes.
- A new rubric version, `rubric-2026.11.1`, published by the Initiative under
  [decision 0002](0002-rubric-versions-are-published-by-the-initiative.md): Watchdog's emitter stages the catalog and
  requests it by pull request (`publish-rubric-catalog.sh --pr`); it differs from `rubric-2026.10.5` in exactly the
  fields the proposal lists and in `rubricVersion`.
- Watchdog, on its own side (noted, not adopted): its catalog emitter declares `advisory` explicitly instead of deriving
  it from `evaluator`, so the evaluator change cannot alter a declaration; its acceptance-criteria documents in Assay
  mark a dimension advisory by the catalog's `advisory`, not by `evaluator`.

## What did not

- **No contributor is promoted to scored.** Whether a deterministic D19 or LA3 should count in its lens moves published
  scores and needs evidence from at least one production cycle of the deterministic reading. Each such promotion is its
  own later proposal.
- **DM8 and LA4 stay in the catalog** although their rules emit no row today: they run and disclose what they did not
  judge.
