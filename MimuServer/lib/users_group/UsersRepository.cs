using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LocalMimu.Models;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace LocalMimu.Repositories;

public class UsersRepository
{
    private Dictionary<Guid, User> _users = new Dictionary<Guid, User>();
    private readonly string _path = "users.json";
    private readonly IStorage _storage;
    public bool IsAnomymous;
    private readonly string _sqlPath = "Data Source=localmimu.db";
    public UsersRepository(IStorage storage)
    {
        _storage = storage;
    }

    public async Task<List<User>> SearchUsersAsync(string query)
    {
        var result = _users.Values.Where(u => u.Username.ToLower().Contains(query.ToLower())).ToList();
        return result;
    }

    public async Task AddUser(User user)
    {
        using(var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO Users (Id, Username, Name) VALUES (@id, @username, @name);";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", user.Id.ToString());
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@name", user.Name);

                await command.ExecuteNonQueryAsync();
            }
            
        }
    }

    public void DeleteUser(User user)
    {
        _users.Remove(user.Id, out user); 
    }

    public void AnonymousMode()
    {
        IsAnomymous = true;
    }

    public async Task<User?> FindByUsername(string username)
    {
        using(var connection = new SqliteConnection(_sqlPath))
        {
            await connection.OpenAsync();
            var query = "SELECT Id, Username, Name FROM Users WHERE Username = @username LIMIT 1";
            using(var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username); // мне пришлось.. и Я ЗНАЛ НО НЕ БЫЛ УВЕРЕН

                using(var reader = await command.ExecuteReaderAsync())
                {
                    if(await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0));
                        var uname = reader.GetString(1);
                        var name = reader.GetString(2);

                        return new User(name, uname) {Id = id};
                    }
                }
                
            }
            return null;
        }
    }

    public async Task<User?> AuthAsync(string username)
    {
        var user = await FindByUsername(username);

        if (user != null)
        {
            user.Status = UserStatus.Online;
            return user;
        }
        else
        {
            return null;
        }
    }

    public async Task SaveData()
    {
        string json = JsonSerializer.Serialize(_users);
        await _storage.Save(_path, json);
    }
    public async Task LoadData()
    {
        if (!await _storage.Exists(_path)) return;
        string json = await _storage.Load(_path); 
        if (string.IsNullOrWhiteSpace(json)) return;

        _users = JsonSerializer.Deserialize<Dictionary<Guid, User>>(json) ?? new();


    }
    public User? GetUserById(Guid id)
    {
        return _users.GetValueOrDefault(id);
    }

    public async Task<bool> Register(string name, string username)
    {
        var existingUser = await FindByUsername(username);

        if (existingUser != null)
        {
            return false;
        }
        else
        {
            User regUser = new User(name, username);
            await AddUser(regUser);
            await SaveData();
            return true;
        }
    }



}
