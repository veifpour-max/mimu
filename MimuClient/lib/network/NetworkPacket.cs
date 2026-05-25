namespace LocalMimu.Models;

public enum PacketType
{
    Register,
    Auth,
    SearchUser,
    ChatMessage,
    GroupMessage,
    ServerResponse

}

public class NetworkPacket
{
    public PacketType Type {get; set;}
    public string PayLoad {get; set;}

    public NetworkPacket(PacketType type, string payload)
    {
        Type = type;
        PayLoad = payload;
        
    }
}