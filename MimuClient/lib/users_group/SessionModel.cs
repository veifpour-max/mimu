namespace LocalMimu.Models;

public class SessionModel
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string ServerAddress { get; set; }
    public Guid Id {get; set;}

    public SessionModel(){}

    public SessionModel(string username, string pass, Guid myId, string server)
    {
        Id = myId;
        Username = username;
        PasswordHash = pass;
        ServerAddress = server;
    }
}