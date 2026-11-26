# ED1: Handler temporal coupling

**Lens:** eventDriven · **Scored:** yes

## Property
Whether event handlers stay asynchronous (no blocking remote HTTP/gRPC calls awaited inside a handler).

## Applies to
Any language in which code handles events or messages delivered to it.

## Population
The population is production code as [production-code.md](../production-code.md) defines it. This dimension adds nothing to that definition and leaves nothing more out.

A repository is in scope only when both of these hold:

1. **It is event-driven.** At least 3 of these 4 properties hold for its production code. The count of 3 is listed under Pending re-definition.
   - **Integration events:** it declares at least one integration event, which is a message type that crosses a service boundary.
   - **Event handlers:** it has at least one event handler (defined below).
   - **Message bus:** it declares a dependency on a message bus or broker client library (for example a client for a message broker or a service-bus framework).
   - **Command and query handlers:** it has at least one in-process command or query handler (for example a mediator request handler).
2. **It has an event surface.** Its production code has at least one of these:
   - an event type (a domain event or an integration event);
   - a consumer that receives messages from a message broker, a bus or an actor system;
   - a handler that publishes messages to a message bus or event stream. Here a handler means an event handler, an in-process command or query handler, or an actor or message consumer.

   Dispatching to an in-process mediator does not count as publishing, and neither does making an HTTP call.

In a repository that is in scope, the population is every **event handler** in production code. An event handler is a handling entry point: a method, function or anonymous function that a delivered event or message is passed to, and that acts on it. For example:

- the handling method of a type that declares itself a handler or consumer of events or messages (for example `Handle`, `HandleAsync`, `Consume` or `ConsumeAsync`);
- a method or anonymous function that code registers as the callback for an event or message type (for example `OnOrderPlaced` in `bus.Subscribe<OrderPlaced>(OnOrderPlaced)`, or the lambda in the same call written with a lambda);
- the handler a function returns for an event or message delivered to it;
- a callback clause of a process that handles an incoming message (for example an Erlang OTP `handle_cast` or `handle_info` clause).

**What counts as one handler.** Each handling entry point is one event handler. A type with two handling methods, for example one for each of two event types, is two handlers. A type that registers three callbacks is three handlers. Each callback clause is one handler. An anonymous function inside a handler's body is part of that handler and is not counted separately, unless it is itself registered as a callback.

These are not event handlers:

- in-process command or query handlers, including a function that returns one;
- an anonymous function applied to events the caller already holds (for example a parallel loop over a list of events);
- a timer callback that receives no event;
- a method that takes an event but is only ever called directly and never registered;
- a transport adapter that receives from a broker and passes each message on to a handler.

## Unit and denominator
**Unit:** a coupled handler. This is an event handler that makes a remote call while it handles an event and waits for the reply before it finishes.

A **remote call** is a request to another service or process over the network whose response the handler waits for. For example:

- an HTTP request (any request method) or a gRPC call;
- a call through a generated REST client, where the interface member declares the HTTP request it makes;
- a blocking mail send;
- a synchronous call to a process on another node.

Any way of waiting counts. An `await` counts, because the handler still does not finish until the other service answers. Blocking synchronously on the request also counts (for example `.Wait()` or `.GetResult()` on the request's task, or the synchronous form of an asynchronous request).

These are not remote calls:

- publishing or sending a message to a bus or broker (for example a bus `SendAsync`);
- a local wait with no remote party (for example trying to take a local semaphore, or sleeping).

A remote call counts for a handler in two places:

- in the handler's own body, including anonymous functions inside it;
- in the repository's own code that the handler calls, down to the depth under Pending re-definition. This includes a private helper, an injected concrete class, and a repository class that implements an injected interface the repository declares.

A handler counts as coupled only once, however many remote calls it makes.

**Denominator:** the number of event handlers in the population, counted one per handling entry point.

## Thresholds
- A handler is coupled when it makes at least one remote call and waits for the reply (see Unit).
- **Isolation threshold:** a coupled share of 0.10 (10 %). At or above this share, the score's ceiling is 6.
- **Population saturation:** 200 handlers. At or above this count, the population allows the full ceiling lift.

## Score
Let *n* be the number of event handlers and *v* the number of coupled handlers.

- If *v* = 0, the score is **10**.
- Otherwise:
  - share *d* = min(*v* / *n*, 1)
  - isolation *i* = max(0, 1 − *d* / 0.10)
  - headroom *h* = min(1, max(0, log10(*n*) / log10(200)))
  - ceiling *c* = 6 + 2 · *i* · *h*
  - score = 1 + (1 − *d*) · (*c* − 1), kept between 1 and *c*, rounded to 2 decimals.

Any coupled handler keeps the score below 8. The ceiling comes close to 8 only when a very small share of handlers is coupled and there are at least 200 handlers. For example, 1 coupled handler out of 200 gives a ceiling of 7.9 and a score of 7.87. A small population has a lower ceiling. Once 10 % or more of the handlers are coupled, the ceiling is 6. When every handler is coupled, the score is 1.

A service within the repository is scored with the same curve, over the handlers in that service's files.

## Not measured when
- The repository is not event-driven (see Population). It then gets no score, not a 10.
- The repository is event-driven but its production code has no event surface (see Population).
- The repository is in scope but its production code has no event handlers.

## What the result is not
- It is not a check that handlers are fast or that they use `async`/`await`. An awaited HTTP call still couples the handler to the other service.
- It does not judge in-process command or query handlers. They count only toward the event-driven test and the event surface.
- It does not count reads or writes to the handler's own data store, or publishing a follow-up message.
- It does not see a remote call made deeper in the repository's own code than the call depth below. It also does not see a remote call made inside third-party code, unless the call into third-party code is itself the request.
- It does not count how many remote calls a handler makes, only whether it makes one.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Event-driven test:** the repository counts as event-driven when at least 3 of the 4 properties listed under Population hold.
- **Call depth:** a remote call counts when it is in the handler's own body or in the repository's own code at most three calls deep from it. A call four or more levels down is not seen.
