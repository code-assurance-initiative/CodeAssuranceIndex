# ED2: Event/command shape

**Lens:** eventDriven · **Scored:** yes

## Property
Whether commands have a single handler (one owner of the decision) and fan-out is modelled with events.

## Applies to
This dimension applies to any language where a message type has handlers. A handler declares which message it handles. For example, it implements a generic handler interface over the message type (C#, F#, Java, Kotlin, TypeScript), or it matches the message in a synchronous request callback (Erlang).

The unit differs in one language:

- **Erlang:** a command is a synchronous request. It is a tuple whose first element is an atom naming an action that changes state (for example `{add_user, ...}`). A module owns the command when its synchronous request callback handles that tuple. An asynchronous (fire-and-forget) message is not a command.

## Population
Production code as [production-code.md](../production-code.md) defines it.

The dimension applies only to a repository that has an event surface. That means at least one of these:

- an event type (a domain event or an integration event);
- a consumer of a message broker or an actor;
- a handler that publishes to a message bus or event stream (publishing through an in-process mediator does not count, and neither does an outbound HTTP call);
- code that writes to a store and then publishes the change to a broker.

A repository whose only handlers serve in-process requests has no event surface. For example, a pure CQRS application with no events and no broker has none.

## Unit and denominator
**A command** is a message that asks the system to make one change. It is an instruction, not an event (which reports something that already happened) and not a query (which asks for data and changes nothing). If any handler handles a message as an event, that message is an event.

**A handler** is a type that handles the message. A type counts once for each command it handles, even if it declares that command more than once. Handlers in different projects count together when they handle the same declared type.

**A violation** is one command that has either of these:

- 2 or more handlers;
- no handler, while the command is dispatched in process or sent point-to-point to a queue the repository itself consumes.

**Denominator (n):** the number of commands that meet at least one of these conditions:

- the repository handles the command at least once;
- the repository dispatches it in process;
- the repository sends it to one of its own queues.

A command the repository declares but never handles is not counted if it is only sent elsewhere or not sent at all.

## Thresholds
- A command is clean when it has exactly 1 handler.
- A command with 2 or more handlers is a violation. In Erlang, a command is a violation at 3 or more owning modules (see Pending re-definition).
- A command with no handler is a violation only when it is dispatched in process or sent to the repository's own queue.

## Score
Let v be the number of violations and n the denominator.

- If v = 0, the score is 10.
- Otherwise:
  - density d = min(v / n, 1)
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, max(0, log10(n) / log10(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), rounded to 2 decimals

With any violation the score stays below 8. For example, 1 violation in 200 commands scores 7.87, and the score approaches 8 as the number of commands grows. Once 10 % or more of commands are violations, the score is at most 6. As every command becomes a violation, the score falls toward 1.

## Not measured when
- The repository has no event surface (see Population). All of the lens's dimensions abstain together.
- The denominator is 0. That means the repository has no command that it handles, dispatches in process, or sends to its own queue.

## What the result is not
- It does not check that a command's single handler is correct, or that its events reach their subscribers.
- It does not show whether one handler hides fan-out that should be an event. It only counts handlers per command.
- It does not see a command whose handlers live in another service or repository. Such a command is not in the denominator.
- It does not judge the content of events, for example whether an event carries only data.

## Pending re-definition
This is a parameter of the existing reading's model. Today's score depends on it. A later decision under decision 0013 will replace it with a threshold on the property.

- **Erlang owners:** in Erlang, a command is a violation only when 3 or more modules own it.
