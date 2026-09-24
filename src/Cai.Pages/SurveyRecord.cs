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

        return new SurveyRecord(OneReadingPerMeasurement(ordered));
    }

    /// <summary>
    /// Fold deliveries that describe the SAME measurement into one reading, keeping the latest filing.
    /// </summary>
    /// <remarks>
    /// <para>★★ A DELIVERY IS IMMUTABLE, SO SAYING MORE ABOUT A MEASUREMENT MEANS FILING AGAIN. The
    /// producer filed thousands of deliveries under MINOR 1.0 — a schema with no language, no origin and
    /// no security reading — while holding all three in its own run bundles. The only way those facts can
    /// reach a page is a second delivery about the same scan: same commit, same instant, more said about
    /// it. Nothing can be edited or withdrawn, and that is the design.</para>
    ///
    /// <para>★★ WITHOUT THIS FOLD THAT SECOND FILING LIES ABOUT THE TRAJECTORY. A portrait counts
    /// deliveries as measurements over time and plots one point each, so a corpus-wide backfill would
    /// double every repository's history overnight — "2 measurements" for one scan, two points on one
    /// date, a trend drawn through a duplicate. The page would be reporting the FILING rather than the
    /// MEASURING, and those are not the same event.</para>
    ///
    /// <para>★ THE KEY IS THE MEASUREMENT, NOT THE FILING: the commit measured and the instant it was
    /// measured at. Two readings of one commit taken on different days are genuinely two measurements and
    /// stay two.</para>
    ///
    /// <para>★ AND A DELIVERY THAT CANNOT PROVE SAMENESS IS KEPT. Missing a commit or a scan instant is
    /// not evidence of being a duplicate, and losing a real reading is worse than carrying a duplicate —
    /// only one of those two is recoverable.</para>
    /// </remarks>
    private static List<DeliveryPayload> OneReadingPerMeasurement(List<DeliveryPayload> ordered)
    {
        var kept = new List<DeliveryPayload>(ordered.Count);
        var byMeasurement = new Dictionary<(string Commit, string ScannedAt), int>();

        foreach (var delivery in ordered)
        {
            var commit = delivery.Subject.Commit;
            var scannedAt = delivery.Measurement?.ScannedAt;
            if (string.IsNullOrWhiteSpace(commit) || string.IsNullOrWhiteSpace(scannedAt))
            {
                kept.Add(delivery);
                continue;
            }

            var key = (commit, scannedAt);
            if (byMeasurement.TryGetValue(key, out var at))
            {
                // Ordered by issuance, so a later one is always the fuller telling of the same measurement.
                kept[at] = delivery;
                continue;
            }

            byMeasurement[key] = kept.Count;
            kept.Add(delivery);
        }

        return kept;
    }
}
