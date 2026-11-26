# PF2: Allocation hygiene

**Lens:** performance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property
Whether the code is written to minimise allocations so it doesn't pressure its host's memory manager — buffer/slice views over copies, object pooling, stack or value-type allocation, and buffer writers. Reward-only: credited where present, never penalised where a simpler style is fine.

## Applies to
- **Measured:** C#, F#, Visual Basic .NET, Go, Java, Kotlin, Scala, Swift and Dart.
- **Not applicable:** Python, JavaScript, TypeScript, Ruby, PHP, Elixir and Erlang. In these languages the runtime makes most allocation decisions, so the standard does not rate allocation-aware style in them today.
- **Rust is also not applicable.** Its ordinary parameter types are borrowed slices, and every heap allocation is written out. The style this dimension rewards is already how the language is normally written.
- This dimension does not read any other language.

## Population
The population is production code as [production-code.md](../production-code.md) defines it, in the measured languages. Tests and benchmark code are not part of it. This dimension adds no exclusions of its own.

## Unit and denominator
**Unit:** one use in code of an allocation-aware idiom.

- Comments and string literals do not count.
- Every occurrence of an idiom in the code is one use. The same idiom written again, in the same file or in another file, is another use. One occurrence is never counted twice.
- A "presized" collection or builder is one created with an explicit initial capacity. The capacity must be a size, not another collection to copy.

What counts, by language:

- **C#:**
  - the span and memory types `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>` and `ReadOnlyMemory<T>`
  - the `ArrayPool<T>`, `MemoryPool<T>` and `ObjectPool` pools
  - `stackalloc`
  - `ValueTask`
  - a `readonly struct` or `ref struct` declaration
  - `IBufferWriter<T>`
  - the `string.Create` method
  - the `SkipLocalsInit` attribute
- **F#:**
  - The C# idioms in F# form:
    - span and memory types, and the `AsSpan` and `AsMemory` methods
    - the three pools
    - `NativePtr.stackalloc`
    - `ValueTask` and the `valueTask` computation expression
    - `IBufferWriter`
    - the `String.Create` method
    - the `SkipLocalsInit` attribute
  - Value-type choices:
    - the `Struct`, `IsByRefLike` and `IsReadOnly` attributes
    - struct tuples
    - value options (`voption`, `ValueOption`, `ValueSome`)
    - the `InlineIfLambda` attribute
  - A presized `ResizeArray`, `StringBuilder`, `Dictionary` or `HashSet`.
  - These do not count: `inline`, `Array.zeroCreate`, `ValueNone`, and `byref` or `inref` parameters.
- **Visual Basic .NET:**
  - span and memory types, and the `AsSpan` and `AsMemory` methods
  - the three pools
  - `ValueTask`
  - `IBufferWriter(Of T)`
  - the `String.Create` method
  - the `SkipLocalsInit` attribute
  - a presized `List`, `StringBuilder`, `Dictionary` or `HashSet`
  - A plain `Structure` does not count.
- **Go:**
  - a `sync.Pool`
  - a `Grow` method call
  - the `strconv` append functions (for example `strconv.AppendInt`)
  - a slice made with both a length and a capacity
  - a map made with a size hint
  - reslicing a slice to zero length to reuse its backing array
  - A `bytes.Buffer` alone does not count.
- **Java, Kotlin and Scala:**
  - Primitive-specialised collections: a collection type named for a primitive key or element (for example `IntArrayList`, `Int2ObjectOpenHashMap` or `IntIntHashMap`), and Android sparse arrays (for example `SparseIntArray`).
  - Object and buffer pools, for example a `GenericObjectPool`, an `ObjectPool`, a Netty `Recycler` or a `PooledByteBufAllocator`.
  - NIO buffers (for example `ByteBuffer`) and `MemorySegment`.
  - A presized `StringBuilder`, `ArrayList`, `HashMap`, `HashSet`, `ArrayDeque` or `LinkedHashMap`, and the `HashMap.newHashMap` and `HashSet.newHashSet` factory methods.
  - **Kotlin adds:**
    - unboxed primitive arrays (`IntArray`, `LongArray`, `FloatArray`, `DoubleArray`, `ShortArray`) and their builder functions (for example `intArrayOf`)
    - primitive collections (for example `IntList` or `MutableIntObjectMap`)
    - inline value classes (`value class`, `@JvmInline`)
    - `ByteArray` and `CharArray` do not count.
  - **Scala adds:** value classes (`extends AnyVal`) and the `specialized` annotation.
