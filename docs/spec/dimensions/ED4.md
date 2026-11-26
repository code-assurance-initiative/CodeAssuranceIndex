# ED4: Outbox / dual-write

**Lens:** eventDriven · **Scored:** yes

## Property
Whether state changes and message publishes are atomic (a transactional outbox) rather than a crash-unsafe dual write.

## Applies to
Any programming language. Only code that changes durable state or publishes messages matters.

## Population
The population is production code as [production-code.md](../production-code.md) defines it.

Within that code, the population is the repository's **command-handling flows**. There are two kinds:

- **Handler flows:** each callable of a message, event or command handler, whatever that callable does. A handler is a type or module whose job is to handle messages, events or commands. Examples are a message consumer, an event handler, a command handler and an actor that handles messages.
- **Other flows:** each other entry point that changes durable state or publishes a message. An entry point that does neither is not in the population.

An **owner** is the unit that holds a callable and limits who can call it. Where the language has types, the owner is a type (for example a class). Otherwise it is a module (for example a Go package, an Erlang module or a C file). An **entry point** is one of these:

- A public action of a web controller.
- In any owner that is not a handler, a callable that code outside that owner can call. That means public or exported, as the language defines visibility. Examples are a non-private method (a protected one included), an exported Go function, and a function an Erlang module exports. A repository's public save operation and an application service's operation are both entry points.
- A request endpoint written as an anonymous function registered inside another callable. It is its own flow, separate from the callable that registers it.
- A background loop.

A callable that only its own owner can call is a helper, not an entry point. Examples are a private method, an unexported Go function, and a function an Erlang module does not export.

Other flows join the population only when the repository has at least one handler, or when at least one of them is a dual write.

A flow is the entry's own body plus the helpers of its own owner that it calls (see Pending re-definition). Helpers count as follows:

- **In a handler:** every callable is a flow. A helper that is a dual write on its own counts once, as itself. It is not counted again through each callable that calls it.
- **In other flows:** a helper is part of each entry point that calls it. If a helper is a dual write, each entry point that calls it is a dual write. A called callable that is itself an entry point counts once, as itself.

**Construction code:** in a language that has constructors or initialisers, this is the code a handler runs while it is being constructed. It includes the message handlers it registers there (for example an actor registering its message handlers). It belongs to that handler and is not in the denominator. If one path of it changes state and publishes, with no outbox and no shared transaction, it counts as one single-flow dual write. Its state changes and publishes also take part in the split pairing below.

Code outside these flows and outside construction code is not in the population.

## Unit and denominator
**Unit:** a **dual write**. There are two kinds.

**1. A single-flow dual write.** This is one flow, or one handler's construction code, that does both of the following on one path, and neither goes through an outbox:

- **A state change:** a write to durable storage. Examples are saving a unit of work, and inserting, updating, deleting or upserting a row or document. A write that stages a row for the next commit counts.
- **A publish:** handing a message to a broker or another transport that carries it out of the process for later delivery. Examples are a message queue, an event stream and a durable queue in the platform. A wrapper the repository declares counts as a publish when it reaches such a transport. So does delivery to subscribers that each run in a new unit of work of their own.

Each such flow or construction code counts once.

**2. A split dual write.** This is a pair where one side changes state and the other side publishes. Each side is a handler flow or a handler's construction code. Neither side may be a single-flow dual write or go through an outbox. Both sides must belong to the same handler, or to two handlers of the same message type. The split kind adds at most one dual write to the repository's count. It counts only when no handler flow and no construction code is a single-flow dual write. A single-flow dual write in an other flow does not stop it from counting (see Pending re-definition). It does not change the denominator.

These are **not** a publish:

- dispatch through an in-process mediator
- an HTTP call
- a live push to connected clients (for example a hub or a websocket)
- an in-process stream or channel

These are **not** a dual write:

- The message is written to an outbox in the same transaction as the state change. This can happen directly, or through a helper or dependency that writes to the outbox. An outbox is storage for outgoing messages that is committed together with the state and sent afterwards.
- The state change and the publish run inside one shared transaction.
- Every state change and every publish sit on branches that exclude each other, so no single pass does both.
- The write only claims the message so that its send is not repeated (claim-first dispatch).
- The flow publishes first and then writes only a record of the delivery it received (an at-least-once inbox). It changes no other state.

**Denominator:** the number of command-handling flows in the population, at least 1. Construction code is not counted in it.

## Thresholds
- A flow or construction code is a single-flow dual write when one path has at least one state change and at least one publish, with no outbox and no shared transaction.
- Isolation is the extra headroom a lone dual write gets. It falls to zero at a density of 0.10 (10 % of flows).
- Population headroom reaches its full value at 200 flows.

## Score
Let `v` be the number of dual writes of both kinds, and `n` the denominator.

- If `v = 0`, the score is **10**.
- Otherwise:
  - `d = min(v / n, 1)`
  - `isolation = max(0, 1 − d / 0.10)`
  - `headroom = clamp(log10(n) / log10(200), 0, 1)`
  - `ceiling = 6 + 2 × isolation × headroom`
  - `score = 1 + (1 − d) × (ceiling − 1)`, clamped to [1, ceiling] and rounded to 2 decimals.

So a single dual write caps the score at 8, and many dual writes push it toward 1. A score for one service uses the same curve, counting only the flows and dual writes in that service's files.

## Not measured when
- The repository has no event-driven surface: no event types, no message consumers, and no code that publishes onto a broker.
- The repository has no message, event or command handler, and none of its other flows is a dual write. Without handlers, a score exists only when at least one other flow is a dual write, so that score is below 10.
- The repository has handlers, but no flow in the population and no handler's construction code changes durable state or publishes a message, and nothing is a dual write.

In each case the dimension has no score. It is not a 10.

## What the result is not
- It does not check that an outbox works: that the outbox is relayed, that delivery is ordered, or that consumers are idempotent.
- It does not cover dual writes across two durable stores where no message is published.
- It does not cover distributed transactions or sagas across services.
- A 10 means no dual write was found in the flows read. It does not mean every publish is transactional.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Helper depth:** a flow includes the helpers of its own owner that it calls, up to 2 calls deep.
- **Wrapper depth:** a publishing wrapper the repository declares is followed up to 3 calls deep to decide whether it reaches a broker.
- **Split dual writes:** at most 1 per repository. One is counted only when no handler flow and no construction code is a single-flow dual write. Single-flow dual writes in other flows do not stop it from counting.
