namespace LocalMimu.Models;

public class EncryptedPayload
{
    public string NonceBase64 {get; set;}
    public string ChiperTextBase64 {get; set;}
    public string TagBase64 {get; set;}

}