using Microsoft.Data.Sqlite;

namespace Cai.Web.Noise;

/// <summary>The reported findings' SQL — what a rater, or a reader, has to look at.</summary>
/// <remarks>Owned by <see cref="SqliteNoiseStore"/>, which hands it the same connection factory it uses itself.</remarks>
internal sealed class SqliteNoiseFindingStore(Func<SqliteConnection> open) : INoiseFindingStore
{
    /// <inheritdoc />
    public void RecordFindings(IReadOnlyList<FindingRecord> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        if (findings.Count == 0)
        {
            return;
        }

        using var conn = open();
        using var tx = conn.BeginTransaction();

        foreach (var f in findings)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            // ★ DO NOTHING on conflict: the same defect reported by a second tool is the SAME finding, and the
            // first row already describes it. Overwriting would churn the row for no gain and lose nothing either.
            cmd.CommandText =
                """
                INSERT INTO noise_findings
                    (finding_id, period, tool, repo_id, pinned_sha, file_path, line, rule_id, title, claim_class)
                VALUES ($id, $period, $tool, $repo, $sha, $file, $line, $rule, $title, $class)
                ON CONFLICT (finding_id) DO NOTHING
                """;
            cmd.Parameters.AddWithValue("$id", f.FindingId);
            cmd.Parameters.AddWithValue("$period", f.Period);
            cmd.Parameters.AddWithValue("$tool", f.Tool);
            cmd.Parameters.AddWithValue("$repo", f.RepoId);
            cmd.Parameters.AddWithValue("$sha", f.PinnedSha);
            cmd.Parameters.AddWithValue("$file", (object?)f.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$line", (object?)f.Line ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rule", f.RuleId);
            cmd.Parameters.AddWithValue("$title", (object?)f.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$class", f.ClaimClass);
            cmd.ExecuteNonQuery();

            // ★★ AND WHO REPORTED IT, which the finding row cannot say twice. Two tools reporting one defect
            // produce one finding and two rows here — the difference between "this cost was spent on you" and
            // "this cost was spent on a finding you also reported".
            using var who = conn.CreateCommand();
            who.Transaction = tx;
            who.CommandText =
                """
                INSERT INTO noise_finding_tools (finding_id, tool) VALUES ($id, $tool)
                ON CONFLICT (finding_id, tool) DO NOTHING
                """;
            who.Parameters.AddWithValue("$id", f.FindingId);
            who.Parameters.AddWithValue("$tool", f.Tool);
            who.ExecuteNonQuery();
        }

        tx.Commit();
    }

    /// <inheritdoc />
    public FindingRecord? FindFinding(string findingId)
    {
        using var conn = open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT finding_id, period, tool, repo_id, pinned_sha, file_path, line, rule_id, title, claim_class
            FROM noise_findings WHERE finding_id = $id
            """;
        cmd.Parameters.AddWithValue("$id", findingId ?? "");
        using var reader = cmd.ExecuteReader();

        return reader.Read()
            ? new FindingRecord(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetString(9))
            : null;
    }
}
