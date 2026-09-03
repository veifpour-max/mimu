using System.IO;
using System.Text;
namespace LocalMimu.Models;

public class LimitedReadLine
{
    private const int MaxLineChars = 128 * 1024;

    public async Task<string?> ReadLineLimitedAsync(StreamReader reader, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buf = new char[8192];

        while (true)
        {
            int n = await reader.ReadAsync(buf.AsMemory(0, buf.Length), ct);
            if (n == 0) return null;

            for (int i = 0; i < n; i++)
            {
                if (buf[i] == '\n')
                    return sb.ToString().TrimEnd('\r');
                sb.Append(buf[i]);
            }

            if (sb.Length > MaxLineChars)
            {
                Console.WriteLine("Клиент отключён: превышен лимит строки");
                return null;
            }
        }
    }
}