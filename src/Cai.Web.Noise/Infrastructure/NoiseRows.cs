using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Cai.Web.Noise;

/// <summary>How the store's rows read back into records — one mapper per table that more than one query reads.</summary>
internal static class NoiseRows
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>A row of <c>noise_disputes</c>, in the column order of the store's dispute SELECT.</summary>
    public static DisputeRecord Dispute(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        DateTimeOffset.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.IsDBNull(7) ? null : reader.GetString(7),
        reader.IsDBNull(8)
            ? null
            : DateTimeOffset.Parse(reader.GetString(8), System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>A row of <c>noise_submissions</c>, read by column name.</summary>
    public static SubmissionReceipt Receipt(SqliteDataReader reader) => new(
        SubmissionId: reader.GetString(reader.GetOrdinal("submission_id")),
        Period: reader.GetString(reader.GetOrdinal("period")),
        Tool: reader.GetString(reader.GetOrdinal("tool")),
        ToolVersion: reader.GetString(reader.GetOrdinal("tool_version")),
        ReceivedAt: DateTimeOffset.Parse(
            reader.GetString(reader.GetOrdinal("received_at")),
            System.Globalization.CultureInfo.InvariantCulture),
        Accepted: reader.GetInt32(reader.GetOrdinal("accepted")) == 1,
        Problems: JsonSerializer.Deserialize<List<string>>(
            reader.GetString(reader.GetOrdinal("problems_json")), Json) ?? [],
        HoldoutRepositories: reader.GetInt32(reader.GetOrdinal("holdout_repositories")),
        CoveredRepositories: reader.GetInt32(reader.GetOrdinal("covered_repositories")),
        Uncovered: JsonSerializer.Deserialize<List<string>>(
            reader.GetString(reader.GetOrdinal("uncovered_json")), Json) ?? [],
        JudgingUnavailable: reader.IsDBNull(reader.GetOrdinal("judging_unavailable"))
            ? null
            : reader.GetString(reader.GetOrdinal("judging_unavailable")));
}
