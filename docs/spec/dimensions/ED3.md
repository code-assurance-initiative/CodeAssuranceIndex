# ED3: Event naming

**Lens:** eventDriven · **Scored:** yes

## Property
Whether events are named in the past tense (a clarity nudge — low weight).

## Applies to
Any language in which an event has a name in the code. The name can be one of these:

- a type (a class, record or struct);
- a case of an event union or enum;
- a value of a closed set that says which event a message carries.

## Population
Production code as [production-code.md](../production-code.md) defines it.

The population is the repository's **events**. An event records a fact that has already happened, and the code passes it on to whoever cares about it. A named type is an event when either of these holds:

- **It declares itself an event.** For example, it implements or inherits the repository's domain-event or integration-event contract, or its name ends in `Event` (including `DomainEvent` and `IntegrationEvent`).
- **The code treats it as an event.** For example, it is published, raised by an aggregate, appended to an event stream, or handled by an event handler.

**Event unions.** Code may declare its events as the cases of a union or enum that it marks as its event set, for example by the set's name or by the event contract it implements. Each case is then one event and is judged by its own name. The union or enum type itself is not an event.

**Envelopes.** Some messages have a field that names which event they carry, and that field's type is a closed set the repository declares. For such a message, the events are the values of that set. The envelope's own type name is not an event.

These are not events and are left out:

- **Commands.** A command is a message sent to one receiver to ask for something. Its name is imperative by design. Sending a message, or consuming one from a broker, does not on its own make it an event.
- **Value sets.** An enum or set of constants that is a value inside a message is not an event. Examples are a priority, a kind, a status or a channel kind.
- **Event contracts.** An interface or abstract type that events implement or inherit names a kind of thing, not a fact. Examples are `IDomainEvent` and `DomainEventBase`.
- **State machine inputs.** A package may declare a closed set of states next to a closed set of inputs. Those inputs are the alphabet of a state machine, not domain events.
- **Non-event types on a contract surface.** A request, response, paging or summary type is not an event just because it sits in a shared contracts or messages module.
- **Error types.** An error or result union is not an event set.

## Unit and denominator
- **Unit:** one event, judged by its name.
- **Words of a name:** first drop a trailing `IntegrationEvent`, `DomainEvent` or `Event` from the name. Then split what is left into its words. For example, `OrderStatusChangedToPaid` splits into Order, Status, Changed, To, Paid.
- **Past tense:** a name is in the past tense when **any** of its words is an English past-tense verb form, either a past participle or a simple past:
  - a regular form: a verb stem plus "-ed", for example `Placed`, `Changed`, `Used` or `Freed`;
  - an irregular form, for example `Paid`, `Sent`, `Built`, `Written`, `Withheld`, `Rebuilt`, `Began`, `Ran`, `Drew`, `Took` or `Went`.
- **Same-form verbs:** some verbs have a past form spelled the same as the base form, for example `set`, `cut`, `put`, `read`, `spread`, `cast` and `reset`. When such a word starts a name of two or more words, it is the imperative verb and not a past form. So `PriceSet` is in the past tense and `SetPrice` is not. A name of one word keeps the past reading.
- **Not past forms:** nouns and verb stems that only look like past forms, for example `Bed`, `Seed`, `Speed`, `Garden`, `Token` and `Open`.
- **Numerator:** the events whose name is in the past tense.
- **Denominator:** all events in the population. Each case of an event union counts as one event, and so does each value of an envelope's set.

## Thresholds
- An event counts as past tense when at least one word of its name is a past-tense verb form as defined above. The name is read after the suffix is dropped.
- The score floor is 7.

## Score
score = 7 + 3 × (past-tense events / events), rounded to two decimals.

- If every event is in the past tense, the score is 10.
- If no event is in the past tense, the score is 7.
- In between, the score is linear. For example, if 8 of 10 events are in the past tense, the score is 9.40.

The dimension is advisory. Its score is published, but it does not change the event-driven lens score. A repository has one ED3 score.

## Not measured when
The repository's production code has no events, so the population is empty. The dimension is then reported as not applicable. It is not scored 10.

## What the result is not
- It does not check whether events are designed well, kept small or scoped correctly. It reads only the tense of the name.
- It is not advice to rename an event that is already stored or published. The type name is written with every stored event and read by every subscriber, so renaming it can break replay and consumers. The nudge applies to new events.
- It is not a judgement on commands, which are rightly imperative.
- It is not exact English grammar. A past form that is also a common noun, for example `Ground` or `Shot`, can be misread.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Envelope:** a message counts as an envelope only when all of these hold:
  - it has at least 3 fields that are not constants;
  - one of those fields names the event, so there are at least 2 fields besides it;
  - that field's type is a set the repository declares, with at least 2 values.
