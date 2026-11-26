# AX9: CQS / query purity

**Lens:** architecture · **Scored:** yes

## Property
Whether read (query) handlers stay side-effect-free — a query that writes persistent state or raises events breaks CQS and makes reads unsafe to retry, cache, or route to a read replica.

## Applies to
Any programming language. The dimension reads code that answers read requests through query handlers. It does not read web markup or styles.

## Population
The population is production code, as [production-code.md](../production-code.md) defines it. Within it, the dimension reads the repository's query handlers (see Unit). Command handlers, event consumers and any other code that is not a query handler are outside the population. Such code counts only when a query handler calls it (see Thresholds).

## Unit and denominator
**Unit: a query handler.** A query handler is code whose declared job is to answer one read request and return its result. The code's own structure declares that job, not the name it carries. For example:

- a query handler in a mediator or CQRS setup, that is, a handler for a request whose result is a value;
- a query object that performs its own read and returns the result;
- in code without handler types, the function or clause that answers a read request, for example an HTTP GET endpoint or a server process's synchronous reply to a query.

Names such as `GetOrderQueryHandler` often show this role, but a name alone does not make code a query handler. Code that answers a request with no result (a command) is not a query handler.

A query handler's **read entry point** is the method or function that answers the request and returns the result. A method on the same type that returns nothing is not part of the read, for example a message-consumer entry point next to the query entry point. Each query handler counts once.

A query handler is **impure** when its read path writes persistent state or raises events, as Thresholds defines. Otherwise it is **pure**.

**Denominator:** all query handlers in the population. The measured share is pure query handlers ÷ all query handlers.

## Thresholds
- **Writes persistent state:** the read path performs an operation that changes a durable store. Examples: saving pending changes to a database; adding, updating, removing, inserting, deleting, storing or upserting through a repository, data set, data session or store; a set-based update or delete; a raw write statement sent to the database. The same operations on an in-memory collection, for example a list or dictionary, are not writes.
- **Raises events:** the read path publishes an event or message to a publisher or bus.
- **Read path:** the body of the read entry point, plus the repository's own code it calls, up to the depth under Pending re-definition. A call through an interface reaches the repository's own implementations of that interface. A call into third-party code counts when the call itself is a write or a publish. The body of the third-party code is not part of the read path.
- A single write or publish anywhere on the read path makes the handler impure. Further writes do not change the result.
- A write counts even when it is meant as a read-side cache update or a lazy initialisation, for example a get-or-create.

## Score
score = 3.0 + 7.0 × (pure query handlers ÷ all query handlers), rounded to 2 decimals.

- If every query handler is pure, the score is 10.0.
- If no query handler is pure, the score is 3.0, the floor.
- Between those two points the score is linear. Example: 6 impure handlers out of 149 give 3.0 + 7.0 × 143/149 = 9.72.

## Not measured when
The production code has no query handlers. The dimension is then not applicable, and the repository gets no score rather than a low one. A repository that does not answer reads through query handlers is not penalised.

## What the result is not
- It does not check that commands return nothing, or that the code follows CQRS. It reads only the query side.
- It is not a full trace of data flow. A write made through reflection, or deeper than the read path reaches, does not count.
- It does not judge whether a write is legitimate. A deliberate read-through cache write or a get-or-create still makes the handler impure.
- It does not measure how many writes a query makes or how serious they are. Changes to in-memory state, logging and metrics are not writes.
- It says nothing about code that is not a query handler.

## Pending re-definition
These values are parameters of the existing reading's model. Today's scores depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Read-path depth:** the read path follows the repository's own code up to 3 calls below the read entry point.
- **Implementations per interface:** for a call through an interface, at most 16 of the repository's own implementations of that interface are read.
