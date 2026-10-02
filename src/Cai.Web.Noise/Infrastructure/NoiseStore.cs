using System.Text.Json;
using Cai.Web.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Cai.Web.Noise;

/// <summary>
/// SQLite storage for the submission register and the verdict record.
/// </summary>
/// <remarks>
/// <para>★★ THE SUBMISSION REGISTER WAS A DICTIONARY, and its own comment said so: "a restart currently forgets
/// that a vendor already submitted, which is precisely the hole the no-withdrawal rule exists to close". That
/// rule is the standard's answer to the worst failure available to it — a vendor runs, dislikes the result, and
/// the published set quietly becomes "the results people were happy with". A rule defeated by a process restart
/// is not a rule, and it was the FIRST thing an unfriendly participant would have found.</para>
///
/// <para>★★ AND THE CLAIM IS ENFORCED BY THE DATABASE, not by a check above it. A partial UNIQUE index on
/// (period, tool) where accepted = 1 means two concurrent submissions cannot both win: one insert fails. An
/// in-process check plus a later insert is a race, and this is exactly the operation somebody has a motive to
/// race.</para>
///
/// <para>★★ THE VERDICT RECORD IS THE OPEN-JUDGING CLAIM. 01-scope-and-governance promises "every judge prompt,
/// every model and version, every raw verdict with its reasoning, and every human adjudication. Published in
/// full. A reader who disagrees with a verdict must be able to find it, read the reasoning, and say so." None of
/// that was stored anywhere — the cascade resolved votes in memory and returned an answer. It was the one claim
/// a sceptic tests first, and it was the one with nothing behind it.</para>
///
/// <para>★ One database file, several tables — the same file the registry uses, resolved from the same option, so
/// a deploy has one thing to back up and one thing to point outside the app directory.</para>
/// </remarks>
internal sealed class SqliteNoiseStore : INoiseStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly string _connectionString;
    private readonly SqliteNoiseDisputeStore _disputes;
    private readonly SqliteNoiseFindingStore _findings;
    private readonly SqliteNoiseCostStore _costs;

    public SqliteNoiseStore(
        IOptions<RegistryOptions> options, IHostEnvironment env, ILogger<SqliteNoiseStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(env);
        ArgumentNullException.ThrowIfNull(logger);

        var resolved = SqliteSchema.ResolveDbPath(options.Value, env);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = resolved }.ToString();
        using (var conn = Open())
        {
            NoiseSchema.Create(conn);
        }

        _disputes = new SqliteNoiseDisputeStore(Open);
        _findings = new SqliteNoiseFindingStore(Open);
        _costs = new SqliteNoiseCostStore(Open);

        logger.LogInformation("Noise store (SQLite) at {Path}", resolved);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <inheritdoc />
    public bool TryRecordSubmission(
        SubmissionReceipt receipt, DateTimeOffset? runStartedAt, string? configurationJson)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO noise_submissions
                (submission_id, period, tool, tool_version, received_at, run_started_at, accepted,
                 problems_json, holdout_repositories, covered_repositories, uncovered_json,
                 configuration_json, judging_unavailable)
            VALUES ($id, $period, $tool, $toolVersion, $receivedAt, $runStartedAt, $accepted,
                    $problems, $holdout, $covered, $uncovered, $configuration, $judging)
            """;
        cmd.Parameters.AddWithValue("$id", receipt.SubmissionId);
        cmd.Parameters.AddWithValue("$period", receipt.Period);
        cmd.Parameters.AddWithValue("$tool", receipt.Tool);
        cmd.Parameters.AddWithValue("$toolVersion", receipt.ToolVersion);
        cmd.Parameters.AddWithValue("$receivedAt", receipt.ReceivedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$runStartedAt", runStartedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$accepted", receipt.Accepted ? 1 : 0);
        cmd.Parameters.AddWithValue("$problems", JsonSerializer.Serialize(receipt.Problems, Json));
        cmd.Parameters.AddWithValue("$holdout", receipt.HoldoutRepositories);
        cmd.Parameters.AddWithValue("$covered", receipt.CoveredRepositories);
        cmd.Parameters.AddWithValue("$uncovered", JsonSerializer.Serialize(receipt.Uncovered, Json));
        cmd.Parameters.AddWithValue("$configuration", configurationJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$judging", (object?)receipt.JudgingUnavailable ?? DBNull.Value);

        try
        {
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException e) when (e.SqliteErrorCode == 19)
        {
            // ★ The unique claim already exists. Losing this race is the rule working, not an error to log
            // and swallow — the caller turns it into the conflict a participant is told about.
            return false;
        }
    }

    /// <inheritdoc />
    public SubmissionReceipt? FindSubmission(string submissionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM noise_submissions WHERE submission_id = $id";
        cmd.Parameters.AddWithValue("$id", submissionId ?? "");
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? NoiseRows.Receipt(reader) : null;
    }

    /// <inheritdoc />
    public bool AlreadySubmitted(string tool, string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(1) FROM noise_submissions WHERE tool = $tool AND period = $period AND accepted = 1";
        cmd.Parameters.AddWithValue("$tool", tool ?? "");
        cmd.Parameters.AddWithValue("$period", period ?? "");
        return Convert.ToInt64(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<SubmissionReceipt> ListSubmissions(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT * FROM noise_submissions WHERE period = $period ORDER BY received_at DESC";
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<SubmissionReceipt>();
        while (reader.Read())
        {
            list.Add(NoiseRows.Receipt(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public void RecordVerdict(VerdictRecord verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO noise_verdicts
                (verdict_id, period, finding_id, round, judge, model, model_version, prompt_id,
                 verdict, reasoning, recorded_at, model_family, temperature)
            VALUES ($id, $period, $finding, $round, $judge, $model, $modelVersion, $promptId,
                    $verdict, $reasoning, $recordedAt, $family, $temperature)
            """;
        cmd.Parameters.AddWithValue("$id", Guid.CreateVersion7().ToString("n"));
        cmd.Parameters.AddWithValue("$period", verdict.Period);
        cmd.Parameters.AddWithValue("$finding", verdict.FindingId);
        cmd.Parameters.AddWithValue("$round", verdict.Round);
        cmd.Parameters.AddWithValue("$judge", verdict.Judge);
        cmd.Parameters.AddWithValue("$model", verdict.Model);
        cmd.Parameters.AddWithValue("$modelVersion", verdict.ModelVersion);
        cmd.Parameters.AddWithValue("$promptId", verdict.PromptId);
        cmd.Parameters.AddWithValue("$verdict", verdict.Verdict);
        cmd.Parameters.AddWithValue("$reasoning", verdict.Reasoning);
        cmd.Parameters.AddWithValue("$recordedAt", verdict.RecordedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$family", verdict.ModelFamily);
        cmd.Parameters.AddWithValue("$temperature", verdict.Temperature);
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public void RecordResolution(ResolutionRecord resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        // ★ REPLACE on (period, finding): a re-judge corrects how a finding settled rather than leaving two
        // answers on the record. The raw VERDICTS are append-only — those are the evidence — but "how it
        // settled" has one current value.
        cmd.CommandText =
            """
            INSERT INTO noise_resolutions
                (period, finding_id, state, verdict, settled_at_round, actionability_contested,
                 actionable, reason, recorded_at)
            VALUES ($period, $finding, $state, $verdict, $round, $contested, $actionable, $reason, $recordedAt)
            ON CONFLICT(period, finding_id) DO UPDATE SET
                state = excluded.state, verdict = excluded.verdict,
                settled_at_round = excluded.settled_at_round,
                actionability_contested = excluded.actionability_contested,
                actionable = excluded.actionable, reason = excluded.reason,
                recorded_at = excluded.recorded_at
            """;
        cmd.Parameters.AddWithValue("$period", resolution.Period);
        cmd.Parameters.AddWithValue("$finding", resolution.FindingId);
        cmd.Parameters.AddWithValue("$state", resolution.State);
        cmd.Parameters.AddWithValue("$verdict", resolution.Verdict ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$round", resolution.SettledAtRound ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$contested", resolution.ActionabilityContested ? 1 : 0);
        cmd.Parameters.AddWithValue(
            "$actionable", resolution.Actionable is { } a ? (a ? 1 : 0) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$reason", resolution.Reason);
        cmd.Parameters.AddWithValue("$recordedAt", resolution.RecordedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public void RegisterPrompt(string promptId, string text, DateTimeOffset now)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        // ★ Stored ONCE per id rather than on every verdict. The same prompt answers thousands of findings, and
        // a record that repeats it is a record nobody downloads.
        cmd.CommandText =
            """
            INSERT INTO noise_prompts (prompt_id, text, first_seen_at)
            VALUES ($id, $text, $at)
            ON CONFLICT(prompt_id) DO NOTHING
            """;
        cmd.Parameters.AddWithValue("$id", promptId ?? "");
        cmd.Parameters.AddWithValue("$text", text ?? "");
        cmd.Parameters.AddWithValue("$at", now.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public IReadOnlyList<VerdictRecord> ListVerdicts(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT * FROM noise_verdicts WHERE period = $period ORDER BY recorded_at, finding_id, round, judge";
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<VerdictRecord>();
        while (reader.Read())
        {
            list.Add(new VerdictRecord(
                reader.GetString(reader.GetOrdinal("period")),
                reader.GetString(reader.GetOrdinal("finding_id")),
                reader.GetInt32(reader.GetOrdinal("round")),
                reader.GetString(reader.GetOrdinal("judge")),
                reader.GetString(reader.GetOrdinal("model")),
                reader.GetString(reader.GetOrdinal("model_version")),
                reader.GetString(reader.GetOrdinal("prompt_id")),
                reader.GetString(reader.GetOrdinal("verdict")),
                reader.GetString(reader.GetOrdinal("reasoning")),
                DateTimeOffset.Parse(
                    reader.GetString(reader.GetOrdinal("recorded_at")),
                    System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(reader.GetOrdinal("model_family")),
                reader.GetDouble(reader.GetOrdinal("temperature"))));
        }

        return list;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResolutionRecord> ListResolutions(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM noise_resolutions WHERE period = $period ORDER BY finding_id";
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<ResolutionRecord>();
        while (reader.Read())
        {
            var verdictOrdinal = reader.GetOrdinal("verdict");
            var roundOrdinal = reader.GetOrdinal("settled_at_round");
            var actionableOrdinal = reader.GetOrdinal("actionable");
            list.Add(new ResolutionRecord(
                reader.GetString(reader.GetOrdinal("period")),
                reader.GetString(reader.GetOrdinal("finding_id")),
                reader.GetString(reader.GetOrdinal("state")),
                reader.IsDBNull(verdictOrdinal) ? null : reader.GetString(verdictOrdinal),
                reader.IsDBNull(roundOrdinal) ? null : reader.GetInt32(roundOrdinal),
                reader.GetInt32(reader.GetOrdinal("actionability_contested")) == 1,
                reader.IsDBNull(actionableOrdinal) ? null : reader.GetInt32(actionableOrdinal) == 1,
                reader.GetString(reader.GetOrdinal("reason")),
                DateTimeOffset.Parse(
                    reader.GetString(reader.GetOrdinal("recorded_at")),
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <inheritdoc />
    public IReadOnlyList<PromptRecord> ListPrompts(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT p.* FROM noise_prompts p
            WHERE p.prompt_id IN (SELECT DISTINCT prompt_id FROM noise_verdicts WHERE period = $period)
            ORDER BY p.prompt_id
            """;
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<PromptRecord>();
        while (reader.Read())
        {
            list.Add(new PromptRecord(
                reader.GetString(reader.GetOrdinal("prompt_id")),
                reader.GetString(reader.GetOrdinal("text")),
                DateTimeOffset.Parse(
                    reader.GetString(reader.GetOrdinal("first_seen_at")),
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <inheritdoc />
    public string? ConfigurationJson(string submissionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT configuration_json FROM noise_submissions WHERE submission_id = $id";
        cmd.Parameters.AddWithValue("$id", submissionId ?? "");
        var value = cmd.ExecuteScalar();
        return value is null or DBNull ? null : (string)value;
    }

    // ── The dispute register, the findings and the cost ledger keep their own SQL in their own types; the
    //    store answers for them under every role it fills.

    /// <inheritdoc />
    public void RaiseDispute(DisputeRecord dispute) => _disputes.RaiseDispute(dispute);

    /// <inheritdoc />
    public DisputeRecord? FindDispute(string disputeId) => _disputes.FindDispute(disputeId);

    /// <inheritdoc />
    public bool ResolveDispute(string disputeId, string outcome, string reasoning, DateTimeOffset resolvedAt) =>
        _disputes.ResolveDispute(disputeId, outcome, reasoning, resolvedAt);

    /// <inheritdoc />
    public IReadOnlyList<DisputeRecord> ListDisputes(string period) => _disputes.ListDisputes(period);

    /// <inheritdoc />
    public IntentRecord RegisterIntent(string period, string tool, DateTimeOffset now)
    {
        using var conn = Open();

        using (var insert = conn.CreateCommand())
        {
            // ★ DO NOTHING on conflict — the first registration's timestamp is the claim, and a later one must
            // not overwrite it. Enforced by the key rather than by a read-then-write above it.
            insert.CommandText =
                """
                INSERT INTO noise_intent (period, tool, registered_at)
                VALUES ($period, $tool, $at)
                ON CONFLICT (period, tool) DO NOTHING
                """;
            insert.Parameters.AddWithValue("$period", period);
            insert.Parameters.AddWithValue("$tool", tool);
            insert.Parameters.AddWithValue("$at", now.ToString("O"));
            insert.ExecuteNonQuery();
        }

        using var read = conn.CreateCommand();
        read.CommandText =
            "SELECT period, tool, registered_at FROM noise_intent WHERE period = $period AND tool = $tool";
        read.Parameters.AddWithValue("$period", period);
        read.Parameters.AddWithValue("$tool", tool);
        using var reader = read.ExecuteReader();
        reader.Read();

        return new IntentRecord(
            reader.GetString(0), reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public IReadOnlyList<IntentRecord> ListIntent(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT period, tool, registered_at FROM noise_intent WHERE period = $period "
          + "ORDER BY registered_at, tool";
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<IntentRecord>();
        while (reader.Read())
        {
            list.Add(new IntentRecord(
                reader.GetString(0), reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <inheritdoc />
    public void RecordFindings(IReadOnlyList<FindingRecord> findings) => _findings.RecordFindings(findings);

    /// <inheritdoc />
    public FindingRecord? FindFinding(string findingId) => _findings.FindFinding(findingId);

    /// <inheritdoc />
    public void RecordCost(CostEntry entry) => _costs.RecordCost(entry);

    /// <inheritdoc />
    public IReadOnlyList<CostTally> CostFor(string period) => _costs.CostFor(period);

    /// <inheritdoc />
    public IReadOnlyList<string> JudgedPeriods()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT period FROM noise_resolutions ORDER BY period DESC";
        using var reader = cmd.ExecuteReader();

        var periods = new List<string>();
        while (reader.Read())
        {
            periods.Add(reader.GetString(0));
        }

        return periods;
    }

    /// <inheritdoc />
    public IReadOnlyList<PeriodTally> PublishedTallies()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT period, payload_json FROM noise_publications ORDER BY published_at, publication_id";
        using var reader = cmd.ExecuteReader();

        var list = new List<PeriodTally>();
        while (reader.Read())
        {
            var period = reader.GetString(0);
            try
            {
                var root = System.Text.Json.JsonDocument.Parse(reader.GetString(1)).RootElement;
                var noise = Read(root, "noise");
                var judged = noise
                           + Read(root, "validAndActionable")
                           + Read(root, "validNotActionable");
                list.Add(new PeriodTally(period, judged, noise));
            }
            catch (System.Text.Json.JsonException)
            {
                // ★ A payload that will not parse is skipped rather than throwing: one unreadable row must not
                // take the rolling figure — and every other endpoint — down with it. It is a stored artefact,
                // not live input, so there is nothing to reject back to a caller.
            }
        }

        return list;

        static int Read(System.Text.Json.JsonElement root, string name) =>
            root.TryGetProperty(name, out var v)
            && v.ValueKind == System.Text.Json.JsonValueKind.Number
            && v.TryGetInt32(out var n) ? n : 0;
    }

    /// <inheritdoc />
    public void RecordRejudge(IReadOnlyList<RejudgeRecord> verdicts)
    {
        ArgumentNullException.ThrowIfNull(verdicts);
        if (verdicts.Count == 0)
        {
            return;
        }

        using var conn = Open();
        using var tx = conn.BeginTransaction();

        foreach (var v in verdicts)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                INSERT INTO noise_rejudge
                    (period, finding_id, verdict, model, model_version, prompt_id, reasoning, recorded_at)
                VALUES ($period, $finding, $verdict, $model, $version, $prompt, $reasoning, $at)
                ON CONFLICT (period, finding_id) DO UPDATE SET
                    verdict = excluded.verdict, model = excluded.model,
                    model_version = excluded.model_version, prompt_id = excluded.prompt_id,
                    reasoning = excluded.reasoning, recorded_at = excluded.recorded_at
                """;
            cmd.Parameters.AddWithValue("$period", v.Period);
            cmd.Parameters.AddWithValue("$finding", v.FindingId);
            cmd.Parameters.AddWithValue("$verdict", v.Verdict);
            cmd.Parameters.AddWithValue("$model", v.Model);
            cmd.Parameters.AddWithValue("$version", v.ModelVersion);
            cmd.Parameters.AddWithValue("$prompt", v.PromptId);
            cmd.Parameters.AddWithValue("$reasoning", v.Reasoning);
            cmd.Parameters.AddWithValue("$at", v.RecordedAt.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<RejudgeRecord> ListRejudge(string period)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT period, finding_id, verdict, model, model_version, prompt_id, reasoning, recorded_at
            FROM noise_rejudge WHERE period = $period ORDER BY finding_id
            """;
        cmd.Parameters.AddWithValue("$period", period ?? "");
        using var reader = cmd.ExecuteReader();

        var list = new List<RejudgeRecord>();
        while (reader.Read())
        {
            list.Add(new RejudgeRecord(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                DateTimeOffset.Parse(reader.GetString(7), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <inheritdoc />
    public void RecordPublication(string period, string payloadJson, DateTimeOffset publishedAt)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO noise_publications (publication_id, period, payload_json, published_at)
            VALUES ($id, $period, $payload, $at)
            """;
        cmd.Parameters.AddWithValue("$id", Guid.CreateVersion7().ToString("n"));
        cmd.Parameters.AddWithValue("$period", period);
        cmd.Parameters.AddWithValue("$payload", payloadJson);
        cmd.Parameters.AddWithValue("$at", publishedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public (string PayloadJson, DateTimeOffset PublishedAt, IReadOnlyList<DateTimeOffset> History)?
        LatestPublication(string period)
    {
        using var conn = Open();

        var history = new List<DateTimeOffset>();
        using (var all = conn.CreateCommand())
        {
            all.CommandText =
                "SELECT published_at FROM noise_publications WHERE period = $period ORDER BY published_at";
            all.Parameters.AddWithValue("$period", period ?? "");
            using var reader = all.ExecuteReader();
            while (reader.Read())
            {
                history.Add(DateTimeOffset.Parse(
                    reader.GetString(0), System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        if (history.Count == 0)
        {
            return null;
        }

        using var latest = conn.CreateCommand();
        latest.CommandText =
            """
            SELECT payload_json, published_at FROM noise_publications
            WHERE period = $period ORDER BY published_at DESC, publication_id DESC LIMIT 1
            """;
        latest.Parameters.AddWithValue("$period", period ?? "");
        using var row = latest.ExecuteReader();
        if (!row.Read())
        {
            return null;
        }

        return (
            row.GetString(0),
            DateTimeOffset.Parse(row.GetString(1), System.Globalization.CultureInfo.InvariantCulture),
            history);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> PublishedPeriods()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT DISTINCT period FROM noise_publications ORDER BY period DESC";
        using var reader = cmd.ExecuteReader();

        var periods = new List<string>();
        while (reader.Read())
        {
            periods.Add(reader.GetString(0));
        }

        return periods;
    }
}
