using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Cai.Web.Registry;

/// <summary>
/// What the SQLite stores share: where the database file lives, and the additive migrations that grow a table in
/// place. The registry and the noise standard each kept a copy of both, and the copies were already one edit away
/// from disagreeing about what a safe migration is.
/// </summary>
internal static class SqliteSchema
{
    /// <summary>The column definitions a store migrates with: a type, its nullability, and a literal default.</summary>
    private static readonly Regex ColumnDefinition = new(
        @"^(TEXT|INTEGER|REAL|BLOB)( NOT)? NULL( DEFAULT (-?\d+(\.\d+)?|''))?$", RegexOptions.CultureInvariant);

    /// <summary>
    /// <see cref="RegistryOptions.DbPath"/> resolved against the content root, with its directory created.
    /// </summary>
    public static string ResolveDbPath(RegistryOptions options, IHostEnvironment env)
    {
        var path = options.DbPath;
        var resolved = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(env.ContentRootPath, path));
        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
        return resolved;
    }

    /// <summary>Idempotent <c>ALTER TABLE … ADD COLUMN</c>: adds the column unless it already exists, so re-running
    /// a store's initialisation on an already-migrated DB is a no-op (SQLite has no ADD COLUMN IF NOT EXISTS).</summary>
    // ★★ DDL TAKES NO PARAMETERS. Not the table name, not the column, not the definition — the statement has to
    //    be planned before a bound value could be known, so the safety of the ALTER cannot come from binding. It
    //    comes from the inputs being ours: every caller passes a literal. These guards make that a property of
    //    the METHOD rather than a habit of its callers. Identifiers are quoted the way SQLite quotes them, with
    //    any embedded delimiter doubled; the column definition is syntax rather than a value, so it cannot be
    //    quoted at all and is instead held to the narrow shape the stores actually emit. The existence check is
    //    a query, not DDL, so it binds the table name like any other value.
    public static void AddColumnIfMissing(SqliteConnection conn, string table, string column, string columnDef)
    {
        var quotedTable = QuoteIdentifier(table);
        var quotedColumn = QuoteIdentifier(column);
        if (!ColumnDefinition.IsMatch(columnDef))
        {
            throw new ArgumentException($"unsupported column definition: '{columnDef}'", nameof(columnDef));
        }

        if (HasColumn(conn, table, column))
        {
            return;
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {quotedTable} ADD COLUMN {quotedColumn} {columnDef}";
        alter.ExecuteNonQuery();
    }

    private static bool HasColumn(SqliteConnection conn, string table, string column)
    {
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT 1 FROM pragma_table_info($table) WHERE name = $column";
        check.Parameters.AddWithValue("$table", table);
        check.Parameters.AddWithValue("$column", column);
        return check.ExecuteScalar() is not null;
    }

    /// <summary>A plain identifier, quoted as SQLite quotes one — embedded delimiters doubled, so a name can
    /// never close its own quoting and be read on as statement structure.</summary>
    private static string QuoteIdentifier(string identifier)
    {
        if (identifier.Length == 0 || !identifier.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            throw new ArgumentException($"not a plain SQL identifier: '{identifier}'", nameof(identifier));
        }

        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
