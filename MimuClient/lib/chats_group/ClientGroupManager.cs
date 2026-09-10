namespace LocalMimu.Models;

public class ClientGroupManager
{
    public Guid? Id {get; set;}
    public required string GroupName {get; set;}
    public required Guid OwnerId {get; set;}
    public required List<Guid> _members = new(); // required если делает то что я думаю то крутой красавчик(должен при созданири обьекта ТРЕБОВАТЬ)
    public bool IsGroup {get; set;}

    public bool IsAllowedToChat(Guid myId)
    {
        if (IsGroup || myId == OwnerId)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
public class CryptoVaultForKeys
{
    private readonly Dictionary<Guid, Dictionary<Guid, byte[]>> _vault = new();

    public void KeyWrite(Guid groupId, Guid senderId, byte[] key)
    {
        if (!_vault.ContainsKey(groupId))
        {
            _vault[groupId] = new Dictionary<Guid, byte[]>();
        }
        _vault[groupId][senderId] = key;
    }
    public byte[]? KeyGet(Guid groupId, Guid senderId)
    {
        if(_vault.ContainsKey(groupId) && _vault[groupId].ContainsKey(senderId))
        {
            return _vault[groupId][senderId];
        }
        return null;
    }


}