using LocalMimu.Models;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Linq;
using System.Net;

namespace LocalMimu.Repositories;


public class GroupsRepository
{
    private readonly string ConnectionPath = DbConfig.ConnectionString;

    public GroupsRepository(string sql)
    {
        ConnectionPath = sql;
    }

    public async Task CreateGroup(GroupChat groupChat, DateTime createdAt)
    {
        var query = "INSERT INTO Groups (Id, Name, OwnerId, CreatedAt) VALUES (@id, @name, @ownerid, @createdAt)";
        var memberQuery = "INSERT INTO GroupMembers (GroupId, UserId) VALUES (@groupid, @userid)";

        using (var connection = new SqliteConnection(ConnectionPath))
        {
            await connection.OpenAsync();

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", groupChat.Id.ToString());
                command.Parameters.AddWithValue("@name", groupChat.Name);
                command.Parameters.AddWithValue("@ownerid", groupChat.OwnerId.ToString());
                command.Parameters.AddWithValue("@createdAt", createdAt);

                await command.ExecuteNonQueryAsync();
            }

            foreach (var memberId in groupChat.Members)
            {
                using (var command = new SqliteCommand(memberQuery, connection))
                {
                    command.Parameters.AddWithValue("@groupid", groupChat.Id.ToString());
                    command.Parameters.AddWithValue("@userid", memberId.ToString());

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public async Task<List<GroupChat>> GetAllAsync()
    {
        var groups = new List<GroupChat>();
        var members = new Dictionary<string, List<Guid>>();

        using (var connection = new SqliteConnection(ConnectionPath))
        {
            await connection.OpenAsync();

            using (var command = new SqliteCommand("SELECT Id, Name, OwnerId FROM Groups", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    groups.Add(new GroupChat
                    {
                        Id = Guid.Parse(reader.GetString(0)),
                        Name = reader.GetString(1),
                        OwnerId = Guid.Parse(reader.GetString(2)),
                        Members = new List<Guid>()
                    });
                }
            }

            using (var command = new SqliteCommand("SELECT GroupId, UserId FROM GroupMembers", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var groupId = reader.GetString(0);
                    if (!members.TryGetValue(groupId, out var list))
                    {
                        list = new List<Guid>();
                        members[groupId] = list;
                    }
                    list.Add(Guid.Parse(reader.GetString(1)));
                }
            }
        }

        foreach (var group in groups)
        {
            if (members.TryGetValue(group.Id.ToString(), out var list))
            {
                group.Members = list;
            }
        }

        return groups;
    }

    public async Task<List<GroupChat>> GetByMemberAsync(Guid userId)
    {
        var resultGroups = new List<GroupChat>();
        var query = "SELECT g.Id, g.Name, g.OwnerId FROM Groups g INNER JOIN GroupMembers gm ON g.Id = gm.GroupId WHERE gm.UserId = @userid;";

        using (var connection = new SqliteConnection(ConnectionPath))
        {
            await connection.OpenAsync();

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userid", userId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var name = reader.GetString(1);
                        var ownerId = Guid.Parse(reader.GetString(2));

                        var payload = new GroupChat() { Id = id, Name = name, OwnerId = ownerId, Members = new List<Guid>()};
                        resultGroups.Add(payload);
                    }

                }
                foreach (var group in resultGroups)
                {
                    var request = "SELECT UserId FROM GroupMembers WHERE GroupId = @groupid";
                    using(var comm = new SqliteCommand(request, connection))
                    {
                        using(var read = await comm.ExecuteReaderAsync())
                        {
                            while(await read.ReadAsync())
                            {
                                var userid = Guid.Parse(read.GetString(0));
                                group.Members.Add(userid);
                            }
                        }
                    }
                }
            }
        }
        return resultGroups;


    }

}