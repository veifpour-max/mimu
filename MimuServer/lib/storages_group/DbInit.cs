using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace LocalMimu.Repositories;

public static class DbInitializer
{
    private static readonly string ConnectionString = DbConfig.ConnectionString;

    public static void Initialize()
    {
        using(var connection = new SqliteConnection(ConnectionString)){

            connection.Open();

            var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users(
                Id Text PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Salt TEXT NOT NULL DEFAULT ''
            );";

            var createMessagesTable = @"
            CREATE TABLE IF NOT EXISTS Messages(
                Id TEXT PRIMARY KEY,
                Text TEXT NOT NULL,
                SenderId TEXT NOT NULL,
                ReceiverId TEXT NOT NULL,
                SentAt TEXT NOT NULL,
                Status INTEGER NOT NULL,
                FOREIGN KEY(SenderId) REFERENCES Users(Id),
                FOREIGN KEY(ReceiverId) REFERENCES Users(Id)

            );";

            using (var command = new SqliteCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }
            using (var command = new SqliteCommand(createMessagesTable, connection))
            {
                command.ExecuteNonQuery();
            }

        }
    }
}