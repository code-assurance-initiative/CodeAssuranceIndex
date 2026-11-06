# Proposal 0011: An interim pragmatic rule, so a clear improvement is not frozen until a steering group exists

- **Number:** 0011
- **Filed:** 2026-11-06
- **Proposer:** Jimmy Borch, for the CAI team (drafted with Claude Code)
- **Declared interests:** the proposer is the acting maintainer and sole decider, and maintains the CCA engine measured
  by the benchmark. This proposal changes *how* changes are decided while there is one decider; it therefore affects
  every proposal the decider brings, including the decider's own engine's. That is the conflict the proposal is about,
  and the safeguards below are chosen to hold despite it.
- **Moves scores:** no. It changes the decision procedure, not any dimension, weight, formula or key. No published
  score moves.
- **Status:** Accepted by [decision 0011](../decisions/0011-interim-pragmatic-governance.md)

## Problem

`GOVERNANCE.md` holds the standard **still** until a steering group exists: "CAI is not materially changed while the
body that ought to decide such changes is still being put together," and only number-neutral corrections continue.

That was written when the group was thought to be close. It is not. As of 2026-11-06 there is **no founding date** for
the steering group and no founded association to appoint it; a realistic wait is one to five years. A standstill of
that length does not protect the standard — it defeats its purpose. CAI exists to **raise** software quality and to
make demands of it; a standard that cannot adopt a clear, evidenced improvement for years is not cautious, it is the
bureaucracy it was built to replace. The cost is concrete: proposals 0008, 0009 and 0010 are three evidenced
improvements (a benchmark defect that scored real detections as misses, the security concepts a known-defect corpus
needs, and a language-neutral architecture signal) that the standstill would freeze with no one to unfreeze them.

The standstill is also already not being followed: decisions 0001, 0002, 0003, 0005, 0006 and 0007 were all taken by
the acting maintainer, and 0006 was explicitly a score-moving change accepted with "the 30-day comment period runs
after adoption." The rule on the page and the practice in the repository disagree. `GOVERNANCE.md` says that where the
two disagree, the page is wrong and should be corrected. This proposal corrects it.

See issue [#45](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/45) is specific to 0010; the
governance gap is recorded here and in this proposal's pull request.

## Change

Replace the standstill with an **interim pragmatic rule**, in force only until the steering group exists.

Replace, in `GOVERNANCE.md` "Who decides what the standard says — today", the paragraph beginning "Until the steering
group exists, the standard is **held still**" and the paragraph "That standstill is the substitute for governance"
with:

> Until the steering group exists, the standard is **not frozen**: it is changed deliberately, on the record, and
> reversibly. The acting maintainer may decide any proposal — a number-moving one included — rather than wait for a
> body that has no founding date. Three things make that safe enough to do without the group, and they are mechanical,
> not a matter of good behaviour:
>
> 1. **Every decision is recorded** in `docs/decisions/`, with the proposer's and decider's conflict stated, the
>    reasons written, and a note that it was taken under this interim rule.
> 2. **The comment period runs after adoption**, not before: 30 days for a change that moves a number, 14 otherwise.
>    An unresolved objection from a commenter **not affiliated with the proposer** reopens the decision. Nothing is
>    accepted in a way that cannot be reopened.
> 3. **The steering group may revisit any interim decision** when it exists, and this rule asks it to. An interim
>    decision is adopted as the working rule, not as a settled one.
>
> The protections of the mechanical guarantees below — a score-moving change mints a new, immutable rubric version;
> nothing scores in secret; the licences permit a fork — hold throughout, so a disputed decision can always be read,
> traced and, failing persuasion, forked.

And, in step 4 of "How a change to the standard is decided", replace "Until the steering group exists, the acting
maintainer decides — and may only **accept** a proposal that moves a score if no unresolved objection remains from a
commenter who is not affiliated with the proposer; otherwise the proposal waits for the steering group." with:

> Until the steering group exists, the acting maintainer decides, under the interim pragmatic rule above: the decision
> is recorded with its conflict and reasons, the comment period runs after adoption, and an unaffiliated objection
> reopens it. A proposal is no longer made to wait for the group.

## Effect on published scores

None. The procedure changes; no dimension, weight, formula, band or key changes, so no published score moves. What
changes is that number-moving proposals can be *decided* now (each then taking effect only through its own rubric
version and comment period), instead of waiting indefinitely.

## Alternatives considered

1. **Keep the standstill, wait for the group.** Rejected: open-ended, defeats the standard's purpose, and is already
   not the practice (decision 0006).
2. **Relax it only for number-neutral changes.** Rejected: 0008 and 0010 carry benchmark and advisory effects; a rule
   that still freezes the evidenced improvements is the same problem in a smaller box.
3. **Appoint a provisional reviewer instead of relaxing.** Kept as a goal, not a blocker: bar 2's "second maintainer
   with no tie" remains what turns a merge into a review, and the interim rule's post-adoption comment period is the
   place an unaffiliated voice enters. It should not gate improvements while no such person has been found.

## Questions for the decider

1. Does the post-adoption comment period reopen a decision on **any** unaffiliated objection, or only a sustained one
   the decider cannot answer? (Proposed: any unresolved one, decided in a follow-up decision.)
2. Should the interim rule carry a review date, so it is not forgotten if the group is slow? (Proposed: the acting
   maintainer re-states it still applies at each year's turn, in `GOVERNANCE.md`.)
