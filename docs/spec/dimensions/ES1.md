# ES1: Fold determinism

**Lens:** eventSourcing · **Scored:** yes

## Property
In event-sourced code, whether the functions that rebuild state from each stored event (event folds, for example in aggregates or projections, often named Apply or When) compute the new state only from the prior state and the event: without reading the current time, drawing a random number, generating a new identifier or doing I/O (input or output, for example reading a file, a database or the network). This is what makes replaying the same events always give the same state.

## Applies to
Any language in which the repository's code is event-sourced, meaning its state is rebuilt by replaying stored events. The file kinds are source files in a programming language. Markup and styles are not read.

## Population
The population is production code as [production-code.md](../production-code.md) defines it. This dimension adds one condition: of that code, only the event folds are in scope.

An event fold is a function that takes one stored event and the state it applies to, and gives the state after that event. It can return the new state or update the state it belongs to. Examples:

- an aggregate's handler that applies one event to the aggregate, for example `Apply(OrderPlaced e)` or `When(e)`;
- a projection's or read model's handler that applies one event to the view it rebuilds;
- a decider's evolve function, `(state, event) -> state`;
- the event handler a persistent actor runs while it recovers its state from its journal.

These are not event folds and are out of scope:

- Code that decides on a command and records a new event. Stamping a new event with the current time or a fresh identifier at the moment it is raised is correct, even when the same method then applies the event.
- Event handlers that react to an event with a side effect instead of rebuilding state (for example sending an email or publishing a message).
- Reducers that are not over stored events (for example a UI state reducer over user actions).

A function that is a fold in more than one way (for example an aggregate's Apply that is also its evolve function) is one fold.

## Unit and denominator
- **Unit:** a nondeterministic fold. This is an event fold whose body does at least one of these:
  - reads the current time or a running clock;
  - draws a random number;
  - generates a new identifier (for example a new GUID or UUID);
  - does I/O: reads or writes a file, a directory, the console, the process environment, a database or the network.

  The body includes the anonymous functions it contains. Text in comments and string literals is not a read. A fold that reads a time, identifier or random value from the event itself is pure. A fold counts once, however many such reads it has.
- **Denominator:** the number of event folds in the population.

## Thresholds
- One such read in a fold makes it a nondeterministic fold.
- No other threshold.

## Score
Let *n* be the number of nondeterministic folds, *N* the number of event folds, and *d* = *n* / *N*.

- If *n* = 0, the score is **10**.
- Otherwise:
  - isolation = max(0, 1 − *d* / 0.10)
  - headroom = min(1, max(0, log₁₀(*N*) / log₁₀(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − *d*) × (ceiling − 1), kept between 1 and the ceiling and rounded to two decimals.

So one nondeterministic fold caps the score at 8. The cap is 6 when 10 % or more of the folds are nondeterministic, or when there is only one fold. The score falls toward 1 as *d* approaches 1, and it never reaches 0.

## Not measured when
- The repository has no event-sourced code.
- The repository has event-sourced code but no event folds (*N* = 0). That result is "not measured", not 10.

## What the result is not
- It does not show that replay is correct. A fold can be deterministic and still compute the wrong state.
- It does not check reads a fold makes through other functions it calls, or through state shared outside the fold that is not the prior state.
- It does not judge whether events are immutable, how event schemas change over time, or whether stored events can be erased.
- It does not judge the command side. Reading the clock or making identifiers when raising an event is the correct practice.
- It is not a count of reads. A fold with five reads weighs the same as a fold with one.
