using System.ComponentModel;

namespace LocalMimu.Models;

public class User : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    readonly public DateTime createdAt;
    public bool IsOnline { get; set; }
    public UserStatus Status { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private string? _LastMessageText { get; set; } = "Нет сообщений";
    private int _UnreadCount { get; set; } = 0;

    public string? LastMessageText
    {
        get => _LastMessageText;
        set
        {
            _LastMessageText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageText)));
        }
    }
    public int UnreadCount
    {
        get => _UnreadCount;
        set
        {
            _UnreadCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnreadCount)));
        }
    }

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

