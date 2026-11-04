# Proposal 0007: Producers are named by their products, not by Watchdog

- **Number:** 0007
- **Filed:** 2026-11-04
- **Proposer:** Jimmy Borch, for Canine Development ApS
- **Declared interests:** the proposer runs Canine Development, whose products (Assure, Assert, Assay) and whose engine (the
  Canine Code Analyzer, CCA) produce CAI evidence, and whose own code (kennel.canine.dev) is measured by CAI. This proposal
  renames that producer in the standard's pages and defaults. It changes no score and no rule.
- **Moves scores:** no. No catalog, weight, formula or band changes, and no published delivery is edited.
- **Status:** Accepted by [decision 0007](../decisions/0007-producers-are-named-by-their-products.md), 2026-11-04

## Problem

The product called Watchdog no longer exists. Since 2026-11-01 Canine Development sells one engine and the products built on
it: **Assure** (released now), **Assert** (2026-11-16, the product that was Watchdog), **Assay** (2026-11-30) and the **Canine
Code Analyzer**, CCA for short (2026-12-01). Every one of them is powered by CCA. CCA itself is released as a white-label
engine: anyone may build a product on it, as Canine Development does.

CAI still names Watchdog as its producer in its pages, its defaults and its examples, and several of those names point at
hosts that now serve a notice page:

| Where | What it says today | Kind |
|---|---|---|
| `src/Cai.Web/Badges/IssuerCatalog.cs` | the default badge issuer is `watchdog`, host `watchdog.canine.dev`, base `https://watchdog.canine.dev` | code default; the base URL no longer serves data, so a default badge cannot be drawn |
| `src/Cai.Web/Components/Layout/MainLayout.razor` | "Get a survey" links `https://watchdog.canine.dev`; the footer names it as a surveyor | page text, a dead link |
| `src/Cai.Web/CaiGlossary.cs`, `src/Cai.Web/Components/Pages/ApiReference.razor` | "a surveyor (watchdog.canine.dev)", "the Watchdog surveyor" | page text |
| `src/Cai.Web/Program.cs` | the named HTTP client `watchdog` and the setting `Watchdog:BaseUrl` | code and configuration names |
| `src/Cai.Cli/Program.cs` | `--producer` defaults to `watchdog.canine.dev` | CLI default |
| `docs/spec/cai-delivery-package.md`, `docs/spec/cai-registry.md`, `examples/cai-delivery.sample.json` | the producer `watchdog.canine.dev`, the scanner `watchdog-surveyor` | specification examples |
| the Initiative's site (codeassuranceindex.info): implementations, badge and governance pages | Watchdog as the one implementation and the one badge issuer | published pages |

On the producer's side (kennel.canine.dev), the names that reach CAI are:

| Where | Value | Reaches CAI as |
|---|---|---|
| `src/Kennel.Core/Scans/ScansModule.cs` (`Kennel:Scans:ProducerName`) | `watchdog.canine.dev` | the signed delivery's producer name |
| `src/Kennel.Core/Scans/Execution/DeterministicScanExecutor.cs` | `kennel-deterministic-scan` | the scanner on every survey delivery (the engine) |
| `src/Kennel.Core/Compliance/Report/ComplianceDeliveryService.cs` | `watchdog-compliance` | the scanner on a compliance report delivery (a document built from the same scan, not a scanner) |
| `src/Kennel.Procurement.Core/Governance/Application/EngagementDeliveryService.cs` | `watchdog-governance` | the scanner on a governance report delivery (likewise) |
| `src/Kennel.Core/Compliance/Contracts/DeliveryService.cs` | `watchdog-contract-delivery` | the scanner on a contract delivery (likewise) |
| `src/Kennel.Admin.Core/Admin/Services/CaiRunSubmission.cs` | tool `watchdog` | a benchmark submission's tool |
| `engine/src/CodeHealth.Cli/ToolVersionProbe.cs` | the engine's own version under the key `watchdog` | the tool versions a delivery records |

