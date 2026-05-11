using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Tests.Integration;

/// <summary>
/// Disables SQLite FK enforcement per-connection so test seeds can use synthetic UserId GUIDs
/// that have no corresponding row in AspNetUsers.
/// </summary>
public class DisableSqliteFkInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is SqliteConnection sqlite)
            RunPragma(sqlite);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqliteConnection sqlite)
            RunPragma(sqlite);
        await Task.CompletedTask;
    }

    private static void RunPragma(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = OFF;";
        cmd.ExecuteNonQuery();
    }
}
