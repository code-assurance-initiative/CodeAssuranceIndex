# AX6: Interface segregation

**Lens:** architecture · **Scored:** yes

## Property
Whether interfaces stay focused rather than fat — the Interface-Segregation principle (SOLID 'I').

## Applies to
On this page, a contract is a type that lists operations other code must supply. This dimension covers contracts declared in these languages: C#, Java, TypeScript, Kotlin, Scala, Swift, Dart, F#, Go, PHP, Python, Ruby, Rust and Erlang. That list is the standard's current scope. It is a choice, and it does not mean other languages cannot declare contracts.

Contracts in other languages are outside the scope and add nothing to `n` or `f`. Examples are C++ abstract classes, Objective-C protocols, Visual Basic .NET interfaces and Elixir behaviours. JavaScript has no way to declare a contract, so its code holds nothing to judge. Markup (HTML) and styles (CSS) are out of scope.

## Population
The population is the contracts declared in production code, as [production-code.md](../production-code.md) defines it. The table shows what counts as a contract in each language.

| Language | A contract is | It is in the population when |
|---|---|---|
| C# | an interface, of any visibility | always, even with no member or with every member given a body |
| Java | a public interface, not an annotation type | always, even with no abstract method |
| TypeScript | an exported interface | it declares at least one behaviour member: a method signature, a call or construct signature, or a function-typed property, even an optional one |
| Scala | a public trait that is not sealed | it has at least one required operation |
| Kotlin | a public or internal interface or abstract class that is not sealed | it has at least one required operation |
| Swift | a protocol that is not private | it has at least one required operation |
| Dart | an abstract class or interface that is visible outside its library and not sealed | it has at least one required operation |
| F# | an interface that is not private | it has at least one required operation |
| Go | an exported interface | it has at least one required operation |
| PHP | an interface, or an abstract class | it has at least one required operation |
| Python | a protocol, or any class that declares an abstract method | it has at least one required operation |
| Ruby | a class or module (one class reopened in several files is one) | it has at least one required operation |
| Erlang | a module that declares callbacks | it has at least one required operation |
| Rust | a trait, of any visibility | it has at least one required operation |

These contracts are also outside the population. They count in neither the numerator nor the denominator:

- **Contracts owned elsewhere:** a C# COM interop declaration, and a Scala trait declared under the `java` or `javax` packages that re-implements a platform interface.
- **Closed contracts:** a Scala trait marked do-not-inherit or internal-API. In Rust, a sealed trait or a trait marked hidden from documentation.

## Unit and denominator
The unit is a **contract**.
- The denominator `n` is the number of contracts in the population, across all languages.
- The numerator `f` is the number of those contracts that are **fat**, as Thresholds defines it.
- Each contract counts once.

A contract's **width** is the number of its operations.

**Required operations** are the operations an implementer must write:

| Language | A required operation is |
|---|---|
| C# | any declared method, property, indexer or event, with or without a body, static or not, whatever its visibility |
| Java | an abstract method (not `default`, `static` or `private`) |
| TypeScript | a method signature, call signature, construct signature or function-typed property that is not optional (`?`) |
| Scala | a public `def` with no body, or a public `val`/`var` with no initializer |
| Kotlin | a public function with no body |
| Swift | a method requirement that is not private |
| Dart | a public method with no body |
| F# | an abstract member that is not static and not private |
| Go | an exported method the interface declares itself |
| PHP | in an interface, a public instance method; in an abstract class, a public abstract method |
| Python | in a protocol, a public method or property (not static, not the constructor); in another class, a public method marked abstract. A name starting with `_` is not public |
| Ruby | a public instance method (not `initialize`) whose body only raises `NotImplementedError`, Ruby's way of saying a subclass must supply it |
| Erlang | a `-callback` not listed in `-optional_callbacks` |
| Rust | a `fn` with no default body, or an associated `type` or `const` with no default |

**How operations are counted:**

- **C#:** each declaration counts, so each overload counts on its own. A property or indexer counts once, not once per accessor. An event counts once.
- **Rust:** each required item counts once.
- **All other languages:** each distinct operation name counts once, so an overload set counts once.
  - In Erlang, name and arity together are one name.
  - A setter counts together with the getter of the same property when the contract declares both, the getter takes no argument and the setter takes at most one value. For example, `getX`/`setX`, `x`/`x=` and `x`/`x_=` are each one operation.

