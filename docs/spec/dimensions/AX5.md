# AX5: Architecture & structure

**Lens:** architecture · **Scored:** yes

## Property

Whether the codebase has a recognisable, scale-appropriate structure (a named architectural style, or modular enough for its size) rather than being an ad-hoc ball of mud.

## Applies to

- **Recognised styles, T and S:** production code in any language that declares types (classes, interfaces, structs, records, enums, modules) in namespaces or their equivalent (for example packages, import paths or module nesting).
- **P, L and the size and modularisation judgement:** C# and Visual Basic .NET build projects that list what they compile (for example `.csproj` projects).
- **The rules-kept check:** C# only.

## Population

This page reads production code as [production-code.md](../production-code.md) defines it.

A **production project** is a C# or Visual Basic .NET build project that holds production code. Test projects, benchmark projects and projects of vendored code are not production projects. Generated files do not count toward any project's lines or any part of a module.

The module-proportion check and the rules-kept check also leave out:

- projects that are samples, examples, demonstrations or templates;
- the repository's own developer tools and scripts.

## Unit and denominator

The unit is one verdict for the whole repository. The verdict is built from these properties:

- **L:** the production lines (as [production-code.md](../production-code.md) counts them) of the files the production projects compile.
- **T:** the number of types declared in production code, in every language under Applies to.
- **P:** the number of production projects.
- **S:** the number of distinct namespaces (or their equivalent) that hold at least one of those types.
- **Recognised styles:** zero or more of the styles below, read from those types and projects.

The styles are:

- **DDD-layered:** the code uses domain-driven design's tactical patterns (for example aggregates, entities and value objects with behaviour). It also has both a domain layer and an infrastructure (persistence or data-access) layer.
- **Clean / hexagonal:** the code has an application layer. It also has either (a) a domain layer and an infrastructure layer, or (b) a domain layer and explicit ports (use-case or input/output port interfaces, or a ports or boundaries area). An application layer is either an application or use-case area, or command and query handlers.
- **CQRS:** the code has command and query handlers.
- **Vertical slice:** the code is organised by feature, in one of two shapes:
  - **Feature areas:** at least 2 types sit in feature areas, and they make up at least 5 % of T. In addition, either a mediator dispatches the slices, or the code outside the feature projects has no domain-plus-infrastructure layering.
  - **Project per slice:** each feature or bounded context is its own production project under a features, slices, modules or contexts area. There are at least 8 such projects, or P is at least 4 and at least 50 % of the production projects are such projects.
- **Transaction script:** all of these hold:
  - no other style is recognised;
  - the code does not use domain-driven design;
  - it has a data-access layer or uses a data-access library;
  - it has at least 2 service classes, each with at least one public method.

DDD-layered and clean / hexagonal are the **layered** styles.

There is no denominator. The score is a discrete verdict, not a share.

## Thresholds

**Structure needed (N).** N = min(10, L / 5000 + T / 100), rounded to one decimal.

**Modularised.** The code is modularised when any of these holds:

- a style is recognised;
- P is at least 4;
- S is at least 6 and S × 400 is at least L.

**Appears unstructured.** No style is recognised, the code is not modularised, and N (before rounding) is at least 4.

**Module out of proportion.** This check runs only when a layered style is recognised. It looks at the production projects that have production lines, leaving out the projects that Population excludes from this check. At least 2 such projects are needed. The largest one is out of proportion when all of these hold:

- it holds at least 70 % of those projects' production lines;
- it holds at least 5,000 production lines;
- it declares at least 40 public types;
- its types span at least 6 namespaces;
- it has at least 3 independent parts, each declaring at least 3 types.

To find the independent parts, group the module's types by the first namespace level below the module's root namespace. Types declared in the root namespace itself belong to no group. Groups joined by a reference, in either direction, directly or through other groups, form one part. So no part references another part. A part counts when its groups together declare at least 3 types.

**Rules kept.** This check runs only when a layered or vertical-slice style is recognised, and only on C#. It looks only at the production projects that Population keeps in for this check. A rule is broken at a site in any of these cases:

- **Layered: the core takes an infrastructure client.** A type in the domain, application, use-case or core layer takes, holds or creates an infrastructure client (for example an ORM context, a database client or an HTTP client). Code that wires up the application's services at start-up is exempt.
- **Layered: a domain project references an infrastructure package.** A domain or core project references an infrastructure package (for example an ORM, a database driver, a message-broker or HTTP client, or the web framework).
- **Layered: an endpoint returns a persistence entity.** A web endpoint returns an entity of the solution's database context, either directly or inside a result.
- **Vertical slice: thin endpoints over a shared service.** At least 3 slices have an entry point whose whole body is one call into the same shared service. Those calls reach at least 3 distinct methods of that service, and at least half of those methods branch, loop or throw. A **shared service** is a class or interface (not a record) of the repository's own code that:
  - lies outside every slice and feature area;
  - is not an infrastructure client;
  - declares at least 2 public methods.

## Score

The rules are applied in order. The first rule that matches decides the result.

1. **A style is recognised** (in any language under Applies to, with or without a production project): the score starts at 10.
   - If the largest module is out of proportion, the score becomes 6.
   - If any style rule is broken at one or more sites, the score becomes 7 or the current score, whichever is lower.
   - The number of broken sites does not change the score.
2. **No production project:** not measured.
3. **Appears unstructured and N (rounded) is at least 5.0:** 3.
4. **Appears unstructured and N (rounded) is below 5.0:** not measured.
5. **Otherwise** (the code is modularised, or N is below 4): 10.

The possible scores are 10, 7, 6 and 3.

## Not measured when

- No style is recognised and the repository has no production project.
- No style is recognised and the code is not modularised, but N is at least 4 and, rounded, below 5.0. At this size the code does not yet need an elaborate structure.

## What the result is not

- A 10 does not always mean a good architecture. It can also mean the code is small enough, or modular enough, not to need a named style. The key metric shows which case applies.
- It does not rank styles. Every recognised style scores the same.
- It does not judge whether the chosen style suits the product.
- It does not count dependency violations. One broken rule gives the same score as a hundred.
- A 3 does not prove disorder. It means that no style and no modularisation were found at a size that should have them.
- In a repository without C# or Visual Basic .NET projects, only a recognised style gives a score. Size and modularisation are not judged there.

## Pending re-definition

The values below are parameters of the existing reading's model, and today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Structure needed:**
  - the weights of 5,000 lines and 100 types per point;
  - the cap at 10;
  - the 4 at which code "appears unstructured";
  - the 5.0 that separates a score of 3 from not measured.
- **Modularised:** 4 projects; 6 namespaces; at most 400 lines per namespace.
- **Vertical slice:** the 5 % feature share and the minimum of 2 feature types; 8 slice projects, or 4 projects with a 50 % slice share.
- **Transaction script:** 2 service classes.
- **Module out of proportion:**
  - the 70 % share, 5,000 lines, 40 public types and 6 namespaces;
  - 3 independent parts of at least 3 types each;
  - the module's root namespace is the longest namespace prefix that holds at least 90 % of its types.
- **Thin slice entry point:**
  - an entry point has at most 3 statements, with no branch, loop, throw or other call into the repository's own code;
  - a shared service declares at least 2 public methods;
  - a thin-slice break needs 3 slices and 3 called methods, and at least half of those methods hold logic.
