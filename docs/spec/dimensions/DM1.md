# DM1: Aggregate boundaries

**Lens:** domainModelling · **Scored:** yes

## Property
Whether aggregates reference each other by identity (id) rather than by direct object reference — the core DDD consistency-boundary rule.

## Applies to
Any language where a field or property can state the type it holds. Examples are C#, Visual Basic .NET, F#, Java, Kotlin, Scala, Go, PHP, Python, TypeScript, JavaScript, Dart, Rust and Swift. A member counts only when the code states its type. The type can be stated by a declaration, by a type annotation or type hint, or by the type that the member's initial value constructs. A member with no stated type cannot hold a counted reference. So in code without field types (for example Ruby, or JavaScript and Python without annotations) the dimension finds no held references.

## Population
The population is production code as [production-code.md](../production-code.md) defines it. Within that code, the population is the repository's **aggregate roots**. A type is an aggregate root when any of these holds:

- **Declared:** it derives from an aggregate-root base type, implements an aggregate-root interface or marker, or carries an aggregate-root annotation. The base type, interface, marker or annotation can come from a domain-driven design library or from the project itself.
- **Stored on its own:** it is a domain type that a repository loads or saves by itself, not through a parent. A data context that exposes the type as its own set counts as a repository.
- **Raises events:** it is a domain type, not itself an event, and its own methods raise a domain event.
- **Guards its own state:** it is a domain type that keeps its own private state behind its own command methods. This applies in any language, whether or not the code uses an aggregate-root base type. An F# module that folds a set of events into a state is also an aggregate root.

Not in the population:

- Types that are not aggregate roots, for example value objects, services, repositories and events.
- An entity that another aggregate root holds and that is not loaded or saved on its own. It is a child inside that aggregate.
- An aggregate's own state type, where the aggregate is built by folding events into that state.

## Unit and denominator
**Unit:** a **held reference**. A held reference is an instance member of an aggregate root whose stated type refers to another aggregate root. Each pair of a member and the aggregate root it refers to counts once.

- **Member:** an instance field or property that the aggregate root declares itself. An inherited member counts on the type that declares it. Static members do not count. A field that exists only to back a property does not count separately from that property.
- **The aggregate's state:** an aggregate may hand its whole state to its base type, for example a TypeScript class that passes a props interface as its base class's type argument. In that case the members of that state count as the aggregate's own.
- **Refers to:** any of these counts:
  - the member's type is the other aggregate root;
  - the type contains the other root as an element, a type argument or a union case, for example a list of it, an optional of it, or a map whose values are it;
  - the type is an alias for one of the above.

  A member that refers to its own aggregate root's type does not count.
- A member whose type is the other aggregate's identity is not a held reference. Examples are `CustomerId`, a number key or a string key.

**Denominator:** the number of aggregate roots in the population.

## Thresholds
- One held reference is enough to count. There is no minimum size, depth or frequency.
- There can be more held references than aggregate roots. The score caps the density at 1.

## Score
Let *n* be the number of held references and *A* the number of aggregate roots (*A* ≥ 1).

- If *n* = 0, the score is **10**.
- Otherwise:
  - density *d* = min(*n* / *A*, 1)
  - isolation *i* = max(0, 1 − *d* / 0.10)
  - headroom *h* = min(1, max(0, log₁₀(*A*) / log₁₀(200)))
  - ceiling *c* = 6 + 2 · *i* · *h*
  - score = 1 + (1 − *d*) · (*c* − 1), kept within [1, *c*] and rounded to 2 decimals.

What this means:

- Any held reference keeps the score at 8 or below.
- The score is above 6 only when there is more than one aggregate root and the held references are fewer than 10 % of the roots.
- The ceiling gets close to 8 only at 200 or more roots.
- As the density rises, the score falls toward 1.

| *n* | *A* | Score |
|---|---|---|
| 1 | 1 | 1.00 |
| 1 | 10 | 5.50 |
| 1 | 200 | 7.87 |

**Per deployable service.** A deployable service is a part of the repository that is built and deployed on its own, made of one or more build projects. A file belongs to the service that owns the nearest build project whose folder contains the file. A file that a project lists from outside its folder belongs to every service whose project lists it. A file under no project belongs to no service. Each service gets its own score from the same formula. *A* is the number of aggregate roots declared in the service's files, and *n* is the number of held references whose member is in the service's files. A service with no aggregate roots is not applicable. Per-service scores are reported next to the dimension score and do not change it.

## Not measured when
- **Not the tactical domain-driven design style:** the code does not use the tactical building blocks closely enough to count as that style. The building blocks are aggregate roots, strongly typed ids, value objects, domain events, a separate domain layer, repositories per aggregate, and namespaces per aggregate. The exact rule is under Pending re-definition.
- **No aggregate roots:** the population is empty. The result is "not applicable", never 10.

## What the result is not
- It does not check aggregate size, invariants or transaction scope. It reads only what aggregate roots hold.
- It does not judge references held outside aggregate roots, for example in services, handlers or read models.
- It does not say that a codebase with no aggregate roots has good boundaries. That codebase is not measured.
- A 10 does not prove that the code uses ids. It means only that no aggregate root has a member whose stated type is another aggregate root. In code with few stated types, a 10 says little.
- It covers only the aggregate roots that match a definition under Population. A type meant as a root that matches none of them is outside the population.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Signs of the style.** These are the signs:
  1. at least one aggregate root;
  2. enough strongly typed ids;
  3. enough value objects;
  4. at least one domain event;
  5. a domain layer;
  6. a repository tied to aggregates (one restricted to aggregate roots, or one whose operations take or return an aggregate root or entity);
  7. at least 2 namespaces or folders each named for one aggregate.

  "Enough" means at least 3, or at least 1 that makes up at least a quarter of the tactical types (aggregate roots, entities, value objects and domain events together). In Java code, using a domain-driven design annotation library is an extra sign. In Go code there are four extra signs:
  - a repository whose subject is an aggregate;
  - a domain repository port, meaning a repository interface over a domain type of the repository's own;
  - at least 2 persistence-mapped entities;
  - an application service that depends on a domain repository port.
- **How many signs.** A repository is measured when any of these holds:
  - it has 3 or more signs;
  - it has exactly 2 signs, one of them an aggregate root and the other any sign except value objects;
  - it has exactly 2 signs, and they are domain events and a domain layer;
  - it contains any Go code, has 2 or more signs, and also has at least one of: a domain repository port, at least 2 persistence-mapped entities, or a repository whose subject is an aggregate;
  - it contains any Go code and has at least 2 persistence-mapped business entities in a domain package, plus an application layer over them (an application service or handler, or a web framework's routing);
  - it contains any Scala code, has 2 or more signs, and its domain code is coupled to infrastructure (for example an aggregate state built on a persistence framework);
  - it has a domain layer in Dart code or in Rust code.
- **Go-dominated repositories.** A repository counts as Go-dominated when Go holds at least half its types. Only this rule decides whether it is measured, and the rule above does not apply. It is measured in two cases:
  - it has a domain layer or a domain repository port, together with aggregate roots or domain events;
  - it has at least 2 persistence-mapped business entities in a domain package plus an application layer over them.
- **Support for the claim.** A repository that is not Go-dominated is measured only when, besides meeting the rule above, one of these holds:
  - it declares its own aggregate roots (not test types, library examples or roots without behaviour);
  - it uses a domain-driven design annotation library;
  - it has a domain shape: a domain layer, a repository per aggregate, namespaces per aggregate, a root stored on its own, or (in Go code) a repository whose subject is an aggregate or a domain repository port.
