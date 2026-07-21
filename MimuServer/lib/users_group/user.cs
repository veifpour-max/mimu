namespace LocalMimu.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    readonly public DateTime createdAt;
    public bool IsOnline { get; set; }
    public UserStatus Status { get; set; }
    public string? LastMessageText { get; set; } = "Нет сообщений";
    public int UnreadCount { get; set; } = 0;
    public User(string name, string username)
    {
        Name = name;
        Username = username;
        Guid guid = Guid.NewGuid();
        Id = guid;
        createdAt = DateTime.Now;
        IsOnline = false;

    }
}

