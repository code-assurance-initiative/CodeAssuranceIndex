# Proposals — how a change to CAI is asked for

Every change to the standard, to the benchmark process, or to the process for the benchmark's answer keys starts
here as a numbered proposal, and takes effect only through a numbered decision in [`../decisions/`](../decisions/README.md)
([ADR-0012](../adr/0012-changes-go-through-proposals-and-decisions.md)). The steps, the comment periods and the rule
on who may accept a score-moving change are in
[GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided); this page is how to file one and where
they are listed.

A correction that moves no number and changes no process (a typo, a broken link, a reference-scorer defect against
the published text) is an ordinary pull request, not a proposal. A finding in one survey is not a proposal either:
[CHALLENGE.md](../CHALLENGE.md) has the routes for disputes.

## Filing

1. Open an issue describing the problem, so it can be discussed before anyone writes the change.
2. Open a pull request adding `docs/proposals/NNNN-short-title.md`, copied from the template below, with the next
   free number. Numbers are never reused, including for a proposal that is withdrawn or rejected.
3. Fill in the registration block. **Declared interests are required**: if you build or work for an engine that
   produces CAI or benchmark evidence, or own code measured by CAI, say which. A proposal without a declaration is not
   filed. A declared interest does not disqualify a proposal; an undeclared one does.
4. Add a row to the index below, with status *Proposed*.

A proposal written elsewhere (in another repository, or by someone who does not use pull requests) may be registered
by a maintainer as it stands: the registration block is prepended and the proposal's own text is not edited.

## Lifecycle

| Status | Meaning |
|---|---|
| Proposed | Filed and open for comment on its pull request or issue. |
| Accepted | A decision accepted it, whole or amended; the decision says which, and what took effect. |
| Rejected | A decision rejected it, with written reasons. It stays here. |
| Withdrawn | The proposer withdrew it before a decision. It stays here. |
| Superseded | A later proposal, accepted by a later decision, replaced it. It stays here, linking to both. |

Once filed, a proposal's text is not edited. Only the *Status* line of its registration block changes, and only to
link the decision that moved it. A change of mind after a decision is a new proposal.

## Who decides, today

Until the steering group exists, the acting maintainer decides, for the CAI team: today that is
**Jimmy Borch**, who is the whole team. A score-moving proposal may only be accepted by the acting maintainer if no
unresolved objection remains from a commenter not affiliated with the proposer; otherwise it waits for the steering
group ([GOVERNANCE.md](../GOVERNANCE.md)). The acting maintainer also maintains Watchdog, an engine measured by the
benchmark, so a decision on a proposal that particularly affects Watchdog says so.

## Template

```markdown
# Proposal NNNN: <short title>

- **Number:** NNNN
- **Filed:** YYYY-MM-DD
- **Proposer:** <name, organisation>
- **Declared interests:** <the engine you build or work for, the code measured by CAI you own, or "none">
- **Moves scores:** yes / no
- **Status:** Proposed

## Problem

What is wrong or missing today, with evidence. Link the issue.

## Change

Exactly what the standard or process would say after this proposal. Quote the text being replaced and the text
replacing it.

## Effect on published scores

If scores move: on which corpus it was measured, how many scores move, by how much (median, range), and how many
bands change. If it was not measured, say why.

## Alternatives considered

What else could solve the problem, and why this one.

## Questions for the decider

Anything the proposer cannot settle alone. The decision answers each one.
```

## Index

| Proposal | Title | Filed | Status |
|---|---|---|---|
| [0001](0001-answer-key-dispute-path.md) | A dispute path for CAI answer keys | 2026-10-16 | Proposed |
