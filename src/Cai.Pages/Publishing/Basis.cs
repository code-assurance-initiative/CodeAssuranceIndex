namespace Cai.Pages.Publishing;

/// <summary>
/// What a reading was taken over, in both the forms a page needs it: one of them, and more than one.
/// </summary>
/// <remarks>
/// <para>★★ THE DEFECT. <see cref="Figure"/> writes a count as <c>"&lt;n&gt; &lt;basis&gt;"</c> and every
/// basis in this system was authored as a plural phrase, so a corpus holding exactly one language published
/// "1 languages", one advisory published "1 advisories", and an archive read on one day published "1 dated
/// readings". The corpus e2e driver named eight of them in one run. It is not a fixture artefact: prod prints
/// it the first day a language, an advisory or a country sits at exactly one, and a reader who meets it stops
/// trusting the arithmetic beside it.</para>
///
/// <para>★★ AND IT IS A TYPE RATHER THAN A FORMATTING HELPER, BECAUSE A HELPER WOULD HAVE TO GUESS. Chopping
/// an "s" turns "advisories" into "advisorie" and "countries" into "countrie"; teaching it those two leaves it
/// wrong on the third irregular noun somebody adds, silently, on a published page. English pluralisation is
/// not derivable from a suffix, so the singular is not computed — it is AUTHORED, next to the plural, by
/// whoever knew what the population was. There is no constructor that takes one form, so a basis added later
/// cannot be written without both.</para>
///
/// <para>★ THE POPULATION CHOOSES THE FORM, AND THE POPULATION IS THE ONE NUMBER EVERY KIND OF READING HAS. A
/// count's is its own value; a ratio's is its DENOMINATOR, which is why "1 of 1 survey" comes out right; a
/// scalar's is what it was taken across; a floor's is its bound. One rule, applied inside
/// <see cref="Figure"/>, so no renderer can disagree with another about how many there are.</para>
/// </remarks>
public sealed record Basis
{
    private Basis(string one, string many)
    {
        One = one;
        Many = many;
    }

    /// <summary>The phrase for exactly one — <c>"advisory"</c>, <c>"dated reading"</c>.</summary>
    public string One { get; }

    /// <summary>The phrase for any other number — <c>"advisories"</c>, <c>"dated readings"</c>.</summary>
    public string Many { get; }

    /// <summary>
    /// The population, in both forms.
    /// </summary>
    /// <param name="one">What one of them is called.</param>
    /// <param name="many">What more than one of them are called.</param>
    /// <returns>The basis.</returns>
    /// <exception cref="ArgumentException">
    /// Either form is blank, or the two are the same. ★ THE SAMENESS CHECK IS THE WHOLE GUARD: passing the
    /// plural twice is exactly how a caller in a hurry would keep the defect while satisfying the compiler,
    /// and it is indistinguishable from a considered decision unless it is refused. A noun whose singular and
    /// plural genuinely coincide is a real thing in English and has no example in this corpus today — when one
    /// arrives, give it its own factory and say in writing which noun it is, rather than widening this one.
    /// </exception>
    public static Basis Of(string one, string many)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(one);
        ArgumentException.ThrowIfNullOrWhiteSpace(many);

        var singular = one.Trim();
        var plural = many.Trim();

        return string.Equals(singular, plural, StringComparison.Ordinal)
            ? throw new ArgumentException(
                $"'{singular}' was given as both the singular and the plural of a basis. If it really is one "
                + "of the nouns whose forms coincide, say so with a factory of its own; if it was the plural "
                + "typed twice, write the singular — a page holding exactly one of them prints it.",
                nameof(one))
            : new Basis(singular, plural);
    }

    /// <summary>
    /// The form a population of <paramref name="population"/> takes.
    /// </summary>
    /// <param name="population">How many the reading was taken over.</param>
    /// <returns>The singular at exactly one, the plural otherwise — zero of them included.</returns>
    /// <remarks>
    /// ★ ZERO IS PLURAL, which is English rather than an oversight: "0 advisories" is right and "0 advisory"
    /// is not. Only one is singular.
    /// </remarks>
    public string For(long population) => population == 1 ? One : Many;

    /// <summary>The plural, which is what a basis reads as when no population is in hand.</summary>
    public override string ToString() => Many;
}
