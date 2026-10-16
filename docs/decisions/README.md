# Decisions — what was decided about a proposal, by whom, and why

A proposal in [`../proposals/`](../proposals/README.md) changes nothing on its own. It takes effect only through a
decision recorded here ([ADR-0012](../adr/0012-changes-go-through-proposals-and-decisions.md)), and a refusal is
recorded here the same way. This directory is the published answer to bar 2 of
[GOVERNANCE.md](../GOVERNANCE.md#the-bar-this-page-will-be-held-to): how a change is proposed and how one is refused,
with who decided and why.

## What a decision record holds

One file per decision, `NNNN-short-title.md`, numbered in its own sequence (a decision's number need not match its
proposal's), with:

- **Decision**, **Date**, **Decided by** (the person, and the body they decide for), **Proposal** (a link), and
  **Outcome**: *Accepted*, *Accepted as amended*, or *Rejected*.
- **Conflicts and recusals**: whether the decider, or anyone who took part, has a stake in the proposal, and who
  stood aside. "None" is a statement, not a blank.
- **Comment**: where comment was taken, for how long, and the objections raised and how each was answered.
- **Reasons** for the outcome. A rejection without written reasons is not a decision.
- **Answers** to every question the proposal put to the decider, and every amendment, stated as the rule that now
  holds.
- **What took effect** with the decision (the files and repositories changed to implement it), and **what did not
  yet**, named, so a reader can tell the adopted rule from the parts that are still only promised.

A decision is immutable once recorded. Overruling it, or changing what it adopted, is a new proposal and a new
decision that says which earlier decision it supersedes; the earlier one stays here unchanged, and its index row
links to its successor.

## Index

| Decision | Proposal | Outcome | Date | Decided by |
|---|---|---|---|---|
| [0001](0001-adopt-the-answer-key-dispute-path.md) — Adopt the answer-key dispute path | [0001](../proposals/0001-answer-key-dispute-path.md) | Accepted, with the proposal's four questions answered | 2026-10-16 | Jimmy Borch, for the CAI team |
| [0002](0002-rubric-versions-are-published-by-the-initiative.md): Rubric versions are published by the Initiative | [0002](../proposals/0002-rubric-versions-are-published-by-the-initiative.md) | Accepted, the proposal's three questions answered; comment period not taken | 2026-10-16 | Jimmy Borch, for the CAI team |
