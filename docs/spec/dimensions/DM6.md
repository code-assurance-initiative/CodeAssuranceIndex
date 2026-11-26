# DM6: Domain ↔ infrastructure boundary

**Lens:** domainModelling · **Scored:** yes

## Property
Whether the domain layer (the code that holds the business rules) stays free of infrastructure dependencies, for example persistence, database or cache clients, HTTP clients, web-framework or message-bus types: the clean-architecture dependency rule, that inner domain code never depends on outer infrastructure code.

## Applies to
Any language that declares types or modules (classes, records, structs, enums, modules, as the language defines them), and the package or project manifests that declare a domain project's dependencies. Markup (HTML) and styles (CSS) are not read.

## Population
The population is drawn from production code as [production-code.md](../production-code.md) defines it. It is the repository's **domain units**:

- **Domain types.** A type is a domain type when any of these holds:
  - **Placed in the domain.** The authors put it in a part of the code they name as the domain. For example, a project, package, namespace, module or folder has a `Domain` segment.
  - **Domain by what it does.** It decides business rules over the repository's own entities, wherever it lives.
  - **Persistence-mapped.** A persistence framework maps it to stored rows or documents. Examples are an ORM entity, an active-record model or a document-mapped class.
- **Domain modules.** In languages where a module file, not a type, holds the code (JavaScript, TypeScript), each source file placed in the domain is one domain unit.

One rule wins over all of these: **code in a part the authors name as infrastructure is never in the domain.** For example, a type in an `Infrastructure` namespace or folder is out, even when it is persistence-mapped or decides business rules.

Also out of scope, in addition to what [production-code.md](../production-code.md) excludes: presentation types (for example view models and UI widgets) that a persistence framework does not map.

## Unit and denominator
**Infrastructure** is code from outside the domain in one of these categories:

- **Persistence:** clients for databases, ORMs, document stores, object storage and event stores, and their types, and a platform's global storage API (for example browser local storage). This includes mapping attributes that only tell a store how to key, version or concurrency-check a row (for example a key or timestamp attribute). Validation attributes are not included.
- **HTTP and web:** HTTP clients, web-framework types, remote-procedure-call clients, and a platform's global network API (for example the global `fetch`).
- **Messaging:** message-bus and broker clients and their types.
- **Cache:** cache clients and their types.
- **The repository's own infrastructure:** a type the repository declares in a part it names as infrastructure.
- **Derived infrastructure:** a type whose base type, at any depth, is infrastructure. An example is the repository's own database context deriving from an ORM's context type.

Logging, configuration, dependency injection and validation are not infrastructure for this dimension.

**Unit: a dependency site.** Each of these is one site:

1. **A line of domain code that uses infrastructure.** This is a line in a domain unit that names an infrastructure type or module. It can name it in a field, property, parameter, return type or constructor (including a primary constructor). It can also name it in a base type or directly implemented interface, an attribute, an import (including a `require` or dynamic import), a re-export, an object creation or a call. Naming it as a generic argument or as the generic type itself counts (for example a database set of `Order`). Several references on one line of one type count as one site. Interfaces a type only inherits through a supertype do not count.
2. **A declared infrastructure dependency of a domain project.** A project or package the authors name as the domain may declare in its manifest that it depends on an infrastructure package or web framework. Each such declared dependency is one site, even when no code names it.
3. **A fused persistence-mapped type.** This is a type that is in the population only because it is mapped, and that carries domain behaviour on its own declaration. Domain behaviour here means at least one method of its own, other than a constructor, getter or setter, that changes or decides over its own state (the active-record shape). The type counts as one site. Its mapping (its persistence base type and its mapping attributes) is not a site in itself. A mapped type that only holds data, associations, validations or queries has no site of this kind, but it stays in the denominator.

A type placed in the domain that is also persistence-mapped is read under 1. Its persistence base type and its mapping attributes are sites.

Code declared in a type's own namespace or package is never an infrastructure dependency of that type. For example, domain types inside an infrastructure library's own repository are not charged for living in that library's namespace. Their base types and members still count.

**Numerator:** `v`, the number of dependency sites. One domain unit can add several sites.

**Denominator:** `n`, the number of domain units in the population.

## Thresholds
- One dependency site counts as one violation.
- Density is `d = min(v / n, 1)`. Because one unit can hold several sites, `v` can exceed `n`. Density is then capped at 1.

## Score
- `v = 0`: the score is **10**.
- `v ≥ 1`:
  - isolation `i = max(0, 1 − d / 0.10)`
  - population headroom `h = clamp(log10(n) / log10(200), 0, 1)`
  - ceiling `c = 6 + 2 · i · h`
  - score `= 1 + (1 − d) · (c − 1)`, kept between 1 and `c`, rounded to 2 decimals.

A single site scores at most 8.0, and only in a large domain that is otherwise clean. The ceiling is 6.0 at a density of 10 % or more, and in a domain of one unit. At a density of 100 % or more, the score is 1.0.

Worked examples: 1 site in 200 units gives 7.87. 1 site in 10 units gives 5.5. 5 sites in 10 units gives 3.5. 15 sites in 10 units gives 1.0.

## Not measured when
The repository has no domain units (the population is empty). An empty population is not reported as 10.

## What the result is not
- It does not see infrastructure reached at run time without being named in the code. Examples are reflection, a service locator keyed by string, and a call through a domain-owned port whose adapter lives elsewhere. The last one is the intended design.
- It is not a count of violating types. A type with ten lines that use infrastructure weighs ten times as much as a type with one.
- It does not judge whether the domain layer is well bounded or rich. It only checks whether the domain depends on infrastructure.
- Wrappers around infrastructure:
  - A wrapper that lives in the domain is itself a domain unit with sites.
  - A wrapper in a part the authors name as infrastructure is infrastructure, so domain code that names it has sites.
  - A wrapper that lives in neither (for example in a shared utilities folder) is not seen. It is outside the population, and domain code that uses it is not charged.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each one with a threshold on the property.

- **Density at which isolation ends:** 10 % (the `0.10` in the isolation term).
- **Population at which headroom saturates:** 200 domain units.
- **Ceiling band:** 6.0 for any violation, raised by up to 2.0 for an isolated violation in a large population.
- **Floor:** 1.0.
- **Counting per line:** several references on one line of one type count as one site. Counting per line, not per dependency, is part of today's model.
