namespace LocalMimu.Models;

public class CreateGroupPayload
{
    public string? GroupName {get; set;}
    public List<Guid>? MemberIds {get; set;}
    public bool IsGroup {get; set;}

}
public class GroupKeyPayload
{
    public Guid GroupId {get; set;}
    public Guid TargetUserId {get; set;}
    public string EncryptedSenderKeyBase64 {get; set;}
}
