using System;
using LocalMimu.Models;
using Microsoft.Data.Sqlite;

namespace LocalMimu.Repositories;
public class MessagesRepository
{
    private readonly string? _sqlpath;

    public MessagesRepository(string sqlpath)
    {
        _sqlpath = sqlpath;
    }

    public async Task SaveMessagesAsync(Message msg)
    {
        // вроде все верно так еще и DI применяем.. кайф
        using(var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO Messages (Id, Text, SenderId, ReceiverId, SentAt, Status) VALUES (@id, @text, @senderId, @receiverId, @sentAt, @status);";
            using(var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@text", msg.Text);
                command.Parameters.AddWithValue("@senderId", msg.SenderID.ToString());
                command.Parameters.AddWithValue("@receiverId", msg.ReceiverID.ToString());
                command.Parameters.AddWithValue("@sentAt", msg.SentAt.ToString("o"));
                command.Parameters.AddWithValue("@status", (int)msg.Status);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}