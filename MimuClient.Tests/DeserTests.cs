using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class DeserTests
{
    [Fact]
    public void SerJson_RoundTrips_Object()
    {
        var original = new Message("hello", Guid.NewGuid(), Guid.NewGuid(), MessageType.Text);

        var json = Deser.SerJson(original);
        var restored = Deser.DeserJson<Message>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Text, restored!.Text);
        Assert.Equal(original.SenderID, restored.SenderID);
        Assert.Equal(original.ReceiverID, restored.ReceiverID);
    }

    [Fact]
    public void DeserJson_NullOrEmpty_ReturnsDefault()
    {
        Assert.Null(Deser.DeserJson<Message>(null));
        Assert.Null(Deser.DeserJson<Message>(""));
        Assert.Null(Deser.DeserJson<Message>("  "));
    }

    [Fact]
    public void SerJson_SerializesNetworkPacketWithType()
    {
        var packet = new NetworkPacket(PacketType.Auth, "payload");

        var json = Deser.SerJson(packet);
        var restored = Deser.DeserJson<NetworkPacket>(json);

        Assert.NotNull(restored);
        Assert.Equal(PacketType.Auth, restored!.Type);
        Assert.Equal("payload", restored.PayLoad);
    }
}