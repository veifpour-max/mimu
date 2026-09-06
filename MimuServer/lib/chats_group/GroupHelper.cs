namespace LocalMimu.Models;

public class CreateGroupPayload
{
    public string? GroupName {get; set;}
    public List<Guid>? MemberIds {get; set;}

}
public class GroupKeyPayload
{
    public Guid GroupId {get; set;}
    public Guid TargetUserId {get; set;}
    public string EncryptedSenderKeyBase64 {get; set;}
}

public class GroupChat
{
    public Guid Id {get; set;}
    public string Name {get; set;}
    public Guid OwnerId {get; set;}

    // todo - интегрировать с бд
    public List<Guid> Members {get; set;}

}