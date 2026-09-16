using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class NetworkPacketTests
{
    [Fact]
    public void Constructor_AssignsTypePayloadAndRequestId()
    {
        var packet = new NetworkPacket(PacketType.SearchUser, "alice");

        Assert.Equal(PacketType.SearchUser, packet.Type);
        Assert.Equal("alice", packet.PayLoad);
        Assert.False(string.IsNullOrWhiteSpace(packet.RequestId));
    }

    [Fact]
    public void TwoPackets_HaveNotSameRequestIds()
    {
        var first = new NetworkPacket(PacketType.Ping, "");
        var second = new NetworkPacket(PacketType.Ping, "");

        Assert.NotEqual(first.RequestId, second.RequestId);
    }
}