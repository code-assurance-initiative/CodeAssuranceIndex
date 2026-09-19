# Contributing

CAI is an open standard with a reference implementation. Both live here, and they take
contributions differently — so the first question is which one you are changing.

## Which thing are you changing?

**The reference scorer, the CLI, the registry, the site** — ordinary code. Open a PR.

**The standard itself** — the rules that decide a number. Open an *issue* first, making the case.
A rule change is not a code change: it moves every score already published under the version it
lands in, which is why score-moving changes mint a new rubric version
([ADR-0004](docs/adr/0004-versioned-frozen-rubrics.md)) and why the standard is currently **held
still** while its steering group is formed ([GOVERNANCE.md](docs/GOVERNANCE.md)). A PR that changes
a rule without that conversation will be asked for the conversation, not merged.

**A finding you think is wrong** — that is the implementation's problem, not the standard's.
[CHALLENGE.md](docs/CHALLENGE.md) has the routes.

## Getting it running

```sh
dotnet build Cai.slnx
dotnet test Cai.slnx          # the suite is fast; run it before you push
dotnet run --project src/Cai.Cli -- score examples/evidence.sample.json
```

## What a good PR looks like here

This codebase has an unusual convention and it is worth knowing before you write a comment:
**comments explain WHY, and they are often long.** A comment that restates the code is noise, but
one that records the defect a guard exists for — what went wrong, when, and what it cost — is the
reason the guard survives the next person who thinks it is unnecessary. Read a few files before
writing; the house style will be obvious.

Beyond that:

- **A test that fails without your change.** For a bug fix, write it first and watch it fail —
  a test that passes either way documents nothing.
- **Determinism is not negotiable in the fold.** No wall-clock, no randomness, no ambient I/O in
  `Cai.Scoring` ([ADR-0002](docs/adr/0002-deterministic-reproducible-scoring.md)). Same evidence
  plus same rubric must give the same number on anyone's machine, forever.
- **Say what you did not do.** A PR that names its own gaps is easier to trust than one that
  implies completeness.

## Architecture decisions

Anything that changes the shape of the system gets an ADR in `docs/adr/`. Read
[ADR-0001](docs/adr/0001-record-architecture-decisions.md) for the format. They are immutable once
accepted: a decision that is revisited gets a new ADR that supersedes the old one, rather than an
edit that erases what was previously believed.

## Security

Do not open an issue. See [SECURITY.md](SECURITY.md).
