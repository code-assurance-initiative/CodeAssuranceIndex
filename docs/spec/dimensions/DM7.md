# DM7: Repository granularity

**Lens:** domainModelling · **Scored:** yes

## Property
Whether repositories are per aggregate root (not per child entity) so the root's invariants can't be bypassed.

## Applies to
Any programming language that declares named types (classes, structs, records, interfaces, traits or protocols), for example C#, Visual Basic .NET, Java, Kotlin, Scala, Go, TypeScript, Rust, Dart and Swift. It does not apply to markup (HTML) or styles (CSS).

## Population
- Production code as [production-code.md](../production-code.md) defines it. This dimension adds no exclusions of its own.
- **The tactical-style rule.** The code's domain model must be in the tactical domain-driven design style. That means it declares domain types in that style: aggregates, entities, value objects or domain events, for example through a base type, a marker or an equivalent declaration. If it does not, the dimension is not measured (see below).
- A codebase in that style that declares no aggregate root is still measured. No repository in it can be over a child entity, but the query-handle condition still applies.
- The repositories of the domain model are in scope. A repository is a type whose job is to load and save one kind of domain object for the rest of the code. Two forms count:
  - a repository contract (an interface, trait or protocol);
  - a repository class that has no contract of its own.
- Out of scope:
  - stores whose signatures name no domain entity, for example read-model, projection or reporting stores;
  - storage engines and infrastructure stores that are not repositories of the domain model, for example a transaction or connection interface, or a database driver's own types.

## Unit and denominator
- **Unit:** a repository of a domain type. All the repository contracts and implementations over the same domain type count as one repository. For example, `IOrderRepository` and the class that implements it are one repository. If a repository's domain type cannot be named as exactly one entity, that repository counts as a unit on its own.
- **The domain type a repository is over** is the entity it loads and returns. If it returns no entity, it is the entity it accepts.
- **A repository counts as a violation** if either of these holds:
  1. **It is over a child entity.** The domain type is an entity that is not an aggregate root and that an aggregate root owns. "Owns" means the root holds the entity in a field or property and either creates it or is the only way to create it. For example, an `OrderLineRepository` next to an `Order` that holds and creates its `OrderLine`s. An entity declared as an aggregate root (by its base type or a marker) is never a child.
  2. **It hands the store's query machinery to callers.** A public member of the repository returns a live, composable query over the store instead of aggregates. A public member is a member of the repository's contract, or a public method, property or field of a repository class. A member that returns such a query asynchronously counts too. For example `IQueryable<T>` or `DbSet<T>` in .NET, or a document store's collection or query object. A sequence that has already been materialised (a list or enumerable of aggregates) is not a query handle.
- A repository counts as a violation at most once, even when both conditions hold.
- **Denominator:** the number of repositories (units) in the population.

## Thresholds
- A repository can be over a child entity only if the codebase declares at least one aggregate root.
- A repository with no violation is clean. A repository gets no partial credit: it either violates or it does not.

## Score
Let *n* be the number of repositories and *v* the number of repositories that violate.

- If *v* = 0, the score is **10**.
- Otherwise:
  - density *d* = min(*v* / *n*, 1)
  - isolation = max(0, 1 − *d* / 0.10)
  - headroom = min(1, max(0, log10(*n*) / log10(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − *d*) × (ceiling − 1), clamped to [1, ceiling] and rounded to 2 decimals

Any violation therefore caps the score at 8. The cap falls to 6 once violations reach 10 % of repositories, or when there is only one repository. As more repositories violate, the score falls toward 1.

## Not measured when
- The code's domain model is not in the tactical domain-driven design style as the tactical-style rule under Population defines it. The dimension is then absent, not zero.
- The population holds no repository (*n* = 0), for example because the code uses a document session or the persistence framework directly. The result is "not applicable".

## What the result is not
- It does not say whether the aggregate boundaries are drawn well. It only checks whether repositories respect the boundaries the code declares.
- It does not judge how a repository works inside. A repository class may build queries internally. Only what crosses its public surface counts.
- It is not a count of repository types. Several types over one domain type count as one repository.
- A high score does not mean other code never reaches child entities. Code that bypasses repositories entirely is not checked here.
- It does not judge read-model or reporting stores.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Rot density:** the ceiling's isolation term reaches 0 at a violation density of 0.10.
- **Population headroom:** the ceiling's headroom term reaches 1 at 200 repositories.
- **Ceiling band:** a score with any violation has a ceiling between 6 and 8.
