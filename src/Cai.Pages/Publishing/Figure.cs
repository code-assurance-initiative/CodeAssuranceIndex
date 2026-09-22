using System.Globalization;

namespace Cai.Pages.Publishing;

/// <summary>
/// A number that is published, together with what it is a number OF: the population it was taken over, the
/// phrase that names that population, and the instant of the reading. Constructed only through
/// <see cref="Ratio"/>, <see cref="Count"/>, <see cref="Scalar"/> and <see cref="Floor"/>, each of which
/// refuses a reading that cannot be true.
/// </summary>
/// <remarks>
/// <para>★★ THE PUBLIC CONFUSION THIS EXISTS FOR. The site showed "53.5 median" — the median of the
/// per-language medians — while another true reading of the same corpus is 52.6, the median across
/// repositories. Both are correct. Neither said which population it was taken over, so a reader holding both
/// concluded that one of them was a lie.</para>
///
/// <para>★★ AND THE WORSE SHAPE: a percentage outlives its denominator. "76.8 % carry a known-vulnerable
/// component" is true of the 1,588 projects whose dependencies could be resolved and FALSE of the 3,464 in the
/// corpus. Copy-paste moves the percentage and leaves the denominator behind, and the sentence still reads
/// perfectly well. So there is nothing here to copy that is missing its population: the renderers below are the
/// only way to write the value down, and every one of them names the basis.</para>
///
/// <para>★ The share of a <see cref="Ratio"/> is DERIVED from the two counts rather than supplied beside them.
/// A caller cannot hand over "76.8" alongside "1,219 of 1,588" and have the two disagree, because there is no
/// parameter in which to hand over the 76.8.</para>
///
/// <para>★ <see cref="Value"/> is public for computation and for machine mirrors (JSON/CSV), where the
/// denominator travels in its own field. It is NOT for a page: a formatted <see cref="Value"/> on a page is a
/// bare number again, and the convention test in <c>Kennel.Architecture.Tests</c> is what says so.</para>
///
/// <para>A record with get-only properties, deliberately: structural equality for free, and <c>with</c> cannot
/// reach any of the parts — so a median cannot be given a numerator after its factory decided it has none.</para>
/// </remarks>
public sealed record Figure
{
    /// <summary>How a count is written for a reader: thousands separated, never a float's full expansion.</summary>
    private const string CountFormat = "#,0";

    /// <summary>A reading as a reader writes it: one decimal at most, thousands separated for a large total.</summary>
    private const string ReadingFormat = "#,0.#";

    /// <summary>A share, always to one decimal — <c>76.8</c>, and <c>100.0</c> when it is all of them.</summary>
    private const string ShareFormat = "0.0";

    /// <summary>An unambiguous day, the same in every locale a reader might be in.</summary>
    private const string DayFormat = "d MMMM yyyy";

    /// <summary>The instant of the reading, to the minute and in UTC — see <see cref="Provenance"/>.</summary>
    private const string InstantFormat = "d MMMM yyyy 'at' HH:mm 'UTC'";

    private Figure(
        FigureKind kind,
        double value,
        long? numerator,
        long population,
        long? unseenPopulation,
        Basis basis,
        DateTimeOffset takenAt)
    {
        Kind = kind;
        Value = value;
        Numerator = numerator;
        Population = population;
        UnseenPopulation = unseenPopulation;
        Basis = basis;
        TakenAt = takenAt;
    }

    /// <summary>What shape of reading this is, which is what decides how it is written down.</summary>
    public FigureKind Kind { get; }

