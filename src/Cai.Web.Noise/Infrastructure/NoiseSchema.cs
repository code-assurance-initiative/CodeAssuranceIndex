using Cai.Web.Registry;
using Microsoft.Data.Sqlite;

namespace Cai.Web.Noise;

/// <summary>The Noise Standard's tables in the shared SQLite file, and the additive migrations that grew them.</summary>
internal static class NoiseSchema
{
    /// <summary>Create the standard's tables and apply its additive migrations. Idempotent — run on every start.</summary>
    public static void Create(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            PRAGMA journal_mode = WAL;

            CREATE TABLE IF NOT EXISTS noise_submissions (
                submission_id        TEXT PRIMARY KEY,
                period               TEXT NOT NULL,
                tool                 TEXT NOT NULL,
                tool_version         TEXT NOT NULL,
                received_at          TEXT NOT NULL,
                run_started_at       TEXT NULL,
                accepted             INTEGER NOT NULL,
                problems_json        TEXT NOT NULL,
                holdout_repositories INTEGER NOT NULL,
                covered_repositories INTEGER NOT NULL,
                uncovered_json       TEXT NOT NULL,
                configuration_json   TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_submissions_period ON noise_submissions(period);

            -- ★★ THE NO-WITHDRAWAL RULE, ENFORCED HERE rather than by a check above it. Partial: only an
            -- ACCEPTED submission claims the slot, so a rejected run stays on the register (it is evidence)
            -- without blocking a corrected one. Two concurrent submissions cannot both win — one insert fails,
            -- which an in-process check followed by an insert could never guarantee.
            CREATE UNIQUE INDEX IF NOT EXISTS ux_noise_submissions_claim
                ON noise_submissions(period, tool) WHERE accepted = 1;

            CREATE TABLE IF NOT EXISTS noise_prompts (
                prompt_id     TEXT PRIMARY KEY,
                text          TEXT NOT NULL,
                first_seen_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS noise_verdicts (
                verdict_id    TEXT PRIMARY KEY,
                period        TEXT NOT NULL,
                finding_id    TEXT NOT NULL,
                round         INTEGER NOT NULL,
                judge         TEXT NOT NULL,
                model         TEXT NOT NULL,
                model_version TEXT NOT NULL,
                prompt_id     TEXT NOT NULL,
                verdict       TEXT NOT NULL,
                reasoning     TEXT NOT NULL,
                recorded_at   TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_verdicts_period  ON noise_verdicts(period);
            CREATE INDEX IF NOT EXISTS ix_noise_verdicts_finding ON noise_verdicts(period, finding_id);

            -- ★★ APPEND-ONLY, keyed by nothing but its own id. A correction to a published number is a
            -- SECOND row, so it is visible as a correction: on the one figure where §01 says being seen to
            -- suppress ends the standard, a store that overwrote would make the second publication
            -- indistinguishable from the first.
            CREATE TABLE IF NOT EXISTS noise_publications (
                publication_id TEXT PRIMARY KEY,
                period         TEXT NOT NULL,
                payload_json   TEXT NOT NULL,
                published_at   TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_publications_period ON noise_publications(period, published_at);

            -- ★★ BESIDE the verdicts, never instead of them. A dispute that could delete what it overturned
            -- would be a withdrawal mechanism, and the register would quietly become "the verdicts nobody
            -- objected to". noise_verdicts is untouched by anything in here.
            -- ★★ (period, tool) is the KEY, so a second registration cannot move the timestamp: the moment it
            -- was made is the entire claim.
            -- ★★ Keyed by the DERIVED finding id, so two tools reporting one defect write one row. `tool` is
            -- stored because the register needs it and is NEVER served with the evidence: a rater told which
            -- vendor produced a finding is being asked a different question.
            CREATE TABLE IF NOT EXISTS noise_findings (
                finding_id  TEXT PRIMARY KEY,
                period      TEXT NOT NULL,
                tool        TEXT NOT NULL,
                repo_id     TEXT NOT NULL,
                pinned_sha  TEXT NOT NULL,
                file_path   TEXT NULL,
                line        INTEGER NULL,
                rule_id     TEXT NOT NULL,
                title       TEXT NULL,
                claim_class TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_findings_period ON noise_findings(period);

            -- ★★ WHO REPORTED IT — one row per (finding, tool). The finding row itself keeps the FIRST reporter
            -- because a second tool reporting the same defect is the same finding, which is the property the
            -- pooled union needs. But the cost of judging that finding was spent on BOTH of them, and attributing
            -- it wholly to whoever submitted first overstates one participant and understates the other.
            CREATE TABLE IF NOT EXISTS noise_finding_tools (
                finding_id TEXT NOT NULL,
                tool       TEXT NOT NULL,
                PRIMARY KEY (finding_id, tool)
            );

            -- ★★ APPEND-ONLY, one row per judgement or rated item. The marginal cost of a participant is the
            -- sum over its rows, so a ledger that overwrote could not answer "what did this period cost" twice
            -- with the same number. `tool` is NULL when the finding is unknown — that cost is real and must not
            -- vanish into a smaller total.
            CREATE TABLE IF NOT EXISTS noise_cost (
                cost_id       TEXT PRIMARY KEY,
                period        TEXT NOT NULL,
                tool          TEXT NULL,
                kind          TEXT NOT NULL,
                finding_id    TEXT NOT NULL,
                model_seconds REAL NULL,
                input_tokens  INTEGER NULL,
                output_tokens INTEGER NULL,
                at            TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_cost_period ON noise_cost(period);

            CREATE TABLE IF NOT EXISTS noise_intent (
                period        TEXT NOT NULL,
                tool          TEXT NOT NULL,
                registered_at TEXT NOT NULL,
                PRIMARY KEY (period, tool)
            );

            CREATE TABLE IF NOT EXISTS noise_disputes (
                dispute_id           TEXT PRIMARY KEY,
                period               TEXT NOT NULL,
                finding_id           TEXT NOT NULL,
                raised_by            TEXT NOT NULL,
                reason               TEXT NOT NULL,
                raised_at            TEXT NOT NULL,
                outcome              TEXT NULL,
                resolution_reasoning TEXT NULL,
                resolved_at          TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_noise_disputes_period ON noise_disputes(period, raised_at);

            CREATE TABLE IF NOT EXISTS noise_rejudge (
                period        TEXT NOT NULL,
                finding_id    TEXT NOT NULL,
                verdict       TEXT NOT NULL,
                model         TEXT NOT NULL,
                model_version TEXT NOT NULL,
                prompt_id     TEXT NOT NULL,
                reasoning     TEXT NOT NULL,
                recorded_at   TEXT NOT NULL,
                PRIMARY KEY (period, finding_id)
            );

            CREATE TABLE IF NOT EXISTS noise_resolutions (
                period                  TEXT NOT NULL,
                finding_id              TEXT NOT NULL,
                state                   TEXT NOT NULL,
                verdict                 TEXT NULL,
                settled_at_round        INTEGER NULL,
                actionability_contested INTEGER NOT NULL,
                actionable              INTEGER NULL,
                reason                  TEXT NOT NULL,
                recorded_at             TEXT NOT NULL,
                PRIMARY KEY (period, finding_id)
            );
            """;
        cmd.ExecuteNonQuery();

        // ★ Additive on a table that may already exist in a dev database — SQLite has no ADD COLUMN IF NOT
        // EXISTS, so this is guarded, exactly as the registry store does it.
        SqliteSchema.AddColumnIfMissing(conn, "noise_submissions", "configuration_json", "TEXT NULL");

        // ★★ The panel's shape, per verdict (#10). Additive for the same reason: a dev database already holds
        // verdicts recorded before these were required, and dropping them to add two columns would destroy the
        // record the standard promises to keep.
        // ★★ Why a participant files no per-judge verdicts (#27). Additive for the same reason.
        SqliteSchema.AddColumnIfMissing(conn, "noise_submissions", "judging_unavailable", "TEXT NULL");

        SqliteSchema.AddColumnIfMissing(conn, "noise_verdicts", "model_family", "TEXT NOT NULL DEFAULT ''");
        SqliteSchema.AddColumnIfMissing(conn, "noise_verdicts", "temperature", "REAL NOT NULL DEFAULT 0");
    }
}
