# DM9: Scattered domain decisions

**Lens:** domainModelling · **Scored:** yes

## Property
Whether one domain rule is decided — or one measurement produced — in more than one place, rather than living on the type that owns it. A repeated judgement or a repeated derivation, not repeated text, so a clone detector cannot see either; and centralising the judgement does not fix the derivation, because two producers of the same numbers can disagree while every rule reading them is correct.

## Applies to
This dimension applies to any programming language on the production-lines list in which code declares named types with members, for example classes, structs, records and interfaces. Code written outside any type (for example a module's top-level functions) belongs to its file's module, and that module is the place where the code sits.

It does not apply to markup (HTML) or styles (CSS).

## Population
The population is production code as [production-code.md](../production-code.md) defines it. This dimension adds nothing to that population and leaves nothing more out.

The repository is read as a whole. A rule or a measurement repeated across two projects, packages or modules of one repository counts the same as one repeated inside a single project.

An **owner** must be a named type declared in the repository's production code. These are never owners:
- types from libraries or from the language's standard library;
- types with no declaration, for example tuples and anonymous types. Their member names belong only to the place that writes them.

## Unit and denominator
The dimension counts two kinds of unit and adds them together. There is no denominator: the score reads an absolute count.

**1. A scattered decision.**
- A **comparison** tests a value with equal, not equal, less than, at most, greater than or at least, in any spelling the language offers. Examples are an operator, a negated operator, or a pattern that tests a member against a relational bound. A negation folds into the operator, and operand order does not matter, so `0 < o.Total` and `!(o.Total <= 0)` are both "Total greater than 0". Choosing among branches by a member's value (for example a `switch` on a status) is dispatch, not a comparison.
- A **predicate** is one boolean condition. It is made of comparisons joined by and, or, not and grouping.
- A **judged member** is a property or instance field that a predicate compares. These are not judged members:
  - a constant, an enum value or a static read-only value. Each of these is the value a member is compared with;
  - a member that the predicate only passes as an argument, or only builds into another value (for example into a string).
- A **rule** is a predicate's judgement of the members of one owner. It needs at least two different judged members of that owner. One predicate can carry rules about several owners.
- A rule is **decided away** when its predicate sits in a place other than its owner. A place is a type, or a module for code outside types. A nested type is its own place. A rule decided on its own owner is never counted.
- Two predicates ask the **same rule** when all of these hold:
  - they judge the same owner;
  - they judge the same set of members of that owner;
  - for every member in that set, some comparison in one predicate and some comparison in the other can be true for the same value.

  These pairs of comparisons cannot both be true:
  - Two comparisons against fixed values (literals, constants, enum values, null) where no single value satisfies both. Examples are `> 5` and `< 3`, or equality with two different fixed values.
  - Two comparisons against the same named value (the same variable, parameter, member or static read-only value) whose operators no single value can satisfy together. Examples are `o.X > limit` and `o.X <= limit`, `o.X > limit` and `o.X < limit`, `o.X == limit` and `o.X < limit`, or `o.X == limit` and `o.X != limit`.

  Predicates whose comparisons are like that ask different rules. Every pair not covered above counts as able to be true together. That includes comparisons against two different named values, against a named value and a fixed one, or against a value that cannot be identified. A copy that has drifted still asks the same rule (for example `BusFactor <= 1` and `BusFactor == 1`). Same-rule is transitive: if A asks the same rule as B, and B asks the same rule as C, all three are one rule.
- **One scattered decision** is one rule that is decided away in at least 2 distinct places. A place that restates a rule several times counts as one place.

**2. A scattered derivation.**
- A **measurement type** is a class, struct or record declared in production code that meets all of these:
  - it has no base type other than the language's root type;
  - it has exactly one non-private constructor written for it. A record's generated copy constructor does not count. A record whose header declares its constructor has exactly one.
  - it declares no static method that returns the type (a factory);
  - it has at least 2 parts, and all of them are numeric. The parts are its public instance properties or, if it has none, its instance fields. Numeric means integer, floating-point or decimal, or a nullable form of these. Characters, booleans and text are not numeric.
- A construction **produces** a measurement when its result is one of these:
  - the value that a method, property or accessor returns directly;
  - the value stored in a variable, field or property;
  - a member's expression body.

  A construction in any other position only passes the value. Examples are a construction passed inline as an argument, one passed as the argument of a returned call, and one returned by an anonymous function.
- **One scattered derivation** is one measurement type that is produced in at least 2 distinct places other than the type itself.

**Count:** n is the number of scattered decisions plus the number of scattered derivations. Each rule and each measurement type counts once, however many places hold it.

## Thresholds
- A rule judges at least **2** different members of one owner. A check on one member is never counted.
- A rule is scattered when it is decided in at least **2** places other than its owner.
- A measurement type has at least **2** numeric parts.
- A measurement is scattered when it is produced in at least **2** places other than its own type.

## Score
- If n = 0, the score is **10**.
- If n ≥ 1, the score is **6 + 4 × (1 − min(1, n / 10))**, rounded to 2 decimals.

Each counted unit lowers the score by 0.4. At 10 or more units the score stays at its floor of **6**.

The dimension is advisory. Its score is reported, but today it is not part of the Domain Modelling lens score or the repository's CAI.

## Not measured when
The dimension is not measured when the repository's production code has no language in which code declares named types with members.

In every language this dimension applies to, both kinds of unit are counted over the whole population. A reading that counted only one kind, or only part of the population, has not fully measured this dimension, and its count is not n.

## What the result is not
- It does not measure duplicate text. Two copies of one rule written in different words count. Identical text that asks two different questions does not.
- It does not judge whether a rule is correct. It shows only that the rule has more than one home.
- It does not count a rule over a single member, even when that rule is copied verbatim.
- It does not count a measurement type that has a base type, more than one constructor, a factory or a non-numeric part, even when two places produce it.
- It does not count rules about library types, tuples or anonymous types.
- It is not a density. A large repository and a small one with the same count get the same score.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each one with a threshold on the property.

- **What a measurement is.** The shape conditions are: no base type, exactly one constructor, no factory, and only numeric parts.
- **What producing is.** Only a construction that is returned, stored or used as an expression body counts as producing. Every other position counts as passing.
- **Curve:** a floor of 6, reached at 10 counted units.
