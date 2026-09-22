namespace Cai.Pages.Publishing;

/// <summary>What shape of reading a <see cref="Figure"/> is, which is what decides how it may be written down.</summary>
public enum FigureKind
{
    /// <summary>A share: a numerator counted out of a population (<c>1,219 of 1,588</c>).</summary>
    Ratio = 0,

    /// <summary>A tally of the population itself (<c>3,464 surveyed projects</c>) — the count IS the population.</summary>
    Count = 1,

    /// <summary>A reading taken OVER a population without being a share of it — a median, a mean, a total.</summary>
    Scalar = 2,

    /// <summary>
    /// A LOWER BOUND on a tally, taken over a population part of which could not be looked at — <c>at least
    /// 240 surveys carrying this advisory, and a further 1,204 whose lists were cut short</c>.
    /// </summary>
    /// <remarks>
    /// ★ Its own kind rather than a <see cref="Count"/> with a caveat beside it, because the caveat is the
    /// half that gets lost. A floor's blind spot travels inside the figure and every renderer states it.
    /// </remarks>
    Floor = 3,
}
