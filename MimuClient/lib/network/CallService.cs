namespace LocalMimu.Models;

public record class TurnCredentials
{
    public string? TurnUrl {get; set;}
    public string? Username {get; set;}
    public string? Password {get; set;}
    public long TTLSeconds {get; set;}
}
public record class CallSignalPayload
{
    public Guid CallId {get; set;}
    public Guid SenderId {get; set;}
    public Guid TargetUserId {get; set;}
    public string? Data {get; set;}
}
