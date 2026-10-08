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
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO Messages (Id, Text, SenderId, ReceiverId, SentAt, Status) VALUES (@id, @text, @senderId, @receiverId, @sentAt, @status);";
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
        var query = "SELECT Id, Text, SenderId, ReceiverId, SentAt, Status FROM Messages WHERE (SenderId = @myId AND ReceiverId = @targetId) OR (SenderId = @targetId AND ReceiverId = @myId) ORDER BY SentAt ASC;";

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
    public async Task UpdateMessageStatusAsync(string msgId, MessageStatus status)
    {
        var query = "UPDATE Messages SET Status = @status WHERE Id = @id;";
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@status", (int)status);
                command.Parameters.AddWithValue("@id", msgId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    public async Task<List<Guid>> GetContactIdsAsync(Guid myId)
    {
        var contacts = new List<Guid>();

        var query = "SELECT DISTINCT SenderId, ReceiverId FROM Messages WHERE SenderId = @myId OR ReceiverId = @myId";

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
                        var sender = Guid.Parse(reader.GetString(0));
                        var receiver = Guid.Parse(reader.GetString(1));

                        var contactId = sender == myId ? receiver : sender;

                        if (!contacts.Contains(contactId))
                        {
                            contacts.Add(contactId);
                        }
                    }

                }
            }
        }
        return contacts;
    }
    public async Task SaveGroupMessageAsync(GroupMessagePayload msg)
    {

        var query = "INSERT INTO GroupMessages(Id, GroupId, Text, SenderId, SentAt, Status) VALUES (@id, @groupid, @text, @senderid, @sentat, @status);";
        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", msg.MessageId.ToString());
                command.Parameters.AddWithValue("@groupid", msg.GroupId.ToString());
                command.Parameters.AddWithValue("@text", msg.EncryptedText);
                command.Parameters.AddWithValue("@senderid", msg.SenderId.ToString());
                command.Parameters.AddWithValue("@sentAt", msg.SentAt);
                command.Parameters.AddWithValue("@status", MessageStatus.Delivered);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
    public async Task<List<GroupMessagePayload>> GetGroupHistoryAsync(Guid groupId)
    {
        var history = new List<GroupMessagePayload>();
        var query = "SELECT Id, GroupId, Text, SenderId, SentAt, Status FROM GroupMessages WHERE GroupId = @groupid ORDER BY SentAt ASC;";

        using (var connection = new SqliteConnection(_sqlpath))
        {
            await connection.OpenAsync();

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@groupid", groupId.ToString());
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var groupid = Guid.Parse(reader.GetString(1));
                        var text = reader.GetString(2);
                        var senderId = Guid.Parse(reader.GetString(3));
                        var sentAt = DateTime.Parse(reader.GetString(4));
                        // var status = (MessageStatus)(reader.GetInt32(5));
                        // статус не будем передавать по сети пока что

                        var sendMsg = new GroupMessagePayload()
                        {
                            MessageId = id,
                            GroupId = groupid,
                            EncryptedText = text,
                            SenderId = senderId,
                            SentAt = sentAt,
                        };
                        history.Add(sendMsg);



                    }
                }
            }
        }
        return history;
    }

}