**Published deliveries are signed and cannot be edited.** Every delivery published so far carries `watchdog.canine.dev` as its
producer, and the signature covers it. They stay exactly as published.

**There is one scanner.** Every product measures through the same engine, CCA; Assure, Assert and Assay are presentations of
its measurement, not separate scanners. The scanner field today names the engine on a survey (`kennel-deterministic-scan`) and,
on three kinds of document built from the same scan, a product-and-document label (`watchdog-compliance`, `watchdog-governance`,
`watchdog-contract-delivery`) that says nothing about which engine measured.

## Change

1. **The producer is the product the customer used; the scanner is always the engine.** A delivery names its producer:
   `assure.canine.dev`, `assert.canine.dev`, `assay.canine.dev`, or `cca.canine.dev` for evidence produced through the engine
   directly (the public corpus, the benchmark, a product built on CCA that names no producer of its own). Its scanner is `cca`
   on every delivery, whatever document carries it, with the engine's version beside it as today. The kind of document (survey,
   compliance report, governance report, contract delivery) is not a scanner and is not written there. The benchmark tool is
   `cca`, and the engine's version is recorded under `cca`.
2. **"Powered by CCA" is a property of the scanner, not of the producer.** CAI shows the producer as the product a reader can
   go to, and the scanner as the engine that measured. A product someone else builds on CCA is a producer in its own right
   under its own name, with `cca-*` as its scanner; CAI gives it no different standing.
3. **What was published under Watchdog stays Watchdog's.** Watchdog is a producer in its own right, now retired: what it
   produced is shown as produced by Watchdog, exactly as signed, with no note linking it to any other product. Which product a
   company sees as its successor is that company's business, not the standard's. CAI lists Watchdog among its producers as
   retired. No delivery is re-signed, re-issued or withdrawn.
4. **The badge issuer.** The default issuer becomes `cca`, host `cca.canine.dev`, base `https://cca.canine.dev` (where the
   public corpus is served from 2026-11-04). The id `watchdog` stays accepted as an alias for `cca` for good, so every badge URL
   already embedded in a README keeps drawing.
5. **The Initiative's pages and examples** name the producers above instead of Watchdog: the "Get a survey" link goes to
   `https://assure.canine.dev`, the implementations page lists the four products and CCA, the glossary, API reference and
   specification examples use `assure.canine.dev` / `cca`. The HTTP client and setting are renamed `cca` and
   `Cca:BaseUrl`, with `Watchdog:BaseUrl` read as a fallback for one release.
6. **On the producer's side** (implemented in kennel.canine.dev once this is accepted, not by CAI): the defaults and ids in
   the second table above change to the values in point 1. The signed canonical string `watchdog-delivery-v1` and the enterprise
   licence format id do not change: they are inside signatures and licence files already issued, and a later format version
   takes the new name.

## Effect on published scores

None. No score, band, lens or rubric changes, and no published delivery is edited. Only producer and scanner names on
deliveries minted after the decision differ, and Watchdog is listed as a retired producer.

## Alternatives considered

- **Keep `watchdog.canine.dev` as the producer forever.** Rejected: it names a product that no longer exists, on a host
  that no longer serves data, and a reader cannot go anywhere from it.
- **One producer, `canine.dev`, for everything.** Rejected: CAI's reader needs to know which product made the claim, and a
  third party building on CCA would be indistinguishable from Canine Development's own products.
- **Re-sign the published deliveries under the new names.** Rejected: a signed publication is not edited, and re-issuing
  hundreds of them to change a name would make the record look rewritten.

## Questions for the decider

None left open. The proposer asked the owner of the products three questions before filing, and the change above carries the
answers: Watchdog's deliveries stay Watchdog's with no successor note; the `watchdog` badge alias stays for good; evidence produced
through the engine directly names `cca.canine.dev` as its producer.
