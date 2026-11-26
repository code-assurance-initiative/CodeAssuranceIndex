# ES2: Immutable events

**Lens:** eventSourcing · **Scored:** yes

## Property

Whether persisted events are immutable facts (init-only/readonly) — a settable event member lets stored history be rewritten.

## Applies to

Any language with event sourcing. Event sourcing here means code that stores events in an event store or event stream and rebuilds state by replaying them. The dimension reads type declarations: classes, records, structs and the language's equivalents. Only the meaning of a settable member changes from language to language (see Unit and denominator).

## Population

Production code as [production-code.md](../production-code.md) defines it. Within it, only the **persisted event types** declared in the repository's own production code are in scope. A persisted event type is a type whose values an event store holds. It is any of these:

- an event that an event-sourced aggregate applies when it replays its stream (a type taken by an aggregate's apply, when or fold step);
- a domain event of the event-sourced model;
- any type that code appends to an event store or event stream itself, whatever it is called or marked.

These are not in the population:

- integration messages and notifications that are only published to other services or handlers (for example through a message bus or an outbox), where no aggregate replays them and no code appends them to the store;
- event types declared outside the repository (for example in a framework or a dependency);
- the aggregates, stores and streams themselves.

Each persisted event type counts once.

## Unit and denominator

**Unit:** a **mutable persisted event**. This is a persisted event type with at least one member that lets code outside the event change a stored instance after it has been built. Members the type declares and members it inherits both count. Static members and constants are not state of a stored event, so they never count. In most languages a settable member is one of these:

- **Public settable property:** a property with a public setter that still works after construction. In C#, an init-only setter (`init`) does not count, and neither does a private, protected or internal setter.
- **Public writable field:** a public field that is not readonly, final or constant.
- **Public mutable collection:** a public member whose value can be changed in place, even when the member itself is read-only. Examples are an array, or a list, set or dictionary that offers add, remove or clear. Read-only and immutable collections do not count.

Where the language itself differs:

- **Swift:** a stored `var` property that code outside the type can see. A `struct` event with only `let` properties is immutable.
- **Dart:** a public field that is not `final`.
- **Go:** exported fields are how events are serialised, so they do not count. A Go event is mutable when it has an exported method that changes its own fields after construction (for example a `Set...` method on a pointer receiver).
- **Rust:** public fields do not count. A Rust event is mutable when it has a public method that changes it (a method that takes `&mut self`, or a `set_*` setter).
- **Erlang** and other single-assignment languages: a stored value cannot be changed in place, so no persisted event is mutable.

**Denominator:** the number of persisted event types in the population.

## Thresholds

- An event is mutable when it has **1 or more** settable members. Extra settable members on the same event still count as one mutable event.
- The density is the number of mutable events divided by the number of persisted events, capped at 1.

## Score

Let `m` be the number of mutable persisted events and `n` the number of persisted event types (at least 1). Let `d = min(m / n, 1)`.

- If `m = 0`, the score is **10**.
- Otherwise:
  - isolation = `max(0, 1 − d / 0.10)`
  - headroom = `min(1, max(0, log10(n) / log10(200)))`
  - ceiling = `6 + 2 × isolation × headroom`
  - score = `1 + (1 − d) × (ceiling − 1)`, kept between 1 and the ceiling and rounded to 2 decimals.

This means a single mutable event keeps the score below 8. Isolation is below 1 whenever `d > 0`, so the ceiling never reaches 8. It only gets close to 8 when one mutable event sits among a very large number of events. For example, 1 mutable event among 200 events scores 7.87, and 1 among 1,000 scores 7.97. Once 10 % or more of the events are mutable, the ceiling is 6. As every event becomes mutable, the score falls towards 1. The score is never below 1.

A service's score uses the same formula over the persisted event types declared in that service's files.

## Not measured when

- The repository's production code has no persisted event types. Two examples: the code is not event-sourced, or it stores state rather than events. Having no persisted events does not give a score of 10.

## What the result is not

- It does not say whether code actually rewrites a stored event. It reads only whether an event's type allows a rewrite.
- It does not check the event store, for example whether the store allows updates or deletes.
- It does not judge event payload design, versioning, naming or personal data. Other ES and ED dimensions cover these.
- It does not cover published integration messages that are never stored.
- It does not check deep immutability. A mutable object held inside an event's immutable member is not counted, unless the member itself is a mutable collection.

## Pending re-definition

These points on the curve are parameters of the existing reading's model. Today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Isolation point:** 10 % mutable events. At this density or above, the ceiling for any non-zero count is 6.
- **Population saturation:** 200 persisted event types. At this size or above, the population term of the ceiling is at its full value of 1. The ceiling is then `6 + 2 × isolation`, which is below 8 for any non-zero count.
- **Ceiling band:** 6 to just below 8 for any non-zero count, with a floor of 1.
