# DM4: Rich vs anemic model

**Lens:** domainModelling · **Scored:** yes

## Property
Whether aggregates/entities carry the behaviour that protects their invariants, rather than being data bags driven by external services.

## Applies to
This dimension applies to object-oriented code that declares domain entities. Examples are C#, Visual Basic .NET, Java, Kotlin, Scala, Python, PHP, Ruby, Go, TypeScript, JavaScript, Dart, Rust and Swift.

Some data is immutable by design, so it is never rich or anemic. Examples are Elixir and Erlang records, and F# aggregates written as modules of functions.

There are two readings:
- **Rule A** gives the share of rich entities.
- **Rule B** finds anemic entities whose behaviour sits in another type.

The repository's dominant language decides which reading applies.

- **Dominant language:** count the production types in each language. Each language counts separately, except that TypeScript and JavaScript count together as one language. A language is dominant when no other language has more types. In a tie, both languages are dominant.
- **The repository shows a domain model** (see Population):
  - if Dart is dominant, rule B for Dart applies;
  - otherwise, if Rust is dominant, rule B for Rust applies;
  - otherwise rule A applies. This includes a repository where TypeScript/JavaScript or Swift is dominant.
- **The repository shows no domain model.** Test the languages in this order: Erlang, Swift, Dart, Rust, TypeScript/JavaScript, Ruby, Python. Stop at the first language that is dominant and whose own code holds a persisted business data model.
  - For Swift, Dart or Rust, rule B for that language applies.
  - For any other language on the list, or when no language passes, the dimension is not measured.

A language's code holds a **persisted business data model** in either of two cases:
- a type in that language is mapped to a database (for example by an ORM);
- a repository type in that language loads or saves another type declared in that language.

## Population
This dimension reads production code as [production-code.md](../production-code.md) defines it, with the additions below. It also leaves out code samples inside the repository's own documentation.

**Terms.** Each term is used here as domain-driven design uses it:
- **Entity:** a type whose instances have their own identity, which lasts while their data changes.
- **Aggregate root:** the entity that owns a cluster of related objects and guards their rules. Outside code changes the cluster only through the root.
- **Value object:** a type defined only by its values, with no identity of its own. An example is an amount of money.
- **Typed identifier:** a type that wraps one entity's identity value, for example `OrderId`.
- **Domain event:** a type that records something that happened in the domain, for example `OrderPlaced`.
- **Repository:** a type that loads and saves entities or aggregates.
- **Domain layer:** the code, project or folder that holds the business model, kept apart from infrastructure and presentation.
- **Bounded context:** one part of a larger system with its own model.
- **Domain-modelling annotation set:** a set of annotations or attributes that mark types as aggregates, entities or value objects.

**The repository shows a domain model** when its code is written as one. It needs one of the following:
- enough of the domain-model signs (listed under Pending re-definition);
- a domain layer in its Dart code, or a domain layer in its Rust code;
- where it contains Go, a business data model with an application layer over it. This means persisted business entities in its domain or model code, with a service, handler or web layer over them.

Meeting one of these is not enough on its own. The repository must also do one of the following:
- **Declare its own aggregate roots.** These do not count:
  - aggregates that belong to a test;
  - aggregates that are a library's building blocks for its users' domains, and examples of them;
  - aggregates that carry no behaviour.

  Using a domain-modelling annotation set also counts.
- **Have a domain structure.** Examples are a domain layer, repositories over aggregates or over the entities they load and save, or one namespace per aggregate. A library whose aggregates are only building blocks or examples can still show a domain model this way.

A repository where Go types make up at least half of all types follows its own rule (see Pending re-definition).

**Rule A** reads **domain entities**. A domain entity is a concrete type that the code models as an aggregate root or as an entity. For example, the type derives from an aggregate or entity base type, or carries an entity declaration. A type mapped by an ORM counts only when it is the domain entity itself. These types are left out:
- abstract types, and any type that another type in the repository derives from;
- persistence rows: types whose job is to be a database row, for example an Active Record model or a mobile local-database row;
- transport and view shapes, for example DTOs, requests, responses, view models and payloads;
- read models, projections, and a local read-only copy of an entity from another bounded context;
- typed identifiers and value objects.

A domain entity is **measured** when both of these hold:
1. **It holds data:** it has at least one instance field or property.
2. **Its state can change after construction:** code outside the entity can set one of its properties or instance fields, or the entity's own code writes its own state. An entity that nothing can change after construction is a recorded fact. It is neither rich nor anemic, so it is left out.

**Rule B** reads **candidate entities** in the language that selected it. A candidate is a domain type that holds stored data. Types whose role is not the domain model are left out, for example:
- data carriers: DTOs, messages, and configuration and command-line types;
- database rows and row mappings;
- typed identifiers and value objects;
- presentation, state-container and infrastructure types.

Each language narrows the candidates further:
- **Dart:** only classes in the domain layer are candidates.
- **Rust:** only structs are candidates. A struct with value semantics is a value object. A struct whose every field is a time or duration reading (for example two timestamps) is not a candidate.
- **Swift:** only reference types (classes) are candidates. Types that persist themselves are left out, and so are objects managed by an object-graph store.

A **behaviour holder** is a type whose job is to hold and run domain operations on other types, for example a service, manager, use case or handler.

