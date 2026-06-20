using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using LocalMimu.Models;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace LocalMimu.Repositories;

public class UsersRepository
{
    public bool IsAnomymous;
    private readonly string _sqlPath = "Data Source=localmimu.db";
    public UsersRepository(string sql)
    {
        _sqlPath = sql;
    }

    public async Task<List<User>> SearchUsersAsync(string query)
    {
        var queryToServer = "SELECT Id, Username, Name FROM Users WHERE Username LIKE @query";

        var usersList = new List<User>();

        using(var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            using(var command = new SqliteCommand(queryToServer, connection))
            {
                command.Parameters.AddWithValue(@query, $"%{query}%");

                using(var reader = await command.ExecuteReaderAsync())
                {
                    while(await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var uname = reader.GetString(1);
                        var name = reader.GetString(2);

                        var adding = new User(name, uname) {Id = id};
                        
                        usersList.Add(adding);
                    }
                }

            }
        }
        return usersList;

    }

    public async Task AddUser(User user, string passwordHash)
    {
        using (var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO Users (Id, Username, Name, PasswordHash) VALUES (@id, @username, @name, @passwordHash);";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", user.Id.ToString());
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);

                await command.ExecuteNonQueryAsync();
            }

        }
    }

    public void AnonymousMode()
    {
        IsAnomymous = true;
    }

    public async Task<User?> FindByUsername(string username)
    {
        using (var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            var query = "SELECT Id, Username, Name FROM Users WHERE Username = @username LIMIT 1";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var uname = reader.GetString(1);
                        var name = reader.GetString(2);

                        return new User(name, uname) { Id = id };
                    }
                }

            }
            return null;
        }
    }

    public async Task<User?> AuthAsync(string username, string clientHash)
    {
        // как я понимаю мы отказались от findbyusername из-за паролей и невозможности добавления их в этот метод, ведь он используется и в поиске, и в реге, и во входе

        var query = "SELECT Id, Username, Name, PasswordHash FROM Users WHERE Username = @username LIMIT 1";

        using(var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            using(var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                using(var reader = await command.ExecuteReaderAsync())
                {
                    if(await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var uname = reader.GetString(1);
                        var name = reader.GetString(2);
                        var pass = reader.GetString(3);

                        if(pass == clientHash)
                        {
                            var user = new User(name, uname) {Id = id};
                            user.Status = UserStatus.Online;
                            return user;
                        }
                    }
                }
            }
        }
        return null;
    }

    public async Task<User?> GetUserById(Guid id)
    {
        var query = "SELECT Id, Username, Name FROM Users WHERE Id = @id LIMIT 1;";
        using (var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", id.ToString());
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var idFromDb = Guid.Parse(reader.GetString(0));
                        var uname = reader.GetString(1);
                        var name = reader.GetString(2);

                        return new User(name, uname) {Id = idFromDb};
                    }

                }
            }
        }
        return null;
    }

    public async Task<bool> Register(Guid id, string name, string username, string password)
    {
        var existingUser = await FindByUsername(username);

        if (existingUser != null)
        {
            return false;
        }
        else
        {
            User regUser = new User(name, username) {Id = id};
            await AddUser(regUser, password);
            return true;
        }
    }



}
