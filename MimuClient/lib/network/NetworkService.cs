using System.Net.Sockets;

namespace LocalMimu.Models;
public class NetworkService
{
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    public event Action<Message>? OnMessageReceived;

    private Queue<TaskCompletionSource<string>> _pending = new();

    public async Task ConnectAsync(string ip, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public async Task<string> SendAndWaitAsync(NetworkPacket packet)
    {
        var tcs = new TaskCompletionSource<string>();
        _pending.Enqueue(tcs);
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
                            Console.WriteLine($"{shTools.FormatTime(finalMsg.SentAt)} | {finalMsg.SenderUsername}: | {finalMsg.Text} ");
                        }
                    }
                }


                if (msg != null && msg.Type == PacketType.ServerResponse)
                {
                    if(_pending.TryDequeue(out var tcs))
                    {
                        tcs.SetResult(msg.PayLoad);
                    }
                }
                if(msg != null && msg.Type == PacketType.Ping)
                {
                    await SendPacket(new NetworkPacket(PacketType.Pong, ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка! {ex.Message}");
                break;
            }

        }
    }
}