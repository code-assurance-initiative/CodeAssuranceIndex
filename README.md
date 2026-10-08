# CAI — the Code Assurance Index

**An open, reproducible 0–100 standard for the health of a C#/.NET codebase. Same code in, same score out.**

This repository is the home of the CAI standard: the website (codeassuranceindex.info) and — over time — the reference
implementation that lets anyone compute and verify the number.

The model is **method open, judgment separate**: the *standard* (how a codebase is measured) is open and free; an
independent, signed *survey* (the deductions and what to do) is the work of whoever issues it. Engines that produce CAI
evidence qualify against the standard ([docs/ENGINE-QUALIFICATION.md](docs/ENGINE-QUALIFICATION.md)); the standard owns
none of them. How the standard is governed and licensed: [docs/GOVERNANCE.md](docs/GOVERNANCE.md),
[docs/IPR.md](docs/IPR.md); the normative documents: [docs/spec/](docs/spec/); research and thesis topics:
[docs/RESEARCH-TOPICS.md](docs/RESEARCH-TOPICS.md).

## The standard

- One **0–100** score for a whole codebase, computed deterministically from evidence under a **frozen, versioned rubric**.
- Composed of ~120 dimensions grouped into **ten lenses** (five core, always on; the rest model-aware).
- **Reproducible**: same evidence + same rubric version ⇒ the same number. Falsifiable by design.
- A **measurement, never a certification** — the tool evidences the automatable slice; a named human attests the rest.
- **Contestable**: a number that won't reproduce is falsifiable proof, and every part of the method can be disputed
  through a documented path — see [`docs/CHALLENGE.md`](docs/CHALLENGE.md).

Bands: **Exemplary** 90–100 · **Strong** 70–89 · **Adequate** 50–69 · **Weak** 25–49 · **Critical** 0–24.

## Repository layout

```
src/          production code  — Cai.Scoring (the scorer library), Cai.Delivery (the signed delivery package),
              Cai.Pages (the survey and corpus pages the standard composes), Cai.Web (the site + JSON API
              host), Cai.Web.Registry (the delivery registry), Cai.Web.Noise (the Noise Standard),
              Cai.Cli (the `cai` tool) — one project per standard under a thin host (ADR-0011)
tests/        Cai.Tests        — xUnit suite: the scorer, the delivery format, the pages, the registry and the
              Noise Standard over HTTP, and the CLI end to end
benchmarks/   Cai.Benchmarks   — BenchmarkDotNet micro-benchmarks for the scoring hot paths
rubrics/      frozen, versioned rubric catalogs (owned and served by Cai.Web)
examples/     a sample evidence bundle for the CLI and calculator
docs/         architecture overview + Architecture Decision Records (ADRs)
deploy/       the verify-before-swap deploy notes
```

Everything builds and tests from one solution file: `dotnet build Cai.slnx` · `dotnet test Cai.slnx`.

## The reference scorer (`/src/Cai.Scoring`)

The open, reproducible half of the standard, in C# (Apache-2.0). Measuring a codebase into an **evidence bundle** is
the analyzer's job; **scoring** the bundle is open — the headline is a worst-first ordered-weighted average of the lens
scores (`Σ lensScore × owaWeight`), banded. Because the weights are published in the evidence, anyone can reproduce or
falsify a published number with no access to the engine.

```
dotnet test Cai.slnx                                                                          # the suite
dotnet run --project src/Cai.Cli -- score  examples/evidence.sample.json --rubrics rubrics
dotnet run --project src/Cai.Cli -- verify examples/evidence.sample.json --rubrics rubrics             # ✓ reproduced
dotnet run --project src/Cai.Cli -- verify examples/evidence.sample.json --rubrics rubrics --expect 90 # ✗ mismatch (exit 1)
```

`Cai.Scoring` is the library (bands, lenses, evidence bundle, the fold); `Cai.Cli` is the `cai` tool. `--rubrics` is
required: a CAI number is only meaningful under the rubric it was folded with, so the CLI folds under the catalog the
evidence names, read from the published archive. `Cai.Tests` covers determinism, banding, the fold and verify. The evidence-bundle format + the algorithm are documented at
[codeassuranceindex.info/spec](https://codeassuranceindex.info/spec.html#evidence).

## The app (`/src/Cai.Web`)

codeassuranceindex.info is a hosted ASP.NET Core + Blazor app — it OWNS the rubric catalogs and serves them, the scorer, and the
content over one origin:

| Route | What it is |
|---|---|
| `/` | The definition + the Open · Verifiable · Referenced trinity + the bands. |
| `/spec` | The open algorithm (dimensions → lenses → CAI), versioned with the rubric-version picker. |
| `/lenses` | The ten lenses — generated live from the catalog. |
| `/dimensions` | The full ~124-dimension catalog, by lens + rubric version. |
| `/calculator` | Paste an evidence bundle → CAI + lens roll-up (and verify the claim if present). |
| `/verify` | The proof engine — reproduce a published number from its evidence. |
| `/registry` | The public record of signed surveys. |
| `/badge` | The badge + the honest mark-usage policy. |
| `/api-reference` | The rubric + scoring API (what producers call). |
| `/api/rubrics`, `/api/rubrics/{v}/catalog`, `/api/score`, `/api/verify` | The JSON API (rate-limited; loopback exempt). |

Run locally: `cd src/Cai.Web && Rubrics__Root=$(pwd)/../../rubrics dotnet run` → http://localhost:5000. It's deployed
as a systemd service behind nginx — see [`deploy/DEPLOY.md`](deploy/DEPLOY.md).

## License

The standard and this content are Apache-2.0 (see `LICENSE`).
