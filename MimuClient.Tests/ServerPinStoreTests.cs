using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class ServerPinStoreTests
{
    [Fact]
    public void Pin_SaveAndLoadRoundtrip()
    {
        var host = "pin-test-" + Guid.NewGuid().ToString("N");

        ServerPinStore.Set(host, 1234, "AABBCC");
        var loaded = ServerPinStore.Get(host, 1234);

        Assert.Equal("AABBCC", loaded);
    }

    [Fact]
    public void Pin_UnknownHost_ReturnsNull()
    {
        var host = "no-such-" + Guid.NewGuid().ToString("N");

        Assert.Null(ServerPinStore.Get(host, 1));
    }

    [Fact]
    public void Pin_SameHostDifferentPort_IsSeparateEntry()
    {
        var host = "multi-" + Guid.NewGuid().ToString("N");

        ServerPinStore.Set(host, 1000, "PIN_A");
        ServerPinStore.Set(host, 2000, "PIN_B");

        Assert.Equal("PIN_A", ServerPinStore.Get(host, 1000));
        Assert.Equal("PIN_B", ServerPinStore.Get(host, 2000));
    }
}
