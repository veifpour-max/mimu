using System.Text.Json;

namespace LocalMimu.Models;

class DeserText
{
    public static string? originText;
    
    public DeserText(string origintext)
    {
        originText = origintext;
    }

    public void UserDeser(string text)
    {
        JsonSerializer.Deserialize<User>(text);
    }
    public void MsgDeser(string text)
    {
        JsonSerializer.Deserialize<Message>(text);
    }
    public void NPDeser(string text)
    {
        JsonSerializer.Deserialize<NetworkPacket>(text);
    }

}