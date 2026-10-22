# Proposal 0003: Survey pages link the producer's public report, and producers are encouraged to publish one

- **Number:** 0003
- **Filed:** 2026-10-22
- **Proposer:** Jimmy Borch, for the CAI team
- **Declared interests:** the proposer maintains Watchdog, a commercial engine and today the only producer whose
  surveys this site publishes. This proposal would put a link to Watchdog's report, and a card naming Watchdog, on
  every survey page Watchdog produced. Those are the most visited pages the Initiative publishes, so Watchdog gains
  traffic and visibility from it directly. That is why the change below fixes the card's wording and shape in this
  repository, gives every producer the same card on the same terms, and lets no producer write any part of it.
- **Moves scores:** no. No rubric, dimension, weight or scorer rule changes. A survey's number is the same with or
  without a report, and the change below forbids making it depend on one.
- **Status:** Proposed

## Problem

Discussed in [issue #28](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/28).

A survey page shows a score, the dimension scores it folds from, and a route to reproduce the arithmetic. It does not
show the findings, and it gives no route to them or to the party that answers for them. The producer is named once,
as plain text in the provenance paragraph.

That leaves three things the standard already promises without a way to do them from the page:

1. **Disputing a measurement.** [CHALLENGE.md](../CHALLENGE.md) §3 says a finding that mis-measures someone's code is
   the issuer's to answer, through a route the issuer publishes. A maintainer who lands on their project's survey page
   cannot see which finding to dispute and is not told where to go.
2. **Judging the evidence, not just the arithmetic.** `cai verify` proves that a number follows from its evidence
   bundle. It cannot show whether the bundle is a fair reading of the code. A reader can judge that only from the
   findings: whether they are real, whether they point at code that matters, whether the noise is tolerable.
3. **Comparing producers.** [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md) will qualify engines on a sealed
   benchmark. Readers and the owners of measured code will also judge engines the everyday way: by reading what each
   one says about code they know. With one producer that matters little. With two it is how a reader tells them apart,
   and the standard should make that comparison easy rather than leave it to whichever producer advertises harder.

Until 2026-09-22 the pages carried this link, because the producer composed them. When the Initiative took the pages
over (`docs/plans/cai-owns-its-pages.md`), the new builder correctly stopped producers from choosing the routes out of
the page, and the report link went with that. Nothing in the delivery package could carry it back.

## Change

### 1. The delivery package carries an optional report link (package MINOR 1.2)

[`spec/cai-delivery-package.md`](../spec/cai-delivery-package.md) §3.1 gains one optional payload field, covered by the
signature like every other field:

| Field | Type | Notes |
|---|---|---|
| `report` | object \| absent | `{ url }`: where the producer's public report for **this delivery** can be read. Absent means the producer offers none. |

`report.url` must:

- be `https`, and readable without an account, a login or a payment;
- describe the same subject and the same commit as the delivery, not a moving "latest" view, so that the page's number
  and the report's findings are about the same code;
- stay readable for as long as the survey that links it is published.

`schemas/cai-delivery-1.0.schema.json` gains the field. Verifiers that do not know it ignore it, as §8 already
allows for a MINOR bump.

### 2. The survey page shows it, in the standard's words

When the newest delivery behind a survey carries `report.url`, the page's link cards gain two, composed by this
repository and identical for every producer:

| Card | Label | Note | Link |
|---|---|---|---|
| The report | "The full report by {producer}" | "Every finding behind this score, as {producer} published it" | `report.url` |
| The producer | "Measured by {producer}" | "The engine that produced the evidence, and where to dispute a finding" | the producer's listed home page |

`{producer}` is the producer's display name. The producer card's mark and home page come from a **producer listing**
held in this repository (`docs/producers/` or equivalent: display name, home page, mark, dispute route), added by pull
request like any other change here. A producer can therefore change neither the wording nor the destination of either
card from its own systems, and a mark that has not been listed is not shown.

When the delivery carries no `report.url`, neither card appears, and the page does not say that a report is missing.
Not publishing a report is a legitimate choice (see 3) and the page does not penalise it.

The publishing sweep checks that `report.url` answers before it builds a page. If it does not, the page is built
without the two cards and the sweep reports the failure. A card that leads to an error page is worse than no card.

### 3. Guidance to producers (new section "Publishing your reports" in ENGINE-QUALIFICATION.md)

> A producer **should** publish a public report for every survey of open-source code it delivers. The report is how
> readers check the evidence behind a CAI, how the owners of measured code find what to dispute and what to fix, and
> how readers compare producers. A producer that publishes reports helps all three.
>
> A producer **may** publish a report for code that is not open source **only with the recorded consent of the
> organisation that owns the code.**
>
> A public report **must not**:
> - disclose a security finding that is not yet fixed in a form that helps someone exploit it. Report it as a count, a
>   category or a severity, and disclose the detail through the project's own security policy;
> - identify individual contributors beyond what the project's own public history already shows. For code that is not
>   open source, it identifies no individual at all.
>
> Publishing or withholding a report never changes a score, and a producer must not offer a different score, or a
> different measurement, in exchange for publication.

This is guidance (a "should"), not a condition of publishing a CAI. Under [ADR-0003](../adr/0003-free-paid-firewall.md)
the deterministic number and its evidence are open; the survey's judgement and advice are the producer's product. This
proposal leaves that line where it is: it does not require a producer to give its product away. It asks producers to
publish, explains why, and gives those that do a fair place to be found.

## Effect on published scores

None. No catalog, rule or scorer changes. Pages change: surveys whose newest delivery carries `report.url` gain two
link cards. Deliveries already issued are signed and immutable and do not carry the field, so a survey gains the cards
at its producer's next delivery for it, not retroactively.

## Alternatives considered

- **Require producers to publish reports.** Rejected: it moves the line ADR-0003 draws and turns a producer's product
  into a condition of the standard. It would also force a report for code whose owner has not agreed to one.
- **Let the producer supply the cards, or a free-form link list, in the delivery.** Rejected for the reason the pages
  moved here: a producer that writes the routes out of the standard's pages can point its readers anywhere, including
  at a sales page. One field with fixed rules gives the reader the report and nothing else.
- **A producer-level URL pattern held in the listing, instead of a per-delivery field.** Simpler, and it would light up
  every existing survey at once. Rejected because nothing then ties the report to the delivery: a pattern resolves to
  whatever the producer serves today, which may be about a later commit than the number on the page. A signed,
  per-delivery link is a claim about this measurement.
- **Name the producer as text only (today).** It identifies the producer but does not help a reader reach the
  findings, or the place to dispute them, which is the problem in issue #28.
- **Host the findings on this site.** Rejected: the Initiative would then publish, and appear to stand behind, a
  producer's judgement. The report is the producer's, and the producer answers for it.

## Questions for the decider

1. **Is "should" the right strength for open-source subjects?** The proposer's view: yes for now. A stronger rule
   belongs with engine qualification, decided by the steering group, once there is more than one producer to apply it
   to.
2. **Where does the producer listing live, and who may add to it?** The proposer suggests `docs/producers/<name>.md`,
   added by a pull request from the producer and merged by a maintainer, with the same declared-interest line as a
   proposal.
3. **Should the sweep's reachability check also verify that the report names the delivery's commit?** It would catch a
   report that drifted to a later commit, but it needs a machine-readable marker in the report. The proposer suggests
   leaving it out of this proposal and adding it later if drift is seen.

## What Watchdog will do on its own side (noted, not part of the proposal)

If accepted, Watchdog will send `report.url` on each delivery for a published open-source survey, pointing at the
run-specific public report (`/api/oss/{owner}/{name}/report?run={runId}`) with the owner and name in the forge's own
casing. It will send nothing for private code unless the owner has opted in to a public report. Watchdog's public
report already removes security findings, and it will be checked against the contributor rule above before the first
delivery carries the field.
