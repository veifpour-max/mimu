namespace LocalMimu.Models;

public class ServerState
{
    public static bool IsFlaged { get; set; }
    public static string? rawText { get; set; }

    public ServerState(bool isflaged, string rawtext)
    {
        IsFlaged = isflaged;
        rawText = rawtext;
    }

}