- **Swift:**
  - the `reserveCapacity` method
  - `ContiguousArray`
  - the unsafe buffer and byte access methods: `withUnsafeBufferPointer`, `withUnsafeMutableBufferPointer`, `withUnsafeBytes`, `withUnsafeMutableBytes` and `withUnsafeTemporaryAllocation`
  - `ManagedBuffer`
  - noncopyable types (`~Copyable`)
  - the `borrowing` and `consuming` parameter modifiers
- **Dart:**
  - Typed-data lists other than `Uint8List` (`Int8List` through `Float64List`).
  - The SIMD types `Float32x4`, `Int32x4` and `Float64x2` and their lists.
  - Views rather than copies: the `sublistView` method, the `Uint8List.view` and `ByteData.view` constructors, the typed `as...List` view methods (for example `asInt32List`), and `asByteData`.
  - A fixed-length list (`growable: false`), a non-copying `BytesBuilder` (`copy: false`), and `TransferableTypedData`.
  - These do not count: `Uint8List` alone, `ByteData` alone, `List.filled`, `const` and extension types.

**Languages read together:**
- If the repository has any production C#, the uses and lines come from C# alone. The other measured languages are not read for this dimension.
- Otherwise, the uses and lines are pooled across F#, Visual Basic .NET, Go, Java, Kotlin, Scala, Swift and Dart.
- The activation rule under Thresholds uses the same set of languages.

**Denominator:** the lines of the production files in the languages read together. Today's reading counts these lines as follows (see Pending re-definition):
- **C#:** every line that is not blank. Comment-only lines count.
- **The other measured languages:** every line of the file, including blank and comment-only lines.

**Density:** uses × 1,000 ÷ lines, which gives uses per 1,000 lines. With no lines, the density is 0.

## Thresholds
- **When the dimension applies:** the languages read together hold at least 400 lines, counted the same way as the denominator, and one of the conditions below holds. The conditions depend on which languages are read:
  - **With production C#:** the repository is a packaged library (code built and published as a reusable package), or it ships a benchmark suite in C#, or C# has at least 8 uses.
  - **Without production C#:** the repository ships a benchmark suite in one of the measured languages (for example a Go benchmark suite, a JVM benchmark project, or a Swift, Dart, F# or Visual Basic .NET benchmark project), or the pooled languages have at least 8 uses. Being a packaged library does not count here. A benchmark suite in a language this dimension does not measure does not count either.
- **Saturation:** a density of 8 or more uses per 1,000 lines scores the maximum.
- **Floor:** a density of 0 scores 6.0.

## Score
score = 6.0 + min(density, 8) ÷ 8 × 4.0

The result is clamped to the range 6.0 to 10.0 and rounded to one decimal. An exact midpoint rounds to the even digit, so 6.25 becomes 6.2 and 6.35 becomes 6.4.

The score rises in a straight line from 6.0 with no uses to 10.0 at 8 uses per 1,000 lines. Nothing lowers it below 6.0.

## Not measured when
- **The activation rule is not met:** the languages read together fail the rule under Thresholds. A repository with production C# that fails the rule in C# is not measured, even if its other measured languages would pass. A repository without C# that is a packaged library, has fewer than 8 uses and has no benchmark suite in a measured language is not measured.
- **Only languages that are not applicable:** all the production code is in languages listed as not applicable. The result is then "not applicable", with the reason.
- **No language this dimension reads:** the repository has no production code in any of the measured languages.

## What the result is not
- It counts uses of allocation-aware idioms, not allocations measured while the code runs. Code can allocate little without these idioms, or use them and still allocate.
- It does not know which code is hot. A use anywhere counts the same.
- It penalises nothing. A score of 6.0 means no idiom was found, not that the code has an allocation problem.
- No published score includes it today, because the one existing engine reports it advisory.

## Pending re-definition
These are parameters of the existing reading's model. Today's reading depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Line count:** the denominator and the 400-line floor count lines as a size measure, not production lines as [production-code.md](../production-code.md) defines them. In C#, every non-blank line counts, comment-only lines included. In the other measured languages, every line of the file counts, blank and comment-only lines included.
- **Activation:** at least 400 lines (counted as above) plus one condition. With production C#, the condition is a packaged library, a benchmark suite in C#, or at least 8 uses. Without C#, it is a benchmark suite in a measured language or at least 8 uses, and a packaged library does not count. The rule decides by what kind of repository it is, not by whether the code puts pressure on memory.
- **C# first:** where production C# exists, only C# is read. The other measured languages are pooled only when there is no C#.
- **Floor of 6.0:** the score for no uses.
- **Saturation at 8 uses per 1,000 lines:** the density that earns 10.0.
