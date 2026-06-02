using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using LocalMimu.Models;
using LocalMimu.Repositories;


Dictionary<Guid, TcpClient> _clients = new();

Message message = new Message();

IStorage mainStorage = new FileStorage();

UsersRepository repo = new UsersRepository(mainStorage);

ChatManager chatManager = new ChatManager(mainStorage);

object _lock = new object();

TcpListener server = new TcpListener(IPAddress.Any, 5000);

server.Start();

DbInitializer.Initialize();

Console.WriteLine("Сервер запущен. Ожидание подключения...");

while (true)
{
    TcpClient client = await server.AcceptTcpClientAsync();

    Console.WriteLine("[Server] Клиент подключен!");
    _ = HandleClientAsync(client, chatManager);

}
async Task HandleClientAsync(TcpClient client, ChatManager chatManager)
{
    var stream = client.GetStream();
    var writer = new StreamWriter(stream) { AutoFlush = true };
    var reader = new StreamReader(stream);
    Guid assignedId = Guid.Empty;
    while (true)
    {

        try
        {
            var received = await reader.ReadLineAsync();
            var authPacket = JsonSerializer.Deserialize<NetworkPacket>(received);

            if (authPacket != null && authPacket.Type == PacketType.Auth)
            {
                var incomingUsername = authPacket.PayLoad;

                if (!string.IsNullOrWhiteSpace(incomingUsername))
                {
                    var result = await repo.AuthAsync(incomingUsername);
                    await repo.SaveData();
                    if (result != null)
                    {
                        lock (_lock) { _clients[result.Id] = client; }
                        assignedId = result.Id;
                        var resultInJson = JsonSerializer.Serialize(result);
                        var packetResponse = new NetworkPacket(PacketType.ServerResponse, resultInJson);
                        var finalPacket = JsonSerializer.Serialize(packetResponse);
                        await writer.WriteLineAsync(finalPacket);
                        Console.WriteLine($"[Server] Юзер {assignedId} получил ответ сервера.");
                        break;
                    }
                    else if (result == null)
                    {
                        var response = new NetworkPacket(PacketType.ServerResponse, "null");
                        var finalPacket = Deser.SerJson(response);
                        await writer.WriteLineAsync(finalPacket);
                        Console.WriteLine($"Неудачная попытка входа для {incomingUsername}");
                    }
                }


            }

            if (authPacket != null && authPacket.Type == PacketType.Register)
            {
                var originUser = JsonSerializer.Deserialize<User>(authPacket.PayLoad);
                if (originUser != null && !string.IsNullOrWhiteSpace(originUser.Username) && !string.IsNullOrWhiteSpace(originUser.Name))
                {
                    if (await repo.FindByUsername(originUser.Username) == null)
                    {
                        await repo.AddUser(originUser);
                        await repo.SaveData();
                        string serverResponse = "1";
                        var jsonAnswer = JsonSerializer.Serialize(serverResponse);
                        await writer.WriteLineAsync(jsonAnswer);
                        lock (_lock) { _clients[originUser.Id] = client; }
                        assignedId = originUser.Id;
                        Console.WriteLine($"Клиент успешно зарегистрирован: {assignedId}");
                        break;

                    }
                    else
                    {
                        await writer.WriteLineAsync(JsonSerializer.Serialize("0"));
                        break;
                    }

                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось войти. {ex.Message}");
            break;
        }
    }

    while (true)
    {
        try
        {

            var received = await reader.ReadLineAsync();
            if (received == null) break;
            var msg = JsonSerializer.Deserialize<NetworkPacket>(received);
            if (msg != null && msg.Type == PacketType.ChatMessage)
            {
                Console.WriteLine($"Сервер получил чат-пакет {msg.Type} | Перессылка.");
                var finalMsg = JsonSerializer.Deserialize<Message>(msg.PayLoad);

                var sender = repo.GetUserById(finalMsg.SenderID);
                finalMsg.SenderUsername = sender != null ? sender.Username : "Unknown";
                var finalAnswermsg = Deser.SerJson(finalMsg);
                msg.PayLoad = finalAnswermsg;
                var final = Deser.SerJson(msg);
                await SendPrivateMessage(finalMsg, final);
                await chatManager.SaveMsg(finalMsg);
                Console.WriteLine($"[{finalMsg.SentAt:HH:mm:ss}] от {finalMsg.SenderID} до {finalMsg.ReceiverID}: {finalMsg.Text}");
            }

            if (msg != null && msg.Type == PacketType.SearchUser)
            {
                var findingTheUser = await repo.FindByUsername(msg.PayLoad);
                var SearchJson = JsonSerializer.Serialize(findingTheUser);
                var ServerResponsing = new NetworkPacket(PacketType.ServerResponse, SearchJson);
                var finalData = JsonSerializer.Serialize(ServerResponsing);

                await writer.WriteLineAsync(finalData);
                Console.WriteLine($"Ответ отправлен");

            }
            if (msg != null && msg.Type == PacketType.GetChats)
            {
                Console.WriteLine($"Получен запрос чатов от пользователя: {msg.PayLoad}");
                Guid desering = Guid.Parse(msg.PayLoad);
                List<Guid> checking = chatManager.GetContactIds(desering);
                List<User> contactUser = new List<User>();

                foreach (var user in checking)
                {
                    var u = repo.GetUserById(user);
                    if (u != null)
                    {
                        contactUser.Add(u);
                    }
                }

                var jsonList = Deser.SerJson(contactUser);
                var packet = new NetworkPacket(PacketType.ServerResponse, jsonList);
                var finalpacket = Deser.SerJson(packet);

                await writer.WriteLineAsync(finalpacket);
                Console.WriteLine($"Отправлен список чатов ({checking.Count} шт.) для {desering}");
            }
        }
        catch
        {
            if (assignedId != Guid.Empty)
            {
                lock (_lock) { _clients.Remove(assignedId); }
            }
            if (client.Connected)
            {
                await Task.Delay(1000);
                client.Dispose();
            }
            Console.WriteLine($"[Server] Клиент {assignedId} отключился");
            return;
        }

    }

}
async Task BroadcastMessage(string messageText) // этот пока не трогаю, он и не нужен.
{
    byte[] data = System.Text.Encoding.UTF8.GetBytes(messageText);
    foreach (var pair in _clients)
    {
        try
        {
            TcpClient c = pair.Value;

            if (c.Connected)
            {
                await c.GetStream().WriteAsync(data);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Критическая ошибка связи] {ex.Message}");
            continue;

        }
    }

}
async Task SendPrivateMessage(Message msg, string rawJson)
{
    // фух, вроде разобрался.
    TcpClient targetClient = null;
    lock (_lock)
    {
        _clients.TryGetValue(msg.ReceiverID, out targetClient);
    }
    if (targetClient != null)
    {
        if (targetClient.Connected)
        {
            try
            {
                var stream = targetClient.GetStream();
                var writer = new StreamWriter(stream) { AutoFlush = true };
                await writer.WriteLineAsync(rawJson);
                Console.WriteLine($"Сообщение отправлено. {msg.ReceiverID}");
            }
            catch
            {
            }
        }
    }
    else
    {
        Console.WriteLine($"[SERVER] Пользователь {msg.ReceiverID} оффлайн. Сообщение в базе данных");
    }
}



