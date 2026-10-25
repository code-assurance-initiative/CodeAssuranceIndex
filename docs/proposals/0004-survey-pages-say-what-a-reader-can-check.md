# Proposal 0004: A survey page promises only the checks a reader can make, and public surveys publish their evidence

- **Number:** 0004
- **Filed:** 2026-10-26
- **Proposer:** Jimmy Borch, for the CAI team
- **Declared interests:** the proposer maintains Watchdog, today the only producer whose surveys this site publishes.
  Part B below asks producers to publish more of their evidence, which costs Watchdog work and exposes its findings
  to scrutiny; Part A changes a card that today flatters every producer's number. Neither part changes a score.
- **Moves scores:** no. No rubric, dimension, weight or scorer rule changes.
- **Status:** Proposed

## Problem

Discussed in [issue #30](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/30).

Every survey page carries a card, "Verify this score yourself", with the note "Reproduce the number from the
published evidence", linking to /verify/ (`src/Cai.Pages/SurveyPageBuilder.cs`, the links block).

That is not true for a reader without an account, checked on 2026-10-25 against the AfterLutz/Akkatecture survey
(commit 3b948b48, rubric-2026.10.5, headline 57.8):

1. The page publishes the headline, the commit, the rubric version and the eight lens scores. It does not publish the
   per-dimension evidence the lens scores were folded from.
2. /verify/ checks a delivery the reader already holds. It offers nothing to download for a public survey.
3. The registry serves deliveries only to their owner and the parties they grant (`/api/registry/deliveries*`
   require authorisation; only `/keys` and `/health` are open).

So a stranger can recompute the last step, lens scores to headline (the reference CLI folds Akkatecture's eight
published lens scores to 57.8; an independent script gives 57.8455), but not the step the standard exists for,
evidence to lens scores. The buyer of a delivery can do both, offline. The card says something only the buyer can do,
on a page written for everyone.

## Change

**Part A, now.** Replace the card's note.

- Replaced: "Reproduce the number from the published evidence"
- With, while a survey's evidence is not public: "Re-fold the headline from the lens scores below. The evidence
  behind them is checkable by the holder of the delivery."
- With, once a survey's evidence is public (Part B): "Reproduce the number from the published evidence", linking the
  evidence file and the command.

**Part B, as a producer obligation for public surveys.** A survey of a public repository that this site publishes
carries its evidence package (the per-dimension evidence and the rubric version, the same document a delivery holds
before signing), served beside the page, so that `cai score` over it reproduces the page's headline. A producer that
does not supply it may still be listed; its pages then carry Part A's wording. Private deliveries are unchanged.

## Effect on published scores

None. Part A changes one sentence; Part B adds a file beside a page. No number is recomputed.

## Alternatives considered

- **Leave the card and publish evidence first.** Leaves a false sentence live until every producer delivers, which
  could be never.
- **Remove the card.** Hides the check a reader can make today (lens scores to headline), which is worth keeping.
- **Publish evidence for private deliveries too.** Out of scope: the delivery is the buyer's.
