# Decision 0007: Producers are named by their products, not by Watchdog

- **Decision:** 0007
- **Date:** 2026-11-04
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, and today the whole team, until the steering
  group exists: [GOVERNANCE.md](../GOVERNANCE.md))
- **Proposal:** [0007: Producers are named by their products, not by Watchdog](../proposals/0007-producers-are-named-by-their-products.md)
  ([pull request #37](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/36))
- **Outcome:** Accepted as written. The proposal carried no open questions: the proposer put three to the products' owner
  before filing, and the change already carries the answers.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who also runs Canine Development, whose products and engine produce CAI
evidence and whose code CAI measures. Nobody else could decide, so nobody recused.

The decision changes names only. It moves no score, changes no rule a scorer applies and edits no published delivery, so it
cannot favour the decider's products in any measurement. The steering group, once it exists, can confirm or replace it.

## Comment

None was taken. The proposal was filed and accepted on the same day, without the 14-day comment period that
[GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided) sets for a change that moves no number. That is a
departure from the published process, recorded as one, as decisions 0001 to 0006 were.

Why it was accepted anyway: it moves no number, the names it replaces point readers at a product that no longer exists and at
hosts that serve no data, and every published record stays as signed. Objections remain welcome: anyone may file a proposal to
amend or replace this decision, and it will be decided under the full comment period.

## Reasons

- A producer is who made a claim, so a reader must be able to go to it. `watchdog.canine.dev` names a retired product on a host
  that serves a notice page.
- One engine measures for every product. Writing a product or document label in the scanner field made it say something other
  than which engine measured; `cca` on every delivery says exactly that.
- A signed publication is not edited. What Watchdog produced stays Watchdog's, as signed. A successor is a company's own
  matter, not the standard's, so no note ties Watchdog's records to another product.

## What took effect

- Proposal 0007's status and the proposal and decision indexes.

## What did not yet

Each of these lands as its own pull request, linking this decision:

- In this repository: the default badge issuer `cca` at `https://cca.canine.dev`, with `watchdog` kept as an alias for good;
  Watchdog listed as a retired producer; the "Get a survey" link, the glossary, the API reference and the specification
  examples naming the producers above; the HTTP client and setting renamed `cca` and `Cca:BaseUrl`, reading `Watchdog:BaseUrl`
  as a fallback for one release; the CLI's default producer.
- On the Initiative's site (codeassuranceindex.info): the implementations, badge and governance pages.
- On the producer's side, in kennel.canine.dev (noted, not adopted here): the producer name on new deliveries, `cca` as the
  scanner on every delivery, `cca` as the benchmark tool and the engine version's key.
