using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using LocalMimu.Models;
using LocalMimu.Repositories;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Data;

ConcurrentDictionary<Guid, ClientConnection> _clients = new();
ConcurrentDictionary<string, int> _failedAttempts = new();
ConcurrentDictionary<string, DateTime> _bannedIps = new();

UsersRepository repo = new UsersRepository(DbConfig.ConnectionString);

MessagesRepository msgRepo = new MessagesRepository(DbConfig.ConnectionString);

X509Certificate2 serverCert = new X509Certificate2("server.pfx", "12345");

MinioService _minio = new();

object _lock = new object();

TcpListener server = new TcpListener(IPAddress.Any, 8000);

server.Start();

await DbInitializer.Initialize();

Console.WriteLine("Сервер запущен. Ожидание подключения...");

while (true)
{
    TcpClient client = await server.AcceptTcpClientAsync();

    Console.WriteLine($"[Server] Клиент подключен!");
    _ = HandleClientAsync(client, msgRepo);

}
async Task HandleClientAsync(TcpClient client, MessagesRepository messagesRepository)
{
    string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
    if (_bannedIps.TryGetValue(ip, out DateTime timeNow))
    {
        if (DateTime.UtcNow < timeNow)
        {
            client.Close();
            return;
        }
        else
        {
            _bannedIps.TryRemove(ip, out _);
        }
    }
    var stream = client.GetStream();
    var sslStream = new SslStream(stream, false);

    try
    {
        await sslStream.AuthenticateAsServerAsync(serverCert, clientCertificateRequired: false, checkCertificateRevocation: true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка рукопожатия: {ex.Message}");
        client.Close();
        return;
    }
    var writer = new StreamWriter(sslStream) { AutoFlush = true };
    var reader = new StreamReader(sslStream);
    var connetion = new ClientConnection { Writer = writer, Client = client };
    Guid assignedId = Guid.Empty;
    while (true)
    {

        try
        {
            var received = await reader.ReadLineAsync();
            if (received == null) break;
            if (string.IsNullOrWhiteSpace(received))
            {
                continue;
            }

            var authPacket = JsonSerializer.Deserialize<NetworkPacket>(received);

            if (authPacket != null && authPacket.Type == PacketType.Auth)
            {
                Console.WriteLine("Пакет Auth дошел до сервера.");
                var data = Deser.DeserJson<LoginPayload>(authPacket.PayLoad);

                User? user = await repo.AuthAsync(data.Username, data.Password);

                if (user != null)
                {
                    var serUser = Deser.SerJson(user);
                    var packet = new NetworkPacket(PacketType.ServerResponse, serUser);
                    var final = Deser.SerJson(packet);
                    _failedAttempts.TryRemove(ip, out _);
                    await connetion.SendAsync(final);
                    _clients[user.Id] = connetion;
                    assignedId = user.Id;
                    break;
                }
                else
                {
                    var packet = new NetworkPacket(PacketType.ServerResponse, "");
                    var final = Deser.SerJson(packet);
                    _failedAttempts.AddOrUpdate(ip, 1, (key, oldValue) => oldValue + 1);
                    await connetion.SendAsync(final);
                    if (_failedAttempts.TryGetValue(ip, out int attempts))
                    {
                        if (attempts >= 5)
                        {
                            _bannedIps[ip] = DateTime.UtcNow.AddMinutes(15);
                            Console.WriteLine($"{ip} заблокирован на 15 минут за брутфорс/попытку неправомерного доступа");
                        }
                    }
                    Console.WriteLine("Ошибка авторизации: неверный пароль");
                }
            }

            if (authPacket != null && authPacket.Type == PacketType.Register)
            {
                var data = Deser.DeserJson<RegisterPayload>(authPacket.PayLoad);
                bool success = await repo.Register(data.id, data.Name, data.Username, data.Password, data.PublicKey);

                if (success)
                {
                    _clients[data.id] = connetion;
                    assignedId = data.id;
                    string serverResponse = "1";
                    var jsonAnswer = JsonSerializer.Serialize(serverResponse);
                    await connetion.SendAsync(jsonAnswer);
                    Console.WriteLine($"Клиент успешно зарегистрирован: {assignedId}");
                    break;
                }
                else
                {
                    _failedAttempts.AddOrUpdate(ip, 1, (key, oldValue) => oldValue + 1);
                    if (_failedAttempts.TryGetValue(ip, out int attempts))
                    {
                        if (attempts >= 5)
                        {
                            Console.WriteLine($"{ip} заблокирован на 15 минут за брутфорс/попытку неправомерного доступа");
                            _bannedIps[ip] = DateTime.UtcNow.AddMinutes(15);
                        }
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

    _ = Task.Run(async () =>
            {
                try
                {
                    while (client.Connected)
                    {
                        await Task.Delay(30000);
                        var ping = new NetworkPacket(PacketType.Ping, "");
                        var serPing = Deser.SerJson(ping);
                        await connetion.SendAsync(serPing);
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

    while (true)
    {
        try
        {

            var received = await reader.ReadLineAsync();
            if (received == null)
            {
                break;
            }
            if (string.IsNullOrWhiteSpace(received))
            {
                continue;
            }
            var msg = JsonSerializer.Deserialize<NetworkPacket>(received);
            if (msg == null)
            {
                Console.WriteLine("Десериализация вернула NULL");
                continue;
            }
            Console.WriteLine($"Прилетел пакет: {(int)msg.Type}");

            if (msg != null && msg.Type == PacketType.ChatMessage)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                Console.WriteLine($"Сервер получил чат-пакет {msg.Type} | Перессылка.");
                var finalMsg = JsonSerializer.Deserialize<Message>(msg.PayLoad);
                if (finalMsg == null || finalMsg.SenderID != assignedId)
                {
                    Console.WriteLine($"[SECURITY] {assignedId} прислал чужой SenderID ({finalMsg?.SenderID}). Пакет отброшен.");
                    continue;
                }
                var sender = await repo.GetUserById(finalMsg.SenderID);
                finalMsg.SenderUsername = sender != null ? sender.Username : "Unknown";
                var finalAnswermsg = Deser.SerJson(finalMsg);
                msg.PayLoad = finalAnswermsg;
                var final = Deser.SerJson(msg);
                await SendPrivateMessage(finalMsg, final);
                await msgRepo.SaveMessagesAsync(finalMsg);
                Console.WriteLine($"[{finalMsg.SentAt:HH:mm:ss}] от {finalMsg.SenderID} до {finalMsg.ReceiverID}: {finalMsg.Text}");
            }

            else if (msg != null && msg.Type == PacketType.SearchUser)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                var makedResult = msg.PayLoad.Split('|');
                Guid myId = Guid.Parse(makedResult[0]);
                if (myId != assignedId) continue;
                var findingTheUser = await repo.FindByUsername(msg.PayLoad);
                var SearchJson = JsonSerializer.Serialize(findingTheUser);
                var ServerResponsing = new NetworkPacket(PacketType.ServerResponse, SearchJson);
                var finalData = JsonSerializer.Serialize(ServerResponsing);

                await writer.WriteLineAsync(finalData);
                Console.WriteLine($"Ответ отправлен");

            }
            else if (msg != null && msg.Type == PacketType.GetChats)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
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
            else if (msg != null && msg.Type == PacketType.GetChatsHistory)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                var makedResult = msg.PayLoad.Split('|');
                Guid myId = Guid.Parse(makedResult[0]);
                Guid targetId = Guid.Parse(makedResult[1]);

                List<Message> history = await msgRepo.GetChatHistoryAsync(myId, targetId);

                var serHistory = Deser.SerJson(history);

                var netPack = new NetworkPacket(PacketType.ServerResponse, serHistory);
                var finalPack = Deser.SerJson(netPack);

                await connetion.SendAsync(finalPack);
            }
            else if (msg != null && msg.Type == PacketType.MessageDelivered)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                var splitingResult = msg.PayLoad.Split("|");
                if (splitingResult.Length == 2)
                {
                    var msgId = splitingResult[0];
                    var originalReceiverId = Guid.Parse(splitingResult[1]);

                    await msgRepo.UpdateMessageStatusAsync(msgId, MessageStatus.Delivered);
                    var final = Deser.SerJson(msg);
                    ClientConnection target = null;
                    _clients.TryGetValue(originalReceiverId, out target);
                    if (target != null && target.Client.Connected)
                    {
                        await target.SendAsync(final);
                    }
                }
            }
            else if (msg != null && msg.Type == PacketType.GetPublicKey)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                var id = Guid.Parse(msg.PayLoad);
                if (id != assignedId) return;
                var key = await repo.GetPublicKeyAsync(id);

                string answerToSend = string.Join("|", id, key);
                var send = new NetworkPacket(PacketType.ServerResponse, answerToSend);
                var serSend = Deser.SerJson(send);
                await connetion.SendAsync(serSend);
            }
            else if (msg != null && msg.Type == PacketType.RequestUploadUrl)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                Console.WriteLine("Запрос на получение url получен!");
                var url = await _minio.GenerateUploadUrl(msg.PayLoad);
                Console.WriteLine($"url сгенерирован! {url}");
                var send = new NetworkPacket(PacketType.ServerResponse, url.ToString());
                Console.WriteLine("networkpacket сделан!");
                var desering = Deser.SerJson(send);
                Console.WriteLine("networkpacket сериализован");
                await connetion.SendAsync(desering);
                Console.WriteLine("ОТВЕТ ОТПРАВЛЕН!");
            }
            else if (msg != null && msg.Type == PacketType.RequestDownloadUrl)
            {
                Console.WriteLine($"[DEBUG] Распарсил пакет. Type в цифрах: {(int)msg.Type}. Type в тексте: {msg.Type}");
                var url = await _minio.GetDownloadUrl(msg.PayLoad);
                Console.WriteLine($"url сгенерирован! {url}");
                var send = new NetworkPacket(PacketType.ServerResponse, url.ToString());
                var sering = Deser.SerJson(send);
                await connetion.SendAsync(sering);
                Console.WriteLine("ОТВЕТ ОТПРАВЛЕН!");
            }

        }
        catch
        {
            if (assignedId != Guid.Empty)
            {
                _clients.TryRemove(assignedId, out _);
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
async Task SendPrivateMessage(Message msg, string rawJson)
{

    ClientConnection targetConnection = null;
    _clients.TryGetValue(msg.ReceiverID, out targetConnection);
    if (targetConnection != null && targetConnection.Client.Connected)
    {
        try
        {
            await targetConnection.SendAsync(rawJson);
            Console.WriteLine($"Сообщение отправлено. {msg.ReceiverID}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка доставки сообщения: {msg.ReceiverID} >> {ex.Message}");

            _clients.TryRemove(msg.ReceiverID, out _);
        }
    }
}





