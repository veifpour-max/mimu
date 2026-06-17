namespace LocalMimu.Models;

public class LoginPayload
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }

    public LoginPayload(string username, string passwordhash)
    {
        Username = username;
        PasswordHash = passwordhash;
    }
}
public class RegisterPayload
{

    public Guid id {get; set;} = Guid.NewGuid();
    public string Name { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }

    public RegisterPayload(string name, string username, string passwordhash)
    {
        Name = name;
        Username = username;
        PasswordHash = passwordhash;
    }
}