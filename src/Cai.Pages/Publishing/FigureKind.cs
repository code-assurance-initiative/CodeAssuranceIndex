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
    /// A reading on the index's own 0–100 scale — a CAI, or a median of them — which is published to one
    /// decimal wherever it appears.
    /// </summary>
    /// <remarks>
    /// ★★ ITS OWN KIND RATHER THAN A <see cref="Scalar"/>, because the two disagree about digits and a
    /// scalar carries totals as well. "One decimal at most" is right for 63,266 findings and wrong for a
    /// score: a language whose median landed on a whole number published <c>66</c> in the corpus sheet's
    /// table and <c>66.0</c> on the page that table links to, in a column beside <c>64.4</c>. The same
    /// number, spelled two ways, on two surfaces of one standard. A kind rather than a format flag so that
    /// every renderer's switch has to decide, rather than falling through to the scalar arm.
    /// </remarks>
    Score = 4,

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
