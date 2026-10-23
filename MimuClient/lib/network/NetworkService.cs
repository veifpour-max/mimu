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

    public event Action<GroupMessagePayload>? OnGroupMessageReceived;

    public event Action<GroupKeyPayload>? OnGroupKeyReceived;

    public event Action<Guid, MessageStatus>? OnMessageStatusChanged;

    ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

    public async Task ConnectAsync(string ip, int port)
    {
        DisposeOldResources();
        _isListening = false;
        OnStateChanged?.Invoke(ConnectionStates.Connecting);
        _client = new TcpClient();

        try
        {
            using var cts = new CancellationTokenSource(5000);
            await _client.ConnectAsync(ip, port).WaitAsync(cts.Token);
            var stream = _client.GetStream();
            var sslStream = new SslStream(stream, false, (sender, cert, chain, errors) => true);
            await sslStream.AuthenticateAsClientAsync(ip).WaitAsync(cts.Token);

            _reader = new StreamReader(sslStream);
            _writer = new StreamWriter(sslStream) { AutoFlush = true };
            OnStateChanged?.Invoke(ConnectionStates.Connected);
        }
        catch
        {
            DisposeOldResources();
            throw;
        }
    }
    private void DisposeOldResources()
    {
        foreach (var kvp in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(kvp.Key, out var tcs))
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
        try
        {
            await SendPacket(packet);
        }
        catch
        {
            _pendingRequests.TryRemove(packet.RequestId, out _);
            throw;
        }

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled());
        var answer = await tcs.Task;
        if (answer.StartsWith("ERROR|"))
        {
            throw new Exception("Сервер отклонил запрос: " + answer["ERROR|".Length..]);
        }
        return answer;
    }
    public async Task SendPacket(NetworkPacket packet)
    {
        var send = Deser.SerJson(packet);

        if (_writer == null)
        {
            throw new InvalidOperationException("Writer равен нулю. Отправка невозможна");
        }
        await _writer.WriteLineAsync(send);

        await _writer.FlushAsync();

    }
    public async Task<bool> RegisterAsync(RegisterPayload registerPayload)
    {
        var serializedUserPayload = Deser.SerJson(registerPayload);
        var regJson = new NetworkPacket(PacketType.Register, serializedUserPayload);
        var finalUser = Deser.SerJson(regJson);
        if (_writer == null || _reader == null)
        {
            throw new Exception("Сокет не инициализирован");
        }
        using var cts = new CancellationTokenSource(10000);
        await _writer.WriteLineAsync(finalUser);
        var waiting = await _reader.ReadLineAsync(cts.Token);
        if (waiting == null)
        {
            throw new Exception("Сервер сбросил соединение");
        }
        if (!shTools.check(waiting))
        {
            Console.WriteLine("Что-то пошло не так..");
            return false;
        }
        var finalAnswer = Deser.DeserJson<NetworkPacket>(waiting);
        if (finalAnswer == null)
        {
            return false;
        }
        if (finalAnswer.PayLoad == "OK")
        {
            return true;
        }
        return false;
    }

    public async Task<User?> AuthenticateAsync(LoginPayload login, int timeout = 10000)
    {
        if (_writer == null || _reader == null)
        {
            throw new Exception("Сокет не инициализирован");
        }
        using var cts = new CancellationTokenSource(timeout);
        var loginSer = Deser.SerJson(login);
        var authPacket = new NetworkPacket(PacketType.Auth, loginSer);
        var finalPacket = Deser.SerJson(authPacket);
        await _writer.WriteLineAsync(finalPacket);
        var waiting = await _reader.ReadLineAsync(cts.Token);

        if (waiting == null)
        {
            throw new Exception("Сервер сбросил соединение");
        }

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
                if (msg != null && msg.Type == PacketType.SendingGroupKey)
                {
                    var payload = Deser.DeserJson<GroupKeyPayload>(msg.PayLoad);
                    if (payload != null)
                    {
                        OnGroupKeyReceived?.Invoke(payload);
                    }
                }
                if (msg != null && msg.Type == PacketType.GroupMessage)
                {
                    var payload = Deser.DeserJson<GroupMessagePayload>(msg.PayLoad);
                    if (payload != null)
                    {
                        OnGroupMessageReceived?.Invoke(payload);
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is ObjectDisposedException)
                {
                    OnStateChanged?.Invoke(ConnectionStates.Disconnected);
                    // без этого _writer останется не-null, и первая отправка после
                    // разрыва молча уйдет в мертвый сокет вместо исключения
                    DisposeOldResources();
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