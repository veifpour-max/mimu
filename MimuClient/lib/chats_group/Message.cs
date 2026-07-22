using System.ComponentModel;
using System.Diagnostics.Tracing;
using LocalMimu.Models;

namespace LocalMimu.Models;

public enum MessageType { Text }

public class Message : INotifyPropertyChanged
{
    public string? Text { get; set; }
    public Guid SenderID { get; set; }
    public DateTime SentAt { get; set; }
    public MessageStatus _Status { get; set; }
    public Guid ReceiverID { get; set; }
    public MessageType Type { get; set; }
    public string? SenderUsername { get; set; }
    public Guid Id { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public MessageStatus Status
    {
        get => _Status;
        set
        {
            if (_Status != value)
            {
                _Status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }
    }
    public Message()
    {
        _Status = MessageStatus.Sent;
    }
    public Message(string text, Guid senderId, Guid receiverId, MessageType type)
    {
        Text = text;
        SenderID = senderId;
        SentAt = DateTime.Now;
        _Status = MessageStatus.Sent;
        ReceiverID = receiverId;
        Type = type;
        Id = Guid.NewGuid();
    }
}