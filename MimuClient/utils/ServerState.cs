namespace LocalMimu.Models;

public class ServerState
{
    public static readonly SemaphoreSlim ResponseSignal = new SemaphoreSlim(0, 1);
    public static string? rawText { get; set; }

    public ServerState(string rawtext)
    {
        rawText = rawtext;
    }

}