    /// <summary>
    /// The reading. For a <see cref="FigureKind.Ratio"/> this is the share as a fraction of one (0.768, not
    /// 76.8) and is derived from <see cref="Numerator"/> and <see cref="Population"/>; for a
    /// <see cref="FigureKind.Count"/> it is the tally; for a <see cref="FigureKind.Scalar"/> it is the median,
    /// mean or total the caller measured; for a <see cref="FigureKind.Floor"/> it is the BOUND — the largest
    /// number the reading can defend, with the truth somewhere between it and it plus
    /// <see cref="UnseenPopulation"/>. Always finite.
    /// </summary>
    /// <remarks>
    /// ★ A floor's value is the one that must never be summed or ranked against another figure's:
    /// "at least 240" plus "180" is at least 420 and not 420, and ordering advisories by a censored count
    /// orders them by how truncated their briefs were. This type offers no arithmetic and no ordering to any
    /// kind, so there is no operator a floor can ride in on — see the reflection test that keeps it that way.
    /// A machine mirror (JSON/CSV) must carry <see cref="Kind"/> and <see cref="UnseenPopulation"/> in their
    /// own fields for the same reason it already carries the denominator in its own field.
    /// </remarks>
    public double Value { get; }

    /// <summary>
    /// The counted part of a share — <c>1,219</c> of <c>1,588</c> — and <c>null</c> for every other kind,
    /// because a median has no numerator and must not be able to acquire one.
    /// </summary>
    public long? Numerator { get; }

    /// <summary>
    /// The population the reading was taken over: the DENOMINATOR of a share, the set a median or a total was
    /// taken across, or — for a count or a floor — the tally itself. Never negative, and never zero except for
    /// a count or a floor.
    /// </summary>
    public long Population { get; }

    /// <summary>
    /// The short phrase naming the population, as a reader meets it: <c>"projects whose dependencies
    /// resolved"</c>, <c>"languages with a field guide"</c>. Non-blank and trimmed. It reads after "of" or
    /// "across", so it is a plural noun phrase rather than a sentence.
    /// </summary>
    public Basis Basis { get; }

    /// <summary>The instant the reading was taken, normalised to UTC. Never <c>default</c>.</summary>
    public DateTimeOffset TakenAt { get; }

    /// <summary>
    /// How many members of the population could NOT be looked at, so that the reading above them is a lower
    /// bound: <c>1,204</c> surveys whose advisory list was cut short, any of which may belong to the count
    /// without having been countable. Positive for a <see cref="FigureKind.Floor"/> and <c>null</c> for every
    /// other kind, because a measurement has no blind spot and must not be able to acquire one.
    /// </summary>
    public long? UnseenPopulation { get; }

    /// <summary>
    /// A share of a population: <paramref name="numerator"/> out of <paramref name="denominator"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The denominator is not positive (0/0 is not 0 %, so a caller whose group came back empty must publish
    /// nothing rather than a confident zero); either count is negative; or the numerator exceeds the
    /// denominator, which means the two were counted over different populations — the copy-paste defect itself.
    /// </exception>
    public static Figure Ratio(long numerator, long denominator, Basis basis, DateTimeOffset takenAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numerator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(numerator, denominator);

        return new Figure(
            FigureKind.Ratio,
            (double)numerator / denominator,
            numerator,
            denominator,
            null,
            Named(basis),
            Dated(takenAt));
    }

