# AX2: Stateful singletons

**Lens:** architecture · **Scored:** yes

## Property
Whether singleton services avoid mutable shared instance state that concurrent callers would race on.

## Applies to
This dimension applies to any language in which concurrent callers can share mutable memory. This page defines counted shared instances (see Population) for C#, F#, Java, Kotlin, Scala, Python, Ruby, Go and Swift. Some other shared-memory languages have no such definition on this page, for example C, C++, Objective-C, Visual Basic .NET, Groovy and Clojure. For a product in one of those languages, the dimension is not measured (see Not measured when). It is not "not applicable".

In some languages this race cannot be written, so the dimension does not apply to them:

- **Rust:** the compiler refuses unsynchronised shared mutation.
- **TypeScript and JavaScript:** each process runs one event loop.
- **F# compiled to JavaScript:** it runs on one event loop (for example with Fable).
- **Dart:** isolates share no memory.
- **PHP:** it builds its objects per request.
- **Elixir and Erlang:** processes share no memory.

## Population
The population is production code as [production-code.md](../production-code.md) defines it, with these additions:

- **Only the repository's own types.** A singleton counts only when the repository declares its type. A framework or library type registered as a singleton is out of scope (for example a database data source or an HTTP client).
- **Single-threaded hosts.** A type that runs only on one thread is out of scope. For example, a type that declares it runs only in the browser, or a type whose every registration is in a single-threaded browser host.
- **Start-up code.** The type that holds the program's entry point is out of scope for static state.

A **shared instance** is one instance that concurrent callers all reach. The **counted shared instances** are the following, by language:

- **C#:** a type the repository declares and registers with the dependency-injection container as a singleton.
- **Java, Kotlin:** a class that a container makes one instance of by default, with no narrower scope declared. For example a Spring component, service, repository, controller or configuration class, a JSR-330 or CDI singleton, or the return type of a bean factory method.
- **Scala:** a container singleton as above, or a request-serving class or object (for example a Play controller or an Akka HTTP routes holder).
- **Python:** a module whose functions a threaded web framework serves as synchronous request handlers. Asynchronous handlers are out of scope because they run on one event loop.
- **Ruby:** a request-handling class (for example a Rails controller or a Sinatra app), or a Rack app instance.
- **Go:** the value whose methods are request handlers. Also a package whose request handlers write package variables, or whose handlers call functions that write them.
- **Swift:** the instance a router holds whose methods are request handlers. Also a file whose registered handler closures capture variables or write file-scope or static variables.
- **F#:** a module whose request handlers write the module's mutable values.

In C#, two more kinds of instance are shared but are **not counted shared instances**:

- an unregistered instance that a counted shared instance creates and keeps in one of its fields, either directly or in a collection;
- an instance held in a static field of any type.

**Process-global static state (C#):** a static field is one slot for the whole process, whatever the lifetime of its type. So a C# type with mutable static state can be a holder even when it is not a singleton.

## Unit and denominator
A **holder** is any shared instance (counted or not), or a C# type with mutable static state. A holder counts once as **racy** when it has at least one **unguarded write** to its **mutable shared state**.

**Mutable shared state** is:

- **Instance state of a shared instance:** a field that is not read-only, a read-only field that holds a mutable collection, or an auto-property with a setter.
- **Static state (C#):** a static field that is neither constant nor a read-only scalar.
- **Other languages:** module or package variables, class variables, globals and captured variables, as listed under Population.

These are not mutable shared state:

- a field or property whose type is thread-safe (for example a concurrent or immutable collection, an atomic, or a lazily initialised value);
- an init-only property;
- a Swift struct's own stored property, and every member of a Swift actor or of a `@MainActor` type or method.

**A write** is an assignment, an increment, or a call that changes a collection. It counts whether the holder's own code makes it or another type makes it through the shared instance. In Python and Ruby, only a read-modify-write counts (for example `n += 1` or `d[k] += 1`). In Go, every write counts.

**A write is guarded** in any of these cases:

- it runs inside a critical section: a lock, a semaphore that admits one caller at a time, or the write side of a reader-writer lock;
- it is an atomic operation;
- it is a plain store (not a read-modify-write) to a volatile field (C#, Java, Kotlin, Scala);
- it runs only where a single thread is guaranteed: construction, container start-up or shutdown callbacks, injection setters, or a private helper reached only from those;
- it runs inside a helper whose contract is to run its callee once under a lock (thread-safe lazy initialisation);
- one flow owns the state: a background loop's own private state, or a UI view's writes made on its UI thread.

A write in a callback that is only registered (for example an event handler, a timer or a cancellation callback) runs later on another thread. The code around the registration does not guard it.

- **Numerator k:** the number of racy holders, of every kind.
- **S:** the number of counted shared instances. These are not in S: instances that a counted shared instance keeps, instances held in a static field, and C# types with mutable static state. When they are racy, they count in k.
- **Denominator N:** the larger of S and k, and at least 1.

## Thresholds
- A holder is racy at one unguarded write. More writes or more fields do not add to the count.
- The isolation term runs out at a racy share of 0.10 (see Score).
- The population term saturates at N = 200 (see Score).

## Score
With k racy holders and N as above:

- If k = 0, the score is 10.
- Otherwise:
  - d = k / N
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, log10(N) / log10(200))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), rounded to two decimals

Any racy holder caps the score at 8. A single racy holder among 200 or more counted shared instances scores close to 8. When k is at least S (d = 1), the score is 1.

## Not measured when
- C# is read and the repository has no counted shared instance and no racy holder.
- There is no C#, the languages read hold no counted shared instance and no racy holder, and part of the product is in a shared-memory language this page does not define counted shared instances for (for example C, C++ or Objective-C). This is not measured, and the result does not claim there are no singletons.
- There is no C#, the languages read hold no counted shared instance and no racy holder, and no part of the product is in such a language. This is reported as not applicable.
- The repository's product is only in languages where the race cannot be written (see Applies to). This is reported as not applicable.

## What the result is not
- It is not proof of a data race. A write may be synchronised by a custom mechanism, or by a thread-safety guarantee given only in a name or comment. Such a write still reads as unguarded.
- It is not proof of thread safety. A score of 10 means no unguarded write to the state described above. It says nothing about races between awaits, check-then-act sequences across guarded writes, or state outside the population.
- It does not weigh how often a race would happen or how bad it would be. One racy counter weighs as much as a corrupted cache.
- It is not a judgement on singletons as a design.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Isolation density 0.10:** the racy share at which the ceiling falls back to 6.
- **Population saturation 200:** the N at which a single racy holder earns the full lift of the ceiling.
- **Ceiling 6 to 8 and floor 1:** the band that every non-zero count is scored in.
- **Denominator max(S, k):** some racy holders are not counted in S. These are C# types with mutable static state, instances a counted shared instance keeps, and instances held in a static field. They raise the denominator only when k exceeds S.
