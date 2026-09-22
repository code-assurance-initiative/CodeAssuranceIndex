using Cai.Delivery;

namespace Cai.Pages;

/// <summary>
/// Everything the standard knows about one measured subject: every delivery published about it, newest
/// last.
/// </summary>
/// <remarks>
/// <para>★★ THE SEQUENCE, NOT A READING. A single delivery is one measurement at one commit. The page
/// shows a trajectory — where the subject started, where it is now, how many readings there have been —
/// and NO producer supplies that, deliberately. The standard holds every delivery for a subject, so the
/// standard derives the climb. Two producers each computing "the climb" their own way would put two
/// different trend lines under one standard.</para>
/// <para>★ ORDER IS THE CALLER'S RESPONSIBILITY and is asserted, not assumed: a trajectory read out of an
/// unordered set is a chart of nothing.</para>
/// </remarks>
public sealed record SurveyRecord
{
    private SurveyRecord(IReadOnlyList<DeliveryPayload> deliveries) => Deliveries = deliveries;

    /// <summary>Every published delivery about this subject, oldest first.</summary>
    public IReadOnlyList<DeliveryPayload> Deliveries { get; }

    /// <summary>The most recent delivery — the reading a page leads with.</summary>
    public DeliveryPayload Latest => Deliveries[^1];

    /// <summary>Builds a record from deliveries about one subject.</summary>
    /// <exception cref="ArgumentException">The set is empty, or the deliveries are not all about one subject.</exception>
    public static SurveyRecord From(IEnumerable<DeliveryPayload> deliveries)
    {
        ArgumentNullException.ThrowIfNull(deliveries);
        var ordered = deliveries.OrderBy(d => d.IssuedAt, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0)
        {
            throw new ArgumentException("A survey record needs at least one delivery.", nameof(deliveries));
        }

        // ★ ONE SUBJECT, CHECKED. Two repositories folded into one record would publish a trajectory that
        //   belongs to neither of them, and nothing downstream could tell.
        var subject = ordered[0].Subject.Repository;
        if (ordered.Any(d => !string.Equals(d.Subject.Repository, subject, StringComparison.Ordinal)))
        {
            throw new ArgumentException("Every delivery in a survey record must be about the same subject.", nameof(deliveries));
        }

        return new SurveyRecord(ordered);
    }
}