**Inherited operations:**

- **All languages except C#:** an operation the contract takes from a contract it extends or embeds is not counted. It belongs to the contract that declares it.
- **C#:** some inherited members count, by the inherited-line rule under Pending re-definition.

## Thresholds
- **Width:** a contract with more than 15 operations (16 or more) is fat. In C# there is one exception: a contract is not fat when every counted member has a default body.
- **Stubbing implementers (Rust only):** a trait is also fat when enough of its implementations in production code stub its methods. The counts are under Pending re-definition.
  - A **stub** is a method body that does nothing with what it was handed.
  - That is an empty body, or a body that only says the work is not done (for example `todo!`, `unimplemented!`, `unreachable!` or `panic!`).
  - It is also a single expression that uses neither `self` nor any input and returns either a fixed constructed value, where the method takes inputs, or an empty, absent or error value (for example `None`, `()`, `Err(..)`, `X::default()`, `vec![]`, or `Ok`/`Some` of one of these).
- **Cohesive contracts (all languages except C# and Rust):** a contract whose width is one responsibility stays in the population but is never fat. These are cohesive:
  - **An exhaustive visitor:** one operation for each case of a closed set of types it walks, each operation taking its case, and nothing else.
  - **A client of a remote service:** every operation is mapped to an HTTP endpoint, for example with `@GET` or `@PostMapping`.
  - **A tagless-final algebra:** the contract has a type parameter, and every operation returns that parameter applied to a result (for example `F[Receipt]`).
- **Covered share:** the dimension is scored only when the covered languages hold at least half (50 %) of the repository's production lines. See Not measured when.

## Score
`score = 6 + 4 × (1 − f / n)`, rounded to 2 decimals.

- If no contract is fat, the score is 10.
- If every contract is fat, the score is 6, and it never goes lower.
- In between, the score falls in a straight line as the share of fat contracts grows.

## Not measured when
- **No contract to judge:** the repository's production code is in languages this dimension covers but declares no contract (`n = 0`). The dimension is not applicable. A repository written only in JavaScript is one example.
- **Contracts outside the scope:** the repository's production code is in languages outside this dimension's scope. The dimension is not measured. Such a repository is outside the scope; it is not a repository without contracts.
- **Too little of the code covered:** the covered languages (the list above, with JavaScript) hold less than half (50 %) of the repository's production lines, as [production-code.md](../production-code.md) counts them. The dimension is then not scored on the rest. At 50 % or more it is scored, and the languages it did not read are disclosed with the result.

This 50 % cut-off is this dimension's own rule. It is separate from the 95 % rule in [production-code.md](../production-code.md), which applies to the whole repository: it decides whether an engine may publish the repository's CAI at all, based on the languages that engine reads.

## What the result is not
- It does not check which operations each caller actually uses. Width, and in Rust stubbing, stand in for that.
- In C#, a contract can be fat only because of overloads: many signatures but few operations. Collapsing the overloads, not splitting the contract, would then remove the burden. The contract still counts as fat. In the other languages an overload set counts once, so overloads alone cannot make a contract fat.
- Outside C#, a contract that is wide only through what it inherits is not fat. Each contract is judged on the operations it declares itself.
- It is not a measure of class size or cohesion. A concrete class with many methods is not a contract.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them. A later decision under decision 0013 will replace each with a threshold on the property.

- **Covered share:** the dimension is scored only when the covered languages hold at least 50 % of the repository's production lines.
- **C# inherited line:** this rule applies to an interface that declares from 1 to 15 members of its own and extends exactly one interface of the repository's production code.
  - Follow that line upward. Add the base, then the base's own single base, and so on, while each base declares members of its own.
  - The line ends at a base that has two or more such bases, or at a base that declares nothing.
  - The members along the line are added to the width, unless those members number more than 15 on their own.
  - Platform interfaces do not count as bases.
- **Rust stubbing implementers:** a trait is fat when all of these hold:
  - it has at least 6 required methods (associated types and constants do not count here);
  - at least 2 implementations each stub at least 3 of those methods;
  - each of those implementations stubs at least half of those methods.
- **Exhaustive visitor:** each operation is named after the type of the case it handles, and the words before those type names are at most 2 distinct verbs.
