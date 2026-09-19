# Governance

**Status: being formed.** This document says what is true today, not what is intended. When the
two disagree, this file is wrong and should be corrected.

## Who decides what the standard says

Today: one person, publishing under the Code Assurance Initiative. The steering group that is
meant to decide is being assembled and does not exist yet. Until it does, the standard is **held
still** — CAI will not be materially changed while the body that ought to decide such changes is
still being put together.

That standstill is the substitute for governance, and it is a weak one. It is written down so the
weakness is visible rather than papered over.

## What holds in the meantime

These are mechanical, and they work whether anyone behaves well or not:

- **A score-moving change mints a new rubric version.** The version a score names never changes
  underneath it, so the rules it was judged by can still be read ([ADR-0004](adr/0004-versioned-frozen-rubrics.md)).
- **Every score names its rubric version**, so a number that moves can be traced to the reason.
- **Nothing scores in secret.** A way of scoring that is not in the published rules is rejected
  before it can ship, so there is no private rule to appeal to.
- **The licence permits a fork.** Apache-2.0 means disagreeing with the Initiative does not require
  its permission.

## The bar this page will be held to

The Initiative will not describe its governance as *independent* until all four are true:

1. The steering group exists, its members are named here, and no single company controls it.
2. How a change is proposed — and how one is refused — is published, with who decided and why.
3. More than one organisation scores codebases under the standard, so it is not being written
   around a single product.
4. The rubric versions are published by the Initiative, not by an implementation.

**The fourth is the one that is furthest away, and it is the most important.** Rubric catalogs are
currently minted and pushed by Watchdog, which is an implementation and a commercial product. An
implementation that writes the rules it is measured by is precisely the conflict this page exists
to be honest about. Until that changes, "open standard" describes the licence and the published
rules — not the decision rights.

## Disagreeing

Two different complaints, two routes — see [CHALLENGE.md](CHALLENGE.md).

- **A finding is wrong.** That is the implementation's to answer.
- **A rule is wrong.** That is the standard's. Open an issue making the case.

A standard is read most carefully by the people who did not write it, and that is the reading this
one wants.
