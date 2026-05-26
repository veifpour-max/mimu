using System.Net.Sockets;

namespace LocalMimu.Models;

public class NetworkService
{
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;

    public StreamReader Reader => _reader;
    public StreamWriter Writer => _writer; // что делает =>? указывает?

    public async Task ConnectAsync(string ip, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) {AutoFlush = true};
    }
    public async Task SendPacket(NetworkPacket packet)
    {
        var send = Deser.SerJson(packet);
        await _writer.WriteLineAsync(send);

    }
    public async Task? SendAuthInfo(string info)
    {
        var send = Deser.SerJson(info);
        await _writer.WriteLineAsync(send);
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
                            // удалим пока вызовы некоторые.
                            Console.WriteLine($"{finalMsg.SentAt:HH:mm:ss} | {finalMsg.SenderID.ToString().Substring(0, 4)}: | {finalMsg.Text} ");
                        }
                    }
                }


                if (msg != null && msg.Type == PacketType.ServerResponse)
                {
                    Console.WriteLine("[DEBUG] Пакет от сервера пришел в фоновый поток");
                    ServerState.rawText = msg.PayLoad;
                    ServerState.IsFlaged = true;
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