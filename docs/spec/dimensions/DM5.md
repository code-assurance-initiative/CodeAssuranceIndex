# DM5: Encapsulated state

**Lens:** domainModelling · **Scored:** yes

## Property
Whether entities (domain objects with an identity whose rules, or invariants, they must keep) protect their state, writable only inside the entity or when it is constructed, instead of exposing publicly writable state that bypasses those invariants, such as public setters or the entity's own mutable collections handed out directly rather than as a copy or read-only view. Softened where the code uses a persistence framework that rebuilds entities from stored state (for example Entity Framework or Marten).

## Applies to
This dimension reads production code, as [production-code.md](../production-code.md) defines it, in a repository that has a domain model.

The **dominant language** is the language whose production code declares the most types (for example classes, structs, records and interfaces). If two languages tie, the one listed first below wins.

**Repository with a domain layer.** A domain layer is a layer of entities, for example a `domain` folder or package of aggregates and entities.

1. If the dominant language is Dart, the value-identity unit applies, over Dart code.
2. If the dominant language is Rust, the value-identity unit applies, over Rust code.
3. In every other case, the general unit applies, over the entities in every language present. Entities from all languages form one population. In languages where data cannot be changed once created (for example Elixir and Erlang), an entity counts in the population and cannot offend.

**Repository with no domain layer.** The dimension is measured only when the dominant language keeps a persisted business data model in its own code. That means types mapped to storage, or types that a repository port loads and saves. This applies to three dominant languages only:

1. Swift: the value-identity unit, over Swift code.
2. Dart: the value-identity unit, over Dart code.
3. Rust: the value-identity unit, over Rust code.

In every other language, a persisted data model alone does not bring the repository into scope.

## Population
**General unit.** The population is the repository's entities:

- **Entities:** types with an identity that they keep as their state changes (for example an `Order` with an `Id` that derives from an entity or aggregate-root base).
- **Child entities:** a class that an aggregate root holds in a collection and that identifies itself by a number local to the aggregate (for example `Order.Parcels`, where each `Parcel` has a `Number`).
- **Declared value objects:** types their authors declare to be value objects, for example through a value-object base or marker, or by placing them with the value objects. These are graded the same way as entities.

The following are also out of scope, in addition to what [production-code.md](../production-code.md) excludes:

- **Persistence rows that are not the domain model:** model classes that belong to the storage layer rather than the domain (for example Django, Rails, Ecto or JPA rows in a persistence layer), and a persistence type that mirrors a separate domain type.
- **Commands, events, read models, configuration and serialisation wrappers,** even when they carry an identity.
- **Code samples inside documentation.**

**Value-identity unit.**

- **Swift:** every class, except active-record classes that combine an entity with its storage. A row of an object-graph persistence framework (for example Core Data, Realm or SwiftData) stays in the population and never offends.
- **Dart:** every class, except storage-mapped rows and presentation classes. Only a class in the domain layer that is not a data carrier can offend. A data carrier is, for example, a DTO, a serialised or JSON round-trip model, an immutable value class or a storage row. Other classes in the population count in N and never offend.
- **Rust:** every type that implements its own hashing or equality by hand. Only a struct can offend. Storage rows, serialisation DTOs, id wrapper types (newtypes) and command-line argument types count in the population and never offend.

## Unit and denominator
The unit is one entity in the population. An entity either offends or does not, so it counts at most once. Only entities in the population can offend, so k is never more than N.

- N is the number of entities in the population.
- k is the number of entities in the population that offend.

**General unit.** An entity offends when callers can change its state from outside without going through its own methods. It offends if it has either of the following:

1. **A publicly writable member.** This is a public instance property with a public setter that is not init-only, or a public instance field that is neither read-only nor constant. Some languages add other forms:
   - In languages without properties, a public setter method also counts. A public setter method is one that only assigns one of the entity's own business fields and checks no rule (for example `SetName` in Go, or `setName` in Java).
   - **Go:** an exported mutable field also counts. So does mutable state that an embedded type promotes into the entity.
   - **Python:** a public instance attribute (one whose name does not start with an underscore) also counts.
2. **An exposed own collection.** This is a public instance member whose type is a mutable collection and that hands out the entity's own stored collection. A mutable collection is one whose members can be added or removed, for example a list, set, dictionary, queue or stack. Examples of handing it out: a public field, an auto-property, or a getter that returns the stored field itself.

None of the following offends:

- a read-only view (for example `IReadOnlyList<T>` over a private field)
- a getter that returns a copy or a projection
- a member that is private, protected, internal or init-only, or that can be set only in the constructor

An array is not an exposed own collection, because its length is fixed. A public setter or writable field of array type still offends under rule 1.

**Value-identity unit.** An entity offends when its equality or identity is computed from its own fields while those fields can still be changed from outside:

- **Swift:** the class's identity is a content digest (for example a cryptographic hash such as SHA-256) computed from its own stored fields. Conforming to `Hashable` alone does not make it one. It offends when at least 2 of its stored fields can be changed from outside, that is, stored `var`s with no `private(set)` and no property wrapper. A row of an object-graph persistence framework never offends.
- **Dart:** a domain-layer class that is not a data carrier has value equality (for example `Equatable`, or an overridden `==` or `hashCode`) and at least 1 field that is not final, not private and has no getter.
- **Rust:** a struct that is not a storage row, serialisation DTO, id wrapper or command-line argument type hand-writes `Hash` or `PartialEq` and has a `pub` (publicly writable) identity field, such as its id, a reference to another entity, or a stored hash.

## Thresholds
- **Offending:** for the general unit, one publicly writable member or one exposed own collection is enough to make an entity offend. For the value-identity unit, the minimum number of fields per language is given above.
- **Isolation:** an offending share below 10 % counts as isolated (see Score).
- **Population size:** isolation earns its full lift at a population of 200 entities.
- **Softening:** softening applies when the repository depends on a persistence framework that rebuilds entities from stored state through their members (for example Entity Framework Core or Marten). It applies to the general unit only.

## Score
Let d = k / N. Round every score to 2 decimals.

**Without softening (and always for the value-identity unit):**

- If k = 0, the score is 10.
- Otherwise:
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, log10(N) / log10(200))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), clamped to [1, ceiling]

What this means in practice:

- A repository with any offending entity scores at most 8.
- An offending share of 10 % or more, or a population of one entity, scores at most 6.
- If every entity offends, the score is 1.

**With softening (general unit only):**

- score = 6 + 4 × (1 − d)

This runs from 10 (no entity offends) down to 6 (every entity offends).

## Not measured when
- The repository has no domain layer, and its dominant language is not Swift, Dart or Rust with a persisted business data model (see Applies to).
- The population is empty (N = 0). The result is "not measured", not a 10. A population whose entities are all clean scores 10.

## What the result is not
- It does not check whether an entity's methods actually enforce its invariants. It checks only whether state can be written without going through them.
- It does not judge whether an entity is rich or anemic.
- It does not decide whether a persistence framework requires a public setter. Under such a framework the score is softened, not waived. So adding that dependency raises the score with no change to the entities.
- It does not cover changes to an element inside a collection, or writes to elements of an array.
- It is not a count of setters. An entity with ten public setters weighs the same as one with a single public setter.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Lifted ceiling:** when any entity offends, the best score rises from 6 to at most 8. The lift is 2 × isolation × headroom. Isolation reaches 0 at an offending share of 10 %, and headroom is full at 200 entities.
- **Softening floor:** under a persistence framework that rebuilds entities, the lowest score is 6.
- **Swift field minimum:** a Swift class offends only when at least 2 of its stored fields can be changed from outside.
