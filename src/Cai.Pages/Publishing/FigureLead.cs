namespace Cai.Pages.Publishing;

/// <summary>
/// Which half of a reading goes in the display slot when a <see cref="Figure"/> is split across one.
/// </summary>
/// <remarks>
/// <para>★★ IT EXISTS BECAUSE A SHARE AND ITS COUNT ARE DIFFERENT SENTENCES, AND ONLY ONE OF THEM IS A
/// QUANTITY. "76.8%" is a claim about a population; "1,161" is a tally somebody can go and check. A page whose
/// headlines are percentages argues — a reader takes in the number and the argument arrives with it — and a
/// page whose headlines are counts records. That is a decision about the PAGE, so it is made once at the page
/// and never inside the figure: the same reading is published both ways in this system, and neither rendering
/// is a truncation of the other.</para>
///
/// <para>★★ NEITHER SETTING CAN DROP THE POPULATION. Whichever half leads, the other half of the split states
/// the counts and names the basis, so the guarantee <see cref="Figure"/> exists for is untouched — this
/// chooses which of two complete renderings a reader meets first, and there is still no member that hands out
/// a lead on its own.</para>
///
/// <para>★ ONLY A <see cref="FigureKind.Ratio"/> HAS TWO ANSWERS. A count, a median and a floor already lead
/// with a number, so both settings give the same split for them. That is deliberate rather than an oversight:
/// a call site should not have to know which kind it holds in order to ask for the reading it wants, and a
/// setting that threw on three kinds out of four would make every use of it conditional.</para>
/// </remarks>
public enum FigureLead
{
    /// <summary>The reading itself leads: a share leads with its percentage. The default.</summary>
    Reading = 0,

    /// <summary>
    /// The tally leads: a share leads with its numerator, and the whole reading — percentage, both counts and
    /// the basis — goes in the line beneath.
    /// </summary>
    Tally = 1,
}
