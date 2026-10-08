# Governance

**Status: being formed.** This document says what is true today, and then what the process is. Where the two
disagree, this file is wrong and should be corrected.

## Who decides what the standard says — today

Today: one person, publishing under the Code Assurance Initiative. The steering group that is meant to decide is
being assembled and does not exist yet, and the association that is meant to own the standard has not been founded
yet. Until the steering group exists, the standard is **held still**: CAI is not materially changed while the body
that ought to decide such changes is still being put together. Corrections that move no number (clarifications,
documentation, defects in the reference scorer that make it disagree with the published text) continue.

That standstill is the substitute for governance, and it is a weak one. It is written down so the weakness is
visible rather than papered over.

## What holds in the meantime

These are mechanical, and they work whether anyone behaves well or not:

- **A score-moving change mints a new rubric version.** The version a score names never changes underneath it, so the
  rules it was judged by can still be read ([ADR-0004](adr/0004-versioned-frozen-rubrics.md)).
- **Every score names its rubric version**, so a number that moves can be traced to the reason.
- **Nothing scores in secret.** A way of scoring that is not in the published rules is rejected before it can ship, so
  there is no private rule to appeal to.
- **The licences permit a fork.** Code under Apache-2.0 and the specification under CC BY 4.0 ([IPR.md](IPR.md)) mean
  disagreeing with the Initiative does not require its permission.

## How a change to the standard is decided

The same process applies now and after the steering group exists; only *who decides* at step 4 changes.

1. **Issue.** Anyone opens an issue describing the problem — not yet the solution. Defects in the published text, in
   the reference scorer, or in how a rule measures, are all welcome. Issues that are not about the rules (a finding in
   one survey, for example) are routed per [CHALLENGE.md](CHALLENGE.md).
2. **RFC.** A change to what the standard says — a dimension, a weight, a formula, a band, the qualification scheme,
   this process — is written as an RFC: a pull request adding `docs/rfcs/NNNN-short-title.md` from
   [the template](rfcs/0000-template.md). It states the problem, the change, what it does to published scores
   (measured, where it can be), and the alternatives considered.
3. **Public comment.** The RFC is open for comment for **at least 30 days** (14 for a change that moves no number).
   Comments happen on the pull request, where everyone can read them.
4. **Decision.** The steering group decides by simple majority of the members who are not conflicted (see below),
   with at least half of all members taking part. Until the steering group exists, the acting maintainer decides —
   and may only **accept** an RFC that moves a score if no unresolved objection remains from a commenter who is not
   affiliated with the proposer; otherwise the RFC waits for the steering group.
5. **Record.** Every decision is written into the RFC file: accepted or declined, by whom, the vote where there was
   one, and **the reasons**. A declined RFC stays in the repository with its reasons; a decline without written
   reasons is not a decision.
6. **Release.** An accepted RFC that moves a score ships only in a new rubric version, with the measured effect on
   published scores stated in its release note.

## The steering group (being formed)

Its purpose is to make step 4 a decision by several independent parties instead of one.

- **5 to 7 members**, each a named person representing a named organisation, elected for two years at a time.
- **No single organisation or group of companies holds more than one seat.**
- **Suppliers of measurement tools or of code measured by CAI hold at most one third of the seats.** The rest are
  public bodies and communities that buy and own software, and education and research.
- Members, their organisations and their declared interests are listed in this file.

The intended owner of the standard is an independent, non-profit association whose members elect the steering group.
Until it is founded, the steering group is appointed by invitation and says so here.

## Conflicts of interest

- **A member whose organisation supplies an engine** (a tool that produces CAI evidence) does not take part in
  decisions about engine qualification, and does not vote on an RFC that particularly affects its own engine. The
  recusal is recorded in the RFC.
- **Every declared interest is public**, here and in the RFC.
- The standard **does not own an engine.** The reference implementation is a separate community project
  ([open-assurance/reference-implementation](https://github.com/open-assurance/reference-implementation)) and is
  qualified like any other engine ([ENGINE-QUALIFICATION.md](ENGINE-QUALIFICATION.md)).

## The bar this page will be held to

The Initiative will not describe its governance as *independent* until all four are true:

1. The steering group exists, its members are named here, and no single company controls it.
2. How a change is proposed — and how one is refused — is published, with who decided and why. *(The process above
   is published; it has not yet decided anything under a steering group.)*
3. More than one organisation scores codebases under the standard, so it is not being written around a single
   product.
4. The rubric versions are published by the Initiative, not by an implementation.

**The fourth is the one that is furthest away, and it is the most important.** Rubric catalogs are currently minted
and pushed by one implementation, which is a commercial product. An implementation that writes the rules it is
measured by is precisely the conflict this page exists to be honest about. Until that changes, "open standard"
describes the licences and the published rules — not the decision rights.

## Disagreeing

Two different complaints, two routes — see [CHALLENGE.md](CHALLENGE.md).

- **A finding is wrong.** That is the implementation's to answer.
- **A rule is wrong.** That is the standard's. Open an issue, and if it needs a change to the rules, an RFC.

A standard is read most carefully by the people who did not write it, and that is the reading this one wants.
