# IC1: Incompleteness & stubs

**Lens:** codeHealth · **Scored:** yes

## Property

Unfinished work recognised by the shape of the code, not by keywords such as TODO: members that only throw a not-implemented error, methods that take inputs and return a constant, asynchronous methods that never wait on anything, code disabled by a constant false condition in source or in conditional compilation (for example `if (false)` or `#if false`), and skeleton types most of whose members are unfinished in one of these ways.

## Applies to

Any programming language that has functions or methods. It does not apply to markup (HTML) or styles (CSS). A rule that depends on a construct applies only where the language has that construct: asynchronous methods, conditional compilation, overridable members, interfaces and types with members.

## Population

Production code as [production-code.md](../production-code.md) defines it, with one more exclusion: this dimension leaves out examples, samples and demonstrations. That includes code that exists to be quoted in the project's documentation, and sample or demonstration projects.

## Unit and denominator

**Denominator: functions.** A function is a named method or function that has a body, declared in a type or at the top level of a file or module. An anonymous function, or a function declared inside another function, is part of the function that encloses it and is not counted on its own. Abstract or interface members without a body are not functions. Properties and their accessors are not in the denominator.

**Numerator: unfinished findings.** Each of the following counts as one finding:

1. **Not-implemented stub.** A method, or a property whose accessors do nothing else, whose whole body throws the language's not-implemented error (for example `NotImplementedException`, `NotImplementedError`, `todo!()`, `???`). An ordinary error whose message says the work is not implemented or not done also counts.
2. **Empty body with inputs.** A method that takes one or more parameters and has an empty body. A method whose whole body returns an already-completed asynchronous result that carries no value (for example `Task.CompletedTask`) counts as an empty body.
3. **Hollow constant return.** A method whose whole body returns a constant without doing any work, when the method takes parameters or its name promises a computation (for example `Calculate`, `Validate`, `Parse`).
4. **Fake asynchronous method.** A method declared asynchronous that never waits on an asynchronous operation and does not hand one on to its caller. An asynchronous iterator, which the language requires to be marked asynchronous, does not count.
5. **Disabled code.** Each branch or loop guarded by a condition that is a constant false (for example `if (false)`, `while (false)`, or `if (0)` where the language treats 0 as false), and each conditional-compilation region whose condition is a constant false (for example `#if false`). A `do { … } while (false)` block runs once and is not disabled code.
6. **Skeleton type.** A type that meets the skeleton threshold below. It counts as one finding in addition to the findings for its members.

A member that only throws a "not supported" error is not a finding. Neither is an empty method without parameters.

**Deliberately inert members.** The following are not findings. The list is closed.

In any of shapes 1 to 3:

- a method the language compiles away unless a build symbol is defined;
- a member that a contract requires the type to declare, where the type has declared, in code, that it lacks the capability the member provides (for example `Read` on a stream that declares it cannot read, or `Add` on a collection that declares itself read-only).

A not-implemented stub (shape 1) is also not a finding when:

- the not-implemented error carries a message saying why the member is not implemented. A comment does not count;
- the member is an explicit interface implementation, which can be called only through that interface;
- the member is the overridable default of an extension point that the code base overrides somewhere;
- the member implements an interface member and belongs to a private nested type, which nothing outside its containing type can see.

An empty body (shape 2) or a hollow constant return (shape 3) is also not a finding when:

- a subtype may override the member, or the member overrides a base member;
- the member releases resources (for example `Dispose`) and there is nothing to release;
- the member's name says it does nothing (for example `NoOp`, `DoNothing`, `Ignore`);
- the member has the language's standard event-handler parameters (a sender and event arguments);
- a comment on or inside the member says why it does nothing. A bare debt marker such as TODO, FIXME or HACK does not count as saying why;
- the member's type is a null object: it implements a contract, every method in it is empty or returns a constant, and its name or its documentation says it does nothing;
- the member is in one part of a type split across files, every method in that part is empty or returns a constant, and another part of the same type does real work;
- the language or a framework requires the body to exist but never runs it (for example a stub that maps a method onto a database function, or the result method of an awaiter that returns no value).

An empty body (shape 2) is also not a finding when the member implements an interface member. A hollow constant return (shape 3) is also not a finding when:

- it is an explicit interface implementation;
- it returns null and the interface member it implements declares null a valid answer;
- it returns the default value of the method's own type parameter;
- the method is installed as its own type's default handler for a replaceable delegate.

## Thresholds

- **Skeleton type:** at least 4 declared members (methods, properties, indexers and events, with or without a body), and at least half of them unfinished in shapes 1 to 3 above. Fake asynchronous members do not count toward the half.
- **Empty body with inputs:** at least one parameter.
- **Hollow constant return:** at least one parameter, or a name that promises a computation.

## Score

Let *c* be the number of findings and *f* the number of functions.

- If *c* = 0, the score is **10**.
- Otherwise:
  - rate = min(*c* / *f*, 1)
  - score = clamp(9.0 − 40 × rate − 0.7 × ln(*c*), 2.0, 9.0), rounded to two decimals.

Any finding at all caps the score at 9.0. One finding among 10,000 functions scores 9.0. Ten findings among 1,000 functions score about 6.99. The score never falls below 2.0.

## Not measured when

The population holds no function. Examples are a repository whose production code only declares constants and data types, and one that is only markup and styles. With no functions there is no rate. Any findings are still reported, but there is no score.

## What the result is not

- It is not a judgement of intent. A deliberate stub reads as unfinished unless the code shows it is inert in one of the ways listed above.
- It is not a count of TODO or FIXME comments.
- It does not score commented-out code, blanket warning suppressions, skipped tests or placeholder data.
- It is not a general dead-code or reachability measure. Only code disabled by a constant false condition counts.
- It does not measure test code, and it says nothing about whether the finished code is correct.
