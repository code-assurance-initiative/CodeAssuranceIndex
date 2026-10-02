using Microsoft.Data.Sqlite;

namespace Cai.Web.Noise;

/// <summary>The cost ledger's SQL: what judging and rating cost, per participant.</summary>
/// <remarks>Owned by <see cref="SqliteNoiseStore"/>, which hands it the same connection factory it uses itself.</remarks>
internal sealed class SqliteNoiseCostStore(Func<SqliteConnection> open) : INoiseCostStore
{
    /// <inheritdoc />
    public void RecordCost(CostEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var conn = open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO noise_cost
                (cost_id, period, tool, kind, finding_id, model_seconds, input_tokens, output_tokens, at)
            VALUES ($id, $period, $tool, $kind, $finding, $seconds, $in, $out, $at)
            """;
        cmd.Parameters.AddWithValue("$id", Guid.CreateVersion7().ToString("n"));
        cmd.Parameters.AddWithValue("$period", entry.Period);
        cmd.Parameters.AddWithValue("$tool", (object?)entry.Tool ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$kind", entry.Kind);
        cmd.Parameters.AddWithValue("$finding", entry.FindingId);
        cmd.Parameters.AddWithValue("$seconds", (object?)entry.ModelSeconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$in", (object?)entry.InputTokens ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$out", (object?)entry.OutputTokens ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$at", entry.At.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public IReadOnlyList<CostTally> CostFor(string period)
    {
        using var conn = open();
        using var cmd = conn.CreateCommand();

        // ★ Summed in SQL, grouped on the tool with NULL as its own group — the unattributed row. COUNT over a
        // nullable column counts non-nulls, which is exactly what "judgements that reported a time" means.
        // ★★ ATTRIBUTED BY JOIN, not by the column. A finding two tools reported produces two rows here — the
        // judgement was spent on both — so `reporters = 1` is what separates the marginal cost from the cost
        // that would have been paid anyway. The LEFT JOIN keeps a cost whose finding nothing recorded: it is real
        // spend attributable to nobody, and dropping it would make the total smaller than what was spent.
        cmd.CommandText =
            """
            WITH reporters AS (
                SELECT finding_id, COUNT(*) AS n FROM noise_finding_tools GROUP BY finding_id
            )
            SELECT ft.tool,
                   SUM(CASE WHEN c.kind = 'judging' THEN 1 ELSE 0 END)                            AS judgements,
                   SUM(CASE WHEN c.kind = 'judging' AND c.model_seconds IS NULL THEN 1 ELSE 0 END) AS untimed,
                   COALESCE(SUM(c.model_seconds), 0)                                              AS seconds,
                   COALESCE(SUM(c.input_tokens), 0)                                               AS in_tok,
                   COALESCE(SUM(c.output_tokens), 0)                                              AS out_tok,
                   SUM(CASE WHEN c.kind = 'crowd' THEN 1 ELSE 0 END)                              AS crowd,
                   SUM(CASE WHEN c.kind = 'judging' AND COALESCE(r.n, 0) = 1 THEN 1 ELSE 0 END)    AS solely,
                   COALESCE(SUM(CASE WHEN COALESCE(r.n, 0) = 1 THEN c.model_seconds END), 0)       AS solely_seconds
            FROM noise_cost c
            LEFT JOIN noise_finding_tools ft ON ft.finding_id = c.finding_id
            LEFT JOIN reporters r            ON r.finding_id  = c.finding_id
            WHERE c.period = $period
            GROUP BY ft.tool
            ORDER BY ft.tool IS NULL, ft.tool
            """;
        cmd.Parameters.AddWithValue("$period", period ?? "");

        List<CostTally> tallies = [];
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tallies.Add(new CostTally(
                reader.IsDBNull(0) ? null : reader.GetString(0),
                reader.GetInt32(1), reader.GetInt32(2),
                reader.GetDouble(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),
                reader.GetInt32(7), reader.GetDouble(8)));
        }

        return tallies;
    }
}
