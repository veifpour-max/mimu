using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using LocalMimu.Repositories;
using Microsoft.VisualBasic;

namespace LocalMimu.Models;

class ChatManager
{
    private readonly string _path = "messages.json";
    private List<Message> _allMessages = new List<Message>();
    private readonly IStorage _storage;

    public ChatManager(IStorage storage)
    {
        _storage = storage;
        LoadData().Wait();
        
    }

    public async Task<List<Message>> GetMessagesByUserAsync(Guid userId)
    {
        Console.WriteLine("[System] Ищу сообщения в базе...");
        await Task.Delay(1500);

        var result = _allMessages.Where(m => m.SenderID == userId).OrderBy(m => m.SentAt).ToList();

        return result;

    }
    public async Task SaveMessage(Message msg)
    {
        _allMessages.Add(msg);
        Console.WriteLine($"Сообщение сохранено в архив. Всего: {_allMessages.Count}");
        await SaveData();
    }

    private async Task SaveData()
    {
        string jsonSerialize = JsonSerializer.Serialize(_allMessages);
        await _storage.Save(_path, jsonSerialize);
    }

    public async Task SaveMsg(Message msg)
    {
        _allMessages.Add(msg);
        await SaveData();
    }

    public async Task LoadData()
    {

        if (File.Exists(_path))
        {
            string jsontext = await _storage.Load(_path);

            if (!string.IsNullOrWhiteSpace(jsontext))
            {
                _allMessages = JsonSerializer.Deserialize<List<Message>>(jsontext) ?? new List<Message>();
            } 

        }

    }
    public List<Message> GetRecentMessages(int count)
    {
        // и что это делает?
        return _allMessages.TakeLast(count).ToList();
    }

    public List<Message> GetMessagesFromContact(Guid myId, Guid targetID)
    {
        // получается кто отправляет а кто получает могут быть одни в памяти, но для каждого получатель и отправитель разные
        return _allMessages.Where(m => m.SenderID == myId && m.ReceiverID == targetID || m.SenderID == targetID && m.ReceiverID == myId).ToList();
    }

    public List<Guid> GetContactIds(Guid MyId)
    {
        return _allMessages.Where(m => m.SenderID == MyId || m.ReceiverID == MyId).Select(m => m.SenderID == MyId ? m.ReceiverID : m.SenderID).Distinct().ToList();
    }


}

