using System.Net.Sockets;
using System.Net.Security;
using System.ComponentModel;

namespace LocalMimu.Models;

public class NetworkService
{
    private object _lock;
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    public event Action<Message>? OnMessageReceived;

    public event Action<Guid, MessageStatus>? OnMessageStatusChanged;

    private Queue<TaskCompletionSource<string>> _pending = new();

    public async Task ConnectAsync(string ip, int port)
    {
        DisposeOldResources();
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
        while (_pending.TryDequeue(out var tcs))
            tcs.TrySetCanceled();

        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Close();
        _client?.Dispose();

        _writer = null;
        _reader = null;
        _client = null;
    }

    public async Task<string> SendAndWaitAsync(NetworkPacket packet)
    {
        var tcs = new TaskCompletionSource<string>();
        lock (_lock) { _pending.Enqueue(tcs); }
        await SendPacket(packet);
        return await tcs.Task;
    }
    public async Task SendPacket(NetworkPacket packet)
    {
        var send = Deser.SerJson(packet);
        await _writer.WriteLineAsync(send);
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
                    if (_pending.TryDequeue(out var tcs))
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