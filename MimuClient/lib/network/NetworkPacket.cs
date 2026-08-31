namespace LocalMimu.Models;

public enum PacketType
{
    Register,
    Auth,
    SearchUser,
    ChatMessage,
    GroupMessage,
    ServerResponse,
    GetChats,
    GetChatsHistory,
    Ping,
    Pong,
    MessageDelivered,
    GetPublicKey,
    RequestUploadUrl,
    RequestDownloadUrl
}

public class NetworkPacket
{
    public PacketType Type {get; set;}
    public string? PayLoad {get; set;}
    public string RequestId {get; set;}

    public NetworkPacket(PacketType type, string payload)
    {
        Type = type;
        PayLoad = payload;
        RequestId = Guid.NewGuid().ToString();
    }
}