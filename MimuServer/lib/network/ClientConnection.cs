using System.Net.Sockets;

namespace LocalMimu.Models;

public class ClientConnection
{
    public TcpClient Client { get; set; }
    public StreamWriter Writer { get; set; }

    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

    public async Task SendAsync(string rawJson)
    {
        await _sendLock.WaitAsync();
        try
        {
            await Writer.WriteLineAsync(rawJson);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}