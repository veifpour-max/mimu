using System.Net.Sockets;
using System.Net.Security;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net.NetworkInformation;

namespace LocalMimu.Models;

public class NetworkService
{
    private object _lock = new();
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    private bool _isListening = false;
    public event Action<Message>? OnMessageReceived;

    public event Action<Guid, MessageStatus>? OnMessageStatusChanged;

    ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

    public async Task ConnectAsync(string ip, int port)
    {
        DisposeOldResources();
        _isListening = false;
        OnStateChanged?.Invoke(ConnectionStates.Connecting);
        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);
        var stream = _client.GetStream();
        var sslStream = new SslStream(stream, false, (sender, cert, chain, errors) => true);
        await sslStream.AuthenticateAsClientAsync(ip);
        OnStateChanged?.Invoke(ConnectionStates.Connected);
        _reader = new StreamReader(sslStream);
        _writer = new StreamWriter(sslStream) { AutoFlush = true };
    }
    private void DisposeOldResources()
    {
       foreach(var kvp in _pendingRequests)
        {
            if(_pendingRequests.TryRemove(kvp.Key, out var tcs))
            {
                tcs.TrySetCanceled();
            }
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Close();
        _client?.Dispose();

        _writer = null;
        _reader = null;
        _client = null;
        _isListening = false;
    }

    public async Task<string> SendAndWaitAsync(NetworkPacket packet, int timeout = 10000)
    {
        var tcs = new TaskCompletionSource<string>();
        lock (_lock) { _pendingRequests.TryAdd(packet.RequestId, tcs); }
        await SendPacket(packet);

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            throw new Exception($"Сервер не ответил в течении {timeout} мс");
        }

    }
    public async Task SendPacket(NetworkPacket packet)
    {
        Console.WriteLine($"[TRACK 1] Начинаю отправку пакета Type: {packet.Type}");
        try
        {
            var send = Deser.SerJson(packet);
            Console.WriteLine($"[TRACK 2] Сериализация успешна: {send}");

            if (_writer == null)
            {
                Console.WriteLine("[TRACK ERROR] СТОП! _writer равен null!");
                return;
            }

            Console.WriteLine("[TRACK 3] Вызываю _writer.WriteLineAsync...");
            await _writer.WriteLineAsync(send);

            Console.WriteLine("[TRACK 4] Вызываю _writer.FlushAsync()...");
            await _writer.FlushAsync();

            Console.WriteLine($"[TRACK 5] Пакет Type: {packet.Type} физически ушел в ОС!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACK CRASH] Ошибка в SendPacket: {ex.Message}");
        }
    }
    public async Task<bool> RegisterAsync(RegisterPayload registerPayload)
    {
        var serializedUserPayload = Deser.SerJson(registerPayload);
        var regJson = new NetworkPacket(PacketType.Register, serializedUserPayload);
        var finalUser = Deser.SerJson(regJson);
        await _writer.WriteLineAsync(finalUser);
        var waiting = await _reader.ReadLineAsync();
        if (!shTools.check(waiting))
        {
            Console.WriteLine("Что-то пошло не так..");
            return false;
        }
        var finalAnswer = Deser.DeserJson<string>(waiting);
        if (finalAnswer == "1")
        {
            return true;
        }
        return false;
    }

    public async Task<User?> AuthenticateAsync(LoginPayload login)
    {
        var loginSer = Deser.SerJson(login);
        var authPacket = new NetworkPacket(PacketType.Auth, loginSer);
        var finalPacket = Deser.SerJson(authPacket);
        await _writer.WriteLineAsync(finalPacket);
        var waiting = await _reader.ReadLineAsync();

        if (!shTools.check(waiting))
        {
            Console.WriteLine("Что-то пошло не так...");
            return null;
        }
        var deserAnswer = Deser.DeserJson<NetworkPacket>(waiting);
        if (deserAnswer != null)
        {
            var desering = Deser.DeserJson<User>(deserAnswer.PayLoad);
            return desering;
        }
        return null;


    }
    public void StartListening()
    {
        if (_isListening) return;
        _isListening = true;
        _ = StartReceiving();
    }

    public event Action<ConnectionStates?>? OnStateChanged;
    public async Task StartReceiving()
    {
        while (true)
        {
            try
            {
                var receivedMsg = await _reader.ReadLineAsync();
                Console.WriteLine($"[DEBUG] Читаю: {receivedMsg?.Length ?? 0} символов: {receivedMsg}");
                if (receivedMsg == null) throw new Exception("Соединение разорвано");
                var msg = Deser.DeserJson<NetworkPacket>(receivedMsg);

                if (msg != null && msg.Type == PacketType.ChatMessage)
                {
                    if (shTools.check(msg.PayLoad))
                    {
                        var finalMsg = Deser.DeserJson<Message>(msg.PayLoad);
                        if (finalMsg != null)
                        {
                            OnMessageReceived?.Invoke(finalMsg);
                            string ackPayload = $"{finalMsg.Id}|{finalMsg.SenderID}";
                            var ackPacket = new NetworkPacket(PacketType.MessageDelivered, ackPayload);
                            _ = SendPacket(ackPacket);
                            Console.WriteLine($"\n[{shTools.FormatTime(finalMsg.SentAt)}] | {finalMsg.SenderUsername}: {finalMsg.Text}");
                            Console.Write("Вы: ");
                        }
                    }
                }
                if (msg != null && msg.Type == PacketType.MessageDelivered)
                {
                    var split = msg.PayLoad.Split("|");
                    if (split.Length == 2)
                    {
                        var msgId = Guid.Parse(split[0]);
                        OnMessageStatusChanged?.Invoke(msgId, MessageStatus.Delivered);
                    }
                }


                if (msg != null && msg.Type == PacketType.ServerResponse)
                {
                    var reqId = msg.RequestId;
                    if (_pendingRequests.TryRemove(reqId, out var tcs))
                    {
                        tcs.SetResult(msg.PayLoad);
                    }
                }
                if (msg != null && msg.Type == PacketType.Ping)
                {
                    await SendPacket(new NetworkPacket(PacketType.Pong, ""));
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is ObjectDisposedException)
                {
                    OnStateChanged?.Invoke(ConnectionStates.Disconnected);
                    break;
                }
                Console.WriteLine($"Ошибка! {ex.Message}");
                OnStateChanged?.Invoke(ConnectionStates.Disconnected);
                DisposeOldResources();
                break;
            }

        }
    }
}