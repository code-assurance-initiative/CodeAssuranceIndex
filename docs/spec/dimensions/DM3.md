# DM3: Integration-event coupling

**Lens:** domainModelling · **Scored:** yes

## Property

Whether cross-context integration events stay loosely coupled — no producer-owned enums/domain types leaking to consumers. Shared-kernel/contracts types are allowed.

## Applies to

This dimension applies to any language that has named types with data members and has modules, packages or projects that can mark a context boundary (for example C#, Java, Go or TypeScript). It reads source code only.

## Population

The population is production code as [production-code.md](../production-code.md) defines it, with these additions:

- **Domain-modelled repositories only.** The dimension applies only to a repository built in a domain-model style. Its code has a domain layer with aggregates, entities, value objects or domain events.
- **Integration events.** The population is the repository's integration events. An integration event is a message type that one bounded context or service publishes for other contexts or services to consume. For example, a type the code declares to be an integration event.
- A type on a contracts or messages surface counts as an integration event only when the code treats it as an event. That means the code publishes or raises it, an event handler handles it, or its name says it is an event (for example `OrderPlacedEvent`). Request and response models of an HTTP interface are not integration events.
- Domain events that stay inside one context are not integration events.

## Unit and denominator

- **Data member:** a public instance property, or a public instance field that is not a constant. Static members, constants and methods are not data members.
- **Referenced type:** the declared type of a data member, together with every type inside it (for example the element type of a list, or the type arguments of a generic type).
- **Leak (the unit):** one pair of (data member, referenced type) where the referenced type is producer-owned (see Thresholds). A data member that references two producer-owned types gives two leaks. A producer-owned type used in two data members also gives two leaks.
- **Denominator:** the number of integration events in the population. A repository with no integration events is not measured (see Not measured when).

The number of leaks can be larger than the number of events.

## Thresholds

A referenced type is **producer-owned** when the producer's own code in this repository declares it and it is one of these:

- an enum;
- a strongly-typed id: a type that wraps one primitive identity value and compares by that value;
- an entity or aggregate;
- a value object;
- any other type that belongs to the producer's domain layer.

These types are **never** leaks:

- primitives and the platform's own value types, for example strings, numbers, dates and the language's built-in id types;
- types from the platform's standard library and framework;
- **shared-kernel and contracts types.** A type is in this group only when both of these hold:
  - it is in a module that the repository sets aside for contracts shared between contexts, for messaging, or for a shared kernel (for example a module named `Ordering.Contracts`, `Billing.Messages` or `SharedKernel`);
  - it has the shape of a contract: an interface, an enum, a strongly-typed id or an immutable message.

  A rich, mutable domain class in such a module is still a leak.

## Score

The dimension is scored only when there is at least one integration event, so `N` is at least 1. `L` is the number of leaks.

- `L = 0`: the score is **10.0**.
- `L > 0`:
  - `d = min(L / N, 1)` (leak density)
  - `isolation = max(0, 1 - d / 0.10)`
  - `headroom = clamp(log10(N) / log10(200), 0, 1)`
  - `ceiling = 6.0 + 2.0 × isolation × headroom`
  - `score = 1.0 + (1 - d) × (ceiling - 1.0)`, clamped to the range [1.0, ceiling] and rounded to 2 decimals.

With at least one leak, a repository scores at most 8.0. Once leak density reaches 10 %, it scores at most 6.0. Once there are as many leaks as events or more, it scores 1.0.

| Leaks | Events | Score |
|---|---|---|
| 0 | any | 10.0 |
| 1 | 200 | 7.87 |
| 1 | 10 | 5.5 |
| 3 | 3 | 1.0 |

## Not measured when

- **Not domain-modelled:** the repository is not built in a domain-model style (see Population). The dimension does not apply and gives no score.
- **No integration events:** the repository has no integration events. The result is "not applicable", with no score, not a 10.

## What the result is not

- It does not judge whether the contexts are well chosen, or whether the events are needed at all.
- It does not check the event's wire format, versioning or schema compatibility.
- It does not read consumers. A leak counts whether or not any consumer uses the leaked type today.
- A contracts or kernel module counts only where the repository sets it aside as one. A layout that keeps domain types in an unmarked shared module can read clean.
- It does not count domain events that stay inside one context.

## Pending re-definition

These are parameters of the existing reading's model. Today's scores depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Base ceiling:** 6.0, the highest score that at least one leak can earn without isolation headroom.
- **Isolation headroom:** up to 2.0 extra points, which lifts the ceiling to at most 8.0.
- **Isolation density:** 10 %, the leak density at which isolation headroom falls to 0.
- **Population saturation:** 200 events, the population at which headroom reaches its full value. Below that it grows with log10 of the population.
- **Floor:** 1.0, the score that rising leak density pulls the score down to.
