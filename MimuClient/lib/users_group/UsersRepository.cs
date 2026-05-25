using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LocalMimu.Models;

namespace LocalMimu.Repositories;

public class UsersRepository
{
    private Dictionary<Guid, User> _users = new Dictionary<Guid, User>();
    private readonly string _path = "users.json";
    private readonly IStorage _storage;
    public bool IsAnomymous;
    public UsersRepository(IStorage storage)
    {
        _storage = storage;
        //if (!_users.Values.Any(u => u.Username.ToLower() == "alex"))
       // {
       //     AddUser(new User("саша", "alex")).Wait(); // тут вроде решение
       //     AddUser(new User("сашаклон", "alextest")).Wait();
      //  }

    }

    public async Task<List<User>> SearchUsersAsync(string query)
    {
        var result = _users.Values.Where(u => u.Username.ToLower().Contains(query.ToLower())).ToList();
        return result;
    }

    public async Task AddUser(User user)
    {
        _users.Add(user.Id, user);
        await SaveData();
    }

    public void DeleteUser(User user)
    {
        _users.Remove(user.Id, out user); 
    }

    public void AnonymousMode()
    {
        IsAnomymous = true;
    }

    public User? FindByUsername(string username)
    {
        return _users.Values.FirstOrDefault(u => u.Username == username);
    }

    public async Task<User?> AuthAsync(string username)
    {
        await Task.Delay(400);
        var result = _users.Values.FirstOrDefault(m => m.Username.ToLower() == username.ToLower());

        if (result != null)
        {
            result.Status = UserStatus.Online;
            return result;
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
        var existingUser = FindByUsername(username);

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
