using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LocalMimu.Models;
using System.Collections.Generic;
using MimuGui;

namespace LocalMimu.Models;

public class LocalMessagesRepository
{
    private readonly string _filePath;
    private readonly string _sqlpath;

    public LocalMessagesRepository()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appData, "LocalMimu");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        _filePath = Path.Combine(directoryPath, "local_history.db");
        _sqlpath = $"Data Source={_filePath}";
    }

    public async Task InitializeLocalDatabase()
    {
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand("PRAGMA journal_mode=WAL;", connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            var createLocalMessagesTable = @"
            CREATE TABLE IF NOT EXISTS LocalMessages( 
                Id TEXT PRIMARY KEY,
                Text TEXT NOT NULL,
                SenderId TEXT NOT NULL,
                ReceiverId TEXT NOT NULL,
                SentAt TEXT NOT NULL,
                Status INTEGER NOT NULL
            );";
            var createLocalUsersTable = @"
            CREATE TABLE IF NOT EXISTS LocalUsers(
            Id TEXT PRIMARY KEY,
            Username TEXT NOT NULL,
            Name TEXT NOT NULL
            );";

            using (var command = new SqliteCommand(createLocalMessagesTable, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            using (var command = new SqliteCommand(createLocalUsersTable, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

    }

    public async Task SaveUserAsync(User user)
    {
        var query = "INSERT OR REPLACE INTO LocalUsers (Id, Username, Name) VALUES (@id, @username, @name);";
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", user.Id.ToString());
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@name", user.Name);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<List<User>> GetLocalUsersAsync()
    {
        var users = new List<User>();
        var query = "SELECT Id, Username, Name FROM LocalUsers;";

        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse(reader.GetString(0));
                    var username = reader.GetString(1);
                    var name = reader.GetString(2);
                    users.Add(new User(name, username) { Id = id });
                }
            }
        }
        return users;
    }

    public async Task SaveMessagesAsync(Message msg)
    {
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            var query = "INSERT OR IGNORE INTO LocalMessages (Id, Text, SenderId, ReceiverId, SentAt, Status) VALUES (@id, @text, @senderId, @receiverId, @sentAt, @status);";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", msg.Id.ToString());
                command.Parameters.AddWithValue("@text", msg.Text);
                command.Parameters.AddWithValue("@senderId", msg.SenderID.ToString());
                command.Parameters.AddWithValue("@receiverId", msg.ReceiverID.ToString());
                command.Parameters.AddWithValue("@sentAt", msg.SentAt.ToString("o"));
                command.Parameters.AddWithValue("@status", (int)msg.Status);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
    public async Task<List<Message>> GetChatHistoryAsync(Guid myId, Guid targetId)
    {
        var history = new List<Message>();
        var query = "SELECT Id, Text, SenderId, ReceiverId, SentAt, Status FROM LocalMessages WHERE (SenderId = @myId AND ReceiverId = @targetId) OR (SenderId = @targetId AND ReceiverId = @myId) ORDER BY SentAt ASC;";

        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@myId", myId.ToString());
                command.Parameters.AddWithValue("@targetId", targetId.ToString());
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var msgId = Guid.Parse(reader.GetString(0));
                        var text = reader.GetString(1);
                        var sender = Guid.Parse(reader.GetString(2));
                        var receiver = Guid.Parse(reader.GetString(3));
                        var sentAt = DateTime.Parse(reader.GetString(4));
                        var status = (MessageStatus)reader.GetInt32(5);

                        var msg = new Message(text, sender, receiver, MessageType.Text) { Id = msgId, SentAt = sentAt, Status = status };
                        history.Add(msg);
                    }
                }
            }

        }
        return history;
    }

    public async Task<List<Guid>> GetLocalContactsAsync(Guid myId)
    {
        var contacts = new List<Guid>();

        var query = @"
        SELECT DISTINCT SenderId FROM LocalMessages WHERE ReceiverId = @myId
        UNION
        SELECT DISTINCT ReceiverId FROM LocalMessages WHERE SenderId = @myId;";

        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@myId", myId.ToString());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        contacts.Add(Guid.Parse(reader.GetString(0)));
                    }
                }
            }
        }
        return contacts;
    }




}