    /// <summary>
    /// A tally of a population — <c>3,464 surveyed repositories</c> — where the count IS the population.
    /// </summary>
    /// <remarks>
    /// ★ Zero is allowed here and refused everywhere else, deliberately. For a ratio or a scalar the population
    /// is a denominator and zero means the reading does not exist; for a count the population is the
    /// measurement, and "0 projects whose dependencies did not resolve" is a true, publishable sentence.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    public static Figure Count(long count, Basis basis, DateTimeOffset takenAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return new Figure(FigureKind.Count, count, null, count, null, Named(basis), Dated(takenAt));
    }

    /// <summary>
    /// A LOWER BOUND on a tally, taken over a population part of which could not be looked at: at least
    /// <paramref name="atLeast"/> of them, with <paramref name="unseenPopulation"/> more that nobody could
    /// check either way.
    /// </summary>
    /// <param name="atLeast">What was actually counted — the floor, never the estimate above it.</param>
    /// <param name="unseenPopulation">
    /// How many members of the population could not be looked at. Zero means nothing was withheld, and the
    /// reading is returned as an exact <see cref="Count"/> — see the remarks.
    /// </param>
    /// <param name="basis">
    /// The phrase naming what is being counted — <c>"surveys carrying this advisory"</c>. It names the THING,
    /// not the hedge: the hedge is this type's job now, and a basis that carries it as well says it twice.
    /// </param>
    /// <param name="takenAt">The instant the reading was taken.</param>
    /// <returns>The floor, or an exact <see cref="Count"/> when nothing was unseen.</returns>
    /// <remarks>
    /// <para>★★ THE CENSORED COUNT, WHICH USED TO BE A CONVENTION. A survey's advisories survive only in its
    /// dimension brief's rendered list of affected locations, and that list stops at fifty entries per kind
    /// while the metrics block above it carries the true total. "240 surveys carry this advisory" is therefore
    /// not a measurement but the largest defensible number. That was expressed by writing the caveat into the
    /// basis phrase, typing "at least" in the prose beside it and putting a second count of the truncated
    /// surveys further down the page — three things a later caller can each leave out, after which a floor
    /// reads exactly like a measurement. A convention is not a guard. The blind spot travels inside the figure
    /// now, and every renderer states it.</para>
    ///
    /// <para>★ NOT A <see cref="Ratio"/>. A ratio derives an exact share and has no field in which to record
    /// that its numerator was censored, so a floor made into one is wrong by an unknown amount while looking
    /// precise to one decimal.</para>
    ///
    /// <para>★ ZERO UNSEEN IS NOT A FLOOR, IT IS A COUNT, and this normalises rather than refuses. The call
    /// site reads <c>Figure.Floor(seen, cut.SurveysTruncated, …)</c>, and on a sweep where no brief was cut
    /// short that argument is 0 — a legitimate reading that a throw would turn into a caller's try/catch or a
    /// branch at every call site. Nothing was withheld, so nothing is a floor: publishing "at least 240" when
    /// 240 is exact tells a reader there is doubt where there is none, and an understatement is a misstatement
    /// too.</para>
    ///
    /// <para>★ The two counts come FIRST, together, and the instant stays last — the shape every other factory
    /// here has. The blind spot is not a trailing extra that a call written from memory can omit; leaving it
    /// out does not compile.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either count is negative.</exception>
    public static Figure Floor(long atLeast, long unseenPopulation, Basis basis, DateTimeOffset takenAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(atLeast);
        ArgumentOutOfRangeException.ThrowIfNegative(unseenPopulation);

        if (unseenPopulation == 0)
        {
            return Count(atLeast, basis, takenAt);
        }

        return new Figure(
            FigureKind.Floor, atLeast, null, atLeast, unseenPopulation, Named(basis), Dated(takenAt));
    }

    /// <summary>
    /// A reading taken OVER a population without being a share of it: a median, a mean, a total.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not a finite number (an empty group divided by itself upstream arrives here as NaN, and
    /// "NaN" renders on a page looking deliberate), or the population is not positive — the median of nothing
    /// is not zero.
    /// </exception>
    public static Figure Scalar(double value, long population, Basis basis, DateTimeOffset takenAt)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, "a published figure must be a finite number");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(population);

        return new Figure(FigureKind.Scalar, value, null, population, null, Named(basis), Dated(takenAt));
    }

    /// <summary>
    /// The reading with its population, for a headline or a stat: <c>76.8% of projects whose dependencies
    /// resolved (1,219 of 1,588)</c>, <c>52.6 across 3,464 surveyed repositories</c>, <c>3,464 surveyed
    /// repositories</c>, <c>at least 240 surveys carrying this advisory — a further 1,204 could not be
    /// checked</c>. Undated — pair it with <see cref="Provenance"/>, or use <see cref="ToString"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// A <see cref="FigureKind"/> nobody taught this method to write down. The arm exists because the switch
    /// used to end in a catch-all, and a kind added later would have been rendered AS A SCALAR — silently, and
    /// on a page. A reading nobody has decided how to write is refused rather than guessed at.
    /// </exception>
    public string Headline() => Kind switch
    {
        FigureKind.Ratio =>
            $"{(Value * 100d).ToString(ShareFormat, CultureInfo.InvariantCulture)}% of {Counted()} "
          + $"({Numerator!.Value.ToString(CountFormat, CultureInfo.InvariantCulture)} of "
          + $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)})",
        FigureKind.Count => $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}",
        // ★ "at least" first, so the hedge is read before the number rather than after it, and the blind spot
        // in the same breath — the two halves a stat tile cannot separate because they are one string.
        FigureKind.Floor =>
            $"at least {Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()} — a further "
          + $"{UnseenPopulation!.Value.ToString(CountFormat, CultureInfo.InvariantCulture)} could not be checked",
        FigureKind.Scalar =>
            $"{Value.ToString(ReadingFormat, CultureInfo.InvariantCulture)} across "
          + $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}",
        _ => throw new NotSupportedException($"no way to write down a {Kind} figure has been decided"),
    };

    /// <summary>
    /// A TOTAL, written as a sentence that says what it is a total OF: <c>63,266 findings were found across
    /// the 1,888 surveys whose dependencies a scanner could resolve</c>. The caller supplies the middle —
    /// what the value counts and the verb that carries it — and this writes the number and the population.
    /// </summary>
    /// <param name="counting">
    /// What the value counts, as the predicate of the sentence: <c>findings were found</c>,
    /// <c>committed secrets were found</c>. Never blank — a total that does not say what it counts is the
    /// defect this member exists to remove.
    /// </param>
    /// <returns>The sentence, undated.</returns>
    /// <remarks>
    /// <para>★★ WHY A SECOND RENDERER RATHER THAN A SENTENCE AROUND <see cref="Headline"/>. A scalar's
    /// headline is "63,266 across 1,888 surveys whose dependencies a scanner could resolve" — the value, then
    /// the population. A caller wanting to say what the value counts had nowhere to put the noun but the END,
    /// and the corpus sheet did exactly that: "63,266 across 1,888 surveys whose dependencies a scanner could
    /// resolve were found across them", which says "across" twice and never says what 63,266 is. The noun
    /// belongs between the number and its population, and only a renderer that writes both can put it there.
    /// </para>
    ///
    /// <para>★ A SCALAR AND NOTHING ELSE, BECAUSE THE SENTENCE IS ABOUT A TOTAL. A ratio's value is a share
    /// and a count's value IS its population, so neither has a middle to fill; asking for one would produce a
    /// sentence that reads as a measurement of something nobody measured. A median is a scalar too and would
    /// render here as "52.6 medians were found across…", which is why the parameter is the caller's whole
    /// predicate rather than a bare noun this could pluralise on its own.</para>
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="counting"/> is blank.</exception>
    /// <exception cref="NotSupportedException">This figure is not a <see cref="FigureKind.Scalar"/>.</exception>
    public string Total(string counting)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(counting);

        if (Kind is not FigureKind.Scalar)
        {
            throw new NotSupportedException(
                $"a {Kind} figure is not a total: its value is not a quantity taken across the population");
        }

        return $"{Value.ToString(ReadingFormat, CultureInfo.InvariantCulture)} {counting.Trim()} across the "
             + $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}";
    }

    /// <summary>
    /// The reading split across the two slots a stat cell has: the value for the display slot, and the
    /// population it was taken over for the small line beneath it. <c>76.8%</c> over <c>1,161 of 1,511 surveys
    /// whose dependencies a scanner could resolve</c>; <c>at least 240</c> over <c>surveys carrying this
    /// advisory — a further 1,204 could not be checked</c>.
    /// </summary>
    /// <returns>Both halves, always together.</returns>
    /// <remarks>
    /// <para>★★ WHY THIS EXISTS AT ALL. A stat cell's display slot is an <c>&lt;h2&gt;</c>, and every page in
    /// the public corpus handed it <see cref="Headline"/> — so the whole sentence, population and counts
    /// included, was set in heading type. Four lines per cell, in every cell, on every page: correct in every
    /// particular and unreadable. What a reader expects of a statistic is the value large and the population
    /// small beneath it, which is the same information, under the same guarantee, as an overview rather than a
    /// headache.</para>
    ///
    /// <para>★★ AND WHY IT IS NOT A <c>Value()</c>. "76.8%" on its own is exactly the share that outlives its
    /// denominator — the defect this whole type is built against, and one that reads perfectly well while being
    /// false about every population but one. So the lead is not a member: it is one half of a value that always
    /// carries the other, there is nothing here that yields it alone, and the reflection sweeps over this type
    /// read both halves together and hold them to the same rule as <see cref="Headline"/>. The other half of
    /// that guarantee lives at the page: <c>PageNodes.Stat</c> takes a <see cref="Figure"/> and does this split
    /// itself, so a page builder hands over the figure and has no way to hand over half of it.</para>
    ///
    /// <para>★ A FLOOR HEDGES IN THE LEAD. "76.8%" missing its denominator is an imprecise claim; "240" missing
    /// "at least" is a DIFFERENT claim — a censored count printed as a measurement, in the largest type on the
    /// page, where a reader takes it in without reading anything else. The hedge is to a floor what the percent
    /// sign is to a share: part of the value rather than a qualification of it. The size of the blind spot goes
    /// in the support, where the basis is, and neither can be dropped because neither can be had alone.</para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// A <see cref="FigureKind"/> nobody has decided how to split. Explicit per kind and throwing by default,
    /// for the same reason <see cref="Headline"/> is: a catch-all arm here would put a later kind's value in a
    /// display slot under some other kind's rules, silently, and on a page.
    /// </exception>
    public FigureSplit Split() => Split(FigureLead.Reading);

    /// <summary>
    /// The reading split across a stat cell's two slots, with the caller saying which half leads.
    /// </summary>
    /// <param name="lead">
    /// Which half goes in the display slot. <see cref="FigureLead.Reading"/> is what
    /// <see cref="Split()"/> gives; <see cref="FigureLead.Tally"/> puts a share's NUMERATOR in the display
    /// slot and the whole reading beneath it.
    /// </param>
    /// <returns>Both halves, always together.</returns>
    /// <remarks>
    /// <para>★★ A COUNT-LED SHARE IS NOT A DEGRADED ONE. "1,161" over "76.8% of surveys whose dependencies a
    /// scanner could resolve (1,161 of 1,511)" carries strictly more than "76.8%" over "1,161 of 1,511 …"
    /// does: the support is the whole headline rather than half of it. What changes is what a reader takes in
    /// first — a quantity they can go and check, rather than a percentage that arrives already arguing. The
    /// corpus front page is a record of what was measured, so it leads with counts; a page about one share
    /// still leads with the share.</para>
    ///
    /// <para>★ THE NUMERATOR IS THE ONLY THING A TALLY CAN LEAD WITH, and it is repeated in the support
    /// deliberately. The support is <see cref="Headline"/> verbatim, which means the lead is never the only
    /// place a reader can find that number and a support line copied away from the page still says
    /// everything. Repetition is the price of a caption that survives being quoted.</para>
    ///
    /// <para>★ Every kind but <see cref="FigureKind.Ratio"/> already leads with a number, so both settings
    /// give it the same split. See <see cref="FigureLead"/> for why that is deliberate.</para>
    /// </remarks>
    /// <exception cref="NotSupportedException">A kind nobody has decided how to split.</exception>
    public FigureSplit Split(FigureLead lead) =>
        lead is FigureLead.Tally && Kind is FigureKind.Ratio
            ? new FigureSplit(Numerator!.Value.ToString(CountFormat, CultureInfo.InvariantCulture), Headline())
            : Kind switch
    {
        FigureKind.Ratio => new(
            $"{(Value * 100d).ToString(ShareFormat, CultureInfo.InvariantCulture)}%",
            $"{Numerator!.Value.ToString(CountFormat, CultureInfo.InvariantCulture)} of "
          + $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}"),
        FigureKind.Count => new(Population.ToString(CountFormat, CultureInfo.InvariantCulture), Counted()),
        FigureKind.Floor => new(
            $"at least {Population.ToString(CountFormat, CultureInfo.InvariantCulture)}",
            $"{Counted()} — a further "
          + $"{UnseenPopulation!.Value.ToString(CountFormat, CultureInfo.InvariantCulture)} could not be checked"),
        FigureKind.Scalar => new(
            Value.ToString(ReadingFormat, CultureInfo.InvariantCulture),
            $"across {Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}"),
        _ => throw new NotSupportedException($"no way to split a {Kind} figure across a stat cell has been decided"),
    };

    /// <summary>
    /// What the reading was taken over and when, WITHOUT the reading: <c>1,588 projects whose dependencies
    /// resolved, measured 10 September 2026 at 00:33 UTC</c>. For a caption under a figure that is already on
    /// the page.
    /// </summary>
    /// <remarks>
    /// <para>★ A floor states itself here too. For a count the population IS the reading, so this line repeats
    /// it — and repeating a floor's bound without the words that make it a bound is precisely the sentence that
    /// reads as a measurement. The caption is the place a caveat gets dropped, so it is the place it is
    /// hardest to drop.</para>
    ///
    /// <para>★★ THE INSTANT, NOT THE DAY, BECAUSE A DAY IS NOT ENOUGH TO TELL TWO READINGS APART. The public
    /// surveys index published 4,065, then 4,075, then 4,078 measured codebases over one morning — three
    /// different populations, every one of them stamped "15 September 2026" and nothing else. A reader holding
    /// two of those figures has no way to order them, and a citation of either cannot be resolved back to the
    /// reading it came from. The corpus index rebuilds nightly and the surveys index rebuilds in minutes, and
    /// under a day stamp alone the two are indistinguishable in exactly the place it matters. The minute is
    /// what makes the figure citable; UTC because a reading is not in the reader's timezone.</para>
    /// </remarks>
    public string Provenance() =>
        Kind is FigureKind.Floor
            ? $"at least {Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}, with a "
            + $"further {UnseenPopulation!.Value.ToString(CountFormat, CultureInfo.InvariantCulture)} not "
            + $"checked, measured {TakenAt.UtcDateTime.ToString(InstantFormat, CultureInfo.InvariantCulture)}"
            : $"{Population.ToString(CountFormat, CultureInfo.InvariantCulture)} {Counted()}, measured "
            + $"{TakenAt.UtcDateTime.ToString(InstantFormat, CultureInfo.InvariantCulture)}";

    /// <summary>The whole reading in one line: <see cref="Headline"/> plus the day it was taken.</summary>
    public override string ToString() =>
        $"{Headline()}, measured {TakenAt.UtcDateTime.ToString(DayFormat, CultureInfo.InvariantCulture)}";

    /// <summary>
    /// The population's own words, in the form its size takes — the singular at exactly one.
    /// </summary>
    /// <remarks>
    /// ★★ EVERY RENDERER GOES THROUGH HERE, WHICH IS THE POINT. The bug was not that one sentence said "1
    /// languages"; it was that eight of them did, in four renderers, because each interpolated the basis
    /// directly and no renderer knew how many there were. The population is the one number every kind of
    /// reading has — a count's own value, a ratio's DENOMINATOR, a scalar's span, a floor's bound — so asking
    /// it once, here, is what stops two renderings of one figure disagreeing about how many there are.
    /// </remarks>
    private string Counted() => Basis.For(Population);

    /// <summary>
    /// The basis, checked. Both forms were already trimmed and refused blank by <see cref="Publishing.Basis"/>;
    /// what is left to refuse here is no basis at all.
    /// </summary>
    /// <remarks>
    /// ★ It used to default nothing: a blank basis was refused rather than filled in with something like
    /// "projects", which would read as a real claim about a population nobody chose. That still holds — the
    /// refusal has simply moved to the type that now carries both forms.
    /// </remarks>
    private static Basis Named(Basis basis)
    {
        ArgumentNullException.ThrowIfNull(basis);

        return basis;
    }

    /// <summary>
    /// The instant, checked and normalised to UTC. <c>default(DateTimeOffset)</c> renders as "1 January 0001" —
    /// a page that looks dated and is not — so the caller has to use its clock.
    /// </summary>
    private static DateTimeOffset Dated(DateTimeOffset takenAt)
    {
        if (takenAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(takenAt), takenAt, "a published figure must carry the instant it was read");
        }

        return takenAt.ToUniversalTime();
    }
}
