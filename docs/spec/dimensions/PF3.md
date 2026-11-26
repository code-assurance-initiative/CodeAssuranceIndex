# PF3: Async & latency hygiene

**Lens:** performance · **Scored:** yes

## Property
Whether asynchronous work in production code (tests excluded) is kept from blocking a thread or event loop. Two things count. First, a synchronous wait for asynchronous work to finish (sync-over-async): in .NET anywhere in production code, not only inside async methods (for example .Wait() or .GetAwaiter().GetResult() on a task), because a blocked thread can deadlock the very work it waits for; in other languages where the wait sits inside asynchronous code (for example block_on in a Rust async function, runBlocking in a Kotlin suspend function, or block() inside a reactive publisher). Second, a blocking call made inside asynchronous code (for example a blocking sleep or HTTP call inside a Python coroutine, or a synchronous file call inside an async JavaScript function). Either stalls a thread or loop and risks deadlock. And where the code is a reusable .NET library (code packaged for other applications to use), its awaits do not resume on the caller's captured context (for example by using ConfigureAwait(false)), so the library does not deadlock or stall a caller that blocks on it.

## Applies to
- **Blocking waits and blocking calls:** every language that has asynchronous code.
  - In .NET languages (C#, F#, Visual Basic .NET), a synchronous wait on a task counts anywhere. A blocking sleep or synchronous I/O counts only inside asynchronous code.
  - In every other language, both kinds count only inside asynchronous code.
- **Asynchronous code** is code in an asynchronous context, as each language defines one:
  - C# and Visual Basic .NET: an async method or an async lambda.
  - F#: an `async { }` or `task { }` workflow.
  - Python: a coroutine (`async def`).
  - JavaScript and TypeScript: code that runs on the event loop on behalf of other work. That is an async function (including an async callback written inside a synchronous function), a request handler (a function the server calls to serve a request, for example a route handler, whether or not it is async), or a callback the event loop runs later (for example a timer callback, a microtask, or a promise continuation such as `.then`). A synchronous callback that one of these runs inline is part of it.
  - Rust: an `async fn` or an async block.
  - Kotlin: a `suspend` function.
  - Swift: an `async` function, or a `Task { }` block.
  - Dart: an `async` or `async*` function.
  - Java: a method that returns a reactive publisher (for example a Reactor `Mono` or `Flux`).
  - Scala: a method that returns a future or an effect (for example `Future`, `IO`, `ZIO`).
- **Context capture:** .NET libraries only.
- The dimension does not apply to languages with no asynchronous function colour (for example Go, Ruby, PHP, Elixir, Erlang). They have no asynchronous code for a call to stall.

## Population
Production code, as [production-code.md](../production-code.md) defines it. This dimension adds the following:

- **The library population** is used for context capture only. It is the production code of each .NET project that ships as a package for other applications to use. It leaves out:
  - applications, for example a web app, a worker service, a serverless function app, a console or desktop executable, or a packaged command-line tool.
  - UI-component libraries, for example Blazor, WPF, WinForms, MAUI or Avalonia components. These must resume on their renderer's context.
  - libraries that can only run inside a host with no synchronization context, for example a library that runs only inside ASP.NET Core.

  Each project is decided on its own. A project that declares it is not packable is not in the library population.
- **Not counted as blocking:**
  - a program's entry point (`static Main`, or top-level statements) waiting for its own work.
  - the one-time setup or teardown of a test or benchmark harness.
  - in JavaScript or TypeScript, a program that runs to completion and serves no other request (for example a build or capture script).
  - in JavaScript or TypeScript, a worker thread, because blocking it is its purpose.

## Unit and denominator
- **Blocking call (B):** one place in the code that holds the current thread or event loop until other work finishes, in the places that Applies to sets out. Each place counts once, so two in one function count as two. A blocking call is any of these:
  - a synchronous wait on a task or on asynchronous work, for example `.Wait()`, `.GetAwaiter().GetResult()`, `Async.RunSynchronously`, `block_on`, `runBlocking`, `Await.result`, `block()` or `asyncio.run`.
  - a blocking sleep.
  - synchronous network, file or subprocess I/O.
  - a deliberately slow synchronous computation that has an asynchronous form alongside it, for example a synchronous key derivation or compression.
  - a busy-wait: a loop that does nothing but wait, for example for the clock or for a flag that a callback sets.
  - a call to the code's own function that does one of the above in its own body.
- **Not a blocking call:**
  - a wait on a synchronization primitive (for example a semaphore, a lock or a reset event), since no asynchronous work is behind it.
  - a wait on a task that is already complete.
  - in .NET, a wait on a pure timer delay (for example `Task.Delay(n).Wait()`) outside asynchronous code. It only pauses the thread, as a sleep does, and a sleep outside asynchronous code is not counted.
  - a synchronous bridge method's own wait on the task its caller passes in.
  - a call handed to a thread where blocking is allowed, for example `spawn_blocking`, `asyncio.to_thread`, `withContext(Dispatchers.IO)`, `Isolate.run`, `subscribeOn(Schedulers.boundedElastic())`, a GCD queue or a new thread.
  - a call that is itself awaited.
- **Await (A):** one await in the library population: an `await` expression, an `await foreach` or an `await using`.
- **Configured await (C):** an await in the library population that does not resume on the captured context. This happens in one of two ways:
  - it is written that way, for example `ConfigureAwait(false)`, or `ConfigureAwaitOptions` without `ContinueOnCapturedContext`.
  - its project sets this for every await at build time, for example a build-wide `ConfigureAwait` setting or a weaver.
- **Denominators:** B has none. It is a count over the whole repository. Context capture is the share C / A, capped at 1.

## Thresholds
- Context capture is scored only when the repository has at least one project in the library population and that population holds **5 or more** awaits (A ≥ 5).
- Blocking calls have no minimum. Every one counts.

## Score
```
score = 10
      − min(4, 1.5 × B)
      − [library population and A ≥ 5] × 4 × (1 − min(1, C / A))
```
The score is rounded to one decimal and kept between 0 and 10.

- **B** adds up the blocking calls from all languages, under one shared cap of 4.
- **Blocking deduction:** 1.5 points per call, capped at 4. Three or more calls take the full 4.
- **Context-capture deduction:** at most 4. It falls in a straight line from 4 when no await is configured to 0 when every await is configured.
- **Lowest reachable score:** 2.0.
- **10 means:** there is no blocking call, and every await in the library population is configured (or the context-capture check does not apply).

## Not measured when
- The repository has no asynchronous code: no asynchronous context in a language listed above, no await, and no wait on a task.
- The repository's only languages have no asynchronous function colour. The result is "not applicable".
- The engine reads none of the languages that hold the repository's asynchronous code. This is a gap in the engine, not a finding.

## What the result is not
- **Not a measure of latency or throughput.** It counts patterns that stall a thread or loop. It does not time them.
- **Not data-flow proof.** It does not prove that a counted wait deadlocks, or that a blocking call sits on a busy request path.
- **Not a judgement on applications for context capture.** A host application need not configure its awaits and is never charged for it.
- **Not a count of severity.** A one-hour sleep and a short file read count the same.
