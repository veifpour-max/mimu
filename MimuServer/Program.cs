using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using LocalMimu.Models;
using LocalMimu.Repositories;

Dictionary<Guid, TcpClient> _clients = new();

UsersRepository repo = new UsersRepository("Data Source=localmimu.db");

MessagesRepository msgRepo = new MessagesRepository("Data Source=localmimu.db");

object _lock = new object();

TcpListener server = new TcpListener(IPAddress.Any, 5000);

server.Start();

DbInitializer.Initialize();

Console.WriteLine("Сервер запущен. Ожидание подключения...");

while (true)
{
    TcpClient client = await server.AcceptTcpClientAsync();

    Console.WriteLine("[Server] Клиент подключен!");
    _ = HandleClientAsync(client, msgRepo);

}
async Task HandleClientAsync(TcpClient client, MessagesRepository messagesRepository)
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
                Console.WriteLine("Пакет Auth дошел до сервера.");
                var data = Deser.DeserJson<LoginPayload>(authPacket.PayLoad);

                User? user = await repo.AuthAsync(data.Username, data.PasswordHash);

                if (user != null)
                {
                    var serUser = Deser.SerJson(user);
                    var packet = new NetworkPacket(PacketType.ServerResponse, serUser);
                    var final = Deser.SerJson(packet);
                    await writer.WriteLineAsync(final);
                    assignedId = user.Id;
                    break;
                }
            }

            if (authPacket != null && authPacket.Type == PacketType.Register)
            {
                var data = Deser.DeserJson<RegisterPayload>(authPacket.PayLoad);
                bool success = await repo.Register(data.id, data.Name, data.Username, data.PasswordHash);

                if (success)
                {
                    lock (_lock) { _clients[data.id] = client; }
                    assignedId = data.id;
                    string serverResponse = "1";
                    var jsonAnswer = JsonSerializer.Serialize(serverResponse);
                    await writer.WriteLineAsync(jsonAnswer);
                    Console.WriteLine($"Клиент успешно зарегистрирован: {assignedId}");
                    break;
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

        _ = Task.Run(async () =>
        {
            try
            {
                while (client.Connected)
                {
                    await Task.Delay(30000);
                    var ping = new NetworkPacket(PacketType.Ping, "");
                    var serPing = Deser.SerJson(ping);

                    await writer.WriteLineAsync(serPing);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Клиент отвалился по таймауту {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        });
        try
        {

            var received = await reader.ReadLineAsync();
            if (received == null) break;
            var msg = JsonSerializer.Deserialize<NetworkPacket>(received);
            if (msg != null && msg.Type == PacketType.ChatMessage)
            {
                Console.WriteLine($"Сервер получил чат-пакет {msg.Type} | Перессылка.");
                var finalMsg = JsonSerializer.Deserialize<Message>(msg.PayLoad);

                var sender = await repo.GetUserById(finalMsg.SenderID);
                finalMsg.SenderUsername = sender != null ? sender.Username : "Unknown";
                var finalAnswermsg = Deser.SerJson(finalMsg);
                msg.PayLoad = finalAnswermsg;
                var final = Deser.SerJson(msg);
                await SendPrivateMessage(finalMsg, final);
                await msgRepo.SaveMessagesAsync(finalMsg);
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
                List<Guid> checking = await msgRepo.GetContactIdsAsync(desering);
                List<User> contactUser = new List<User>();

                foreach (var user in checking)
                {
                    var u = await repo.GetUserById(user);
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
            if (msg != null && msg.Type == PacketType.GetChatsHistory)
            {
                var makedResult = msg.PayLoad.Split('|');
                Guid myId = Guid.Parse(makedResult[0]);
                Guid targetId = Guid.Parse(makedResult[1]);

                List<Message> history = await msgRepo.GetChatHistoryAsync(myId, targetId);

                var serHistory = Deser.SerJson(history);

                var netPack = new NetworkPacket(PacketType.ServerResponse, serHistory);
                var finalPack = Deser.SerJson(netPack);

                await writer.WriteLineAsync(finalPack);
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



