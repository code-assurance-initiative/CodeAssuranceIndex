# DM2: Strongly-typed ids

**Lens:** domainModelling · **Scored:** yes

## Property
How much of the domain uses strongly-typed ids vs raw Guid/string/int — adoption curve, not all-or-nothing.

## Applies to
Any language in which a domain type's members have a declared type. Examples are C#, Kotlin, Java, Go, TypeScript, Python with type annotations, PHP with typed properties, Swift, Dart and Rust. A member with no declared type holds no id here. The dimension reads only source files that declare domain types. It does not read markup or styles. For Swift, Dart and Rust, today's score uses a different unit and denominator, given under Pending re-definition.

## Population
The population is production code as [production-code.md](../production-code.md) defines it, with these further limits:

- **Domain types.** The types of the repository's own domain model:
  - entities and aggregate roots;
  - domain events that stay inside one bounded context;
  - value objects that hold a reference to an entity.
- **Domain operations.** The operations of the domain's ports and application services. These count only under the swappable-id rule in Unit.
- **Outside the domain model.** These types are not in the population, even when they carry ids:
  - integration events, meaning events published to other contexts or services as a serialised contract;
  - persistence rows, for example ORM-mapped database rows, local database or cache rows, and event-store rows;
  - transport shapes, for example request, response, DTO, message and payload types, unless the type is itself an aggregate;
  - read models;
  - configuration and options types;
  - serialisation mappers;
  - presentation types, for example view models and UI state;
  - test builders and test support.
- **Layered scopes.** A service or folder is out of scope when the repository's own accepted architecture decision record says it is layered rather than domain-driven.
- **Other files.** Examples, samples and the authors' own tools are production code as [production-code.md](../production-code.md) defines them. They hold no domain types unless they declare a domain model.

## Unit and denominator
**Unit: an id.** An id is a place where a domain type holds an identity that the domain model owns. The identity can be the type's own or another domain type's. These are ids:

- a public instance property or public instance field that holds an identity. Its name usually names that identity, for example `Id`, `OrderId`, `customer_id`, `UserID` or `CustomerKey`;
- the identity type that a domain type gives its base type, for example `Order : AggregateRoot<Guid>`;
- the own identity of a type that is an aggregate root;
- an identity kept in a private field and exposed under another name, for example `Number => _id`;
- a port or application-service operation that takes two or more raw ids of the same primitive type. These ids can be swapped by mistake without any error. The operation counts as **one** primitive id, however many such ids it takes. This is in addition to the ids of the domain types.

Each id is one of two kinds:

- **Strongly typed.** The id's type exists only to carry that identity, and two values with the same content are equal. Examples:
  - a C# `readonly record struct OrderId(Guid Value)` or record class;
  - a Kotlin value class;
  - a Go `type OrderID string`;
  - a Rust newtype;
  - a Swift typed-id struct;
  - a Dart extension type;
  - a type that derives from the project's own id base type.

  An optional strongly-typed id still counts as strongly typed. The size limit for such a type is under Pending re-definition.
- **Primitive.** The id's type is a raw primitive. Examples: a string, a 16-, 32- or 64-bit integer, a floating-point number (TypeScript's `number`), a GUID or UUID, or the language's own raw id type. An optional primitive id still counts as primitive.

An id of any other type is not counted. An identity that belongs to no entity of the model is not an id here. Examples: a correlation or trace id, an id issued by an external partner or system, a storage blob key, or a foreign handle.

**Denominator:** all ids in the population, strongly typed plus primitive. Each id counts once.

**Ratio:** adoption = strongly-typed ids / (strongly-typed ids + primitive ids).

## Thresholds
- **Swappable raw ids:** a port or application-service operation counts only when it takes two or more raw ids of the same primitive type. It then counts as one primitive id.
- **Strongly-typed id size:** one or two values (see Pending re-definition).
- There is no minimum number of ids, no minimum share and no cap per type.

## Score
score = 3 + 7 × adoption, rounded to two decimals.

- Adoption 0 (every id primitive) gives 3. A team that has not started, or is partway through a migration, does not score 0.
- Adoption 1 (every id strongly typed) gives 10.
- The score is linear between those two points. For example, 12 of 20 ids strongly typed gives 3 + 7 × 0.6 = 7.2.

A service's own DM2 uses the same curve over the ids in that service's files. For Swift, Dart and Rust, adoption is computed as Pending re-definition states, then fed into the same curve.

## Not measured when
- The repository has no domain model: it declares no entities, aggregates or domain events.
- The population holds no id, so the denominator is 0. The result is then not measured. It is never a 10.
- Swift, Dart and Rust: the conditions under Pending re-definition apply instead of the one above.

## What the result is not
- It is not a check that each id is used correctly. A strongly-typed id that is turned back into its raw value everywhere still counts as strongly typed.
- It does not cover ids outside the domain model. A raw id on a persistence row, on a wire contract or in a vendored SDK model is often correct, and it is not counted.
- It does not judge whether strongly-typed ids are the right choice for this domain. It measures how far the domain has adopted them.
- Some persistence libraries need a raw id field kept next to the typed one. That raw field counts as primitive and lowers the ratio.
- It does not count identities that the model holds without a member, base type or operation that carries them.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Strongly-typed id size:** a strongly-typed id holds one value or two values, and at least one of them is a primitive. A type with more values is not a strongly-typed id.
- **Swift, Dart and Rust: gate.** DM2 is scored only when the repository defines at least one strongly-typed id type of its own. Otherwise it is not measured.
- **Swift, Dart and Rust: unit and denominator.** Once the gate holds, the unit is a domain entity (for Rust, an aggregate). A type that is itself a strongly-typed id is not an entity. Let E be the number of domain entities and P the number of raw primitive ids on them. Then:
  - adoption = (E − P) / E, and 0 when that is negative. Strongly-typed ids are not counted in this ratio;
  - score = 3 + 7 × adoption, rounded to two decimals;
  - not measured when E is 0;
  - 10 when E is at least 1 and no entity holds a raw primitive id.
