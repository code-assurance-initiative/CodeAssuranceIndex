# Decision 0003: Survey pages link the producers' public reports

- **Decision:** 0003
- **Date:** 2026-10-22
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, and today the whole team, until the steering
  group exists: [GOVERNANCE.md](../GOVERNANCE.md))
- **Proposal:** [0003: Survey pages link the producer's public report, and producers are encouraged to publish one](../proposals/0003-survey-pages-link-the-producers-public-report.md)
  ([pull request #29](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/29),
  [issue #28](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/28))
- **Outcome:** Accepted as amended. The guidance to producers (change 3) is adopted as written. Changes 1 and 2 are
  amended in three ways, set out below: a delivery carries a **list** of reports, each naming the system that wrote it;
  the producer listing becomes a file with a host rule; and the standard does **not** probe report URLs. The
  proposal's three questions are answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who also maintains Watchdog. Watchdog is today the only producer,
so it is the only one whose report and name this decision puts on survey pages. Nobody else could decide, so nobody
recused.

The conflict runs in the decider's favour this time, unlike decision 0002's. What limits it is the shape of the change,
not the person deciding: the cards' words are fixed here and are the same for every producer, a producer can link only
a report on its own listed host, and any producer can be listed on the same terms by pull request. The steering group,
once it exists, can confirm or replace this decision, and this decision asks for that.

## Comment

None was taken. The proposal was filed and accepted on the same day, without the 14-day comment period that
[GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided) sets for a change that moves no number. That
is a departure from the published process, and it is recorded as one, as decisions 0001 and 0002 were.

Why it was accepted anyway: the change moves no number, it restores a route that survey pages carried until
2026-09-22 and lost by accident when the pages moved to this repository, and the route it restores is the one
[CHALLENGE.md](../CHALLENGE.md) §3 already promises readers. Objections remain welcome: anyone may file a proposal to
amend or replace this decision, and it will be decided under the full comment period.

## Reasons

- A reader can reproduce a CAI's arithmetic from the page, but cannot see the findings behind it or reach the issuer
  CHALLENGE.md sends them to. The report is where both are.
- `cai verify` proves the number follows from the evidence; only the findings show whether the evidence is a fair
  reading of the code.
- Publication stays a "should". [ADR-0003](../adr/0003-free-paid-firewall.md) makes the survey's judgement the
  producer's product, and this decision does not move that line.

## Amendments

### 1. A list of reports, each naming its system

The proposal gave a delivery one `report: { url }`. It now carries `reports: [ { url, system } ]`.

Why: the producer that measures and the system that writes a report up are not always the same thing. One engine may
measure, and several products built on it may each publish a report about the same scan; a second engine may measure
the same code and publish its own. A single link per delivery would force a producer to choose one, and a report with
no named author cannot be given an honest mark. Each entry names a `system` from the producer listing, and the page
shows a card per report, under that system's mark.

On a page with deliveries from more than one producer, each producer's **newest** delivery speaks for it. The page's
number is still the latest reading; a reader can reach every producer's findings, each dated by its own measurement.

### 2. The producer listing is a file, and a report must live on its system's host

The listing is [`producers/producers.json`](../../producers/producers.json), explained in
[`producers/README.md`](../../producers/README.md), changed by pull request. It names each producer (by the
authenticated name the registry stamps into deliveries), its display name, home page and mark, and the systems it may
name. A report is linked only when:

- the delivery's producer is listed;
- the report's `system` is one of **that producer's** systems, so no producer can borrow another's name or mark;
- the URL is `https` on the system's home host or a subdomain of it, so the mark on the card is true of where the card
  leads.

The mark is a name the site already draws. A new mark is a change to the site, not to a delivery.

### 3. The standard does not probe report URLs

The proposal had the publishing sweep check each report URL before building a page. That is dropped. The sweep runs
every hour over every published subject, so a probe would ask a producer to render about 3,000 reports an hour; and a
cache of the answers would be lost on every deploy of the registry, so cards would disappear and come back after each
one. Instead, keeping a linked report readable for as long as the survey is published is the producer's obligation,
stated in the guidance, and the URL describes the delivery's own commit by rule.

## Answers to the proposal's questions

1. **"Should" is the right strength for open-source subjects.** A stronger rule belongs with engine qualification and
   the steering group, once there is more than one producer to apply it to.
2. **The listing lives at `producers/producers.json`**, added to by a pull request from the producer and merged by a
   maintainer. Each entry carries the producer's declared interests.
3. **No machine check that a report names the delivery's commit.** Left out, as the proposal suggested; it can be
   added if drift is seen.

## What took effect

- Proposal 0003's status and the proposal and decision indexes.
- Delivery package **MINOR 1.3**: `payload.reports`, optional, signed, absent when empty
  (`src/Cai.Delivery`, `schemas/cai-delivery-1.0.schema.json`, [spec](../spec/cai-delivery-package.md) §3.1 and §8).
  Every package issued before it keeps verifying byte for byte.
- [`producers/producers.json`](../../producers/producers.json) and its README, listing Watchdog
  (`watchdog.canine.dev`) with one system, `watchdog`.
- `src/Cai.Pages/ProducerListing.cs` and `SurveyPageBuilder`: the report and "Measured by" cards, from each producer's
  newest delivery, under the listing's words and marks.
- [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md#publishing-your-reports): "Publishing your reports", the
  guidance in change 3 as written, plus the producer's obligation from amendment 3.
- [CHALLENGE.md](../CHALLENGE.md) §3: the survey page is where the issuer's report is linked.
- Packages `Cai.Delivery` and `Cai.Scoring` `0.2.0-ws-k`. `Cai.Scoring`'s source is unchanged from `0.2.0-ws-j`.
- Watchdog, on its own side (noted, not adopted): it sends `reports` with a run-specific link to its public report
  for subjects whose owners opted in to a public page, and re-files the published corpus so existing pages gain the
  card without a rescan.

## What did not yet

- **Marks for systems that do not exist yet.** The site draws marks for Watchdog, Assay and the standard. A producer
  listing a new system with a new mark needs the site to learn it; until then the card shows a plain document.
- **Third-party producers.** The listing rules apply to them, but none is listed.
