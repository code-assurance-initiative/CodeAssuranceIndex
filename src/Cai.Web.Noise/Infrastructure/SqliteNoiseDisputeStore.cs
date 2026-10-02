using Microsoft.Data.Sqlite;

namespace Cai.Web.Noise;

/// <summary>The dispute register's SQL: contested verdicts, and how each contest was answered.</summary>
/// <remarks>Owned by <see cref="SqliteNoiseStore"/>, which hands it the same connection factory it uses itself.</remarks>
internal sealed class SqliteNoiseDisputeStore(Func<SqliteConnection> open) : INoiseDisputeStore
{
    /// <inheritdoc />
    public void RaiseDispute(DisputeRecord dispute)
    {
        ArgumentNullException.ThrowIfNull(dispute);

        using var conn = open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO noise_disputes
                (dispute_id, period, finding_id, raised_by, reason, raised_at)
            VALUES ($id, $period, $finding, $by, $reason, $at)
            """;
        cmd.Parameters.AddWithValue("$id", dispute.DisputeId);
        cmd.Parameters.AddWithValue("$period", dispute.Period);
        cmd.Parameters.AddWithValue("$finding", dispute.FindingId);
        cmd.Parameters.AddWithValue("$by", dispute.RaisedBy);
        cmd.Parameters.AddWithValue("$reason", dispute.Reason);
        cmd.Parameters.AddWithValue("$at", dispute.RaisedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public DisputeRecord? FindDispute(string disputeId)
    {
        using var conn = open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = DisputeSelect + " WHERE dispute_id = $id";
        cmd.Parameters.AddWithValue("$id", disputeId ?? "");
        using var reader = cmd.ExecuteReader();

        return reader.Read() ? NoiseRows.Dispute(reader) : null;
    }

    /// <inheritdoc />
    public bool ResolveDispute(string disputeId, string outcome, string reasoning, DateTimeOffset resolvedAt)
    {
        using var conn = open();
        using var cmd = conn.CreateCommand();

        // ★★ `outcome IS NULL` is the lock. A read-then-write above this could resolve the same dispute twice
        // from two requests, and the second answer would silently replace the first.
        cmd.CommandText =
            """
            UPDATE noise_disputes
               SET outcome = $outcome, resolution_reasoning = $reasoning, resolved_at = $at
             WHERE dispute_id = $id AND outcome IS NULL
            """;
        cmd.Parameters.AddWithValue("$outcome", outcome);
        cmd.Parameters.AddWithValue("$reasoning", reasoning);
        cmd.Parameters.AddWithValue("$at", resolvedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$id", disputeId ?? "");

        return cmd.ExecuteNonQuery() == 1;
    }

    /// <inheritdoc />
    public IReadOnlyList<DisputeRecord> ListDisputes(string period)
    {
        using var conn = open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = DisputeSelect + " WHERE period = $period ORDER BY raised_at, dispute_id";
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<DisputeRecord>();
        while (reader.Read())
        {
            list.Add(NoiseRows.Dispute(reader));
        }

        return list;
    }

    private const string DisputeSelect =
        "SELECT dispute_id, period, finding_id, raised_by, reason, raised_at, outcome, resolution_reasoning, "
      + "resolved_at FROM noise_disputes";
}
