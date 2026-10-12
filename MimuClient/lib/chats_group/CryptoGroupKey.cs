namespace LocalMimu.Models;

public record class CryptoGroupKey
{
    public Guid GroupId { get; set; }
    public Guid SenderId { get; set; }
    public byte[] SenderKey { get; set; }
}
