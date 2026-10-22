# The producer listing

A survey page on codeassuranceindex.info can link the public reports written about its measurement, under
the mark of the system that wrote each one. [Decision 0003](../docs/decisions/0003-survey-pages-link-the-producers-public-report.md)
set the rules; [`producers.json`](producers.json) is the list those rules read.

## What a listing entry does

A delivery may carry `reports`: a list of `{ url, system }` (package MINOR 1.3,
[spec](../docs/spec/cai-delivery-package.md)). A survey page shows a report only when all of these hold:

- the delivery's `producer.name` is listed here as `producer`;
- the report's `system` is one of that producer's `systems`;
- the report's `url` is `https`, on the system's `homepage` host or a subdomain of it.

Anything else in a delivery is ignored by the page, and the producer is still named in plain text as before.
The cards' wording is the standard's and is the same for every producer. A producer cannot change it.

| Field | Meaning |
|---|---|
| `producer` | The authenticated producer name the registry stamps into every delivery. |
| `name` | How the page names the producer: "Measured by {name}". |
| `homepage` | Where the "Measured by" card links. `https`. |
| `mark` | The mark the site draws on the card, as a name the site knows (today: `watchdog`, `assay`, `cai`, `github`, `gitlab`, `html`, `doc`). An unknown name draws a plain document. Adding a new mark is a change to the site. |
| `interests` | What the producer declares, as a proposal would. |
| `systems` | The systems this producer may name as the author of a report: `id` (what a delivery names), `name` ("The full report by {name}"), `homepage` (the host a report URL must be on) and `mark`. |

## Being listed

Open a pull request that adds or changes an entry. A maintainer merges it. An entry that is missing a field
fails the test suite rather than printing an empty card.

A listed producer takes on the guidance in [ENGINE-QUALIFICATION.md](../docs/ENGINE-QUALIFICATION.md#publishing-your-reports):
publish reports for open-source subjects, publish for other code only with the owner's recorded consent,
keep unfixed security detail and individual contributors out of a public report, and never let publication
change a score.