## Unit and denominator
**Rule A.** The unit is a **rich entity**: a measured entity where at least one public operation changes the entity's own state. The change can happen in the operation itself, or in the entity's own private methods that the operation calls. A public operation is a method that is public or visible throughout its own module or package (for example C# `internal`). Constructors and static factories are not operations.

Any of these is a change to the entity's own state:
- assigning or incrementing a field or property declared on the entity, or on a base type in the same project;
- changing a collection the entity owns (adding, removing or clearing);
- recording or raising a domain event.

These do not make an entity rich:
- **Event folds:** a method whose only job is to apply an event that was already recorded to the entity's state.
- **Pass-through setters:** a method whose body only copies its parameters into members, with no condition, calculation, call or event. An example is `SetName(n) { Name = n; }`.
- **Go value-receiver methods:** in Go, a method on a value receiver changes a copy, not the entity.

The denominator is the measured entities. The ratio is rich entities divided by measured entities.

**Rule B.** The unit is an **anemic entity with delegated behaviour**. The denominator is the candidate entities. What counts depends on the language:
- **Dart:** a candidate that has at least one stored field that outside code can set, and no domain operation of its own.
  - A domain operation is any non-static instance method of the type, whether or not it returns a value.
  - These are not domain operations: serialisation, copying, conversion to or from another model, equality, text representation, and a getter that only declares a database column.
  - Such candidates count only when the repository also has a behaviour holder with at least one domain operation of its own.
- **Rust:** a candidate with public fields and no instance method, where no public field has a type declared in this repository. Field types from the language, its standard library or a third-party crate are allowed. It counts only when a behaviour holder has a method that takes the candidate as a parameter.
- **Swift:** a candidate with few ordinary methods. It counts only when another type has static methods that take the candidate as their first parameter.

## Thresholds
- **Rule A:** one public operation that changes the entity's own state makes the entity rich.
- **Rule B, Dart:** one domain operation makes a candidate not anemic. A behaviour holder qualifies with one domain operation.
- **Rule B:** the field, method and holder counts are listed under Pending re-definition.
- **Shows a domain model:** the sign counts are listed under Pending re-definition.

## Score
**Rule A.** With r = rich entities / measured entities:

score = 3 + 7 × r, rounded to 2 decimals.

If no entity is rich, the score is 3. If every entity is rich, the score is 10.

**Rule B.** With v = anemic entities with delegated behaviour and n = candidate entities:
- If v = 0, the score is 10.
- Otherwise, with d = v / n:
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, log10(n) / log10(200))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1). The score is kept between 1 and the ceiling, then rounded to 2 decimals.

## Not measured when
- No reading applies (see Applies to).
- Rule A applies and no domain entity is measured.
- Rule B applies and there are no candidate entities.

## What the result is not
- It does not say whether invariants are correct or complete. It sees state change, not the rule behind it.
- A method that only validates and throws, without changing state, reads as a query.
- It is not a count of methods. One state-changing operation makes an entity rich.
- A low rule A score does not prove that the logic lives in a service. It shows only that the entities do not change their own state.
- It says nothing about types outside the population, such as value objects, DTOs and persistence rows.

## Pending re-definition
These are parameters of the existing reading's model, and today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Dominance:** a tie makes both languages dominant. The tests run in the order given under Applies to.
- **Signs of a domain model.** Each sign counts once:
  1. aggregate roots declared;
  2. dense typed identifiers;
  3. dense value objects;
  4. domain events declared;
  5. a domain layer;
  6. repositories over aggregates, or a repository that loads or saves an aggregate or entity;
  7. two or more namespaces, one per aggregate;
  8. a domain-modelling annotation set in use (a persistence entity annotation alone does not count);
  9. in Go, a repository interface over an aggregate;
  10. in Go, a domain repository over a type declared in the repository;
  11. in Go, at least 2 persistence-mapped domain entities;
  12. in Go, an application service that uses a domain repository.

  "Dense" means at least 3, or at least one quarter of the aggregates, entities, value objects and domain events taken together.
- **Enough signs (any one of these):**
  - 3 or more signs;
  - exactly 2 signs, where one is aggregate roots and the other is not dense value objects;
  - exactly 2 signs, where they are domain events and a domain layer;
  - the repository contains Go, has 2 or more signs, and has a domain repository, at least 2 persistence-mapped domain entities, or a repository over aggregates;
  - the repository contains Go, and at least 2 persisted business entities sit in its domain or model code with a service, handler or web layer over them;
  - the repository contains Scala, has 2 or more signs, and its domain code is coupled to infrastructure. Examples are aggregate state tied to a persistence framework, a serialisation codec on an entity, or a domain port typed with transport types;
  - a domain layer in its Dart code, or a domain layer in its Rust code.
- **Go-dominated repositories:** where Go types are at least half of all types, the sign count and the condition that follows it do not apply. The repository shows a domain model in either of two cases:
  - it has a domain layer or a domain repository, together with aggregate roots or domain events;
  - at least 2 persisted business entities sit in its domain or model code, with a service, handler or web layer over them.
- **Rule B, Dart:** a candidate holds at least 2 stored fields. An anemic entity has at least 1 stored field that outside code can set.
- **Rule B, Rust:** a candidate holds at least 2 fields. An anemic entity has at least 2 public fields.
- **Rule B, Swift:** a candidate holds at least 2 properties. An anemic entity has at most 2 ordinary methods. The type holding its behaviour has at least 2 static methods that take the entity as their first parameter.
- **Rule B curve:** isolation reaches 0 at a density of 0.10, headroom stops growing at a population of 200, the ceiling terms are 6 and 2, and the floor is 1.
- **Rule A curve:** the floor is 3.
