# Security

## Reporting a vulnerability

**Do not open a public issue.** Use GitHub's private advisory form:

<https://github.com/code-assurance-initiative/CodeAssuranceIndex/security/advisories/new>

You should get an acknowledgement within a few days. If you do not, the project is small enough
that the likeliest explanation is that it was missed — say so again, publicly if necessary, and
that is a fair thing to do.

## What counts

The interesting surface is not the website. It is:

- **The verifier.** Anything that makes `DeliveryVerifier` accept a package it should refuse — a
  forged or replayed signature, a payload whose headline does not follow from its own evidence, a
  rubric substituted for the one the package names. A verifier that can be fooled is worse than no
  verifier, because it is trusted.
- **The registry.** Reading a signed delivery you were not granted, or writing one you do not own.
- **The fold.** Any input that makes the scorer produce a different number for the same evidence
  and rubric. Determinism is a security property here, not just a design goal: it is what lets a
  third party check a published claim.

## What does not count

- A score you disagree with. That is [CHALLENGE.md](docs/CHALLENGE.md), and it is a real route.
- Rate limits on the public API. They are deliberate and documented.
- The sample signing key in `examples/`. It is a fixture, production does not trust its key id,
  and it is published on purpose so the example can be reproduced.

## Supported versions

The standard is pre-1.0 and currently held still while governance is formed
([GOVERNANCE.md](docs/GOVERNANCE.md)). Fixes land on `main`; there are no maintained release
branches yet.
